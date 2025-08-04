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
    return new Promise((resolve, reject) => {
      chrome.runtime.sendNativeMessage(this.nativeHostName, message, (response) => {
        if (chrome.runtime.lastError) {
          reject(new Error(chrome.runtime.lastError.message));
        } else {
          resolve(response);
        }
      });
    });
  }

  async getCredentials(request, sendResponse) {
    if (!this.authToken) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      const response = await this.sendNativeMessage({
        action: 'getCredentials',
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
        error: 'Failed to communicate with native host. Please ensure the native host is installed.' 
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