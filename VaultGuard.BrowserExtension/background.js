// Background script for Vault Guard browser extension
class VaultGuardBackground {
  constructor() {
    this.nativeHostName = 'com.passwordmanager.native_host';
    this.authToken = null;
    // Browser link (like 1Password's app integration): when the Vault Guard desktop app is running and this
    // browser has been approved in it, the app answers from its unlocked vault — no sign-in in the extension.
    this.linkSecret = null;   // issued by the app when the user approves this browser
    this.link = null;         // last { linked, paired, unlocked, app, appVersion, protocol }
    this.linkCheckedAt = 0;
    this.init();
  }

  init() {
    // Listen for messages from content script and popup
    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
      this.handleMessage(request, sender, sendResponse);
      return true; // Keep message channel open for async responses
    });

    // Load settings on startup
    this.loadSettings();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get(['authToken']);
      if (result.authToken) {
        this.authToken = result.authToken;
      }
      // The pairing secret stays on this device (storage.local, never synced to other browsers).
      const local = await chrome.storage.local.get(['linkSecret']);
      this.linkSecret = local.linkSecret || null;
    } catch (error) {
      console.error('Vault Guard: Error loading settings:', error);
    }
  }

  async handleMessage(request, sender, sendResponse) {
    try {
      switch (request.action) {
        case 'getCredentials':
          await this.getCredentials(request, sendResponse);
          break;
        case 'getCreditCards':
          await this.getCreditCards(request, sendResponse);
          break;
        case 'generatePassword':
          await this.generatePassword(request, sendResponse);
          break;
        case 'login':
          await this.login(request, sendResponse);
          break;
        case 'logout':
          await this.logout(sendResponse);
          break;
        case 'getSettings':
          await this.getSettings(sendResponse);
          break;
        case 'getTotpCode':
          if (!(await this.hasAccess())) { sendResponse({ success: false, error: 'Not connected' }); break; }
          sendResponse(await this.sendNativeMessage({ action: 'getTotpCode', itemId: request.itemId, domain: request.domain || '' }));
          break;
        case 'captureLogin':
          sendResponse(await this.captureLogin(request, sender));
          break;
        case 'getPendingSave':
          sendResponse(await this.getPendingSave(sender));
          break;
        case 'savePendingLogin':
          sendResponse(await this.savePendingLogin(request, sender));
          break;
        case 'dismissPendingSave':
          sendResponse(await this.dismissPendingSave(request, sender));
          break;
        case 'openPopup':
          // Lets the in-page menu send the user to "Connect" (chrome.action.openPopup needs Chrome 127+).
          try { await chrome.action.openPopup(); sendResponse({ success: true }); }
          catch (e) { sendResponse({ success: false, error: 'Click the Vault Guard button in the toolbar.' }); }
          break;
        case 'getLinkStatus':
          sendResponse({ success: true, link: await this.refreshLink(true) });
          break;
        case 'connectApp':
          await this.connectApp(sendResponse);
          break;
        case 'disconnectApp':
          this.linkSecret = null;
          await chrome.storage.local.remove('linkSecret');
          sendResponse({ success: true, link: await this.refreshLink(true) });
          break;
        case 'testConnection':
          await this.testConnection(sendResponse);
          break;
        case 'passkeyCreate':
        case 'passkeyGet':
          await this.handlePasskey(request, sendResponse);
          break;
        case 'saveTotpSecret':
          await this.saveTotpSecret(request, sendResponse);
          break;
        default:
          sendResponse({ success: false, error: 'Unknown action' });
      }
    } catch (error) {
      console.error('Vault Guard: Error handling message:', error);
      sendResponse({ success: false, error: error.message });
    }
  }

  async sendNativeMessage(message) {
    return new Promise(async (resolve, reject) => {
      try {
        // Get database path from settings if available
        const settings = await chrome.storage.sync.get(['databasePath']);
        if (settings.databasePath) {
          message.databasePath = settings.databasePath;
        }
        
        if (this.linkSecret) {
          message.linkSecret = this.linkSecret;
        }
        message.browser = this.browserName();

        chrome.runtime.sendNativeMessage(this.nativeHostName, message, (response) => {
          if (chrome.runtime.lastError) {
            reject(new Error(chrome.runtime.lastError.message));
          } else {
            resolve(response);
          }
        });
      } catch (error) {
        reject(error);
      }
    });
  }

  async sendWebApiMessage(endpoint, data) {
    // Fallback to web API if native host is not available
    try {
      const apiUrl = await this.getApiUrl();
      
      // Map actions to proper API endpoints
      let apiEndpoint = endpoint;
      let method = 'POST';
      
      if (endpoint === 'getCredentials') {
        apiEndpoint = 'credentials';
        method = 'GET';
      } else if (endpoint === 'getCreditCards') {
        apiEndpoint = 'creditcards';
        method = 'GET';
      } else if (endpoint === 'login') {
        apiEndpoint = 'auth/login';
      } else if (endpoint === 'generatePassword') {
        apiEndpoint = 'password/generate';
      } else if (endpoint === 'testConnection') {
        apiEndpoint = 'health';
        method = 'GET';
      } else if (endpoint === 'passkeyCreate') {
        // Self-hosted/live API must expose the vault-passkey endpoints (see PASSKEYS_SETUP.md).
        apiEndpoint = 'passkey/vault/create';
      } else if (endpoint === 'passkeyGet') {
        apiEndpoint = 'passkey/vault/assert';
      }

      const fetchOptions = {
        method: method,
        headers: {
          'Content-Type': 'application/json'
        }
      };

      if (this.authToken) {
        fetchOptions.headers['Authorization'] = `Bearer ${this.authToken}`;
      }

      if (method === 'POST' && data) {
        fetchOptions.body = JSON.stringify(data);
      }

      const response = await fetch(`${apiUrl}/api/${apiEndpoint}`, fetchOptions);

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      throw new Error(`Web API error: ${error.message}`);
    }
  }

  async getApiUrl() {
    // Get API URL from storage or use default
    const result = await chrome.storage.sync.get(['apiUrl']);
    return result.apiUrl || 'http://localhost:5000';
  }

  async sendMessageWithFallback(action, data) {
    // Check connection preference
    const settings = await chrome.storage.sync.get(['connectionMode']);
    const connectionMode = settings.connectionMode || 'auto'; // auto, native, api, localStorage

    // Try localStorage first if preferred or if it's the only option
    if (connectionMode === 'localStorage') {
      try {
        return await this.sendLocalStorageMessage(action, data);
      } catch (localError) {
        console.log('Vault Guard: localStorage failed:', localError.message);
        // Don't fallback if user explicitly chose localStorage
        throw localError;
      }
    }

    // Try native host if preferred or in auto mode
    if (connectionMode === 'native' || connectionMode === 'auto') {
      try {
        return await this.sendNativeMessage({
          action: action,
          ...data
        });
      } catch (nativeError) {
        console.log('Vault Guard: Native host failed:', nativeError.message);
        
        // If native was explicitly chosen, don't fallback
        if (connectionMode === 'native') {
          throw nativeError;
        }
      }
    }

    // Try web API
    if (connectionMode === 'api' || connectionMode === 'auto') {
      try {
        return await this.sendWebApiMessage(action, data);
      } catch (apiError) {
        console.log('Vault Guard: Web API failed:', apiError.message);
        
        // If API was explicitly chosen, don't fallback
        if (connectionMode === 'api') {
          throw apiError;
        }
      }
    }

    // Last resort: try localStorage in auto mode
    if (connectionMode === 'auto') {
      try {
        return await this.sendLocalStorageMessage(action, data);
      } catch (localError) {
        throw new Error(`All connection methods failed. Check your settings and try again.`);
      }
    }

    throw new Error(`Connection failed using ${connectionMode} mode`);
  }

  async sendLocalStorageMessage(action, data) {
    // Handle actions using chrome.storage.local for offline support
    try {
      switch (action) {
        case 'getCredentials':
          return await this.getCredentialsFromLocalStorage(data.domain);
        case 'generatePassword':
          return await this.generatePasswordLocally(data.options);
        default:
          throw new Error(`Action ${action} not supported in localStorage mode`);
      }
    } catch (error) {
      throw new Error(`localStorage error: ${error.message}`);
    }
  }

  async getCredentialsFromLocalStorage(domain) {
    const result = await chrome.storage.local.get(['cachedCredentials']);
    const allCredentials = result.cachedCredentials || [];
    
    // Filter by domain if specified
    let credentials = allCredentials;
    if (domain) {
      credentials = allCredentials.filter(cred => 
        this.domainMatches(cred.websiteUrl || '', domain)
      );
    }

    return {
      success: true,
      credentials: credentials
    };
  }

  async generatePasswordLocally(options) {
    const length = options?.length || 16;
    const includeUppercase = options?.includeUppercase !== false;
    const includeLowercase = options?.includeLowercase !== false;
    const includeNumbers = options?.includeNumbers !== false;
    const includeSymbols = options?.includeSymbols !== false;

    let charset = '';
    if (includeLowercase) charset += 'abcdefghijklmnopqrstuvwxyz';
    if (includeUppercase) charset += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    if (includeNumbers) charset += '0123456789';
    if (includeSymbols) charset += '!@#$%^&*()_+-=[]{}|;:,.<>?';

    if (charset.length === 0) {
      throw new Error('At least one character type must be selected');
    }

    let password = '';
    const array = new Uint8Array(length);
    crypto.getRandomValues(array);
    
    for (let i = 0; i < length; i++) {
      password += charset[array[i] % charset.length];
    }

    return {
      success: true,
      password: password
    };
  }

  // ---------- "Save this login?" ----------
  // A submitted login is held for up to 2 minutes so the prompt can appear on the page the form leads to.
  // It lives in chrome.storage.session (memory only, never written to disk, not visible to content scripts),
  // keyed by tab, and is dropped as soon as the user answers.

  static PENDING_TTL_MS = 120000;

  pendingKey(tabId) { return `pendingSave:${tabId}`; }

  async sessionStore() {
    return chrome.storage.session ?? { // older browsers: keep it in this worker's memory
      _m: (this._pendingMem ??= {}),
      async get(k) { return { [k]: this._m[k] }; },
      async set(o) { Object.assign(this._m, o); },
      async remove(k) { delete this._m[k]; }
    };
  }

  /** The site the message really came from — the tab's URL, never a value the page could choose. */
  senderSite(sender) {
    try {
      const u = new URL(sender?.tab?.url || sender?.url || '');
      if (u.protocol !== 'https:' && u.protocol !== 'http:') return null;
      return { url: `${u.protocol}//${u.host}`, host: u.hostname.replace(/^www\./, '') };
    } catch { return null; }
  }

  async neverSaveSites() {
    const r = await chrome.storage.local.get(['neverSaveSites', 'offerToSaveLogins']);
    return { list: r.neverSaveSites || [], enabled: r.offerToSaveLogins !== false };
  }

  async captureLogin(request, sender) {
    const site = this.senderSite(sender);
    const password = typeof request.password === 'string' ? request.password : '';
    if (!site || !sender?.tab?.id || !password || password.length > 4096) return { success: false };
    const prefs = await this.neverSaveSites();
    if (!prefs.enabled || prefs.list.includes(site.host)) return { success: false, skipped: true };
    const store = await this.sessionStore();
    await store.set({ [this.pendingKey(sender.tab.id)]: {
      host: site.host, url: site.url, username: String(request.username || '').slice(0, 512), password,
      isNew: !!request.isNew, at: Date.now()
    } });
    return { success: true };
  }

  async readPending(sender) {
    if (!sender?.tab?.id) return null;
    const store = await this.sessionStore();
    const key = this.pendingKey(sender.tab.id);
    const p = (await store.get(key))[key];
    if (!p) return null;
    const site = this.senderSite(sender);
    // Only offer on the same site (sign-in pages may redirect within the site), and not after it expired.
    const sameSite = site && (site.host === p.host || site.host.endsWith('.' + p.host) || p.host.endsWith('.' + site.host));
    if (!sameSite || Date.now() - p.at > VaultGuardBackground.PENDING_TTL_MS) { await store.remove(key); return null; }
    return p;
  }

  /** What to offer for the tab's pending login: nothing, "save" or "update". The password never goes back to the page. */
  async getPendingSave(sender) {
    const p = await this.readPending(sender);
    if (!p) return { offer: null };
    const link = await this.refreshLink(true);
    if (!link?.linked || !link?.paired) return { offer: null };
    if (!link.unlocked) return { offer: { mode: 'locked', host: p.host, username: p.username } };
    const r = await this.sendNativeMessage({ action: 'getCredentials', domain: p.host });
    const creds = (r?.credentials || []);
    const sameUser = creds.filter(c => (c.username || '').toLowerCase() === p.username.toLowerCase());
    if (sameUser.some(c => c.password === p.password)) {            // already saved: nothing to ask
      await (await this.sessionStore()).remove(this.pendingKey(sender.tab.id));
      return { offer: null };
    }
    const existing = sameUser[0] || (!p.username && creds.length === 1 ? creds[0] : null);
    return existing
      ? { offer: { mode: 'update', host: p.host, username: p.username || existing.username, itemId: existing.id, title: existing.title } }
      : { offer: { mode: 'save', host: p.host, username: p.username, title: p.host } };
  }

  async savePendingLogin(request, sender) {
    const p = await this.readPending(sender);
    if (!p) return { success: false, error: 'Nothing to save — the sign-in was too long ago.' };
    const message = { action: 'saveLogin', domain: p.host, url: p.url, username: p.username, loginPassword: p.password,
                      title: String(request.title || p.host).slice(0, 200) };
    if (Number.isInteger(request.itemId)) message.itemId = request.itemId;
    const r = await this.sendNativeMessage(message);
    if (r?.success) await (await this.sessionStore()).remove(this.pendingKey(sender.tab.id));
    return r || { success: false, error: 'Vault Guard did not answer.' };
  }

  async dismissPendingSave(request, sender) {
    const p = await this.readPending(sender);
    if (sender?.tab?.id) await (await this.sessionStore()).remove(this.pendingKey(sender.tab.id));
    if (request.never && p) {
      const prefs = await this.neverSaveSites();
      if (!prefs.list.includes(p.host)) await chrome.storage.local.set({ neverSaveSites: [...prefs.list, p.host] });
    }
    return { success: true };
  }

  browserName() {
    const brands = (navigator.userAgentData && navigator.userAgentData.brands) || [];
    const brand = brands.map(b => b.brand).find(b => !/Not.?A.?Brand|Chromium/i.test(b));
    if (brand) return brand;
    return /Firefox/i.test(navigator.userAgent) ? 'Firefox' : 'Chromium-based browser';
  }

  /** Asks the desktop app (through the native host) whether it's running, unlocked and has approved us. */
  async refreshLink(force = false) {
    if (!force && this.link && Date.now() - this.linkCheckedAt < 5000) return this.link;
    try {
      const response = await this.sendNativeMessage({ action: 'hello', extensionVersion: chrome.runtime.getManifest().version, protocol: 2 });
      this.link = {
        linked: !!response?.linked,
        paired: !!response?.paired,
        unlocked: !!response?.unlocked,
        app: response?.app || null,
        appVersion: response?.appVersion || null,
        protocol: response?.protocol || null,
        needsUpdate: !!response?.protocol && response.protocol < 2,
        error: response?.error || null
      };
    } catch (error) {
      this.link = { linked: false, paired: false, unlocked: false, error: 'Browser link helper not installed. Open Vault Guard once to set it up.' };
    }
    this.linkCheckedAt = Date.now();
    return this.link;
  }

  /** Pair with the running app: the user approves this browser in Vault Guard, which returns a secret. */
  async connectApp(sendResponse) {
    try {
      const response = await this.sendNativeMessage({ action: 'pair' });
      if (response?.success && response.secret) {
        this.linkSecret = response.secret;
        await chrome.storage.local.set({ linkSecret: response.secret });
      }
      const link = await this.refreshLink(true);
      sendResponse({ success: !!response?.success, error: response?.error, link });
    } catch (error) {
      sendResponse({ success: false, error: error.message });
    }
  }

  /** Signed in (legacy) or linked to an unlocked, approving desktop app. */
  async hasAccess() {
    if (this.authToken) return true;
    const link = await this.refreshLink();
    return !!(link && link.linked && link.paired && link.unlocked);
  }

  async getCredentials(request, sendResponse) {
    if (!(await this.hasAccess())) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      const response = await this.sendMessageWithFallback('getCredentials', {
        token: this.authToken,
        domain: request.domain || ''
      });

      if (response.success) {
        sendResponse({ 
          success: true, 
          credentials: response.credentials 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: response.error || 'Failed to get credentials' 
        });
      }
    } catch (error) {
      console.error('Vault Guard: Error fetching credentials:', error);
      sendResponse({ 
        success: false, 
        error: `Failed to communicate with password manager: ${error.message}` 
      });
    }
  }

  async getCreditCards(request, sendResponse) {
    if (!(await this.hasAccess())) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      const response = await this.sendMessageWithFallback('getCreditCards', {
        token: this.authToken,
        domain: request.domain || ''
      });

      if (response.success) {
        sendResponse({ 
          success: true, 
          creditCards: response.creditCards 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: response.error || 'Failed to get credit cards' 
        });
      }
    } catch (error) {
      console.error('Vault Guard: Error fetching credit cards:', error);
      sendResponse({ 
        success: false, 
        error: `Failed to communicate with password manager: ${error.message}` 
      });
    }
  }

  domainMatches(websiteUrl, currentDomain) {
    if (!websiteUrl || !currentDomain) return false;
    
    try {
      // Clean up the website URL
      let cleanUrl = websiteUrl.toLowerCase();
      if (!cleanUrl.startsWith('http://') && !cleanUrl.startsWith('https://')) {
        cleanUrl = 'https://' + cleanUrl;
      }
      
      const urlDomain = new URL(cleanUrl).hostname.replace('www.', '');
      const currentCleanDomain = currentDomain.replace('www.', '');
      
      return urlDomain === currentCleanDomain || 
             urlDomain.endsWith('.' + currentCleanDomain) ||
             currentCleanDomain.endsWith('.' + urlDomain);
    } catch (error) {
      // Fallback to simple string matching
      return websiteUrl.toLowerCase().includes(currentDomain.toLowerCase());
    }
  }

  async generatePassword(request, sendResponse) {
    try {
      const options = request.options || {
        length: 16,
        includeUppercase: true,
        includeLowercase: true,
        includeNumbers: true,
        includeSymbols: true
      };

      // Honor the configured backend (Local SQLite / API / offline) instead of native-only.
      const response = await this.sendMessageWithFallback('generatePassword', {
        options: options
      });

      if (response.success) {
        sendResponse({ 
          success: true, 
          password: response.password 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: response.error || 'Failed to generate password' 
        });
      }
    } catch (error) {
      console.error('Vault Guard: Error generating password:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to communicate with native host for password generation' 
      });
    }
  }

  async login(request, sendResponse) {
    try {
      // Unlock against whichever backend is configured (Local SQLite app or API server).
      const response = await this.sendMessageWithFallback('login', {
        email: request.username, // extension collects "username", backends expect email
        password: request.password
      });

      // Native host returns { token }; the API may return { token | accessToken | jwt }.
      const token = response && (response.token || response.accessToken || response.jwt);

      if (response && (response.success || token)) {
        this.authToken = token || this.authToken;

        // Save token to storage
        await chrome.storage.sync.set({ authToken: this.authToken });

        sendResponse({
          success: true,
          message: response.message || 'Login successful'
        });
      } else {
        sendResponse({
          success: false,
          error: (response && response.error) || 'Login failed'
        });
      }
    } catch (error) {
      console.error('Vault Guard: Login error:', error);
      sendResponse({
        success: false,
        error: `Login failed: ${error.message}. Check your backend setting (Local app vs API) in Settings.`
      });
    }
  }

  async logout(sendResponse) {
    try {
      this.authToken = null;
      await chrome.storage.sync.remove(['authToken']);
      
      sendResponse({ 
        success: true, 
        message: 'Logged out successfully' 
      });
    } catch (error) {
      console.error('Vault Guard: Logout error:', error);
      sendResponse({ 
        success: false, 
        error: 'Logout failed' 
      });
    }
  }

  async getSettings(sendResponse) {
    try {
      const result = await chrome.storage.sync.get(['authToken']);
      const link = await this.refreshLink();

      sendResponse({ 
        success: true, 
        settings: {
          isLoggedIn: !!result.authToken || !!(link.linked && link.paired && link.unlocked),
          link,
          supportInfo: 'This extension uses native messaging to connect directly to your local SQLite database.'
        }
      });
    } catch (error) {
      console.error('Vault Guard: Error getting settings:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to get settings' 
      });
    }
  }

  async handlePasskey(request, sendResponse) {
    // Passkey signing/registration needs the unlocked vault (master key) held by the
    // native host session, so a valid token is required.
    if (!this.authToken) {
      sendResponse({ success: false, error: 'Vault is locked. Sign in to the extension first.', fallback: true });
      return;
    }

    try {
      const { action, ...fields } = request;
      // Route to whichever backend is configured. In API mode this hits the server's
      // vault-passkey endpoints; in Local mode it hits the native host + SQLite.
      const response = await this.sendMessageWithFallback(action, {
        token: this.authToken,
        ...fields
      });
      sendResponse(response || { success: false, error: 'No response from backend', fallback: true });
    } catch (error) {
      console.error('Vault Guard: Passkey request failed:', error);
      // fallback:true lets the page use the platform authenticator if our backend is unavailable.
      sendResponse({ success: false, error: error.message, fallback: true });
    }
  }

  async testConnection(sendResponse) {
    try {
      const settings = await chrome.storage.sync.get(['connectionMode', 'apiUrl']);
      const mode = settings.connectionMode || 'auto';
      const response = await this.sendMessageWithFallback('testConnection', {});

      // A native-host reply includes the resolved SQLite path; an API "health" reply won't.
      const ok = response && (response.success || response.status === 'healthy' || response.healthy);
      if (ok) {
        sendResponse({
          success: true,
          message: response.message || (mode === 'api' ? `Connected to API (${settings.apiUrl || ''})` : 'Connection successful'),
          databasePath: response.databasePath || ''
        });
      } else {
        sendResponse({
          success: false,
          error: (response && response.error) || 'Connection test failed'
        });
      }
    } catch (error) {
      console.error('Vault Guard: Connection test failed:', error);
      sendResponse({
        success: false,
        error: `Connection failed: ${error.message}. Check your backend setting in Settings.`
      });
    }
  }

  // ── TOTP Secret Save ──────────────────────────────────────────────────────────
  // Called by the content script after intercepting an otpauth:// URI on a 2FA
  // setup page. Tries to attach the TOTP secret to the existing vault entry for
  // this hostname, or creates a new entry if none exists.
  async saveTotpSecret(request, sendResponse) {
    const { otpauthUri, issuer, account, secret, hostname } = request;

    // Try native host (SQLite) first — works offline, no auth token needed
    try {
      const nativeResult = await this.sendNativeMessage({
        action: 'saveTotpSecret',
        token: this.authToken,
        otpauthUri,
        issuer,
        account,
        hostname,
      });
      if (nativeResult && nativeResult.success) {
        sendResponse({ success: true, source: 'sqlite', ...nativeResult });
        return;
      }
    } catch (_) {
      // Native host unavailable — fall through to API
    }

    // Fallback: save via REST API
    try {
      const apiUrl = await this.getApiUrl();

      // 1. Find an existing vault item for this hostname
      const searchResp = await fetch(
        `${apiUrl}/api/credentials?url=${encodeURIComponent(hostname)}`,
        { headers: { Authorization: `Bearer ${this.authToken}` } }
      );

      let itemId = null;
      if (searchResp.ok) {
        const items = await searchResp.json();
        if (Array.isArray(items) && items.length > 0) itemId = items[0].id;
      }

      if (itemId) {
        const patchResp = await fetch(`${apiUrl}/api/credentials/${itemId}/totp`, {
          method: 'PATCH',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${this.authToken}` },
          body: JSON.stringify({ otpauthUri, secret }),
        });
        sendResponse({ success: patchResp.ok, source: 'api', error: patchResp.ok ? null : await patchResp.text() });
      } else {
        const createResp = await fetch(`${apiUrl}/api/credentials`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${this.authToken}` },
          body: JSON.stringify({ title: issuer || hostname, websiteUrl: `https://${hostname}`, username: account || '', password: '', totpSecret: otpauthUri }),
        });
        sendResponse({ success: createResp.ok, source: 'api', error: createResp.ok ? null : await createResp.text() });
      }
    } catch (err) {
      sendResponse({ success: false, error: err.message });
    }
  }
}

// Initialize the background script
new VaultGuardBackground();