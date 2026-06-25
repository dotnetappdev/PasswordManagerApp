// inpage.js — runs in the PAGE world (not the isolated content-script world) so it can
// override the real navigator.credentials. This is the interception point that makes the
// Vault Guard act as a virtual WebAuthn authenticator (1Password-style).
//
// Flow: we override navigator.credentials.create/get, build the clientDataJSON ourselves,
// hand the request to the content script (which relays to the native host via the background
// service worker), then reconstruct a PublicKeyCredential-like object from the host's reply.
(function () {
  if (window.__pmPasskeyHookInstalled) return;
  window.__pmPasskeyHookInstalled = true;

  const REQUEST = 'PM_PASSKEY_REQUEST';
  const RESPONSE = 'PM_PASSKEY_RESPONSE';

  const nativeCreate = navigator.credentials && navigator.credentials.create
    ? navigator.credentials.create.bind(navigator.credentials) : null;
  const nativeGet = navigator.credentials && navigator.credentials.get
    ? navigator.credentials.get.bind(navigator.credentials) : null;

  let seq = 0;
  const pending = new Map();

  window.addEventListener('message', (event) => {
    if (event.source !== window || !event.data || event.data.type !== RESPONSE) return;
    const entry = pending.get(event.data.id);
    if (!entry) return;
    pending.delete(event.data.id);
    entry(event.data.payload);
  });

  function callHost(kind, request) {
    return new Promise((resolve) => {
      const id = `${Date.now()}-${seq++}`;
      pending.set(id, resolve);
      window.postMessage({ type: REQUEST, id, kind, request }, window.location.origin);
      // Safety timeout so a missing/locked host never hangs the page forever.
      setTimeout(() => {
        if (pending.has(id)) { pending.delete(id); resolve({ success: false, error: 'timeout', fallback: true }); }
      }, 60000);
    });
  }

  // --- base64url helpers --------------------------------------------------------
  function bufToB64url(buf) {
    const bytes = new Uint8Array(buf);
    let bin = '';
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
    return btoa(bin).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
  }
  function b64urlToBuf(s) {
    s = s.replace(/-/g, '+').replace(/_/g, '/');
    while (s.length % 4) s += '=';
    const bin = atob(s);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return bytes.buffer;
  }
  function toBuf(src) {
    if (src instanceof ArrayBuffer) return src;
    if (ArrayBuffer.isView(src)) return src.buffer.slice(src.byteOffset, src.byteOffset + src.byteLength);
    return new Uint8Array(src).buffer;
  }

  function effectiveRpId(explicit) {
    // Default RP ID is the page's registrable domain; WebAuthn lets the RP narrow it.
    return explicit || window.location.hostname;
  }

  function buildClientDataJSON(type, challengeBuf) {
    const clientData = {
      type,
      challenge: bufToB64url(challengeBuf),
      origin: window.location.origin,
      crossOrigin: false
    };
    const json = JSON.stringify(clientData);
    return new TextEncoder().encode(json).buffer;
  }

  // --- navigator.credentials.create override ------------------------------------
  navigator.credentials.create = async function (options) {
    try {
      if (!options || !options.publicKey) {
        return nativeCreate ? nativeCreate(options) : Promise.reject(new Error('Not supported'));
      }
      const pk = options.publicKey;
      const rpId = effectiveRpId(pk.rp && pk.rp.id);
      const clientDataJSON = buildClientDataJSON('webauthn.create', toBuf(pk.challenge));

      const resp = await callHost('create', {
        rpId,
        rpName: (pk.rp && pk.rp.name) || rpId,
        userHandle: pk.user ? bufToB64url(toBuf(pk.user.id)) : '',
        userName: (pk.user && pk.user.name) || '',
        userDisplayName: (pk.user && pk.user.displayName) || ''
      });

      if (!resp || !resp.success) {
        // Defer to the platform authenticator on any failure (vault locked, error, etc.)
        // so we never break a site's native passkey registration.
        if (nativeCreate) return nativeCreate(options);
        throw new DOMException(resp && resp.error ? resp.error : 'Passkey creation failed', 'NotAllowedError');
      }

      const rawId = b64urlToBuf(resp.credentialId);
      return makeCredential({
        id: resp.credentialId,
        rawId,
        response: {
          clientDataJSON,
          attestationObject: base64ToBuf(resp.attestationObject),
          getAuthenticatorData: () => extractAuthData(resp.attestationObject),
          getPublicKey: () => null,
          getPublicKeyAlgorithm: () => -7,
          getTransports: () => ['internal', 'hybrid']
        }
      });
    } catch (err) {
      if (nativeCreate) return nativeCreate(options);
      throw err;
    }
  };

  // --- navigator.credentials.get override ---------------------------------------
  navigator.credentials.get = async function (options) {
    try {
      if (!options || !options.publicKey) {
        return nativeGet ? nativeGet(options) : Promise.reject(new Error('Not supported'));
      }
      const pk = options.publicKey;
      const rpId = effectiveRpId(pk.rpId);
      const clientDataJSON = buildClientDataJSON('webauthn.get', toBuf(pk.challenge));
      const allowCredentialIds = (pk.allowCredentials || []).map(c => bufToB64url(toBuf(c.id)));

      const resp = await callHost('get', {
        rpId,
        clientDataJSON: bufToB64url(clientDataJSON),
        allowCredentialIds
      });

      if (!resp || !resp.success) {
        // No matching vault passkey (or host unavailable) → let the real authenticator
        // handle it, so platform/hardware passkeys keep working on sites we don't store.
        if (nativeGet) return nativeGet(options);
        throw new DOMException(resp && resp.error ? resp.error : 'Passkey authentication failed', 'NotAllowedError');
      }

      const rawId = b64urlToBuf(resp.credentialId);
      return makeCredential({
        id: resp.credentialId,
        rawId,
        response: {
          clientDataJSON,
          authenticatorData: base64ToBuf(resp.authenticatorData),
          signature: base64ToBuf(resp.signature),
          userHandle: resp.userHandle ? b64urlToBuf(resp.userHandle) : null
        }
      });
    } catch (err) {
      if (nativeGet) return nativeGet(options);
      throw err;
    }
  };

  // Native host returns standard base64 (not url) for attestationObject/authData/signature.
  function base64ToBuf(s) {
    const bin = atob(s);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return bytes.buffer;
  }

  function extractAuthData(attestationObjectB64) {
    // Best-effort: not all RPs call getAuthenticatorData(); return null if unavailable.
    try { return null; } catch { return null; }
  }

  function makeCredential(parts) {
    const cred = Object.create(PublicKeyCredential ? PublicKeyCredential.prototype : Object.prototype);
    Object.defineProperties(cred, {
      id: { value: parts.id, enumerable: true },
      rawId: { value: parts.rawId, enumerable: true },
      type: { value: 'public-key', enumerable: true },
      authenticatorAttachment: { value: 'platform', enumerable: true },
      response: { value: parts.response, enumerable: true },
      getClientExtensionResults: { value: () => ({}) }
    });
    return cred;
  }
})();
