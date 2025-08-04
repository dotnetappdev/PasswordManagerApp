// Content script for detecting and enhancing login forms
class PasswordManagerContentScript {
  constructor() {
    this.observer = null;
    this.icons = new Map();
    this.isAuthenticated = false;
    this.init();
  }

  init() {
    this.loadSettings();
    this.scanForForms();
    this.setupDOMObserver();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get(['isAuthenticated']);
      this.isAuthenticated = result.isAuthenticated || false;
    } catch (error) {
      console.log('Password Manager: Settings not found, using defaults');
    }
  }

  scanForForms() {
    // Find all forms on the page
    const forms = document.querySelectorAll('form');
    forms.forEach(form => this.processForm(form));

    // Also look for standalone password and username fields
    const usernameFields = this.findUsernameFields();
    const passwordFields = this.findPasswordFields();
    
    usernameFields.forEach(field => this.addFieldIcon(field, 'username'));
    passwordFields.forEach(field => this.addFieldIcon(field, 'password'));
  }

  processForm(form) {
    const usernameField = this.findUsernameFieldInForm(form);
    const passwordField = this.findPasswordFieldInForm(form);
    
    if (usernameField) {
      this.addFieldIcon(usernameField, 'username');
    }
    
    if (passwordField) {
      this.addFieldIcon(passwordField, 'password');
    }
  }

  findUsernameFields() {
    const selectors = [
      'input[type="text"][name*="user"]',
      'input[type="text"][name*="login"]',
      'input[type="text"][name*="email"]',
      'input[type="email"]',
      'input[id*="user"]',
      'input[id*="login"]',
      'input[id*="email"]',
      'input[placeholder*="username" i]',
      'input[placeholder*="email" i]',
      'input[autocomplete="username"]',
      'input[autocomplete="email"]'
    ];
    
    return document.querySelectorAll(selectors.join(','));
  }

  findPasswordFields() {
    return document.querySelectorAll('input[type="password"]');
  }

  findUsernameFieldInForm(form) {
    const selectors = [
      'input[type="text"][name*="user"]',
      'input[type="text"][name*="login"]',
      'input[type="email"]',
      'input[autocomplete="username"]',
      'input[autocomplete="email"]'
    ];
    
    for (const selector of selectors) {
      const field = form.querySelector(selector);
      if (field) return field;
    }
    
    // Fallback: first text input before a password field
    const textInputs = form.querySelectorAll('input[type="text"]');
    const passwordField = form.querySelector('input[type="password"]');
    
    if (textInputs.length > 0 && passwordField) {
      return textInputs[0];
    }
    
    return null;
  }

  findPasswordFieldInForm(form) {
    return form.querySelector('input[type="password"]');
  }

  addFieldIcon(field, type) {
    // Avoid duplicate icons
    if (field.dataset.pmIconAdded) return;
    
    const wrapper = this.createFieldWrapper(field);
    const icon = this.createIcon(type, field);
    
    field.dataset.pmIconAdded = 'true';
    this.icons.set(field, icon);
    
    // Position icon
    this.positionIcon(icon, field);
  }

  createFieldWrapper(field) {
    // Only wrap if not already wrapped
    if (field.parentElement.classList.contains('pm-field-wrapper')) {
      return field.parentElement;
    }
    
    const wrapper = document.createElement('div');
    wrapper.className = 'pm-field-wrapper';
    wrapper.style.position = 'relative';
    wrapper.style.display = 'inline-block';
    wrapper.style.width = '100%';
    
    field.parentNode.insertBefore(wrapper, field);
    wrapper.appendChild(field);
    
    return wrapper;
  }

  createIcon(type, field) {
    const icon = document.createElement('div');
    icon.className = `pm-field-icon pm-${type}-icon`;
    icon.innerHTML = type === 'username' ? '👤' : '🔑';
    icon.title = `Fill ${type} with Password Manager`;
    
    // Style the icon
    Object.assign(icon.style, {
      position: 'absolute',
      right: '8px',
      top: '50%',
      transform: 'translateY(-50%)',
      width: '20px',
      height: '20px',
      cursor: 'pointer',
      backgroundColor: '#007acc',
      borderRadius: '3px',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontSize: '12px',
      zIndex: '10000',
      boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
      transition: 'all 0.2s ease'
    });
    
    // Add hover effect
    icon.addEventListener('mouseenter', () => {
      icon.style.backgroundColor = '#005a9e';
      icon.style.transform = 'translateY(-50%) scale(1.1)';
    });
    
    icon.addEventListener('mouseleave', () => {
      icon.style.backgroundColor = '#007acc';
      icon.style.transform = 'translateY(-50%) scale(1)';
    });
    
    // Add click handler
    icon.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.handleIconClick(type, field);
    });
    
    // Add to field wrapper
    const wrapper = field.parentElement;
    if (wrapper.classList.contains('pm-field-wrapper')) {
      wrapper.appendChild(icon);
    }
    
    return icon;
  }

  positionIcon(icon, field) {
    // Adjust positioning based on field styles
    const fieldStyles = window.getComputedStyle(field);
    const fieldHeight = field.offsetHeight;
    
    if (fieldHeight > 40) {
      icon.style.right = '12px';
    }
  }

  async handleIconClick(type, field) {
    try {
      if (!this.isAuthenticated) {
        this.showNotification('Please unlock your Password Manager vault first.');
        return;
      }

      if (type === 'username') {
        await this.showCredentialSelector(field);
      } else if (type === 'password') {
        await this.showPasswordOptions(field);
      }
    } catch (error) {
      console.error('Password Manager: Error handling icon click:', error);
      this.showNotification('Error accessing Password Manager. Please check that your database is loaded.');
    }
  }

  async showCredentialSelector(field) {
    // Get current domain for filtering
    const domain = window.location.hostname;
    
    // Send message to background script to get credentials
    const response = await chrome.runtime.sendMessage({
      action: 'getCredentials',
      domain: domain
    });
    
    if (response.success && response.credentials.length > 0) {
      this.showCredentialPopup(field, response.credentials);
    } else {
      this.showNotification('No credentials found for this website.');
    }
  }

  showCredentialPopup(field, credentials) {
    // Remove existing popup
    const existingPopup = document.querySelector('.pm-credential-popup');
    if (existingPopup) {
      existingPopup.remove();
    }
    
    const popup = document.createElement('div');
    popup.className = 'pm-credential-popup';
    
    // Style the popup
    Object.assign(popup.style, {
      position: 'absolute',
      backgroundColor: 'white',
      border: '1px solid #ccc',
      borderRadius: '4px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      zIndex: '10001',
      minWidth: '200px',
      maxHeight: '300px',
      overflowY: 'auto'
    });
    
    // Add credentials to popup
    credentials.forEach((cred, index) => {
      const item = document.createElement('div');
      item.className = 'pm-credential-item';
      item.innerHTML = `
        <div style="padding: 12px; cursor: pointer; border-bottom: 1px solid #eee;">
          <div style="font-weight: bold; margin-bottom: 4px;">${this.escapeHtml(cred.title)}</div>
          <div style="color: #666; font-size: 14px;">${this.escapeHtml(cred.username)}</div>
        </div>
      `;
      
      item.addEventListener('click', () => {
        this.fillCredentials(field, cred);
        popup.remove();
      });
      
      item.addEventListener('mouseenter', () => {
        item.style.backgroundColor = '#f0f0f0';
      });
      
      item.addEventListener('mouseleave', () => {
        item.style.backgroundColor = 'white';
      });
      
      popup.appendChild(item);
    });
    
    // Position popup near the field
    this.positionPopup(popup, field);
    document.body.appendChild(popup);
    
    // Close popup when clicking outside
    setTimeout(() => {
      document.addEventListener('click', (e) => {
        if (!popup.contains(e.target)) {
          popup.remove();
        }
      }, { once: true });
    }, 100);
  }

  async showPasswordOptions(field) {
    // Show options: Fill existing password or generate new one
    const popup = document.createElement('div');
    popup.className = 'pm-password-popup';
    
    Object.assign(popup.style, {
      position: 'absolute',
      backgroundColor: 'white',
      border: '1px solid #ccc',
      borderRadius: '4px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      zIndex: '10001',
      minWidth: '200px'
    });
    
    popup.innerHTML = `
      <div style="padding: 12px;">
        <div class="pm-option" data-action="fill" style="padding: 8px; cursor: pointer; border-radius: 3px; margin-bottom: 4px;">
          🔑 Fill Saved Password
        </div>
        <div class="pm-option" data-action="generate" style="padding: 8px; cursor: pointer; border-radius: 3px;">
          ⚡ Generate New Password
        </div>
      </div>
    `;
    
    // Add hover effects
    popup.querySelectorAll('.pm-option').forEach(option => {
      option.addEventListener('mouseenter', () => {
        option.style.backgroundColor = '#f0f0f0';
      });
      option.addEventListener('mouseleave', () => {
        option.style.backgroundColor = 'transparent';
      });
      
      option.addEventListener('click', async () => {
        const action = option.dataset.action;
        if (action === 'fill') {
          await this.showCredentialSelector(field);
        } else if (action === 'generate') {
          await this.generatePassword(field);
        }
        popup.remove();
      });
    });
    
    this.positionPopup(popup, field);
    document.body.appendChild(popup);
    
    // Close popup when clicking outside
    setTimeout(() => {
      document.addEventListener('click', (e) => {
        if (!popup.contains(e.target)) {
          popup.remove();
        }
      }, { once: true });
    }, 100);
  }

  async generatePassword(field) {
    const response = await chrome.runtime.sendMessage({
      action: 'generatePassword',
      options: {
        length: 16,
        includeUppercase: true,
        includeLowercase: true,
        includeNumbers: true,
        includeSymbols: true
      }
    });
    
    if (response.success) {
      field.value = response.password;
      field.dispatchEvent(new Event('input', { bubbles: true }));
      field.dispatchEvent(new Event('change', { bubbles: true }));
      this.showNotification('Password generated successfully!');
    } else {
      this.showNotification('Error generating password.');
    }
  }

  fillCredentials(usernameField, credential) {
    // Fill username
    usernameField.value = credential.username;
    usernameField.dispatchEvent(new Event('input', { bubbles: true }));
    usernameField.dispatchEvent(new Event('change', { bubbles: true }));
    
    // Find and fill password field
    const form = usernameField.closest('form');
    const passwordField = form ? 
      form.querySelector('input[type="password"]') : 
      document.querySelector('input[type="password"]');
    
    if (passwordField && credential.password) {
      passwordField.value = credential.password;
      passwordField.dispatchEvent(new Event('input', { bubbles: true }));
      passwordField.dispatchEvent(new Event('change', { bubbles: true }));
    }
    
    this.showNotification('Credentials filled successfully!');
  }

  positionPopup(popup, field) {
    const fieldRect = field.getBoundingClientRect();
    popup.style.left = `${fieldRect.left}px`;
    popup.style.top = `${fieldRect.bottom + 5}px`;
    
    // Adjust if popup goes off screen
    setTimeout(() => {
      const popupRect = popup.getBoundingClientRect();
      if (popupRect.right > window.innerWidth) {
        popup.style.left = `${window.innerWidth - popupRect.width - 10}px`;
      }
      if (popupRect.bottom > window.innerHeight) {
        popup.style.top = `${fieldRect.top - popupRect.height - 5}px`;
      }
    }, 0);
  }

  setupDOMObserver() {
    this.observer = new MutationObserver((mutations) => {
      let shouldScan = false;
      
      mutations.forEach((mutation) => {
        if (mutation.type === 'childList') {
          mutation.addedNodes.forEach((node) => {
            if (node.nodeType === Node.ELEMENT_NODE) {
              if (node.tagName === 'FORM' || 
                  node.querySelector('form') || 
                  node.querySelector('input[type="password"]') ||
                  node.querySelector('input[type="text"]')) {
                shouldScan = true;
              }
            }
          });
        }
      });
      
      if (shouldScan) {
        setTimeout(() => this.scanForForms(), 100);
      }
    });
    
    this.observer.observe(document.body, {
      childList: true,
      subtree: true
    });
  }

  showNotification(message) {
    // Create a simple notification
    const notification = document.createElement('div');
    notification.className = 'pm-notification';
    notification.textContent = message;
    
    Object.assign(notification.style, {
      position: 'fixed',
      top: '20px',
      right: '20px',
      backgroundColor: '#007acc',
      color: 'white',
      padding: '12px 20px',
      borderRadius: '4px',
      zIndex: '10002',
      boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      fontSize: '14px',
      maxWidth: '300px'
    });
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
      notification.remove();
    }, 3000);
  }

  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}

