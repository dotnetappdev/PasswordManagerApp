// Firefox provides the promise-based `browser` API; Chrome, Edge and Brave provide `chrome`.
const ext = globalThis.browser ?? globalThis.chrome;
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
    ext.runtime.onMessage.addListener((request, sender, sendResponse) => {
      this.handleMessage(request, sender, sendResponse);
      return true; // Keep message channel open for async responses
    });

    // Load settings on startup
    this.loadSettings();

    this.setUpMenus();
    this.setUpCommands();
    this.setUpBadge();
  }

  // ---------- Right-click menu ----------
  // On any text field: "Fill login", "Fill a strong password", "Fill card". The page's own script does the work.
  setUpMenus() {
    if (!ext.contextMenus) return;
    const create = async () => {
      try {
        await ext.contextMenus.removeAll();
        const ctx = ['editable'];
        ext.contextMenus.create({ id: 'vg', title: 'Vault Guard', contexts: ctx });
        ext.contextMenus.create({ id: 'vg-login', parentId: 'vg', title: 'Fill login', contexts: ctx });
        ext.contextMenus.create({ id: 'vg-password', parentId: 'vg', title: 'Fill a strong new password', contexts: ctx });
        ext.contextMenus.create({ id: 'vg-card', parentId: 'vg', title: 'Fill card', contexts: ctx });
      } catch (e) { console.warn('Vault Guard: menus', e); }
    };
    ext.runtime.onInstalled?.addListener(create);
    ext.runtime.onStartup?.addListener(create);
    ext.contextMenus.onClicked.addListener((info, tab) => {
      const kind = { 'vg-login': 'login', 'vg-password': 'password', 'vg-card': 'card' }[info.menuItemId];
      if (kind && tab?.id != null) ext.tabs.sendMessage(tab.id, { action: 'vaultGuardContext', kind }, { frameId: 0 }).catch(() => {});
    });
  }

  // ---------- Keyboard shortcuts ----------
  // Ctrl+Shift+L: fill this site's login and sign in. Ctrl+Shift+G: strong password in the selected field.
  // (Change them at chrome://extensions/shortcuts, or Firefox: Add-ons → ⚙ → Manage Extension Shortcuts.)
  setUpCommands() {
    ext.commands?.onCommand.addListener(async (command, tab) => {
      if (command !== 'fill-login' && command !== 'generate-password') return;
      const target = tab ?? (await ext.tabs.query({ active: true, currentWindow: true }))[0];
      if (target?.id != null) ext.tabs.sendMessage(target.id, { action: 'vaultGuardCommand', command }, { frameId: 0 }).catch(() => {});
    });
  }

  // ---------- Toolbar badge ----------
  // The number of logins saved for the site in the current tab, like other password managers. Counts are
  // cached per site for a minute so switching tabs doesn't keep waking the app.
  setUpBadge() {
    if (!ext.action?.setBadgeText) return;
    this._badgeCache = new Map();
    ext.action.setBadgeBackgroundColor?.({ color: '#2252DA' }).catch?.(() => {});
    const update = (tabId) => this.updateBadge(tabId).catch(() => {});
    ext.tabs.onActivated.addListener(({ tabId }) => update(tabId));
    ext.tabs.onUpdated.addListener((tabId, change, tab) => { if (change.status === 'complete' && tab.active) update(tabId); });
  }

  async updateBadge(tabId) {
    const tab = await ext.tabs.get(tabId).catch(() => null);
    const site = tab && this.hostOfUrl(tab.url || '');
    const set = (text) => ext.action.setBadgeText({ tabId, text }).catch(() => {});
    if (!site || !/^https?:/.test(tab.url)) return set('');
    const link = await this.refreshLink();
    if (!link?.linked || !link.paired || !link.unlocked) { this._badgeCache.clear(); return set(''); } // locked: show nothing
    const cached = this._badgeCache.get(site.host);
    if (cached && Date.now() - cached.at < 60000) return set(cached.count ? String(cached.count) : '');
    const r = await this.sendNativeMessage({ action: 'getCredentials', domain: site.host }).catch(() => null);
    const count = (r?.credentials || []).length;
    this._badgeCache.set(site.host, { count, at: Date.now() });
    return set(count ? String(count) : '');
  }

  /** A login was saved or updated: recount for that site. */
  forgetBadge(host) { this._badgeCache?.delete(host); }

  async loadSettings() {
    try {
      const result = await ext.storage.sync.get(['authToken']);
      if (result.authToken) {
        this.authToken = result.authToken;
      }
      // The pairing secret stays on this device (storage.local, never synced to other browsers).
      const local = await ext.storage.local.get(['linkSecret']);
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
        case 'checkPasswordReuse':
          sendResponse(await this.checkPasswordReuse(request, sender));
          break;
        case 'trustSiteForReuse': {
          const site = this.senderSite(sender);
          if (site) {
            const r = await ext.storage.local.get('reuseTrustedSites');
            const list = r.reuseTrustedSites || [];
            if (!list.includes(site.host)) await ext.storage.local.set({ reuseTrustedSites: [...list, site.host] });
          }
          sendResponse({ success: true });
          break;
        }
        case 'captureLogin':
          sendResponse(await this.captureLogin(request, sender));
          break;
        case 'openAndFill':
          sendResponse(await this.openAndFill(request));
          break;
        case 'fillInTab':
          sendResponse(await this.fillInTab(request));
          break;
        case 'startPendingFill':
          sendResponse(await this.startPendingFill(request, sender));
          break;
        case 'takePendingFill':
          sendResponse(await this.takePendingFill(request, sender));
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
          // Lets the in-page menu send the user to "Connect" (ext.action.openPopup needs Chrome 127+).
          try { await ext.action.openPopup(); sendResponse({ success: true }); }
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
          await ext.storage.local.remove('linkSecret');
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
        const settings = await ext.storage.sync.get(['databasePath']);
        if (settings.databasePath) {
          message.databasePath = settings.databasePath;
        }
        
        if (this.linkSecret) {
          message.linkSecret = this.linkSecret;
        }
        message.browser = this.browserName();

        ext.runtime.sendNativeMessage(this.nativeHostName, message).then(resolve, (e) => reject(new Error(e?.message || String(e))));
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
    const result = await ext.storage.sync.get(['apiUrl']);
    return result.apiUrl || 'http://localhost:5000';
  }

  async sendMessageWithFallback(action, data) {
    // Check connection preference
    const settings = await ext.storage.sync.get(['connectionMode']);
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
    // Handle actions using ext.storage.local for offline support
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
    const result = await ext.storage.local.get(['cachedCredentials']);
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

  // ---------- Phishing guard ----------
  /** Asks the app whether the password typed on this tab's site is saved for a different site. */
  async checkPasswordReuse(request, sender) {
    const site = this.senderSite(sender);
    const password = typeof request.password === 'string' ? request.password : '';
    if (!site || password.length < 6 || password.length > 4096) return { matches: [] };
    const prefs = await ext.storage.local.get(['phishingGuard', 'reuseTrustedSites']);
    if (prefs.phishingGuard === false || (prefs.reuseTrustedSites || []).includes(site.host)) return { matches: [] };
    // At most one check every 1.5 s per tab, so typing can't flood the app.
    const now = Date.now(), last = (this._reuseChecked ??= {})[sender.tab?.id] || 0;
    if (now - last < 1500) return { matches: [], throttled: true };
    this._reuseChecked[sender.tab?.id] = now;
    if (!(await this.hasAccess())) return { matches: [] };
    const r = await this.sendNativeMessage({ action: 'checkPasswordReuse', domain: site.host, loginPassword: password });
    return { matches: Array.isArray(r?.matches) ? r.matches.slice(0, 3) : [], site: site.host };
  }

  // ---------- Open & fill / fill and sign in ----------
  // The popup (or the in-page menu) records which login to fill in which tab; the page's own script then asks for
  // it. The password is fetched for the tab's real site, so the app's site check still applies: a login is never
  // filled on a site that doesn't match its saved website. Kept in session memory for 2 minutes, so a sign-in that
  // asks for the username on one page and the password on the next is filled on both.

  static FILL_TTL_MS = 120000;

  fillKey(tabId) { return `pendingFill:${tabId}`; }

  /** "example.co.uk" for login.example.co.uk: good enough to let a sign-in move between a site's own subdomains. */
  siteKey(host) {
    const parts = String(host || '').toLowerCase().replace(/^www\./, '').split('.');
    if (parts.length <= 2) return parts.join('.');
    const sld = parts[parts.length - 2];
    const twoPart = parts[parts.length - 1].length === 2 && ['co', 'com', 'org', 'net', 'gov', 'ac', 'edu'].includes(sld);
    return parts.slice(twoPart ? -3 : -2).join('.');
  }

  hostOfUrl(url) {
    try {
      const u = new URL(/^[a-z][a-z0-9+.-]*:\/\//i.test(url) ? url : `https://${url}`);
      return (u.protocol === 'https:' || u.protocol === 'http:') ? { href: u.href, host: u.hostname.replace(/^www\./, '') } : null;
    } catch { return null; }
  }

  async setPendingFill(tabId, itemId, host) {
    await (await this.sessionStore()).set({ [this.fillKey(tabId)]: { itemId, host, at: Date.now(), usernameDone: false } });
  }

  /** Popup: open the login's website and sign in there. */
  async openAndFill(request) {
    const target = this.hostOfUrl(request.url || '');
    if (!target || !Number.isInteger(request.itemId)) return { success: false, error: 'This login has no website to open.' };
    const [active] = await ext.tabs.query({ active: true, currentWindow: true });
    const blank = active && /^(chrome|edge|brave|about):\/\/(newtab|blank)|^about:blank|^chrome:\/\/new-tab-page/i.test(active.url || active.pendingUrl || '');
    // Record the fill before the site starts loading, so the page can't ask before it's there.
    const tab = blank ? active : await ext.tabs.create({ url: 'about:blank', active: true });
    await this.setPendingFill(tab.id, request.itemId, target.host);
    await ext.tabs.update(tab.id, { url: target.href });
    return { success: true };
  }

  /** Popup: fill this login on the page that's open now (and sign in). The page's own script reports its site. */
  async fillInTab(request) {
    if (!Number.isInteger(request.itemId) || !Number.isInteger(request.tabId)) return { success: false };
    await this.setPendingFill(request.tabId, request.itemId, null);
    const sent = await ext.tabs.sendMessage(request.tabId, { action: 'vaultGuardRunPendingFill' }).then(() => true).catch(() => false);
    if (!sent) {
      await (await this.sessionStore()).remove(this.fillKey(request.tabId));
      return { success: false, error: "Vault Guard can't fill on this page. Reload the page and try again." };
    }
    return { success: true };
  }

  /** In-page menu: a username-only step was filled; carry the login on to the password step. */
  async startPendingFill(request, sender) {
    const site = this.senderSite(sender);
    if (!site || !sender?.tab?.id || !Number.isInteger(request.itemId)) return { success: false };
    await (await this.sessionStore()).set({ [this.fillKey(sender.tab.id)]: { itemId: request.itemId, host: site.host, at: Date.now(), usernameDone: true } });
    return { success: true };
  }

  /**
   * The page asks for its pending fill. request.step: 'peek' (is there one?), 'username' (filled the username
   * only: keep it for the password step), 'done' (filled the password: forget it).
   */
  async takePendingFill(request, sender) {
    if (!sender?.tab?.id) return { pending: false };
    const store = await this.sessionStore();
    const key = this.fillKey(sender.tab.id);
    const p = (await store.get(key))[key];
    if (!p) return { pending: false };
    const site = this.senderSite(sender);
    if (site && !p.host) p.host = site.host; // "fill here": the tab's site, as its page reports it
    if (!site || Date.now() - p.at > VaultGuardBackground.FILL_TTL_MS || this.siteKey(site.host) !== this.siteKey(p.host)) {
      // Expired, or the tab went somewhere else entirely: drop it.
      if (!site || Date.now() - p.at > VaultGuardBackground.FILL_TTL_MS) await store.remove(key);
      return { pending: false };
    }
    if (request.step === 'done') { await store.remove(key); return { pending: false }; }
    if (request.step === 'username') { await store.set({ [key]: { ...p, usernameDone: true, at: Date.now() } }); return { pending: true }; }
    if (!(await this.hasAccess())) return { pending: true, error: 'Connect this browser to Vault Guard first.' };
    const r = await this.sendNativeMessage({ action: 'getCredentials', domain: site.host });
    const c = (r?.credentials || []).find(x => x.id === p.itemId);
    if (!c) return { pending: true, error: r?.error || `Vault Guard won't fill this login on ${site.host}: it doesn't match the login's saved website.` };
    return { pending: true, usernameDone: !!p.usernameDone,
             credential: { id: c.id, title: c.title, username: c.username || '', password: c.password || '', hasTotp: !!c.hasTotp } };
  }

  // ---------- "Save this login?" ----------
  // A submitted login is held for up to 2 minutes so the prompt can appear on the page the form leads to.
  // It lives in ext.storage.session (memory only, never written to disk, not visible to content scripts),
  // keyed by tab, and is dropped as soon as the user answers.

  static PENDING_TTL_MS = 120000;

  pendingKey(tabId) { return `pendingSave:${tabId}`; }

  async sessionStore() {
    return ext.storage.session ?? { // older browsers: keep it in this worker's memory
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
    const r = await ext.storage.local.get(['neverSaveSites', 'offerToSaveLogins']);
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
    if (r?.success) {
      await (await this.sessionStore()).remove(this.pendingKey(sender.tab.id));
      this.forgetBadge(p.host);
      this.updateBadge(sender.tab.id).catch(() => {});
    }
    return r || { success: false, error: 'Vault Guard did not answer.' };
  }

  async dismissPendingSave(request, sender) {
    const p = await this.readPending(sender);
    if (sender?.tab?.id) await (await this.sessionStore()).remove(this.pendingKey(sender.tab.id));
    if (request.never && p) {
      const prefs = await this.neverSaveSites();
      if (!prefs.list.includes(p.host)) await ext.storage.local.set({ neverSaveSites: [...prefs.list, p.host] });
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
      const response = await this.sendNativeMessage({ action: 'hello', extensionVersion: ext.runtime.getManifest().version, protocol: 2 });
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
        await ext.storage.local.set({ linkSecret: response.secret });
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
        await ext.storage.sync.set({ authToken: this.authToken });

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
      await ext.storage.sync.remove(['authToken']);
      
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
      const result = await ext.storage.sync.get(['authToken']);
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
      const settings = await ext.storage.sync.get(['connectionMode', 'apiUrl']);
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