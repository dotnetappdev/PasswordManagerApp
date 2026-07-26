# VaultGuard.Admin & Multi-Tenancy

`VaultGuard.Admin` is a separate Blazor Server "control panel" for super admins: customers, subscriptions,
license keys, and tenant/custom-domain management. It is a **thin client of VaultGuard.API** — it has no
database connection or Identity store of its own, no `VaultGuard.DAL`/`VaultGuard.Crypto` references, and
every action it takes is a normal authenticated HTTP call to the same API the WPF/Blazor/mobile apps use.
This keeps a single source of truth (one database, one Identity system, one set of business rules) and
means securing/scaling VaultGuard.Admin is just securing/scaling another API client.

## Roles: `SuperAdmin` vs `Admin`

VaultGuard already had an in-app `Admin` role (family/vault administration, gated by `RequireAdminAttribute`).
This work adds a separate `SuperAdmin` role (`RequireSuperAdminAttribute`,
`VaultGuard.Models.ApplicationRoles.SuperAdmin`) specifically for the control panel — a family "Admin"
should **not** automatically get access to every customer's license keys and tenants, so the two are kept
distinct. `UserProfileController` (user CRUD) now accepts either role, since both legitimately manage users.

A bootstrap `superadmin@passwordmanager.local` account is seeded by `IdentityDataSeeder` alongside the
existing demo accounts. **Change its password immediately** — see below for why it even has one.

### Why the bootstrap account has a real password

Every other seeded account (`admin@`, `parent@`, `user@`, `child@…@passwordmanager.local`) logs in via
VaultGuard's master-key flow, so its actual ASP.NET Identity password is random and discarded at seed
time — nobody is meant to know it. VaultGuard.Admin is different: it authenticates against ASP.NET
Identity's own built-in endpoints (`POST /login`, `POST /register`, mapped via `MapIdentityApi<ApplicationUser>()`
in `VaultGuard.API/Program.cs`), which need a real, known password. `IdentityDataSeeder` therefore seeds
the SuperAdmin account with `CommonMasterKey` as its actual Identity password too (see
`CreateDefaultUserWithMasterKeyAsync(..., identityPassword: CommonMasterKey)`).

## How VaultGuard.Admin logs in (BFF pattern)

1. `Login.razor` renders a plain, **static** (non-interactive) HTML `<form method="post" action="/admin-auth/login">`
   — deliberately not a Blazor event handler, because `HttpContext.SignInAsync` has to run before the
   response starts, which isn't possible once an interactive Blazor Server circuit (SignalR) is live.
   `App.razor` therefore does **not** set a global `@rendermode` on `<Routes>`; only the pages that need
   interactivity (Dashboard, Users, …) opt in individually with `@rendermode="InteractiveServer"`.
2. `POST /admin-auth/login` (a minimal API endpoint in `VaultGuard.Admin/Program.cs`) calls
   `AdminApiClient.LoginAsync`, which POSTs to the API's `/login?useCookies=false` and gets back a bearer
   access token, then calls `GET /api/admin/me` with that token to confirm the account actually has the
   `SuperAdmin` role.
3. If both succeed, the endpoint signs the browser into **VaultGuard.Admin's own cookie** (a separate
   cookie-auth scheme, `AddCookie()` in `VaultGuard.Admin/Program.cs`), embedding the API's bearer token as
   a claim (`access_token`) inside that cookie. `AdminSession` (scoped per-circuit) reads that claim once
   per page load and `AdminApiClient` attaches it as `Authorization: Bearer …` on every subsequent API call.
4. `Signup.razor` posts to `/admin-auth/register`, which calls the API's `/register` — this creates a
   normal account with **no roles**. An existing SuperAdmin must grant the `SuperAdmin` role via the Users
   page before the new account can actually get into the control panel. Self-service sign-up into
   SuperAdmin would be a real privilege-escalation hole, so this is intentional, not an oversight.

## Pages

