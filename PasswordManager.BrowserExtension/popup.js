// Password Manager Browser Extension Popup Script - Direct Database Access
class PasswordManagerPopup {
  constructor() {
    this.currentScreen = 'loading';
    this.currentTab = 'credentials';
    this.credentials = [];
    this.filteredCredentials = [];
    this.currentDomain = '';
    this.isAuthenticated = false;
    this.databaseLoaded = false;
    this.init();
  }

  async init() {
    this.setupEventListeners();
    await this.getCurrentTabDomain();
    await this.checkInitialState();
  }

  async getCurrentTabDomain() {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (tab && tab.url) {
        const url = new URL(tab.url);
        this.currentDomain = url.hostname;
      }
    } catch (error) {
      console.error('Error getting current tab domain:', error);
    }
  }

  setupEventListeners() {
    // Navigation
    document.getElementById('settingsLink')?.addEventListener('click', () => this.showScreen('settings'));
    document.getElementById('settingsLinkFromDb')?.addEventListener('click', () => this.showScreen('settings'));
    document.getElementById('backBtn')?.addEventListener('click', () => this.goBack());
    document.getElementById('logoutBtn')?.addEventListener('click', () => this.logout());
    document.getElementById('changeDatabaseLink')?.addEventListener('click', () => this.showScreen('database'));
    document.getElementById('changeDatabaseBtn')?.addEventListener('click', () => this.showScreen('database'));

    // Radio button listeners for load method
    document.querySelectorAll('input[name="loadMethod"]').forEach(radio => {
      radio.addEventListener('change', (e) => this.toggleLoadMethod(e.target.value));
    });

    // Database loading
    document.getElementById('loadDatabaseBtn')?.addEventListener('click', () => this.loadDatabase());

    // Login form
    document.getElementById('loginForm')?.addEventListener('submit', (e) => this.handleLogin(e));

    // Settings form
    document.getElementById('settingsForm')?.addEventListener('submit', (e) => this.handleSettingsSave(e));

    // Tabs
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', (e) => this.switchTab(e.target.dataset.tab));
    });

    // Search
    document.getElementById('searchInput')?.addEventListener('input', (e) => this.filterCredentials(e.target.value));

    // View all credentials
    document.getElementById('viewAllBtn')?.addEventListener('click', () => this.loadAllCredentials());

    // Password generator
    document.getElementById('passwordLength')?.addEventListener('input', (e) => {
      document.getElementById('lengthValue').textContent = e.target.value;
    });
    document.getElementById('generateBtn')?.addEventListener('click', () => this.generatePassword());
    document.getElementById('copyPasswordBtn')?.addEventListener('click', () => this.copyPasswordToClipboard());
    document.getElementById('fillPasswordBtn')?.addEventListener('click', () => this.fillPasswordInCurrentTab());
  }

  async checkInitialState() {
    try {
      const response = await chrome.runtime.sendMessage({ action: 'isAuthenticated' });
      
      if (response.success) {
        this.isAuthenticated = response.isAuthenticated;
        this.databaseLoaded = response.databaseLoaded;
        
        if (this.isAuthenticated && this.databaseLoaded) {
          this.showScreen('main');
          await this.loadCredentials();
        } else if (this.databaseLoaded) {
          this.showScreen('login');
          // Pre-fill user email if available
          if (response.userEmail) {
            document.getElementById('userEmail').value = response.userEmail;
          }
        } else {
          this.showScreen('database');
        }
      } else {
        this.showScreen('database');
      }
    } catch (error) {
      console.error('Error checking initial state:', error);
      this.showScreen('database');
    }
  }

  showScreen(screenName) {
    // Hide all screens
    document.querySelectorAll('.screen').forEach(screen => {
      screen.style.display = 'none';
    });
    
    // Show target screen
    const targetScreen = document.getElementById(screenName);
    if (targetScreen) {
      targetScreen.style.display = 'flex';
      this.currentScreen = screenName;
      
      // Update settings when showing settings screen
      if (screenName === 'settings') {
        this.updateSettingsDisplay();
      }
    }
  }

  goBack() {
    if (this.currentScreen === 'settings') {
      if (this.isAuthenticated) {
        this.showScreen('main');
      } else if (this.databaseLoaded) {
        this.showScreen('login');
      } else {
        this.showScreen('database');
      }
    }
  }

  toggleLoadMethod(method) {
    const directMethod = document.getElementById('directMethod');
    const settingsMethod = document.getElementById('settingsMethod');
    
    if (method === 'direct') {
      directMethod.style.display = 'block';
      settingsMethod.style.display = 'none';
    } else if (method === 'settings') {
      directMethod.style.display = 'none';
      settingsMethod.style.display = 'block';
    }
  }

  async loadDatabase() {
    const loadBtn = document.getElementById('loadDatabaseBtn');
    const errorDiv = document.getElementById('databaseError');
    const selectedMethod = document.querySelector('input[name="loadMethod"]:checked').value;
    
    // Show loading state
    loadBtn.textContent = 'Loading...';
    loadBtn.disabled = true;
    errorDiv.style.display = 'none';
    
    try {
      if (selectedMethod === 'direct') {
        await this.loadDatabaseDirect();
      } else if (selectedMethod === 'settings') {
        await this.loadDatabaseFromSettings();
      }
    } catch (error) {
      console.error('Database load error:', error);
      this.showError(errorDiv, 'Error loading database: ' + error.message);
    } finally {
      loadBtn.textContent = 'Load Database';
      loadBtn.disabled = false;
    }
  }

  async loadDatabaseDirect() {
    const fileInput = document.getElementById('databaseFile');
    const errorDiv = document.getElementById('databaseError');
    
    if (!fileInput.files[0]) {
      this.showError(errorDiv, 'Please select a database file');
      return;
    }
    
    const databaseFile = fileInput.files[0];
    
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'loadDatabase',
        databaseFile: databaseFile
      });
      
      if (response.success) {
        this.databaseLoaded = true;
        this.showScreen('login');
      } else {
        this.showError(errorDiv, response.error || 'Failed to load database');
      }
    } catch (error) {
      throw error;
    }
  }

  async loadDatabaseFromSettings() {
    const settingsFileInput = document.getElementById('settingsFile');
    const errorDiv = document.getElementById('databaseError');
    
    if (!settingsFileInput.files[0]) {
      this.showError(errorDiv, 'Please select a settings file');
      return;
    }
    
    const settingsFile = settingsFileInput.files[0];
    
    try {
      // Read and parse settings file
      const settingsText = await this.readFileAsText(settingsFile);
      let settings;
      
      try {
        settings = JSON.parse(settingsText);
      } catch (parseError) {
        throw new Error('Invalid JSON format in settings file');
      }
      
      // Validate settings structure
      if (!settings.databasePath) {
        throw new Error('Settings file must contain "databasePath" field');
      }
      
      // Send settings to background script
      const response = await chrome.runtime.sendMessage({
        action: 'loadDatabaseFromSettings',
        settings: settings
      });
      
      if (response.success) {
        // Show success message and switch to direct method with guidance
        const successDiv = document.createElement('div');
        successDiv.className = 'success-message';
        successDiv.innerHTML = `
          <p><strong>Settings loaded successfully!</strong></p>
          <p>Your preferences have been saved. Please now select your database file:</p>
          <p><strong>Expected location:</strong> ${response.configuredPath}</p>
        `;
        errorDiv.parentNode.insertBefore(successDiv, errorDiv);
        
        // Auto-switch to direct method for user convenience
        document.querySelector('input[name="loadMethod"][value="direct"]').checked = true;
        this.toggleLoadMethod('direct');
        
        // Clear the settings file input
        settingsFileInput.value = '';
        
        // Hide the success message after a few seconds
        setTimeout(() => {
          if (successDiv.parentNode) {
            successDiv.parentNode.removeChild(successDiv);
          }
        }, 8000);
      } else {
        this.showError(errorDiv, response.error || 'Failed to load database from settings');
      }
    } catch (error) {
      throw error;
    }
  }

  readFileAsText(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => resolve(e.target.result);
      reader.onerror = (e) => reject(new Error('Failed to read file'));
      reader.readAsText(file);
    });
  }

  async handleLogin(e) {
    e.preventDefault();
    
    const userEmail = document.getElementById('userEmail').value;
    const masterPassword = document.getElementById('masterPassword').value;
    const loginBtn = document.getElementById('loginBtn');
    const errorDiv = document.getElementById('loginError');
    
    // Show loading state
    loginBtn.textContent = 'Unlocking...';
    loginBtn.disabled = true;
    errorDiv.style.display = 'none';
    
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'authenticate',
        userEmail: userEmail,
        masterPassword: masterPassword
      });
      
      if (response.success) {
        this.isAuthenticated = true;
        this.showScreen('main');
        await this.loadCredentials();
      } else {
        this.showError(errorDiv, response.error || 'Authentication failed');
      }
    } catch (error) {
      console.error('Login error:', error);
      this.showError(errorDiv, 'Connection error: ' + error.message);
    } finally {
      loginBtn.textContent = 'Unlock';
      loginBtn.disabled = false;
    }
  }

  async logout() {
    try {
      await chrome.runtime.sendMessage({ action: 'logout' });
      this.isAuthenticated = false;
      
      // Clear form data
      document.getElementById('userEmail').value = '';
      document.getElementById('masterPassword').value = '';
      this.credentials = [];
      this.filteredCredentials = [];
      
      this.showScreen('login');
    } catch (error) {
      console.error('Logout error:', error);
    }
  }

  async updateSettingsDisplay() {
    try {
      const response = await chrome.runtime.sendMessage({ action: 'getSettings' });
      
      if (response.success) {
        const settings = response.settings;
        document.getElementById('currentDatabase').value = settings.databaseName || 'No database loaded';
        document.getElementById('currentUser').value = settings.userEmail || 'Not authenticated';
        
        // Show configured database path if available
        const configPathField = document.getElementById('configuredPath');
        if (configPathField) {
          configPathField.value = settings.configuredDatabasePath || 'Not configured';
        }
      }
    } catch (error) {
      console.error('Error updating settings display:', error);
    }
  }

  async handleSettingsSave(e) {
    e.preventDefault();
    
    const messageDiv = document.getElementById('settingsMessage');
    
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'saveSettings',
        settings: {
          // Settings are mostly read-only now, but keeping for future extensions
        }
      });
      
      if (response.success) {
        this.showMessage(messageDiv, 'Settings saved successfully!', 'success');
      } else {
        throw new Error(response.error);
      }
    } catch (error) {
      this.showMessage(messageDiv, error.message || 'Failed to save settings', 'error');
    }
  }

  switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.classList.remove('active');
    });
    document.querySelector(`[data-tab="${tabName}"]`)?.classList.add('active');
    
    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
      content.style.display = 'none';
    });
    document.getElementById(`${tabName}Tab`).style.display = 'block';
    
    this.currentTab = tabName;
  }

  async loadCredentials() {
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'getCredentials',
        domain: this.currentDomain
      });
      
      if (response.success) {
        this.credentials = response.credentials;
        this.filteredCredentials = [...this.credentials];
        this.renderCredentials();
      } else {
        console.error('Failed to load credentials:', response.error);
        this.showNoCredentials();
      }
    } catch (error) {
      console.error('Error loading credentials:', error);
      this.showNoCredentials();
    }
  }

  async loadAllCredentials() {
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'getCredentials',
        domain: '' // Empty domain to get all credentials
      });
      
      if (response.success) {
        this.credentials = response.credentials;
        this.filteredCredentials = [...this.credentials];
        this.renderCredentials();
      }
    } catch (error) {
      console.error('Error loading all credentials:', error);
    }
  }

  filterCredentials(searchTerm) {
    if (!searchTerm) {
      this.filteredCredentials = [...this.credentials];
    } else {
      const term = searchTerm.toLowerCase();
      this.filteredCredentials = this.credentials.filter(cred => 
        cred.title.toLowerCase().includes(term) ||
        cred.username.toLowerCase().includes(term) ||
        (cred.websiteUrl && cred.websiteUrl.toLowerCase().includes(term))
      );
    }
    this.renderCredentials();
  }

  renderCredentials() {
    const container = document.getElementById('credentialsList');
    const noCredentialsDiv = document.getElementById('noCredentials');
    
    if (this.filteredCredentials.length === 0) {
      container.innerHTML = '';
      noCredentialsDiv.style.display = 'block';
      return;
    }
    
    noCredentialsDiv.style.display = 'none';
    
    container.innerHTML = this.filteredCredentials.map(cred => `
      <div class="credential-item" data-id="${cred.id}">
        <div class="credential-title">${this.escapeHtml(cred.title)}</div>
        <div class="credential-username">${this.escapeHtml(cred.username)}</div>
        ${cred.websiteUrl ? `<div class="credential-url">${this.escapeHtml(cred.websiteUrl)}</div>` : ''}
      </div>
    `).join('');
    
    // Add click handlers
    container.querySelectorAll('.credential-item').forEach(item => {
      item.addEventListener('click', () => {
        const credId = parseInt(item.dataset.id);
        const credential = this.filteredCredentials.find(c => c.id === credId);
        if (credential) {
          this.fillCredentialInCurrentTab(credential);
        }
      });
    });
  }

  showNoCredentials() {
    document.getElementById('credentialsList').innerHTML = '';
    document.getElementById('noCredentials').style.display = 'block';
  }

  async fillCredentialInCurrentTab(credential) {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      
      await chrome.tabs.sendMessage(tab.id, {
        action: 'fillCredentials',
        credential: credential
      });
      
      // Close popup after filling
      window.close();
    } catch (error) {
      console.error('Error filling credentials:', error);
    }
  }

  async generatePassword() {
    const options = {
      length: parseInt(document.getElementById('passwordLength').value),
      includeUppercase: document.getElementById('includeUppercase').checked,
      includeLowercase: document.getElementById('includeLowercase').checked,
      includeNumbers: document.getElementById('includeNumbers').checked,
      includeSymbols: document.getElementById('includeSymbols').checked
    };
    
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'generatePassword',
        options: options
      });
      
      if (response.success) {
        document.getElementById('passwordOutput').value = response.password;
        document.getElementById('generatedPassword').style.display = 'block';
      } else {
        console.error('Password generation failed:', response.error);
      }
    } catch (error) {
      console.error('Error generating password:', error);
    }
  }

  async copyPasswordToClipboard() {
    const password = document.getElementById('passwordOutput').value;
    
    try {
      await navigator.clipboard.writeText(password);
      
      // Show temporary feedback
      const btn = document.getElementById('copyPasswordBtn');
      const originalText = btn.innerHTML;
      btn.innerHTML = '✓';
      btn.style.backgroundColor = '#28a745';
      
      setTimeout(() => {
        btn.innerHTML = originalText;
        btn.style.backgroundColor = '';
      }, 1000);
    } catch (error) {
      console.error('Error copying to clipboard:', error);
      
      // Fallback: select the text
      const input = document.getElementById('passwordOutput');
      input.select();
      input.setSelectionRange(0, 99999);
      document.execCommand('copy');
    }
  }

  async fillPasswordInCurrentTab() {
    const password = document.getElementById('passwordOutput').value;
    
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      
      await chrome.tabs.sendMessage(tab.id, {
        action: 'fillPassword',
        password: password
      });
      
      // Close popup after filling
      window.close();
    } catch (error) {
      console.error('Error filling password:', error);
    }
  }

  showError(errorDiv, message) {
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';
  }

  showMessage(messageDiv, message, type) {
    messageDiv.textContent = message;
    messageDiv.className = `message ${type}`;
    messageDiv.style.display = 'block';
    
    setTimeout(() => {
      messageDiv.style.display = 'none';
    }, 3000);
  }

  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}

// Initialize popup when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new PasswordManagerPopup();
});