// Listen for messages from popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === 'fillCredentials') {
    fillCredentialsFromPopup(request.credential);
    sendResponse({ success: true });
  } else if (request.action === 'fillPassword') {
    fillPasswordFromPopup(request.password);
    sendResponse({ success: true });
  }
  return true;
});

function fillCredentialsFromPopup(credential) {
  // Find the best username field
  const usernameField = findBestUsernameField();
  const passwordField = findBestPasswordField();
  
  if (usernameField && credential.username) {
    usernameField.value = credential.username;
    usernameField.dispatchEvent(new Event('input', { bubbles: true }));
    usernameField.dispatchEvent(new Event('change', { bubbles: true }));
  }
  
  if (passwordField && credential.password) {
    passwordField.value = credential.password;
    passwordField.dispatchEvent(new Event('input', { bubbles: true }));
    passwordField.dispatchEvent(new Event('change', { bubbles: true }));
  }
  
  showNotification('Credentials filled successfully!');
}

function fillPasswordFromPopup(password) {
  const passwordField = findBestPasswordField();
  
  if (passwordField) {
    passwordField.value = password;
    passwordField.dispatchEvent(new Event('input', { bubbles: true }));
    passwordField.dispatchEvent(new Event('change', { bubbles: true }));
    showNotification('Password filled successfully!');
  } else {
    showNotification('No password field found on this page.');
  }
}

