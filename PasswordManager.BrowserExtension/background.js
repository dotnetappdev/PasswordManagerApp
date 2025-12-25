// Background script for Password Manager browser extension
class PasswordManagerBackground {
  constructor() {
    this.nativeHostName = 'com.passwordmanager.native_host';
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
      const result = await chrome.storage.sync.get(['authToken']);
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

  async sendNativeMessage(message) {
    return new Promise(async (resolve, reject) => {
      try {
        // Get database path from settings if available
        const settings = await chrome.storage.sync.get(['databasePath']);
        if (settings.databasePath) {
          message.databasePath = settings.databasePath;
        }
        
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
        console.log('Password Manager: localStorage failed:', localError.message);
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
        console.log('Password Manager: Native host failed:', nativeError.message);
        
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
        console.log('Password Manager: Web API failed:', apiError.message);
        
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

  async getCredentials(request, sendResponse) {
    if (!this.authToken) {
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
      console.error('Password Manager: Error fetching credentials:', error);
      sendResponse({ 
        success: false, 
        error: `Failed to communicate with password manager: ${error.message}` 
      });
    }
  }

  async getCreditCards(request, sendResponse) {
    if (!this.authToken) {
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
      console.error('Password Manager: Error fetching credit cards:', error);
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

      const response = await this.sendNativeMessage({
        action: 'generatePassword',
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
      console.error('Password Manager: Error generating password:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to communicate with native host for password generation' 
      });
    }
  }

  async login(request, sendResponse) {
    try {
      const response = await this.sendNativeMessage({
        action: 'login',
        email: request.username, // Browser extension sends username, but native host expects email
        password: request.password
      });

      if (response.success) {
        this.authToken = response.token;
        
        // Save token to storage
        await chrome.storage.sync.set({ authToken: this.authToken });
        
        sendResponse({ 
          success: true, 
          message: response.message || 'Login successful' 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: response.error || 'Login failed' 
        });
      }
    } catch (error) {
      console.error('Password Manager: Login error:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to communicate with native host. Please ensure the native host is installed.' 
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
      const result = await chrome.storage.sync.get(['authToken']);
      
      sendResponse({ 
        success: true, 
        settings: {
          isLoggedIn: !!result.authToken,
          supportInfo: 'This extension uses native messaging to connect directly to your local SQLite database.'
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

  async testConnection(sendResponse) {
    try {
      const response = await this.sendNativeMessage({
        action: 'testConnection'
      });

      if (response.success) {
        sendResponse({ 
          success: true, 
          message: response.message || 'Connection successful' 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: response.error || 'Connection test failed' 
        });
      }
    } catch (error) {
      console.error('Password Manager: Connection test failed:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to communicate with native host. Please ensure the native host is installed and registered.' 
      });
    }
  }
}

// Initialize the background script
new PasswordManagerBackground();