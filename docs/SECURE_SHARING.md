# Secure sharing

Share one login, note or Wi-Fi password with someone who doesn't use Vault Guard, through a link that stops working
after a time you choose (1 hour to 30 days) or after it has been opened a set number of times.

## Using it

- **Web and MAUI:** open the item → **Share** (share icon) → choose what to include, the expiry and how many times it
  can be opened, optionally a passphrase → **Create link** → **Copy link**.
- **Windows app (WPF):** open the item → **Share**.

The recipient opens the link and clicks **Show it**. If you set a passphrase, send it separately (for example by
phone); the link alone won't open it.

The Web app keeps shares itself. The desktop and mobile apps create them on your Vault Guard server: set its address
and an API key in **Settings → Sync**. The server's address must start with `https://`.

## How it's protected

- **Encrypted before it leaves your device** with a new random 256-bit key (AES-256-GCM, key from HKDF-SHA256).
- **The key is only in the link**, after the `#`. Browsers never send that part to a server, so the server stores and
  returns only ciphertext and can't read what you shared. The recipient's browser decrypts it.
- **Passphrase (optional):** mixed into the key with PBKDF2-SHA256 (600 000 rounds).
- **Runs out:** each opening uses one view, and the share is deleted when it expires or its last view is used.
  Nothing is fetched until the recipient clicks **Show it**, so link previews in chat apps don't use up a
  one-time link.
- **Creating a share** needs a signed-in user (Web) or the app's API key (API). Opening one doesn't, by design.
- **The viewer page** sends no referrer, isn't cached or indexed, and has a strict Content Security Policy.

Code: `VaultGuard.Services/Sharing` (encryption, client, store), `VaultGuard.API/Sharing/SecureShareEndpoints.cs`
(endpoints and viewer, also used by the Web app). Shares are stored as files under `App_Data/shares` (setting
`Sharing:StoreDir`).
