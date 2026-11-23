# SSH agent

Vault Guard can be your SSH agent: `ssh`, `git` and other SSH tools use the SSH keys in your vault, so private keys
don't have to sit in files on your computer.

## Setting it up (Windows app)

1. Save each key as an **SSH Key** item, with the private key in **Private Key**. Paste it without a passphrase
   (`ssh-keygen -p -f yourkey` removes one); the vault encrypts it. Ed25519, RSA and ECDSA keys work, in OpenSSH or
   PEM format.
2. **Settings → SSH Agent → Use Vault Guard as my SSH agent.**
3. Use `ssh` or `git` as usual. Vault Guard asks **"Use an SSH key?"** before each use; **Allow** covers that key for
   one minute (a `git push` signs more than once).

Check it with `ssh-add -l`, which lists your vault's keys while Vault Guard is unlocked.

**If Windows' own agent is running:** Windows' "OpenSSH Authentication Agent" service may already hold the standard
address (`\\.\pipe\openssh-ssh-agent`). Vault Guard then uses `\\.\pipe\vaultguard-ssh-agent` and Settings says so.
Either disable that service (Services → OpenSSH Authentication Agent → Startup type: Disabled → Stop) and restart
Vault Guard, or set the environment variable `SSH_AUTH_SOCK` to `\\.\pipe\vaultguard-ssh-agent`.

## How it's protected

- Keys are listed only while the vault is unlocked, and travel mode hides keys in vaults not marked safe for travel.
- Every signature needs your approval. Keys can't be added, removed or exported through the agent.
- The pipe accepts connections from your Windows account only.
- The private key is decrypted in memory only to sign, and never written to disk.

Code: `VaultGuard.Services/Ssh` (protocol and signing), `VaultGuard.WPF/Services/WpfSshAgent.cs`. Tested against the
real OpenSSH client: `ssh-add` lists the keys, and `ssh-keygen -Y sign` signs through the agent with Ed25519, RSA and
ECDSA keys, verified by `ssh-keygen -Y verify`.
