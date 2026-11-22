// Vault Guard: idle detection (for auto-lock) and clipboard auto-clear, shared by the Blazor web app and the
// MAUI app (BlazorWebView). Loaded as an ES module from _content/VaultGuard.Components.Shared/js/vault-security.js.

// ---------- activity ----------
let watcher = null;

/** Reports user activity to .NET (at most every `throttleMs`), so the server/app can lock after idle time. */
export function watchActivity(dotnetRef, throttleMs) {
  stopWatching();
  let last = 0;
  const report = () => {
    const now = Date.now();
    if (now - last < throttleMs) return;
    last = now;
    dotnetRef.invokeMethodAsync('ReportActivity').catch(() => {});
  };
  const events = ['pointerdown', 'pointermove', 'keydown', 'wheel', 'touchstart', 'scroll'];
  events.forEach(e => window.addEventListener(e, report, { capture: true, passive: true }));
  const onVisible = () => { if (document.visibilityState === 'visible') dotnetRef.invokeMethodAsync('CheckNow').catch(() => {}); };
  document.addEventListener('visibilitychange', onVisible);
  watcher = { events, report, onVisible };
}

export function stopWatching() {
  if (!watcher) return;
  watcher.events.forEach(e => window.removeEventListener(e, watcher.report, { capture: true }));
  document.removeEventListener('visibilitychange', watcher.onVisible);
  watcher = null;
}

// ---------- clipboard ----------
let pending = null; // { text, timer, onFocus }

/**
 * Copies `text`; after `clearAfterSeconds` (0 = never) wipes the clipboard, unless something else has been
 * copied through here since. Browsers only allow clipboard writes while the page has focus, so if it doesn't at
 * that moment the wipe happens as soon as the user comes back.
 */
export async function copySecret(text, clearAfterSeconds) {
  await navigator.clipboard.writeText(text ?? '');
  cancelPending();
  if (!clearAfterSeconds || clearAfterSeconds <= 0) return true;
  const entry = { text };
  entry.timer = setTimeout(() => clearIfStill(entry), clearAfterSeconds * 1000);
  pending = entry;
  return true;
}

async function clearIfStill(entry) {
  if (pending !== entry) return;
  if (!document.hasFocus()) {
    entry.onFocus = () => { window.removeEventListener('focus', entry.onFocus); clearIfStill(entry); };
    window.addEventListener('focus', entry.onFocus);
    return;
  }
  try { await navigator.clipboard.writeText(''); } catch { /* page lost focus mid-way: nothing more we can do */ }
  if (pending === entry) pending = null;
}

function cancelPending() {
  if (!pending) return;
  clearTimeout(pending.timer);
  if (pending.onFocus) window.removeEventListener('focus', pending.onFocus);
  pending = null;
}

/** Wipe now (used when the vault locks). */
export async function clearNow() {
  cancelPending();
  try { if (document.hasFocus()) await navigator.clipboard.writeText(''); } catch { /* ignore */ }
}

/** Copies non-secret text (e.g. an import report) without scheduling a wipe. */
export async function copyText(text) {
  await navigator.clipboard.writeText(text ?? '');
  return true;
}

/** Saves text as a file download (Web). */
export function downloadText(fileName, text) {
  const url = URL.createObjectURL(new Blob([text ?? ''], { type: 'text/plain;charset=utf-8' }));
  const a = document.createElement('a');
  a.href = url; a.download = fileName; a.style.display = 'none';
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
