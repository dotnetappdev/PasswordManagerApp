// Background script for Password Manager browser extension - Direct SQLite Database Access
// Importing required services
importScripts('lib/sql-wasm.js', 'crypto-service.js', 'database-service.js');

class PasswordManagerBackground {
  constructor() {
    this.databaseService = new DatabaseService();
    this.cryptoService = new CryptoService();
    this.masterKey = null;
    this.currentUser = null;
    this.isAuthenticated = false;
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
      const result = await chrome.storage.sync.get(['databasePath', 'userEmail', 'isAuthenticated']);
      if (result.databasePath) {
        this.databasePath = result.databasePath;
      }
      if (result.userEmail) {
        this.currentUser = result.userEmail;
      }
      this.isAuthenticated = result.isAuthenticated || false;
    } catch (error) {
      console.error('Password Manager: Error loading settings:', error);
    }
  }

  async handleMessage(request, sender, sendResponse) {
    try {
      switch (request.action) {
        case 'loadDatabase':
          await this.loadDatabase(request, sendResponse);
          break;
        case 'loadDatabaseFromSettings':
          await this.loadDatabaseFromSettings(request, sendResponse);
          break;
        case 'authenticate':
          await this.authenticate(request, sendResponse);
          break;
        case 'getCredentials':
          await this.getCredentials(request, sendResponse);
          break;
        case 'generatePassword':
          await this.generatePassword(request, sendResponse);
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
        case 'isAuthenticated':
          await this.checkAuthentication(sendResponse);
          break;
        default:
          sendResponse({ success: false, error: 'Unknown action' });
      }
    } catch (error) {
      console.error('Password Manager: Error handling message:', error);
      sendResponse({ success: false, error: error.message });
    }
  }

  async loadDatabase(request, sendResponse) {
    try {
      if (!request.databaseFile) {
        sendResponse({ success: false, error: 'No database file provided' });
        return;
      }

      // Initialize database service with the file
      const result = await this.databaseService.initialize(request.databaseFile);
      
      if (result.success) {
        // Store the database file name/path info
        await chrome.storage.sync.set({ 
          databaseLoaded: true,
          databaseName: request.databaseFile.name 
        });
        
        sendResponse({ 
          success: true, 
          message: 'Database loaded successfully',
          databaseName: request.databaseFile.name 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: result.error || 'Failed to load database' 
        });
      }
    } catch (error) {
      console.error('Password Manager: Error loading database:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to load database: ' + error.message 
      });
    }
  }

  async loadDatabaseFromSettings(request, sendResponse) {
    try {
      if (!request.settings) {
        sendResponse({ success: false, error: 'No settings provided' });
        return;
      }

      const settings = request.settings;
      
      // Validate required settings
      if (!settings.databasePath) {
        sendResponse({ success: false, error: 'Settings must contain databasePath' });
        return;
      }

      // Store the settings for user reference and future use
      await chrome.storage.sync.set({ 
        settingsLoaded: true,
        configuredDatabasePath: settings.databasePath,
        configuredDatabaseName: settings.databaseName || 'Database from settings',
        userPreferences: {
          databasePath: settings.databasePath,
          databaseName: settings.databaseName,
          autoRememberLocation: settings.autoRememberLocation || false
        }
      });
      
      // Return success with guidance message
      sendResponse({ 
        success: true, 
        message: 'Settings loaded successfully! Your preferences have been saved.',
        configuredPath: settings.databasePath,
        note: 'When selecting your database file, please choose the file located at: ' + settings.databasePath
      });
      
    } catch (error) {
      console.error('Password Manager: Error loading database from settings:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to load database from settings: ' + error.message 
      });
    }
  }

  async authenticate(request, sendResponse) {
    try {
      const { userEmail, masterPassword } = request;
      
      if (!userEmail || !masterPassword) {
        sendResponse({ success: false, error: 'Email and master password required' });
        return;
      }

      // Get user salt from database
      const userSaltBase64 = await this.databaseService.getUserSalt(userEmail);
      if (!userSaltBase64) {
        sendResponse({ success: false, error: 'User not found' });
        return;
      }

      // Get stored password hash
      const storedHash = await this.databaseService.getUserPasswordHash(userEmail);
      if (!storedHash) {
        sendResponse({ success: false, error: 'User authentication data not found' });
        return;
      }

      // Verify master password
      const isValid = await this.cryptoService.verifyMasterPassword(masterPassword, userSaltBase64, storedHash);
      
      if (isValid) {
        // Derive and cache master key
        this.masterKey = await this.cryptoService.deriveMasterKey(masterPassword, userSaltBase64);
        this.currentUser = userEmail;
        this.isAuthenticated = true;
        
        // Save authentication state
        await chrome.storage.sync.set({ 
          userEmail: userEmail,
          isAuthenticated: true 
        });
        
        sendResponse({ 
          success: true, 
          message: 'Authentication successful' 
        });
      } else {
        sendResponse({ 
          success: false, 
          error: 'Invalid master password' 
        });
      }
    } catch (error) {
      console.error('Password Manager: Authentication error:', error);
      sendResponse({ 
        success: false, 
        error: error.message || 'Authentication failed' 
      });
    }
  }

  async getCredentials(request, sendResponse) {
    if (!this.isAuthenticated || !this.masterKey) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      // Get encrypted login items from database
      const domain = request.domain;
      const loginItems = await this.databaseService.getLoginItems(this.currentUser, domain);
      
      // Decrypt passwords
      const credentials = [];
      for (const item of loginItems) {
        try {
          let decryptedPassword = '';
          
          // Only decrypt if we have encrypted password data
          if (item.encryptedPassword && item.passwordNonce && item.passwordAuthTag) {
            const encryptedPasswordData = {
              encryptedPassword: item.encryptedPassword,
              passwordNonce: item.passwordNonce,
              passwordAuthTag: item.passwordAuthTag
            };
            
            decryptedPassword = await this.cryptoService.decryptPassword(encryptedPasswordData, this.masterKey);
          }
          
          credentials.push({
            id: item.id,
            title: item.title,
            username: item.username || '',
            password: decryptedPassword,
            websiteUrl: item.websiteUrl || ''
          });
        } catch (decryptError) {
          console.error('Error decrypting password for item:', item.id, decryptError);
          // Still include the item but without the password
          credentials.push({
            id: item.id,
            title: item.title,
            username: item.username || '',
            password: '', // Empty password if decryption fails
            websiteUrl: item.websiteUrl || ''
          });
        }
      }

      // Filter credentials that match the domain if specified
      let filteredCredentials = credentials;
      if (domain) {
        filteredCredentials = credentials.filter(cred => {
          const websiteUrl = cred.websiteUrl || '';
          return this.domainMatches(websiteUrl, domain);
        });
      }

      sendResponse({ 
        success: true, 
        credentials: filteredCredentials 
      });
    } catch (error) {
      console.error('Password Manager: Error fetching credentials:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to fetch credentials: ' + error.message 
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

  async logout(sendResponse) {
    try {
      // Clear master key from memory
      if (this.masterKey) {
        this.cryptoService.clearArray(this.masterKey);
        this.masterKey = null;
      }
      
      this.currentUser = null;
      this.isAuthenticated = false;
      
      // Clear stored authentication state
      await chrome.storage.sync.remove(['isAuthenticated', 'userEmail']);
      
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

  async checkAuthentication(sendResponse) {
    try {
      const result = await chrome.storage.sync.get(['isAuthenticated', 'userEmail', 'databaseLoaded']);
      
      sendResponse({ 
        success: true, 
        isAuthenticated: this.isAuthenticated && !!this.masterKey,
        userEmail: this.currentUser,
        databaseLoaded: result.databaseLoaded || false
      });
    } catch (error) {
      console.error('Password Manager: Error checking authentication:', error);
      sendResponse({ 
        success: false, 
        error: 'Failed to check authentication status' 
      });
    }
  }

  async getSettings(sendResponse) {
    try {
      const result = await chrome.storage.sync.get([
        'databaseName', 
        'userEmail', 
        'isAuthenticated', 
        'databaseLoaded',
        'configuredDatabasePath',
        'configuredDatabaseName',
        'settingsLoaded',
        'userPreferences'
      ]);
      
      sendResponse({ 
        success: true, 
        settings: {
          databaseName: result.databaseName || '',
          userEmail: result.userEmail || '',
          isAuthenticated: this.isAuthenticated,
          databaseLoaded: result.databaseLoaded || false,
          configuredDatabasePath: result.configuredDatabasePath || '',
          configuredDatabaseName: result.configuredDatabaseName || '',
          settingsLoaded: result.settingsLoaded || false,
          userPreferences: result.userPreferences || {}
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
      const toSave = {};
      
      if (settings.databaseName !== undefined) {
        toSave.databaseName = settings.databaseName;
      }
      
      if (settings.userEmail !== undefined) {
        toSave.userEmail = settings.userEmail;
        this.currentUser = settings.userEmail;
      }
      
      await chrome.storage.sync.set(toSave);
      
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
}

// Initialize the background script
new PasswordManagerBackground();