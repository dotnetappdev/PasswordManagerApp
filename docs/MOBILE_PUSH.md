# Push notifications to the mobile apps

The API can push notifications to a user's registered mobile devices. The pipeline is wired end‑to‑end and
is a **safe no‑op until Firebase Cloud Messaging (FCM) is configured** - so nothing breaks out of the box.

## Server (VaultGuard.API)

Already implemented:

- `PushController` (`/api/push`):
  - `POST /api/push/register` `{ token, platform }` - the app calls this after sign‑in.
  - `POST /api/push/unregister` `{ token }`.
  - `POST /api/push/test` `{ title?, body? }` - sends a test push to the caller's devices.
- `IPushDeviceRegistry` → `PushDeviceSqliteRegistry` - stores tokens in a small local SQLite db
  (`%LocalAppData%/VaultGuard/push/devices.db`), no EF migration required.
- `IPushNotificationService` → `FcmPushNotificationService` - sends via **FCM HTTP v1**.
- `IFcmAccessTokenProvider` → `NullFcmAccessTokenProvider` (returns null → logged no‑op).

### To enable sending

1. Create a Firebase project and a **service account** with the *Firebase Cloud Messaging API* enabled.
2. Set config:
   ```json
   "Push": { "Fcm": { "ProjectId": "your-firebase-project-id" } }
   ```
3. Replace `NullFcmAccessTokenProvider` with a real `IFcmAccessTokenProvider` that returns an OAuth2 access
   token for the service account (scope `https://www.googleapis.com/auth/firebase.messaging`), e.g. using
   `Google.Apis.Auth` `GoogleCredential.FromFile(...).CreateScoped(...).UnderlyingCredential
   .GetAccessTokenForRequestAsync()`. Register it in `Program.cs`.

Send from anywhere via DI:
```csharp
await _push.SendToUserAsync(userId, "New sign‑in", "Your vault was unlocked on a new device.");
```

## Android (VaultGuard.Android)

Wired: `VaultGuardApi.registerPush(...)` and `VaultRepository.registerPushToken(token)` (API mode only).

To make it live, add FCM to the app:

1. Add `google-services.json` to `app/`.
2. Add the plugins/deps:
   ```kotlin
   // build.gradle.kts (root): id("com.google.gms.google-services") version "4.4.2" apply false
   // app/build.gradle.kts: id("com.google.gms.google-services"); implementation("com.google.firebase:firebase-messaging:24.x")
   ```
3. Add a `FirebaseMessagingService`:
   ```kotlin
   class VgMessagingService : FirebaseMessagingService() {
       @Inject lateinit var repository: VaultRepository
       override fun onNewToken(token: String) {
           CoroutineScope(Dispatchers.IO).launch { repository.registerPushToken(token) }
       }
   }
   ```
   Register it in `AndroidManifest.xml` and request `POST_NOTIFICATIONS` on Android 13+.
4. After login, fetch the token (`FirebaseMessaging.getInstance().token`) and call `registerPushToken`.

## iOS (VaultGuard.Ios)

Register for remote notifications (APNs) in `AppDelegate`, obtain the device token, and POST it to
`/api/push/register`. For FCM on iOS, add the Firebase iOS SDK and forward the APNs token; otherwise send
APNs directly (would require an APNs sender alongside the FCM one).
