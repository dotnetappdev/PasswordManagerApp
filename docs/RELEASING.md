# Releasing VaultGuard

There are two independent ways a release gets cut, covering all four shipped apps - **API, Blazor
Web, Android, and WPF**. Both end up as a tagged commit, a `release/*` branch, and a GitHub Release
with a downloadable artifact - the difference is what triggers them and whether anything gets
deployed.

📖 **Back to [Main README](../ReadMe.md)**

## 1. Automatic releases on every merged pull request into `devmain`

Each app has its own workflow that builds it and cuts a release as a side effect - **you don't do
anything manually for this path**, it fires from a normal PR merge:

| App | Workflow | Tag prefix | Artifact |
| --- | --- | --- | --- |
| API | `deploy-api-smarterasp.yml` | `api-v` | Self-contained `win-x64` zip |
| Blazor Web | `deploy-web-smarterasp.yml` | `web-v` | Self-contained `win-x64` zip |
| Android | `build-maui.yml` | `android-v` | `.apk` (debug-signed - see note below) |
| WPF | `build-wpf.yml` | `wpf-v` | `.exe` (Inno Setup) + `.msi` (WiX) installers |

**Trigger:** opening or updating a pull request into `devmain` that touches that app's code (API/Web
are path-filtered to their own project's files; Android/WPF run on any PR into `devmain` or `main`,
but only *release* when the PR targets `devmain`).

**Gate:** API/Web require their own unit tests (`VaultGuard.BackEnd.Tests` / `VaultGuard.Web.Tests`)
to pass in the same job first - a test failure stops the job before publish/release ever run.
Android/WPF release unconditionally on a successful build (they have no separate deploy step to
gate on).

For API/Web specifically, the release step is **not** gated on the SmarterASP.NET deploy actually
succeeding (`continue-on-error: true` on that step) - a deploy outage (e.g. the SmarterASP.NET DNS
issue) still produces a release, it just isn't live on the server yet. The job logs a `::warning::`
on the deploy step when that happens, so check there if you need to know whether a given release
actually made it onto SmarterASP.NET.

Once the gate passes, each workflow:

1. Reads the highest existing tag for its own prefix (`api-v`/`web-v`/`android-v`/`wpf-v`) and bumps
   the patch number - starts at `1.0.0` if none exists yet. There's no version file to edit; the next
   number is always computed from git tags, independently per app.
2. Tags the exact commit from the PR's head and pushes the tag.
3. Creates a matching `release/<prefix>vX.Y.Z` branch off that commit.
4. Builds the artifact table above and attaches it to a new GitHub Release for that tag.

The same `-p:Version=` that gets stamped into the published assembly is what makes the number show
up live in the running apps:

- **API** - `VaultGuard.API/Program.cs`'s `AddSwaggerGen` call reads `Assembly.GetName().Version`, so
  the new version appears in Scalar's title badge and `/swagger/v1/swagger.json` immediately after
  the deploy.
- **Web** - `VaultGuard.Web/Components/Pages/Settings.razor`'s `_appVersion` field reads the same
  assembly version, so Settings → About reflects it as soon as the deploy lands.

See the ReadMe's "Deploying the API to SmarterASP.NET" / "Deploying the Web app to SmarterASP.NET"
sections for the required secrets and hosting details.

**Android signing note:** `VaultGuard.App.csproj` has no signing key configured, so `build-maui.yml`
publishes with the SDK's auto-generated debug keystore. The resulting `.apk` installs fine sideloaded
("install unknown apps") but isn't signed for the Play Store - add a real keystore + signing secrets
to the workflow before treating these APKs as anything beyond internal/test builds.

## 2. Manual combined release

`.github/workflows/release.yml` is the path to reach for when you want to cut a release by hand
without going through a merged PR - it doesn't deploy anywhere, it just builds, zips, and publishes.

```bash
git tag v1.2.3
git push origin v1.2.3
```

Pushing a `vX.Y.Z` tag:

1. `release.yml` creates a `release/v1.2.3` branch off that commit, publishes **both**
   `VaultGuard.API` and `VaultGuard.Web` self-contained for `win-x64` (`-p:Version=1.2.3` stamped
   in), zips each separately (`VaultGuardAPI-1.2.3.zip`, `VaultGuardWeb-1.2.3.zip`), and publishes one
   GitHub Release for the `v1.2.3` tag with both zips attached.
2. `build-api.yml` and `build-web.yml` also each build+zip+release their own project individually on
   the same tag (no release branch, no combined bundle).
3. `build-maui.yml` and `build-wpf.yml` do the same for Android (`.apk`) and WPF (`.exe`/`.msi`).

All five workflows end up contributing files to the *same* GitHub Release for the `v1.2.3` tag rather
than conflicting, but a `v*` push does trigger five separate workflow runs. That's expected.

## Picking a path

- Cutting a release around **one specific app's change**, or want API/Web live on SmarterASP.NET
  immediately? Just merge the PR into `devmain` - path 1 handles all four apps automatically, each
  versioned independently.
- Want a single **combined** release (all four apps, one shared version number) - e.g. to mark a
  milestone? Push a `vX.Y.Z` tag - path 2.

The tag namespaces never collide (`api-v*` / `web-v*` / `android-v*` / `wpf-v*` vs plain `v*`), so
both paths can run side by side without stepping on each other's version numbers.

## Where to find things

- **Releases:** repo → Releases tab, or `https://github.com/dotnetappdev/PasswordManagerApp/releases`.
- **Tags:** `git tag -l 'api-v*'`, `git tag -l 'web-v*'`, `git tag -l 'android-v*'`,
  `git tag -l 'wpf-v*'`, or `git tag -l 'v*'`.
- **Release branches:** `release/api-vX.Y.Z`, `release/web-vX.Y.Z`, `release/android-vX.Y.Z`,
  `release/wpf-vX.Y.Z`, `release/vX.Y.Z` - each is a snapshot of the exact commit that was released,
  useful for a hotfix off an older release without disturbing `devmain`.

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
