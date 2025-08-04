// Background script for Password Manager browser extension - Direct SQLite Database Access & API Server Support
// Importing required services
importScripts('lib/sql-wasm.js', 'crypto-service.js', 'database-service.js', 'api-service.js');

class PasswordManagerBackground {
  constructor() {
    this.databaseService = new DatabaseService();
    this.cryptoService = new CryptoService();
    this.apiService = new ApiService();
    this.masterKey = null;
    this.currentUser = null;
    this.isAuthenticated = false;
    this.useApiMode = false; // Track whether we're using API or direct database access
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
      const result = await chrome.storage.sync.get([
        'databasePath', 
        'userEmail', 
        'isAuthenticated', 
        'useApiMode', 
        'apiUrl', 
        'apiKey'
      ]);
      
      if (result.databasePath) {
        this.databasePath = result.databasePath;
      }
      if (result.userEmail) {
        this.currentUser = result.userEmail;
      }
      
      this.isAuthenticated = result.isAuthenticated || false;
      this.useApiMode = result.useApiMode || false;
      
      // Configure API service if API credentials are available
      if (result.apiUrl && result.apiKey) {
        this.apiService.configure(result.apiUrl, result.apiKey);
        this.useApiMode = true;
      }
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
      
      // Check if this is API mode or direct database mode
      const isApiMode = settings.apiUrl && settings.apiKey;
      const isDatabaseMode = settings.databasePath;
      
      if (!isApiMode && !isDatabaseMode) {
        sendResponse({ 
          success: false, 
          error: 'Settings must contain either (apiUrl + apiKey) for API mode or databasePath for direct database mode' 
        });
        return;
      }

      if (isApiMode && isDatabaseMode) {
        sendResponse({ 
          success: false, 
          error: 'Settings cannot contain both API configuration and database path. Choose one mode.' 
        });
        return;
      }

      let storageData = {
        settingsLoaded: true,
        configuredDatabaseName: settings.databaseName || 'Configuration from settings',
        userPreferences: {
          databaseName: settings.databaseName,
          autoRememberLocation: settings.autoRememberLocation || false
        }
      };

      let responseMessage = '';
      let responseNote = '';

      if (isApiMode) {
        // API Mode Configuration
        try {
          // Test API connection
          this.apiService.configure(settings.apiUrl, settings.apiKey);
          await this.apiService.healthCheck();
          
          // Store API configuration
          storageData.useApiMode = true;
          storageData.apiUrl = settings.apiUrl;
          storageData.apiKey = settings.apiKey;
          storageData.configuredApiUrl = settings.apiUrl;
          
          this.useApiMode = true;
          
          responseMessage = 'API settings loaded successfully! You can now authenticate using your email and master password.';
          responseNote = `Connected to API server at: ${settings.apiUrl}`;
          
        } catch (error) {
          sendResponse({ 
            success: false, 
            error: `Failed to connect to API server: ${error.message}` 
          });
          return;
        }
      } else {
        // Direct Database Mode Configuration
        storageData.useApiMode = false;
        storageData.configuredDatabasePath = settings.databasePath;
        storageData.userPreferences.databasePath = settings.databasePath;
        
        this.useApiMode = false;
        
        responseMessage = 'Database settings loaded successfully! Your preferences have been saved.';
        responseNote = 'When selecting your database file, please choose the file located at: ' + settings.databasePath;
      }

      // Store the settings
      await chrome.storage.sync.set(storageData);
      
      // Return success with appropriate message
      sendResponse({ 
        success: true, 
        message: responseMessage,
        configuredPath: settings.databasePath || settings.apiUrl,
        note: responseNote,
        useApiMode: isApiMode
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

      if (this.useApiMode) {
        // API Mode Authentication
        try {
          const response = await this.apiService.authenticate(userEmail, masterPassword);
          
          if (response && response.success) {
            this.currentUser = userEmail;
            this.isAuthenticated = true;
            
            // Save authentication state
            await chrome.storage.sync.set({ 
              userEmail: userEmail,
              isAuthenticated: true 
            });
            
            sendResponse({ 
              success: true, 
              message: 'Authentication successful (API mode)' 
            });
          } else {
            sendResponse({ 
              success: false, 
              error: response?.message || 'Authentication failed' 
            });
          }
        } catch (error) {
          sendResponse({ 
            success: false, 
            error: `API authentication failed: ${error.message}` 
          });
        }
        
      } else {
        // Direct Database Mode Authentication (existing logic)
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
            message: 'Authentication successful (Database mode)' 
          });
        } else {
          sendResponse({ 
            success: false, 
            error: 'Invalid master password' 
          });
        }
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
    if (!this.isAuthenticated) {
      sendResponse({ success: false, error: 'Not authenticated' });
      return;
    }

    try {
      const domain = request.domain;
      
      if (this.useApiMode) {
        // API Mode - Get credentials from API server
        try {
          const response = await this.apiService.getCredentials(domain);
          
          if (response && response.success) {
            // Transform API response to match expected format
            const credentials = response.data || response.credentials || [];
            
            sendResponse({ 
              success: true, 
              credentials: credentials.map(item => ({
                id: item.id,
                title: item.title || item.name,
                username: item.username || item.email || '',
                password: item.password || '',
                websiteUrl: item.websiteUrl || item.url || ''
              }))
            });
          } else {
            sendResponse({ 
              success: false, 
              error: response?.message || 'Failed to fetch credentials from API' 
            });
          }
        } catch (error) {
          sendResponse({ 
            success: false, 
            error: `API request failed: ${error.message}` 
          });
        }
        
      } else {
        // Direct Database Mode (existing logic)
        if (!this.masterKey) {
          sendResponse({ success: false, error: 'Master key not available' });
          return;
        }

        // Get encrypted login items from database
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
      }
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
      // Clear master key from memory (for database mode)
      if (this.masterKey) {
        this.cryptoService.clearArray(this.masterKey);
        this.masterKey = null;
      }
      
      // Clear API service configuration if in API mode
      if (this.useApiMode) {
        // Note: We don't clear the stored API config, just the active session
        // The API service doesn't need explicit logout for token-based auth
      }
      
      this.currentUser = null;
      this.isAuthenticated = false;
      
      // Clear stored authentication state (but keep API/database configuration)
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
      const result = await chrome.storage.sync.get([
        'isAuthenticated', 
        'userEmail', 
        'databaseLoaded', 
        'useApiMode', 
        'settingsLoaded'
      ]);
      
      // For API mode, we don't need databaseLoaded, we need settingsLoaded with API config
      const isReady = this.useApiMode ? 
        (result.settingsLoaded && this.apiService.isReady()) : 
        result.databaseLoaded;
      
      sendResponse({ 
        success: true, 
        isAuthenticated: this.isAuthenticated && (this.useApiMode || !!this.masterKey),
        userEmail: this.currentUser,
        databaseLoaded: result.databaseLoaded || false,
        useApiMode: this.useApiMode,
        isReady: isReady || false
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
        'userPreferences',
        'useApiMode',
        'configuredApiUrl',
        'apiUrl',
        'editableApiUrl',
        'editableApiKey',
        'editableDatabaseName'
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
          userPreferences: result.userPreferences || {},
          useApiMode: this.useApiMode,
          configuredApiUrl: result.configuredApiUrl || result.apiUrl || '',
          editableApiUrl: result.editableApiUrl || '',
          editableApiKey: result.editableApiKey || '',
          editableDatabaseName: result.editableDatabaseName || '',
          isReady: this.useApiMode ? 
            (result.settingsLoaded && this.apiService.isReady()) : 
            result.databaseLoaded
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
      
      // Handle new editable API settings
      if (settings.editableApiUrl !== undefined) {
        toSave.editableApiUrl = settings.editableApiUrl;
      }
      
      if (settings.editableApiKey !== undefined) {
        toSave.editableApiKey = settings.editableApiKey;
      }
      
      if (settings.editableDatabaseName !== undefined) {
        toSave.editableDatabaseName = settings.editableDatabaseName;
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