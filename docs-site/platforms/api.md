# API Server

`VaultGuard.API` is an ASP.NET Core Web API - the surface the mobile app and browser extension talk to,
and a fine target for scripts, Postman, or your own integrations. See the full
[API Reference](/api/reference) for endpoints.

## Interactive docs

The API serves a live [Scalar](https://scalar.com) API reference at `/scalar` when running, rendered from
the same OpenAPI document Swashbuckle generates at `/swagger/v1/swagger.json` - always in sync with the
deployed code. Scalar replaced the classic Swagger UI for a dashboard-style layout (request/response
samples, a searchable sidebar, try-it-out panel).

::: tip
The `Capture Screenshots` GitHub Actions workflow (`.github/workflows/screenshots.yml`) boots the API
and captures this live - it commits a fresh one on every push that touches `VaultGuard.API/**`.
:::

<img src="/screenshots/api/scalar.png" alt="Scalar API reference UI" style="border-radius: 8px; border: 1px solid var(--vp-c-divider)" />

## Health check

```
GET /health
```

Doesn't require an API key - useful for load balancer / uptime checks.

## Rate limiting

A global per-IP limit applies to the whole API, with a stricter limit specifically on authentication
endpoints (`/api/auth/*`) to slow down credential-stuffing attempts.
