// Vault Guard Browser Extension Popup Script
class VaultGuardPopup {
  constructor() {
    this.currentScreen = 'loading';
    this.currentTab = 'credentials';
    this.credentials = [];
    this.filteredCredentials = [];
    this.currentDomain = '';
    this.init();
  }

  async init() {
    this.setupEventListeners();
    await this.getCurrentTabDomain();
    await this.checkAuthStatus();
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
    document.getElementById('settingsLink').addEventListener('click', () => this.showSettings());
    document.getElementById('settingsBtn').addEventListener('click', () => this.showSettings());
    document.getElementById('backBtn').addEventListener('click', () => this.showScreen('main'));
    document.getElementById('logoutBtn').addEventListener('click', () => this.logout());

    // About and Help - Main Screen
    document.getElementById('aboutBtn').addEventListener('click', () => this.showAbout());
    document.getElementById('helpBtn').addEventListener('click', () => this.showHelp());
    
    // About and Help - Login Screen
    document.getElementById('aboutLinkLogin').addEventListener('click', (e) => {
      e.preventDefault();
      this.showAbout();
    });
    document.getElementById('helpLinkLogin').addEventListener('click', (e) => {
      e.preventDefault();
      this.showHelp();
    });
    
    // Modal close buttons
    document.getElementById('aboutClose').addEventListener('click', () => this.closeModal('aboutModal'));
    document.getElementById('helpClose').addEventListener('click', () => this.closeModal('helpModal'));
    
    // Click outside modal to close
    document.getElementById('aboutModal').addEventListener('click', (e) => {
      if (e.target.id === 'aboutModal') this.closeModal('aboutModal');
    });
    document.getElementById('helpModal').addEventListener('click', (e) => {
      if (e.target.id === 'helpModal') this.closeModal('helpModal');
    });
    
    // Open full help
    const openFullHelpLink = document.getElementById('openFullHelp');
    if (openFullHelpLink) {
      openFullHelpLink.addEventListener('click', (e) => {
        e.preventDefault();
        chrome.tabs.create({ url: chrome.runtime.getURL('HELP.md') });
      });
    }

    // Login form
    document.getElementById('loginForm').addEventListener('submit', (e) => this.handleLogin(e));

    // Settings
    document.getElementById('connectionMode').addEventListener('change', (e) => this.handleConnectionModeChange(e));
    document.getElementById('saveSettingsBtn').addEventListener('click', () => this.saveSettings());
    document.getElementById('testConnectionBtn').addEventListener('click', () => this.testConnection());
    document.getElementById('detectDbBtn').addEventListener('click', () => this.detectDatabasePath());
    document.getElementById('presetSelfHostedBtn').addEventListener('click', () => {
      document.getElementById('apiUrl').value = 'http://localhost:5000';
    });
    document.getElementById('presetCloudBtn').addEventListener('click', () => {
      const input = document.getElementById('apiUrl');
      if (!input.value || input.value === 'http://localhost:5000') input.value = 'https://';
      input.focus();
    });

    // Tabs
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', (e) => this.switchTab(e.target.dataset.tab));
    });

    // Search
    document.getElementById('searchInput').addEventListener('click', (e) => this.filterCredentials(e.target.value));

    // View all credentials
    document.getElementById('viewAllBtn').addEventListener('click', () => this.loadAllCredentials());

    // Password generator
    document.getElementById('passwordLength').addEventListener('input', (e) => {
      document.getElementById('lengthValue').textContent = e.target.value;
    });
    document.getElementById('generateBtn').addEventListener('click', () => this.generatePassword());
    document.getElementById('copyPasswordBtn').addEventListener('click', () => this.copyPasswordToClipboard());
    document.getElementById('fillPasswordBtn').addEventListener('click', () => this.fillPasswordInCurrentTab());
  }

  async checkAuthStatus() {
    try {
      const response = await chrome.runtime.sendMessage({ action: 'getSettings' });
      
      if (response.success) {
        if (response.settings.isLoggedIn) {
          this.showScreen('main');
          await this.loadCredentials();
        } else {
          this.showScreen('login');
        }
      } else {
        this.showScreen('login');
      }
    } catch (error) {
      console.error('Error checking auth status:', error);
      this.showScreen('login');
    }
  }

  showScreen(screenName) {
    // Hide all screens
    document.querySelectorAll('.screen').forEach(screen => {
      screen.style.display = 'none';
    });
    
    // Show target screen
    document.getElementById(screenName).style.display = 'flex';
    this.currentScreen = screenName;
  }

  switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.classList.remove('active');
    });
    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
    
    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
      content.style.display = 'none';
    });
    document.getElementById(`${tabName}Tab`).style.display = 'block';
    
    this.currentTab = tabName;
  }

  async handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const loginBtn = document.getElementById('loginBtn');
    const errorDiv = document.getElementById('loginError');
    
    // Show loading state
    loginBtn.textContent = 'Logging in...';
    loginBtn.disabled = true;
    errorDiv.style.display = 'none';
    
    try {
      const response = await chrome.runtime.sendMessage({
        action: 'login',
        username: username,
        password: password
      });
      
      if (response.success) {
        this.showScreen('main');
        await this.loadCredentials();
      } else {
        errorDiv.textContent = response.error || 'Login failed';
        errorDiv.style.display = 'block';
      }
    } catch (error) {
      console.error('Login error:', error);
      errorDiv.textContent = 'Connection error. Please check your settings.';
      errorDiv.style.display = 'block';
    } finally {
      loginBtn.textContent = 'Login';
      loginBtn.disabled = false;
    }
  }

  async logout() {
    try {
      await chrome.runtime.sendMessage({ action: 'logout' });
      this.showScreen('login');
      
      // Clear form data
      document.getElementById('username').value = '';
      document.getElementById('password').value = '';
      this.credentials = [];
      this.filteredCredentials = [];
    } catch (error) {
      console.error('Logout error:', error);
    }
  }

  async showSettings() {
    // Load current settings before showing settings screen
    const settings = await chrome.storage.sync.get(['connectionMode', 'apiUrl', 'databasePath']);
    
    const connectionModeSelect = document.getElementById('connectionMode');
    const apiUrlInput = document.getElementById('apiUrl');
    const databasePathInput = document.getElementById('databasePath');
    
    connectionModeSelect.value = settings.connectionMode || 'auto';
    apiUrlInput.value = settings.apiUrl || 'http://localhost:5000';
    databasePathInput.value = settings.databasePath || '';
    
    // Show/hide fields based on connection mode
    this.handleConnectionModeChange({ target: connectionModeSelect });
    
    this.showScreen('settings');
  }

  handleConnectionModeChange(e) {
    const connectionMode = e.target.value;
    const apiUrlGroup = document.getElementById('apiUrlGroup');
    const databasePathGroup = document.getElementById('databasePathGroup');
    
    // Show appropriate fields based on connection mode
    // Auto mode shows all options since it tries all methods
    if (connectionMode === 'auto') {
      apiUrlGroup.style.display = 'block';
      databasePathGroup.style.display = 'block';
    } else if (connectionMode === 'api') {
      apiUrlGroup.style.display = 'block';
      databasePathGroup.style.display = 'none';
    } else if (connectionMode === 'native') {
      apiUrlGroup.style.display = 'none';
      databasePathGroup.style.display = 'block';
    } else {
      // localStorage mode
      apiUrlGroup.style.display = 'none';
      databasePathGroup.style.display = 'none';
    }
  }

  async saveSettings() {
    const connectionMode = document.getElementById('connectionMode').value;
    const apiUrl = document.getElementById('apiUrl').value;
    const databasePath = document.getElementById('databasePath').value;
    const messageDiv = document.getElementById('settingsMessage');
    
    try {
      await chrome.storage.sync.set({
        connectionMode: connectionMode,
        apiUrl: apiUrl,
        databasePath: databasePath
      });
      
      messageDiv.textContent = 'Settings saved successfully!';
      messageDiv.className = 'message success';
      messageDiv.style.display = 'block';
      
      setTimeout(() => {
        messageDiv.style.display = 'none';
      }, 2000);
    } catch (error) {
      messageDiv.textContent = 'Failed to save settings: ' + error.message;
      messageDiv.className = 'message error';
      messageDiv.style.display = 'block';
    }
  }

  async testConnection() {
    const testBtn = document.getElementById('testConnectionBtn');
    const messageDiv = document.getElementById('settingsMessage');
    
    testBtn.textContent = 'Testing...';
    testBtn.disabled = true;
    
    try {
      const response = await chrome.runtime.sendMessage({ action: 'testConnection' });

      if (response.success) {
        messageDiv.textContent = 'Connection successful!';
        messageDiv.className = 'message success';
        this.showActiveDb(response.databasePath);
      } else {
        messageDiv.textContent = response.error || 'Connection failed';
        messageDiv.className = 'message error';
      }

      messageDiv.style.display = 'block';
      setTimeout(() => {
        messageDiv.style.display = 'none';
      }, 3000);
    } catch (error) {
      messageDiv.textContent = 'Connection test failed';
      messageDiv.className = 'message error';
      messageDiv.style.display = 'block';
    } finally {
      testBtn.textContent = 'Test Connection';
      testBtn.disabled = false;
    }
  }

  showActiveDb(path) {
    const info = document.getElementById('activeDbInfo');
    if (!info) return;
    if (path) {
      info.innerHTML = '<strong>Active database (native host):</strong><br>' + this.escapeHtml(path);
      info.style.display = 'block';
    } else {
      info.style.display = 'none';
    }
  }

  // Ask the native host which SQLite DB it resolved, and prefill the path field with it
  // so the extension is guaranteed to point at the same vault as the desktop app.
  async detectDatabasePath() {
    const detectBtn = document.getElementById('detectDbBtn');
    const messageDiv = document.getElementById('settingsMessage');
    const databasePathInput = document.getElementById('databasePath');

    detectBtn.textContent = 'Detecting...';
    detectBtn.disabled = true;

    try {
      const response = await chrome.runtime.sendMessage({ action: 'testConnection' });

      if (response.success && response.databasePath) {
        databasePathInput.value = response.databasePath;
        this.showActiveDb(response.databasePath);
        messageDiv.textContent = 'Found database. Click "Save Settings" to use it.';
        messageDiv.className = 'message success';
      } else {
        messageDiv.textContent = response.error || 'Could not detect the database. Is the native host installed?';
        messageDiv.className = 'message error';
      }

      messageDiv.style.display = 'block';
      setTimeout(() => { messageDiv.style.display = 'none'; }, 4000);
    } catch (error) {
      messageDiv.textContent = 'Detection failed: ' + error.message;
      messageDiv.className = 'message error';
      messageDiv.style.display = 'block';
    } finally {
      detectBtn.textContent = 'Detect from app';
      detectBtn.disabled = false;
    }
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

  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  showAbout() {
    document.getElementById('aboutModal').style.display = 'flex';
  }

  showHelp() {
    document.getElementById('helpModal').style.display = 'flex';
  }

  closeModal(modalId) {
    document.getElementById(modalId).style.display = 'none';
  }
}

// Initialize popup when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new VaultGuardPopup();
});
