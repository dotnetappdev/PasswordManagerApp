# Emergency access

Let someone you trust ask for read-only access to your vault, for example if something happens to you. They get it
after a waiting period you choose, unless you decline, so you stay in control while you can.

Both of you need a Vault Guard account on the same Vault Guard server (the Web app).

## Using it

**Adding a trusted contact:** Web → **Emergency Access** → their email, a waiting period (1, 2, 7, 14 or 30 days) →
**Add**. If they haven't signed in to Vault Guard yet, it finishes setting up the next time you both have.

**When they ask:** they open **Emergency Access** → **Request access**. The next time you sign in you're told, and
the page shows **Decline** and **Allow now**. If you do nothing, access opens when the waiting period ends.

**Opening it:** once access is open they see **Open their vault**, which lists your items read-only.

**Changing your mind:** **Remove** a contact at any time; their access ends at once.

## How it's protected

- **Every account has its own key pair** (RSA-2048). The private key is encrypted with that account's vault key, so
  only that person, signed in, can use it.
- **Adding a contact** encrypts your vault key with their public key (RSA-OAEP-SHA256). Only their private key can
  undo that, and the server can't.
- **The server only releases it when access is open:** after you allow it, or after they asked and the waiting
  period passed without you declining. Their browser session then unwraps it with their own unlocked key.
- **It stays current:** each time you unlock, your vault key is re-wrapped for your contacts, so changing your master
  password carries over. If they change theirs, they get a new key pair and it's re-wrapped on your next unlock.
- **Read-only:** they can view your items; nothing is copied into their vault.

Code: `VaultGuard.Services/Emergency/EmergencyAccessService.cs`; Web: `Components/Pages/EmergencyAccess.razor` and
`Components/Emergency/EmergencyAccessKeeper.razor`. Records are stored as files under `App_Data/emergency`
(setting `EmergencyAccess:StoreDir`).
