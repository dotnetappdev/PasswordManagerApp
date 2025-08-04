// Background script for Password Manager browser extension
class PasswordManagerBackground {
  constructor() {
    this.apiUrl = 'http://localhost:5000';
    this.authToken = null;
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
      const result = await chrome.storage.sync.get(['apiUrl', 'authToken']);
      if (result.apiUrl) {
        this.apiUrl = result.apiUrl;
      }
      if (result.authToken) {
        this.authToken = result.authToken;
      }
    } catch (error) {
      console.error('Password Manager: Error loading settings:', error);
    }
  }

  async handleMessage(request, sender, sendResponse) {
    try {
      switch (request.action) {
        case 'getCredentials':
          await this.getCredentials(request, sendResponse);
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
        case 'saveSettings':
          await this.saveSettings(request, sendResponse);
          break;
        case 'testConnection':
          await this.testConnection(sendResponse);
          break;
        default:
          sendResponse({ success: false, error: 'Unknown action' });
      }
    } catch (error) {
      console.error('Password Manager: Error handling message:', error);
      sendResponse({ success: false, error: error.message });
    }
  }

  async getCredentials(request, sendResponse) {
    if (!this.authToken) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      const response = await fetch(`${this.apiUrl}/api/passworditems`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.authToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const items = await response.json();
      
      // Filter credentials that match the domain
      const domain = request.domain;
      const matchingCredentials = items.filter(item => {
        if (item.type !== 0) return false; // Only login items (type 0)
        if (!item.loginItem) return false;
        
        const websiteUrl = item.loginItem.websiteUrl || item.loginItem.website || '';
        return this.domainMatches(websiteUrl, domain);
      }).map(item => ({
        id: item.id,
        title: item.title,
        username: item.loginItem.username || '',
        password: item.loginItem.password || '', // Will be decrypted by API
        websiteUrl: item.loginItem.websiteUrl || item.loginItem.website || ''
      }));

      sendResponse({ 
        success: true, 
        credentials: matchingCredentials 
      });
    } catch (error) {
      console.error('Password Manager: Error fetching credentials:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to fetch credentials. Please check your connection and try again.' 
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

      const password = this.createPassword(options);
      
      sendResponse({ 
        success: true, 
        password: password 
      });
    } catch (error) {
      console.error('Password Manager: Error generating password:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to generate password' 
      });
    }
  }

  createPassword(options) {
    let charset = '';
    
    if (options.includeLowercase) charset += 'abcdefghijklmnopqrstuvwxyz';
    if (options.includeUppercase) charset += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    if (options.includeNumbers) charset += '0123456789';
    if (options.includeSymbols) charset += '!@#$%^&*()_+-=[]{}|;:,.<>?';
    
    if (!charset) {
      charset = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    }
    
    let password = '';
    const array = new Uint8Array(options.length);
    crypto.getRandomValues(array);
    
    for (let i = 0; i < options.length; i++) {
      password += charset[array[i] % charset.length];
    }
    
    return password;
  }

  async login(request, sendResponse) {
    try {
      const response = await fetch(`${this.apiUrl}/api/authentication/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: request.username, // The API might expect email
          password: request.password
        })
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
      }

      const data = await response.json();
      this.authToken = data.token;
      
      // Save token to storage
      await chrome.storage.sync.set({ authToken: this.authToken });
      
      sendResponse({ 
        success: true, 
        message: 'Login successful' 
      });
    } catch (error) {
      console.error('Password Manager: Login error:', error);
      sendResponse({ 
        success: false, 
        error: error.message || 'Login failed' 
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
      console.error('Password Manager: Logout error:', error);
      sendResponse({ 
        success: false, 
        error: 'Logout failed' 
      });
    }
  }

  async getSettings(sendResponse) {
    try {
      const result = await chrome.storage.sync.get(['apiUrl', 'authToken']);
      
      sendResponse({ 
        success: true, 
        settings: {
          apiUrl: result.apiUrl || this.apiUrl,
          isLoggedIn: !!result.authToken
        }
      });
    } catch (error) {
      console.error('Password Manager: Error getting settings:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to get settings' 
      });
    }
  }

  async saveSettings(request, sendResponse) {
    try {
      const settings = request.settings;
      
      if (settings.apiUrl) {
        this.apiUrl = settings.apiUrl;
        await chrome.storage.sync.set({ apiUrl: settings.apiUrl });
      }
      
      sendResponse({ 
        success: true, 
        message: 'Settings saved successfully' 
      });
    } catch (error) {
      console.error('Password Manager: Error saving settings:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to save settings' 
      });
    }
  }

  async testConnection(sendResponse) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 5000);
      
      const response = await fetch(`${this.apiUrl}/api/passworditems`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        },
        signal: controller.signal
      });

      clearTimeout(timeoutId);

      if (response.ok || response.status === 401) {
        // 401 is expected without auth, but means API is reachable
        sendResponse({ 
          success: true, 
          message: 'Connection successful' 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: `Server responded with status ${response.status}` 
        });
      }
    } catch (error) {
      console.error('Password Manager: Connection test failed:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to connect to Password Manager API. Please check the URL.' 
      });
    }
  }
}

// Initialize the background script
new PasswordManagerBackground();