function findBestUsernameField() {
  // Try to find the most likely username field
  const selectors = [
    'input[autocomplete="username"]:not([type="hidden"])',
    'input[autocomplete="email"]:not([type="hidden"])',
    'input[type="email"]:not([type="hidden"])',
    'input[name*="user"]:not([type="password"]):not([type="hidden"])',
    'input[name*="login"]:not([type="password"]):not([type="hidden"])',
    'input[name*="email"]:not([type="password"]):not([type="hidden"])',
    'input[id*="user"]:not([type="password"]):not([type="hidden"])',
    'input[id*="login"]:not([type="password"]):not([type="hidden"])',
    'input[id*="email"]:not([type="password"]):not([type="hidden"])',
    'input[placeholder*="username" i]:not([type="password"]):not([type="hidden"])',
    'input[placeholder*="email" i]:not([type="password"]):not([type="hidden"])'
  ];
  
  for (const selector of selectors) {
    const field = document.querySelector(selector);
    if (field && field.offsetParent !== null) { // Check if visible
      return field;
    }
  }
  
  // Fallback: find first text input near a password field
  const passwordFields = document.querySelectorAll('input[type="password"]:not([type="hidden"])');
  for (const passwordField of passwordFields) {
    const form = passwordField.closest('form');
    if (form) {
      const textField = form.querySelector('input[type="text"]:not([type="hidden"]), input[type="email"]:not([type="hidden"])');
      if (textField && textField.offsetParent !== null) {
        return textField;
      }
    }
  }
  
  return null;
}

function findBestPasswordField() {
  // Find the most visible password field
  const passwordFields = document.querySelectorAll('input[type="password"]:not([type="hidden"])');
  
  for (const field of passwordFields) {
    if (field.offsetParent !== null) { // Check if visible
      return field;
    }
  }
  
  return null;
}

function showNotification(message) {
  // Create a simple notification
  const notification = document.createElement('div');
  notification.className = 'pm-notification';
  notification.textContent = message;
  
  Object.assign(notification.style, {
    position: 'fixed',
    top: '20px',
    right: '20px',
    backgroundColor: '#007acc',
    color: 'white',
    padding: '12px 20px',
    borderRadius: '4px',
    zIndex: '10002',
    boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
    fontSize: '14px',
    maxWidth: '300px',
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
  });
  
  document.body.appendChild(notification);
  
  setTimeout(() => {
    notification.remove();
  }, 3000);
}

// Initialize the content script
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    new PasswordManagerContentScript();
  });
} else {
  new PasswordManagerContentScript();
}