| Page | What it does |
|---|---|
| Dashboard | Aggregate counts (users, tenants, license keys, subscriptions, activations) from `GET /api/admin/dashboard-stats`. |
| Users | Lists all users (`UserProfileController`); a "Manage" dialog toggles roles, assigns a tenant, shows/assigns licenses, and can deactivate/reactivate/delete. |
| License Keys | Full CRUD (issue/edit/delete/revoke) for CD keys, including assigning a key directly to an existing account — see `docs/LICENSING.md`. |
| Subscriptions | Create/cancel billing-plan records (`SubscriptionsController`) for a user or a whole tenant. |
| Tenants & Domains | Create tenants, set a custom domain and walk through DNS verification (below), and assign/manage org-wide license keys per tenant. |
| API Keys | Cross-account oversight of every API key ever issued (`GET /api/admin/api-keys`) — the per-user `ApiKeysController` only ever sees the calling user's own keys. |
| Settings | Licensing signing-key status, one-click key generation, and issuance defaults (default plan / max activations) — see `docs/LICENSING.md` "Settings". |

## Multi-tenancy

Multi-tenancy is **shared-schema**: one database, a `Tenant` row per customer organization, and a nullable
`TenantId` on `ApplicationUser`. A single-tenant install (the default) simply never creates a `Tenant` row
and every user's `TenantId` stays null — nothing changes for existing deployments.

### Subdomain routing

Setting `Tenancy:BaseDomain` in `VaultGuard.API`/`VaultGuard.Web`'s `appsettings.json` (e.g.
`vaultguardapp.com`) makes `TenantResolutionMiddleware` match `{Tenant.Slug}.{BaseDomain}` on every
request's `Host` header and populate a scoped `ICurrentTenantService` for the rest of the pipeline. Leave
it blank to disable subdomain resolution (custom-domain resolution below still works).

### Custom domains (`vaultguard.customer.com`)

1. In VaultGuard.Admin → Tenants & Domains → "Domain", enter the customer's chosen domain
   (`SetCustomDomainAsync` → `POST /api/tenants/{id}/custom-domain`). The API generates a random
   verification token and returns a TXT record name/value.
2. Give the customer that TXT record (`_vaultguard-verify.<domain>` → the token) to publish at their DNS
   provider, **and** have them point the domain itself at this VaultGuard deployment — a CNAME (or A
   record, depending on hosting) for `vaultguard.customer.com`, per whatever the actual server/load
   balancer setup requires (this repo doesn't prescribe a specific host — SmarterASP, Azure App Service
   custom domains + managed certs, or a reverse proxy like Caddy/nginx with on-demand TLS all work; the
   only requirement is that the domain resolves to this app and the app receives the original `Host`
   header).
3. Once the TXT record has propagated (`dig TXT _vaultguard-verify.customer.com`), click "Verify" on the
   Tenants page (`POST /api/tenants/{id}/custom-domain/verify`). This is a manual, SuperAdmin-gated step in
   this pass — it does **not** perform the DNS lookup itself, it trusts the operator's own check. A
   follow-up could automate the TXT lookup and TLS certificate provisioning (e.g. via ACME/Let's Encrypt)
   before flipping `CustomDomainVerified`.
4. Once verified, `TenantResolutionMiddleware` matches that Host header to the tenant on every request in
   both `VaultGuard.API` and `VaultGuard.Web`.

## Known limitations (this pass)

- **Login/register CSRF**: `/admin-auth/login` and `/admin-auth/register` validate an antiforgery token
  (`Login.razor`/`Signup.razor` render `<AntiforgeryToken />`), consistent with the rest of ASP.NET Core's
  form-post protections.
- **No payment provider** — subscriptions are created/cancelled manually by a SuperAdmin; there's no
  Stripe/Paddle webhook wiring (see `docs/LICENSING.md`).
- **Custom-domain verification is manual** (an operator clicks "Verify" after checking DNS themselves,
  rather than the API polling DNS automatically).
- **Static web assets in local dev**: if you run VaultGuard.Admin (or VaultGuard.Web) from a raw built DLL
  outside of `dotnet run`/an IDE, MudBlazor's CSS/JS (served from `_content/MudBlazor/…`) may 404 in that
  narrow scenario — this is a pre-existing ASP.NET Core Razor Class Library static-asset dev-time quirk
  (unrelated to this change; it reproduces identically on the existing VaultGuard.Web project) and does not
  occur under `dotnet run`, an IDE's F5, or a real `dotnet publish` deployment.
