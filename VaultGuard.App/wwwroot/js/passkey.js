// Passkey (WebAuthn) helper for Blazor
window.passkey = {
    async register() {
        if (!window.PublicKeyCredential) {
            throw new Error('WebAuthn not supported');
        }
        // Demo: use random challenge and user id
        const challenge = new Uint8Array(32);
        window.crypto.getRandomValues(challenge);
        const userId = new Uint8Array(16);
        window.crypto.getRandomValues(userId);
        const publicKey = {
            challenge: challenge,
            rp: { name: 'Password Manager' },
            user: {
                id: userId,
                name: 'user@local',
                displayName: 'Password Manager User'
            },
            pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
            authenticatorSelection: { userVerification: 'preferred' },
            timeout: 60000,
            attestation: 'none'
        };
        const cred = await navigator.credentials.create({ publicKey });
        return btoa(String.fromCharCode(...new Uint8Array(cred.rawId)));
    },
    async login(credIdBase64) {
        if (!window.PublicKeyCredential) {
            throw new Error('WebAuthn not supported');
        }
        const challenge = new Uint8Array(32);
        window.crypto.getRandomValues(challenge);
        const publicKey = {
            challenge: challenge,
            allowCredentials: [{
                id: Uint8Array.from(atob(credIdBase64), c => c.charCodeAt(0)),
                type: 'public-key',
                transports: ['internal']
            }],
            timeout: 60000,
            userVerification: 'preferred'
        };
        const assertion = await navigator.credentials.get({ publicKey });
        return true;
    }
};
