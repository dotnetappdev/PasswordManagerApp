# Releasing VaultGuard

There are two independent ways a release gets cut. Both end up as a tagged commit, a `release/*`
branch, and a GitHub Release with a downloadable zip - the difference is what triggers them and
whether anything gets deployed.

📖 **Back to [Main README](../ReadMe.md)**

## 1. Automatic releases on every SmarterASP.NET deploy

`.github/workflows/deploy-api-smarterasp.yml` and `.github/workflows/deploy-web-smarterasp.yml` each
deploy one project (`VaultGuard.API` or `VaultGuard.Web`) and cut a release as a side effect - **you
don't do anything manually for this path.**

**Trigger:** opening or updating a pull request into `devmain` that touches that project's code.

**Gate:** the project's own unit tests (`VaultGuard.BackEnd.Tests` for the API,
`VaultGuard.Web.Tests` for the web app) must pass in the same job. A test failure stops the job
before publish/deploy/release ever run.

Once the SmarterASP.NET deploy succeeds, the workflow:

1. Reads the highest existing `api-vX.Y.Z` (or `web-vX.Y.Z`) tag and bumps the patch number - starts
   at `1.0.0` if neither exists yet. There's no version file to edit; the next number is always
   computed from git tags.
2. Tags the exact commit that was deployed and pushes the tag.
3. Creates a `release/api-vX.Y.Z` (or `release/web-vX.Y.Z`) branch off that commit.
4. Zips the self-contained publish output and attaches it to a new GitHub Release for that tag.

The same `-p:Version=` that gets stamped into the published assembly is what makes the number show
up live in the running apps:

- **API** - `VaultGuard.API/Program.cs`'s `AddSwaggerGen` call reads `Assembly.GetName().Version`, so
  the new version appears in Scalar's title badge and `/swagger/v1/swagger.json` immediately after
  the deploy.
- **Web** - `VaultGuard.Web/Components/Pages/Settings.razor`'s `_appVersion` field reads the same
  assembly version, so Settings → About reflects it as soon as the deploy lands.

See the ReadMe's "Deploying the API to SmarterASP.NET" / "Deploying the Web app to SmarterASP.NET"
sections for the required secrets and hosting details.

## 2. Manual combined release

`.github/workflows/release.yml` is the path to reach for when you want to cut a release by hand
without going through a SmarterASP.NET deploy - it doesn't deploy anywhere, it just builds, zips, and
publishes.

```bash
git tag v1.2.3
git push origin v1.2.3
```

Pushing a `vX.Y.Z` tag:

1. Creates a `release/v1.2.3` branch off that commit.
2. Publishes **both** `VaultGuard.API` and `VaultGuard.Web` self-contained for `win-x64`, with
   `-p:Version=1.2.3` stamped in (same About-tab/Scalar-badge effect as above).
3. Zips each publish output separately (`VaultGuardAPI-1.2.3.zip`, `VaultGuardWeb-1.2.3.zip`).
4. Publishes one GitHub Release for the `v1.2.3` tag with both zips attached.

`build-api.yml` and `build-web.yml` also each build+zip+release their own project individually on
any `v*` tag push (no release branch, no combined bundle) - all three workflows end up contributing
files to the *same* GitHub Release for a given tag rather than conflicting, but a `v*` push does
trigger three separate workflow runs. That's expected.

## Picking a path

- Cutting a release around **one specific project change**, or want it live on SmarterASP.NET
  immediately? Just merge the PR into `devmain` - path 1 handles the rest.
- Want a single **combined** release (both apps, one version number, no deploy) - e.g. for a
  downloadable desktop/offline build, or to mark a milestone? Push a `vX.Y.Z` tag - path 2.

The two tag namespaces never collide (`api-v*` / `web-v*` vs `v*`), so both paths can run side by
side without stepping on each other's version numbers.

## Where to find things

- **Releases:** repo → Releases tab, or `https://github.com/dotnetappdev/PasswordManagerApp/releases`.
- **Tags:** `git tag -l 'api-v*'`, `git tag -l 'web-v*'`, or `git tag -l 'v*'`.
- **Release branches:** `release/api-vX.Y.Z`, `release/web-vX.Y.Z`, `release/vX.Y.Z` - each is a
  snapshot of the exact commit that was released, useful for a hotfix off an older release without
  disturbing `devmain`.

## Rolling back

Every release has a matching `release/*` branch pointing at the exact commit that was published, so
rolling back means deploying (or re-publishing) from that branch rather than reverting commits on
`devmain`:

```bash
git fetch origin release/api-v1.0.3
git checkout release/api-v1.0.3
dotnet publish VaultGuard.API/VaultGuard.API.csproj -c Release --self-contained true -r win-x64 -p:Version=1.0.3 -o publish/api
```

Then deploy that publish output the same way the workflow does (Web Deploy / `msdeploy.exe`, or
manually through the SmarterASP.NET control panel).
