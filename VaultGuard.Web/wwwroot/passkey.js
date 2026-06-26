// WebAuthn (passkey) helpers for Vault Guard.
//
// Two distinct flows live here:
//   * Local biometric UNLOCK (isAvailable/enroll/assert) — gates release of a securely-cached
//     master key for the zero-knowledge vault. No bare "true" is ever returned.
//   * Account FIDO2 ceremony (createForServer/getForServer) — marshals between Fido2NetLib's
//     server options JSON and the browser WebAuthn API; the SERVER cryptographically verifies
//     the attestation/assertion, so this code only transports bytes, it never grants trust.
//
// SECURITY: this file never derives, sees or returns the vault key. The .NET side performs all
// verification and only then releases secrets / issues tokens.
(function () {
    function b64urlToBuf(b64url) {
        const pad = (4 - (b64url.length % 4)) % 4;
        const b64 = b64url.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat(pad);
        const bin = atob(b64);
        const buf = new Uint8Array(bin.length);
        for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
        return buf;
    }

    function bufToB64url(buf) {
        const bytes = new Uint8Array(buf);
        let bin = '';
        for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
        return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    }

    function randomBytes(n) {
        const b = new Uint8Array(n);
        (window.crypto || window.msCrypto).getRandomValues(b);
        return b;
    }

    window.passkey = {
        // ── Local biometric unlock ──────────────────────────────────────────────────────────
        // True only when a platform authenticator (biometric/PIN) is actually usable here.
        async isAvailable() {
            if (!window.PublicKeyCredential) return false;
            try {
                return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
            } catch (e) {
                return false;
            }
        },

        // Enrolls a NEW platform credential bound to this device and returns its
        // credential id (base64url). Throws if WebAuthn is unavailable or the user cancels.
        async enroll(userName, rpName) {
            if (!window.PublicKeyCredential) {
                throw new Error('WebAuthn is not supported on this device');
            }
            const publicKey = {
                challenge: randomBytes(32),
                rp: { name: rpName || 'Vault Guard' },
                user: {
                    id: randomBytes(16),
                    name: userName || 'vault-user',
                    displayName: userName || 'Vault Guard User'
                },
                pubKeyCredParams: [
                    { type: 'public-key', alg: -7 },    // ES256
                    { type: 'public-key', alg: -257 }   // RS256
                ],
                authenticatorSelection: {
                    authenticatorAttachment: 'platform',
                    userVerification: 'required',
                    residentKey: 'preferred'
                },
                timeout: 60000,
                attestation: 'none'
            };
            const cred = await navigator.credentials.create({ publicKey });
            if (!cred) {
                throw new Error('Passkey enrollment was cancelled');
            }
            return bufToB64url(cred.rawId);
        },

        // Performs a REAL assertion with the previously enrolled credential. This triggers the
        // platform biometric/PIN prompt. Returns the asserted credential id (base64url) on
        // success, or throws if it is cancelled / fails. The caller MUST verify the returned id
        // equals the enrolled id before releasing any secret.
        async assert(credentialIdB64url) {
            if (!window.PublicKeyCredential) {
                throw new Error('WebAuthn is not supported on this device');
            }
            const publicKey = {
                challenge: randomBytes(32),
                timeout: 60000,
                userVerification: 'required'
            };
            if (credentialIdB64url) {
                publicKey.allowCredentials = [{
                    id: b64urlToBuf(credentialIdB64url),
                    type: 'public-key',
                    transports: ['internal']
                }];
            }
            const assertion = await navigator.credentials.get({ publicKey });
            if (!assertion) {
                throw new Error('Passkey verification was cancelled');
            }
            return bufToB64url(assertion.rawId);
        },

        // ── Account FIDO2 ceremony (server-verified) ────────────────────────────────────────
        // Takes the Fido2NetLib CredentialCreationOptions JSON from the server, runs
        // navigator.credentials.create(), and returns the attestation as JSON the server can
        // deserialize into AuthenticatorAttestationRawResponse.
        async createForServer(optionsJson) {
            if (!window.PublicKeyCredential) {
                throw new Error('WebAuthn is not supported on this device');
            }
            const opts = JSON.parse(optionsJson);
            opts.challenge = b64urlToBuf(opts.challenge);
            opts.user.id = b64urlToBuf(opts.user.id);
            if (Array.isArray(opts.excludeCredentials)) {
                opts.excludeCredentials = opts.excludeCredentials.map(c => ({ ...c, id: b64urlToBuf(c.id) }));
            }

            const cred = await navigator.credentials.create({ publicKey: opts });
            if (!cred) {
                throw new Error('Passkey creation was cancelled');
            }
            return JSON.stringify({
                id: cred.id,
                rawId: bufToB64url(cred.rawId),
                type: cred.type,
                extensions: cred.getClientExtensionResults ? cred.getClientExtensionResults() : {},
                response: {
                    attestationObject: bufToB64url(cred.response.attestationObject),
                    clientDataJSON: bufToB64url(cred.response.clientDataJSON)
                }
            });
        },

        // Takes the Fido2NetLib AssertionOptions JSON from the server, runs
        // navigator.credentials.get(), and returns the assertion as JSON the server can
        // deserialize into AuthenticatorAssertionRawResponse.
        async getForServer(optionsJson) {
            if (!window.PublicKeyCredential) {
                throw new Error('WebAuthn is not supported on this device');
            }
            const opts = JSON.parse(optionsJson);
            opts.challenge = b64urlToBuf(opts.challenge);
            if (Array.isArray(opts.allowCredentials)) {
                opts.allowCredentials = opts.allowCredentials.map(c => ({ ...c, id: b64urlToBuf(c.id) }));
            }

            const assertion = await navigator.credentials.get({ publicKey: opts });
            if (!assertion) {
                throw new Error('Passkey assertion was cancelled');
            }
            return JSON.stringify({
                id: assertion.id,
                rawId: bufToB64url(assertion.rawId),
                type: assertion.type,
                extensions: assertion.getClientExtensionResults ? assertion.getClientExtensionResults() : {},
                response: {
                    authenticatorData: bufToB64url(assertion.response.authenticatorData),
                    clientDataJSON: bufToB64url(assertion.response.clientDataJSON),
                    signature: bufToB64url(assertion.response.signature),
                    userHandle: assertion.response.userHandle ? bufToB64url(assertion.response.userHandle) : null
                }
            });
        }
    };
})();
