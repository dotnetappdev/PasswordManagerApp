# Auto-lock, clipboard and organization policies

Vault Guard locks the vault after a period of inactivity and wipes copied values from the clipboard. Users
choose both under **Settings → Security** (WPF), **Settings → Lock & Clipboard** (MAUI), or **Settings →
Security → Session & Lock** (web). On Windows an administrator can also enforce limits through Group Policy.

## "Same as Windows"

In the Windows apps, **Auto-lock after → Same as Windows** makes Vault Guard follow the lock timeout Windows
itself enforces:

| Windows setting | Where it's set | Read from |
|---|---|---|
| **Interactive logon: Machine inactivity limit** | Group Policy: Computer Configuration → Windows Settings → Security Settings → Local Policies → Security Options | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\InactivityTimeoutSecs` |
| **Password-protected screen saver** ("On resume, display logon screen") and its wait time | Settings → Personalization → Lock screen → Screen saver, or Group Policy (User Configuration → Administrative Templates → Control Panel → Personalization) | The effective values Windows reports (`SystemParametersInfo`), so Group Policy overrides are included |

If both are set, the shorter one applies, because that's when Windows actually locks. If neither is set,
Windows doesn't lock on its own after a period of inactivity, so Vault Guard uses its default (15 minutes).
Settings shows a line explaining which timeout is in force.

Separately, **Lock when Windows locks or the PC sleeps** (on by default in WPF) locks Vault Guard whenever
Windows locks for any reason: Win+L, sleep, "require sign-in after the display turns off", Dynamic Lock or
switching user.

> In the MAUI app, "Same as Windows" uses Windows' timeout, but measures inactivity inside the app.

## Organization policy (Group Policy / Intune)

These values live under `SOFTWARE\Policies\VaultGuard` in `HKEY_LOCAL_MACHINE` (whole PC) or
`HKEY_CURRENT_USER` (one user). Machine policy wins.

| Value (DWORD) | Effect |
|---|---|
| `AutoLockMaxMinutes` | Longest time Vault Guard may stay unlocked while idle (1–1440). Users can pick shorter times, or "Same as Windows" if Windows locks sooner, but not longer ones or "Never". |
| `ClipboardClearMaxSeconds` | Longest time a copied value may stay on the clipboard (5–3600). "Never" is no longer offered. |
| `LockOnWindowsLock` | `1` = always lock when Windows locks or sleeps; the setting is greyed out. |

Settings shows "Your organization requires…" when a limit applies. The apps re-read the policy at every check,
so changes take effect without restarting the app. The web app applies the policy of the server it runs on.

### Deploying

- **Group Policy:** copy [`deploy/policies/VaultGuard.admx`](../deploy/policies/VaultGuard.admx) to
  `%SystemRoot%\PolicyDefinitions` (or your domain's Central Store), and
  [`en-US/VaultGuard.adml`](../deploy/policies/en-US/VaultGuard.adml) to the `en-US` folder there. The settings
  then appear under **Administrative Templates → Vault Guard**, for both Computer and User Configuration.
- **Intune:** import the ADMX as an imported administrative template (Devices → Configuration → Import ADMX),
  then create a profile from it.
- **One PC, for testing:** `reg import deploy\policies\VaultGuard-example.reg` (10 minutes, 30 seconds, always
  lock with Windows). To remove it: `reg delete "HKLM\SOFTWARE\Policies\VaultGuard" /f`.
