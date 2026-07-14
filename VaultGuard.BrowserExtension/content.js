// Content script for detecting and enhancing login forms
class VaultGuardContentScript {
  constructor() {
    this.apiUrl = 'http://localhost:5000'; // Default API URL
    this.observer = null;
    this.icons = new Map();
    this.init();
  }

  init() {
    this.loadSettings();
    this.scanForForms();
    this.setupDOMObserver();
    this.setupKeyboardShortcuts();
    this.totpDetector = new TotpSetupDetector(this);
    this.totpDetector.scan();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get(['apiUrl', 'authToken']);
      if (result.apiUrl) {
        this.apiUrl = result.apiUrl;
      }
      this.authToken = result.authToken;
    } catch (error) {
      console.log('Vault Guard: Settings not found, using defaults');
    }
  }

  scanForForms() {
    // Find all forms on the page
    const forms = document.querySelectorAll('form');
    forms.forEach(form => this.processForm(form));

    // Also look for standalone password and username fields
    const usernameFields = this.findUsernameFields();
    const passwordFields = this.findPasswordFields();
    
    // Look for credit card fields
    const creditCardFields = this.findCreditCardFields();
    
    usernameFields.forEach(field => this.addFieldIcon(field, 'username'));
    passwordFields.forEach(field => this.addFieldIcon(field, 'password'));
    creditCardFields.forEach(field => this.addFieldIcon(field, 'creditcard'));
    
    // Add focus detection for real-time context switching
    this.setupFieldFocusDetection();
  }

  setupFieldFocusDetection() {
    // Add focus listeners to all relevant input fields
    const relevantFields = document.querySelectorAll('input[type="text"], input[type="email"], input[type="password"]');
    
    relevantFields.forEach(field => {
      field.addEventListener('focus', () => {
        this.handleFieldFocus(field);
      });
      
      field.addEventListener('click', () => {
        this.handleFieldFocus(field);
      });
    });
  }

  async handleFieldFocus(field) {
    // Determine field type and context for 1Password-like behavior
    const form = field.closest('form');
    const formContext = form ? form.dataset.pmFormContext || 'unknown' : 'unknown';
    
    // Store the focused field context for quick access
    this.lastFocusedField = field;
    this.lastFocusedContext = formContext;
    
    // Get field type to determine what to show
    const fieldType = this.getFieldType(field);
    
    // Log for debugging
    console.log('Vault Guard: Field focused', {
      fieldType: fieldType,
      formContext: formContext,
      fieldName: field.name,
      fieldId: field.id
    });
    
    // Show inline password list for password or username fields (1Password-like behavior)
    if ((fieldType === 'password' || fieldType === 'username') && this.authToken) {
      await this.showInlinePasswordList(field, formContext);
    }
  }

  getFieldType(field) {
    const type = field.type;
    const name = (field.name || '').toLowerCase();
    const id = (field.id || '').toLowerCase();
    const autocomplete = (field.autocomplete || '').toLowerCase();
    
    if (type === 'password') return 'password';
    if (type === 'email' || autocomplete === 'email') return 'username';
    if (this.determineCreditCardFieldType(field) !== 'unknown') return 'creditcard';
    if (name.includes('user') || name.includes('login') || id.includes('user') || id.includes('login')) return 'username';
    
    return 'text';
  }

  addFocusIndicator(field) {
    // Remove existing indicators
    const existingIndicators = document.querySelectorAll('.pm-focus-indicator');
    existingIndicators.forEach(indicator => indicator.remove());
    
    // Add subtle focus indicator
    const indicator = document.createElement('div');
    indicator.className = 'pm-focus-indicator';
    indicator.innerHTML = '🔑';
    
    Object.assign(indicator.style, {
      position: 'absolute',
      right: '30px',
      top: '50%',
      transform: 'translateY(-50%)',
      fontSize: '12px',
      opacity: '0.6',
      pointerEvents: 'none',
      zIndex: '9999'
    });
    
    // Position relative to the field
    const wrapper = field.parentElement;
    if (wrapper.style.position !== 'relative') {
      wrapper.style.position = 'relative';
    }
    
    wrapper.appendChild(indicator);
    
    // Remove after a few seconds
    setTimeout(() => {
      indicator.remove();
    }, 3000);
  }

  setupKeyboardShortcuts() {
    // Add 1Password-like keyboard shortcuts (Cmd+\ or Ctrl+\)
    document.addEventListener('keydown', (e) => {
      // Check for Cmd+\ (Mac) or Ctrl+\ (PC/Linux)
      if ((e.metaKey || e.ctrlKey) && e.key === '\\') {
        e.preventDefault();
        this.handleQuickAccess();
      }
    });
  }

  async handleQuickAccess() {
    // Get the currently focused field or the last focused field
    const activeField = document.activeElement;
    const targetField = this.isRelevantField(activeField) ? activeField : this.lastFocusedField;
    
    if (!targetField || !this.isRelevantField(targetField)) {
      this.showNotification('Please focus on a login or payment field first.');
      return;
    }
    
    // Determine the field type and show appropriate options
    const fieldType = this.getFieldType(targetField);
    const form = targetField.closest('form');
    const formContext = form ? form.dataset.pmFormContext || 'unknown' : 'unknown';
    
    try {
      if (fieldType === 'password') {
        await this.showPasswordOptions(targetField, formContext);
      } else if (fieldType === 'creditcard') {
        await this.showCreditCardSelector(targetField, formContext);
      } else {
        await this.showCredentialSelector(targetField, formContext);
      }
    } catch (error) {
      console.error('Vault Guard: Quick access error:', error);
      this.showNotification('Error accessing Vault Guard.');
    }
  }

  isRelevantField(field) {
    if (!field || field.tagName !== 'INPUT') return false;
    
    const type = field.type;
    const fieldType = this.getFieldType(field);
    
    return type === 'text' || type === 'email' || type === 'password' || 
           fieldType === 'creditcard' || fieldType === 'username';
  }

  processForm(form) {
    // Analyze form context to determine if it's payment, login, or mixed
    const formContext = this.analyzeFormContext(form);
    form.dataset.pmFormContext = formContext;
    
    const usernameField = this.findUsernameFieldInForm(form);
    const passwordField = this.findPasswordFieldInForm(form);
    const creditCardFields = this.findCreditCardFieldsInForm(form);
    
    if (usernameField) {
      this.addFieldIcon(usernameField, 'username');
    }
    
    if (passwordField) {
      this.addFieldIcon(passwordField, 'password');
    }

    creditCardFields.forEach(field => this.addFieldIcon(field, 'creditcard'));
  }

  analyzeFormContext(form) {
    const creditCardFields = this.findCreditCardFieldsInForm(form);
    const loginFields = form.querySelectorAll('input[type="password"], input[type="email"], input[name*="user" i], input[name*="login" i]');
    
    // Check for payment-related indicators
    const paymentIndicators = [
      'payment', 'checkout', 'billing', 'order', 'purchase', 'buy', 'cart',
      'price', 'total', 'amount', 'pay', 'credit', 'card', 'cvv', 'expiry'
    ];
    
    const formText = (form.textContent || '').toLowerCase();
    const formClasses = (form.className || '').toLowerCase();
    const formId = (form.id || '').toLowerCase();
    const formAction = (form.action || '').toLowerCase();
    
    const hasPaymentIndicators = paymentIndicators.some(indicator => 
      formText.includes(indicator) || formClasses.includes(indicator) || 
      formId.includes(indicator) || formAction.includes(indicator)
    );
    
    // Determine context based on field types and indicators
    if (creditCardFields.length > 0 && hasPaymentIndicators) {
      return 'payment';
    } else if (creditCardFields.length > 0 && loginFields.length > 0) {
      return 'mixed';
    } else if (loginFields.length > 0) {
      return 'login';
    } else if (creditCardFields.length > 0) {
      return 'payment';
    }
    
    return 'unknown';
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

  findCreditCardFields() {
    const selectors = [
      // Card number fields
      'input[name*="card"][name*="number" i]',
      'input[name*="cardnumber" i]',
      'input[name*="cc-number" i]',
      'input[id*="card"][id*="number" i]',
      'input[id*="cardnumber" i]',
      'input[id*="cc-number" i]',
      'input[autocomplete="cc-number"]',
      'input[placeholder*="card number" i]',
      'input[placeholder*="card-number" i]',
      // CVV fields
      'input[name*="cvv" i]',
      'input[name*="cvc" i]',
      'input[name*="security" i]',
      'input[id*="cvv" i]',
      'input[id*="cvc" i]',
      'input[id*="security" i]',
      'input[autocomplete="cc-csc"]',
      'input[placeholder*="cvv" i]',
      'input[placeholder*="cvc" i]',
      // Expiry fields
      'input[name*="exp" i]',
      'input[name*="expiry" i]',
      'input[id*="exp" i]',
      'input[id*="expiry" i]',
      'input[autocomplete="cc-exp"]',
      'input[autocomplete="cc-exp-month"]',
      'input[autocomplete="cc-exp-year"]',
      'input[placeholder*="expiry" i]',
      'input[placeholder*="exp" i]',
      // Cardholder name
      'input[name*="cardholder" i]',
      'input[name*="card-holder" i]',
      'input[id*="cardholder" i]',
      'input[id*="card-holder" i]',
      'input[autocomplete="cc-name"]',
      'input[placeholder*="cardholder" i]',
      'input[placeholder*="name on card" i]'
    ];
    
    return document.querySelectorAll(selectors.join(','));
  }

  findCreditCardFieldsInForm(form) {
    const selectors = [
      // Card number fields
      'input[name*="card"][name*="number" i]',
      'input[name*="cardnumber" i]',
      'input[name*="cc-number" i]',
      'input[id*="card"][id*="number" i]',
      'input[id*="cardnumber" i]',
      'input[id*="cc-number" i]',
      'input[autocomplete="cc-number"]',
      'input[placeholder*="card number" i]',
      'input[placeholder*="card-number" i]',
      // CVV fields
      'input[name*="cvv" i]',
      'input[name*="cvc" i]',
      'input[name*="security" i]',
      'input[id*="cvv" i]',
      'input[id*="cvc" i]',
      'input[id*="security" i]',
      'input[autocomplete="cc-csc"]',
      'input[placeholder*="cvv" i]',
      'input[placeholder*="cvc" i]',
      // Expiry fields
      'input[name*="exp" i]',
      'input[name*="expiry" i]',
      'input[id*="exp" i]',
      'input[id*="expiry" i]',
      'input[autocomplete="cc-exp"]',
      'input[autocomplete="cc-exp-month"]',
      'input[autocomplete="cc-exp-year"]',
      'input[placeholder*="expiry" i]',
      'input[placeholder*="exp" i]',
      // Cardholder name
      'input[name*="cardholder" i]',
      'input[name*="card-holder" i]',
      'input[id*="cardholder" i]',
      'input[id*="card-holder" i]',
      'input[autocomplete="cc-name"]',
      'input[placeholder*="cardholder" i]',
      'input[placeholder*="name on card" i]'
    ];
    
    return form.querySelectorAll(selectors.join(','));
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
    
    // Set icon and title based on type
    if (type === 'username') {
      icon.innerHTML = '👤';
      icon.title = 'Fill username with Vault Guard';
    } else if (type === 'password') {
      icon.innerHTML = '🔑';
      icon.title = 'Fill password with Vault Guard';
    } else if (type === 'creditcard') {
      icon.innerHTML = '💳';
      icon.title = 'Fill credit card with Vault Guard';
    }
    
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
      if (!this.authToken) {
        this.showNotification('Please log in to the Vault Guard extension first.');
        return;
      }

      // Get form context to determine what items to prioritize
      const form = field.closest('form');
      const formContext = form ? form.dataset.pmFormContext || 'unknown' : 'unknown';
      
      if (type === 'username') {
        await this.showCredentialSelector(field, formContext);
      } else if (type === 'password') {
        await this.showPasswordOptions(field, formContext);
      } else if (type === 'creditcard') {
        await this.showCreditCardSelector(field, formContext);
      }
    } catch (error) {
      console.error('Vault Guard: Error handling icon click:', error);
      this.showNotification('Error accessing Vault Guard. Please check your connection.');
    }
  }

  async showCredentialSelector(field, formContext = 'unknown') {
    // Get current domain for filtering
    const domain = window.location.hostname;
    
    // For payment forms, also try to get credit cards and show them first
    const promises = [
      chrome.runtime.sendMessage({
        action: 'getCredentials',
        domain: domain
      })
    ];
    
    // If it's a payment or mixed form, also get credit cards
    if (formContext === 'payment' || formContext === 'mixed') {
      promises.push(
        chrome.runtime.sendMessage({
          action: 'getCreditCards',
          domain: domain
        })
      );
    }
    
    const responses = await Promise.all(promises);
    const credentialResponse = responses[0];
    const creditCardResponse = responses[1];
    
    const items = [];
    
    // For payment forms, show credit cards first
    if (formContext === 'payment' && creditCardResponse?.success && creditCardResponse.creditCards.length > 0) {
      items.push({
        type: 'creditCards',
        data: creditCardResponse.creditCards,
        label: 'Credit Cards'
      });
    }
    
    // Add credentials
    if (credentialResponse.success && credentialResponse.credentials.length > 0) {
      items.push({
        type: 'credentials',
        data: credentialResponse.credentials,
        label: 'Login Credentials'
      });
    }
    
    // For mixed forms, show credit cards after credentials
    if (formContext === 'mixed' && creditCardResponse?.success && creditCardResponse.creditCards.length > 0) {
      items.push({
        type: 'creditCards',
        data: creditCardResponse.creditCards,
        label: 'Credit Cards'
      });
    }
    
    if (items.length > 0) {
      this.showContextualPopup(field, items, formContext);
    } else {
      this.showNotification('No items found for this website.');
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

  async showPasswordOptions(field, formContext = 'unknown') {
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
          await this.showCredentialSelector(field, formContext);
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

  async showCreditCardSelector(field, formContext = 'unknown') {
    // Get current domain for filtering
    const domain = window.location.hostname;
    
    // Send message to background script to get credit cards and possibly credentials
    const promises = [
      chrome.runtime.sendMessage({
        action: 'getCreditCards',
        domain: domain
      })
    ];
    
    // For mixed forms, also get login credentials
    if (formContext === 'mixed') {
      promises.push(
        chrome.runtime.sendMessage({
          action: 'getCredentials',
          domain: domain
        })
      );
    }
    
    const responses = await Promise.all(promises);
    const creditCardResponse = responses[0];
    const credentialResponse = responses[1];
    
    const items = [];
    
    // Always show credit cards first when user clicked on a credit card field
    if (creditCardResponse.success && creditCardResponse.creditCards.length > 0) {
      items.push({
        type: 'creditCards',
        data: creditCardResponse.creditCards,
        label: 'Credit Cards'
      });
    }
    
    // Add credentials for mixed forms
    if (formContext === 'mixed' && credentialResponse?.success && credentialResponse.credentials.length > 0) {
      items.push({
        type: 'credentials',
        data: credentialResponse.credentials,
        label: 'Login Credentials'
      });
    }
    
    if (items.length > 0) {
      this.showContextualPopup(field, items, formContext);
    } else {
      this.showNotification('No items found.');
    }
  }

  showContextualPopup(field, items, formContext) {
    // Remove existing popup
    const existingPopup = document.querySelector('.pm-contextual-popup');
    if (existingPopup) {
      existingPopup.remove();
    }
    
    const popup = document.createElement('div');
    popup.className = 'pm-contextual-popup';
    
    Object.assign(popup.style, {
      position: 'absolute',
      backgroundColor: 'white',
      border: '1px solid #ccc',
      borderRadius: '4px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      zIndex: '10001',
      maxWidth: '350px',
      maxHeight: '400px',
      overflowY: 'auto'
    });
    
    items.forEach((itemGroup, groupIndex) => {
      // Add section header
      if (items.length > 1) {
        const header = document.createElement('div');
        header.style.cssText = 'padding: 8px 12px; background: #f8f9fa; font-weight: bold; font-size: 12px; color: #666; border-bottom: 1px solid #eee;';
        header.textContent = itemGroup.label;
        popup.appendChild(header);
      }
      
      // Add items
      itemGroup.data.forEach(item => {
        const itemElement = document.createElement('div');
        itemElement.style.cssText = 'padding: 12px; cursor: pointer; border-bottom: 1px solid #f0f0f0; transition: background-color 0.2s;';
        
        if (itemGroup.type === 'creditCards') {
          // Credit card display
          const maskedNumber = item.cardNumber ? `****-****-****-${item.cardNumber.slice(-4)}` : '****-****-****-****';
          itemElement.innerHTML = `
            <div style="font-weight: 500; color: #333;">💳 ${this.escapeHtml(item.title)}</div>
            <div style="font-size: 12px; color: #666; margin-top: 2px;">${this.escapeHtml(item.cardholderName || 'No cardholder name')}</div>
            <div style="font-size: 12px; color: #666;">${maskedNumber}</div>
            <div style="font-size: 12px; color: #666;">Expires: ${this.escapeHtml(item.expiryDate || 'N/A')}</div>
          `;
          
          itemElement.addEventListener('click', () => {
            this.fillCreditCard(field, item);
            popup.remove();
          });
        } else if (itemGroup.type === 'credentials') {
          // Credential display
          itemElement.innerHTML = `
            <div style="font-weight: 500; color: #333;">🔑 ${this.escapeHtml(item.title)}</div>
            <div style="font-size: 12px; color: #666; margin-top: 2px;">${this.escapeHtml(item.username)}</div>
            <div style="font-size: 12px; color: #666;">${this.escapeHtml(item.website || window.location.hostname)}</div>
          `;
          
          itemElement.addEventListener('click', () => {
            this.fillCredentials(field, item);
            popup.remove();
          });
        }
        
        itemElement.addEventListener('mouseenter', () => {
          itemElement.style.backgroundColor = '#f0f0f0';
        });
        
        itemElement.addEventListener('mouseleave', () => {
          itemElement.style.backgroundColor = 'white';
        });
        
        popup.appendChild(itemElement);
      });
    });
    
    this.positionPopup(popup, field);
    document.body.appendChild(popup);
    
    // Close on outside click
    setTimeout(() => {
      document.addEventListener('click', (e) => {
        if (!popup.contains(e.target)) {
          popup.remove();
        }
      }, { once: true });
    }, 100);
  }

  async showInlinePasswordList(field, formContext = 'unknown') {
    // Remove any existing inline dropdown
    const existingDropdown = document.querySelector('.pm-inline-dropdown');
    if (existingDropdown) {
      existingDropdown.remove();
    }
    
    try {
      // Get current domain for filtering
      const domain = window.location.hostname;
      
      // Request credentials from background script
      const response = await chrome.runtime.sendMessage({
        action: 'getCredentials',
        domain: domain
      });
      
      if (!response.success || !response.credentials || response.credentials.length === 0) {
        // No credentials found - don't show dropdown
        return;
      }
      
      // Create the inline dropdown
      const dropdown = document.createElement('div');
      dropdown.className = 'pm-inline-dropdown';
      
      // Style the dropdown with consistent design
      Object.assign(dropdown.style, {
        position: 'absolute',
        backgroundColor: '#ffffff',
        border: '1px solid #e0e6ed',
        borderRadius: '6px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
        zIndex: '10001',
        minWidth: '250px',
        maxWidth: '350px',
        maxHeight: '250px',
        overflowY: 'auto',
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
        fontSize: '14px'
      });
      
      // Add header
      const header = document.createElement('div');
      header.style.cssText = 'padding: 12px; background: #f8f9fa; border-bottom: 1px solid #e0e6ed; font-weight: 600; font-size: 13px; color: #333;';
      header.innerHTML = `
        <div style="display: flex; align-items: center; justify-content: space-between;">
          <span>🔑 ${response.credentials.length} saved for ${domain}</span>
        </div>
      `;
      dropdown.appendChild(header);
      
      // Add credentials list
      response.credentials.forEach((cred, index) => {
        const item = document.createElement('div');
        item.className = 'pm-dropdown-item';
        item.style.cssText = 'padding: 12px; cursor: pointer; border-bottom: 1px solid #f0f0f0; transition: background-color 0.2s;';
        
        item.innerHTML = `
          <div style="font-weight: 500; color: #1a1a1a; margin-bottom: 4px;">${this.escapeHtml(cred.title)}</div>
          <div style="font-size: 12px; color: #6c757d;">${this.escapeHtml(cred.username)}</div>
        `;
        
        // Add click handler to fill credentials
        item.addEventListener('click', () => {
          this.fillCredentials(field, cred);
          dropdown.remove();
        });
        
        // Add hover effect
        item.addEventListener('mouseenter', () => {
          item.style.backgroundColor = '#f8f9ff';
          item.style.borderLeftColor = '#007acc';
          item.style.borderLeftWidth = '3px';
          item.style.borderLeftStyle = 'solid';
          item.style.paddingLeft = '9px';
        });
        
        item.addEventListener('mouseleave', () => {
          item.style.backgroundColor = 'white';
          item.style.borderLeft = 'none';
          item.style.paddingLeft = '12px';
        });
        
        dropdown.appendChild(item);
      });
      
      // Position the dropdown below the field
      this.positionPopup(dropdown, field);
      document.body.appendChild(dropdown);
      
      // Close dropdown when clicking outside or when field loses focus
      const closeDropdown = (e) => {
        if (!dropdown.contains(e.target) && e.target !== field) {
          dropdown.remove();
          document.removeEventListener('click', closeDropdown);
          field.removeEventListener('blur', closeDropdown);
        }
      };
      
      setTimeout(() => {
        document.addEventListener('click', closeDropdown);
        field.addEventListener('blur', closeDropdown);
      }, 100);
      
    } catch (error) {
      console.error('Vault Guard: Error showing inline password list:', error);
    }
  }

  showCreditCardPopup(field, creditCards) {
    // Remove existing popup
    const existingPopup = document.querySelector('.pm-creditcard-popup');
    if (existingPopup) {
      existingPopup.remove();
    }
    
    const popup = document.createElement('div');
    popup.className = 'pm-creditcard-popup';
    
    Object.assign(popup.style, {
      position: 'absolute',
      backgroundColor: 'white',
      border: '1px solid #ccc',
      borderRadius: '4px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      zIndex: '10001',
      maxWidth: '300px',
      maxHeight: '300px',
      overflowY: 'auto'
    });
    
    const header = document.createElement('div');
    header.style.cssText = 'padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; background: #f8f9fa;';
    header.textContent = 'Select Credit Card';
    popup.appendChild(header);
    
    creditCards.forEach(card => {
      const item = document.createElement('div');
      item.style.cssText = 'padding: 12px; cursor: pointer; border-bottom: 1px solid #f0f0f0;';
      
      // Mask card number for display (show only last 4 digits)
      const maskedNumber = card.cardNumber ? `****-****-****-${card.cardNumber.slice(-4)}` : '****-****-****-****';
      
      item.innerHTML = `
        <div style="font-weight: 500; color: #333;">${this.escapeHtml(card.title)}</div>
        <div style="font-size: 12px; color: #666; margin-top: 2px;">${this.escapeHtml(card.cardholderName || 'No cardholder name')}</div>
        <div style="font-size: 12px; color: #666;">${maskedNumber}</div>
        <div style="font-size: 12px; color: #666;">Expires: ${this.escapeHtml(card.expiryDate || 'N/A')}</div>
      `;
      
      item.addEventListener('click', () => {
        this.fillCreditCard(field, card);
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
    
    document.body.appendChild(popup);
    this.positionPopup(popup, field);
    
    // Close on outside click
    setTimeout(() => {
      document.addEventListener('click', (e) => {
        if (!popup.contains(e.target)) {
          popup.remove();
        }
      }, { once: true });
    }, 100);
  }

  fillCreditCard(clickedField, card) {
    try {
      // Determine what type of field was clicked and fill accordingly
      const fieldType = this.determineCreditCardFieldType(clickedField);
      
      if (fieldType === 'cardNumber' && card.cardNumber) {
        clickedField.value = card.cardNumber;
        clickedField.dispatchEvent(new Event('input', { bubbles: true }));
        clickedField.dispatchEvent(new Event('change', { bubbles: true }));
      } else if (fieldType === 'cvv' && card.cvv) {
        clickedField.value = card.cvv;
        clickedField.dispatchEvent(new Event('input', { bubbles: true }));
        clickedField.dispatchEvent(new Event('change', { bubbles: true }));
      } else if (fieldType === 'expiry' && card.expiryDate) {
        clickedField.value = card.expiryDate;
        clickedField.dispatchEvent(new Event('input', { bubbles: true }));
        clickedField.dispatchEvent(new Event('change', { bubbles: true }));
      } else if (fieldType === 'cardholderName' && card.cardholderName) {
        clickedField.value = card.cardholderName;
        clickedField.dispatchEvent(new Event('input', { bubbles: true }));
        clickedField.dispatchEvent(new Event('change', { bubbles: true }));
      } else {
        // Try to auto-fill entire form if it's a general credit card field
        this.autoFillCreditCardForm(clickedField, card);
      }
      
      this.showNotification('Credit card information filled successfully.');
    } catch (error) {
      console.error('Vault Guard: Error filling credit card:', error);
      this.showNotification('Error filling credit card information.');
    }
  }

  determineCreditCardFieldType(field) {
    const name = (field.name || '').toLowerCase();
    const id = (field.id || '').toLowerCase();
    const placeholder = (field.placeholder || '').toLowerCase();
    const autocomplete = (field.autocomplete || '').toLowerCase();
    
    // Card number detection
    if (name.includes('number') || id.includes('number') || 
        placeholder.includes('number') || autocomplete === 'cc-number') {
      return 'cardNumber';
    }
    
    // CVV detection
    if (name.includes('cvv') || name.includes('cvc') || name.includes('security') ||
        id.includes('cvv') || id.includes('cvc') || id.includes('security') ||
        placeholder.includes('cvv') || placeholder.includes('cvc') || autocomplete === 'cc-csc') {
      return 'cvv';
    }
    
    // Expiry detection
    if (name.includes('exp') || id.includes('exp') || placeholder.includes('exp') ||
        autocomplete.includes('cc-exp')) {
      return 'expiry';
    }
    
    // Cardholder name detection
    if (name.includes('cardholder') || name.includes('name') ||
        id.includes('cardholder') || id.includes('name') ||
        placeholder.includes('cardholder') || placeholder.includes('name') ||
        autocomplete === 'cc-name') {
      return 'cardholderName';
    }
    
    return 'unknown';
  }

  autoFillCreditCardForm(startField, card) {
    // Find the form containing this field
    const form = startField.closest('form') || document;
    
    // Fill card number
    if (card.cardNumber) {
      const cardNumberField = form.querySelector('input[autocomplete="cc-number"], input[name*="number" i], input[id*="number" i]');
      if (cardNumberField) {
        cardNumberField.value = card.cardNumber;
        cardNumberField.dispatchEvent(new Event('input', { bubbles: true }));
        cardNumberField.dispatchEvent(new Event('change', { bubbles: true }));
      }
    }
    
    // Fill cardholder name
    if (card.cardholderName) {
      const nameField = form.querySelector('input[autocomplete="cc-name"], input[name*="cardholder" i], input[id*="cardholder" i]');
      if (nameField) {
        nameField.value = card.cardholderName;
        nameField.dispatchEvent(new Event('input', { bubbles: true }));
        nameField.dispatchEvent(new Event('change', { bubbles: true }));
      }
    }
    
    // Fill expiry date
    if (card.expiryDate) {
      const expiryField = form.querySelector('input[autocomplete="cc-exp"], input[name*="exp" i], input[id*="exp" i]');
      if (expiryField) {
        expiryField.value = card.expiryDate;
        expiryField.dispatchEvent(new Event('input', { bubbles: true }));
        expiryField.dispatchEvent(new Event('change', { bubbles: true }));
      }
      
      // Handle separate month/year fields
      const [month, year] = (card.expiryDate || '').split('/');
      if (month && year) {
        const monthField = form.querySelector('input[autocomplete="cc-exp-month"], select[autocomplete="cc-exp-month"]');
        const yearField = form.querySelector('input[autocomplete="cc-exp-year"], select[autocomplete="cc-exp-year"]');
        
        if (monthField) {
          monthField.value = month.trim();
          monthField.dispatchEvent(new Event('input', { bubbles: true }));
          monthField.dispatchEvent(new Event('change', { bubbles: true }));
        }
        
        if (yearField) {
          yearField.value = year.trim();
          yearField.dispatchEvent(new Event('input', { bubbles: true }));
          yearField.dispatchEvent(new Event('change', { bubbles: true }));
        }
      }
    }
    
    // Fill CVV
    if (card.cvv) {
      const cvvField = form.querySelector('input[autocomplete="cc-csc"], input[name*="cvv" i], input[id*="cvv" i], input[name*="cvc" i], input[id*="cvc" i]');
      if (cvvField) {
        cvvField.value = card.cvv;
        cvvField.dispatchEvent(new Event('input', { bubbles: true }));
        cvvField.dispatchEvent(new Event('change', { bubbles: true }));
      }
    }
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
                  node.querySelector('input[type="text"]') ||
                  node.querySelector('input[autocomplete*="cc-"]') ||
                  node.querySelector('input[name*="card" i]') ||
                  node.querySelector('input[name*="cvv" i]')) {
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
    new VaultGuardContentScript();
  });
} else {
  new VaultGuardContentScript();
}

// ─────────────────────────────────────────────────────────────────────────────
// Passkey (WebAuthn) interception bridge.
//
// The content script runs in an isolated world and cannot override the page's
// navigator.credentials directly, so it injects inpage.js into the page world and
// relays its requests to the background service worker (which talks to the native host).
(function installPasskeyBridge() {
  const REQUEST = 'PM_PASSKEY_REQUEST';
  const RESPONSE = 'PM_PASSKEY_RESPONSE';
  const CONFIG = 'PM_PASSKEY_CONFIG';

  // 1. Inject the page-world hook. Whether it actually intercepts passkeys is the user's choice
  //    (Settings → "Intercept website passkeys"), stored as `interceptPasskeys` (default ON). We read
  //    it first and pass the initial value to inpage.js via a data-attribute so there's no window
  //    where it intercepts against the user's wish. Passkey ceremonies are user-initiated (a click),
  //    so the async storage read always resolves well before one runs.
  function injectHook(intercept) {
    try {
      const script = document.createElement('script');
      script.src = chrome.runtime.getURL('inpage.js');
      script.dataset.pmIntercept = intercept ? '1' : '0';
      script.async = false;
      (document.head || document.documentElement).appendChild(script);
      script.onload = () => script.remove();
    } catch (e) {
      console.warn('Vault Guard: failed to inject passkey hook', e);
    }
  }

  try {
    chrome.storage.sync.get({ interceptPasskeys: true }, (res) => {
      injectHook(res && res.interceptPasskeys !== false);
    });
  } catch (e) {
    // storage unavailable → default to intercepting (previous behaviour).
    injectHook(true);
  }

  // Live toggle: when the user flips the setting, tell the already-injected hook without a reload.
  // (Turning it back ON works instantly; the override was installed at load either way.)
  try {
    chrome.storage.onChanged.addListener((changes, area) => {
      if (area === 'sync' && changes.interceptPasskeys) {
        window.postMessage(
          { type: CONFIG, intercept: changes.interceptPasskeys.newValue !== false },
          window.location.origin);
      }
    });
  } catch (_) { /* no storage events available */ }

  // 2. Relay page → background → page.
  window.addEventListener('message', (event) => {
    if (event.source !== window || !event.data || event.data.type !== REQUEST) return;

    const { id, kind, request } = event.data;
    const action = kind === 'create' ? 'passkeyCreate' : 'passkeyGet';

    const reply = (payload) => window.postMessage({ type: RESPONSE, id, payload }, window.location.origin);

    try {
      chrome.runtime.sendMessage({ action, ...request }, (response) => {
        if (chrome.runtime.lastError) {
          // Background/native host unreachable → let the page fall back to the platform authenticator.
          reply({ success: false, error: chrome.runtime.lastError.message, fallback: true });
          return;
        }
        reply(response || { success: false, error: 'No response', fallback: true });
      });
    } catch (e) {
      reply({ success: false, error: e.message, fallback: true });
    }
  });
})();

// ─── TOTP Setup Detector ──────────────────────────────────────────────────────
// Watches for 2FA setup pages and extracts the otpauth:// URI automatically,
// the same way 1Password does — no camera, no manual copy-paste.
class TotpSetupDetector {
  constructor(contentScript) {
    this.cs = contentScript;
    this._shown = false;  // only prompt once per page load
    this._observer = null;
  }

  scan() {
    // 1. Immediate DOM scan
    this._tryScan();

    // 2. Watch for dynamic content (SPAs render the QR after a network call)
    this._observer = new MutationObserver(() => {
      if (!this._shown) this._tryScan();
    });
    this._observer.observe(document.body, { childList: true, subtree: true });
  }

  _tryScan() {
    const uri = this._findOtpauthUri();
    if (uri) {
      this._observer?.disconnect();
      this._shown = true;
      this._promptSave(uri);
    }
  }

  // ── Strategy 1: otpauth:// URI visible anywhere in the DOM ──────────────────
  // Covers: hidden inputs, data- attrs, anchor hrefs, raw text nodes, script vars
  _findOtpauthUri() {
    // Check full page HTML first — fastest
    const html = document.documentElement.innerHTML;
    const inlineMatch = html.match(/otpauth:\/\/totp\/[^\s"'<>]+/i);
    if (inlineMatch) return decodeURIComponent(inlineMatch[0]);

    // Check all img src attributes — some sites embed the uri as a QR src param
    for (const img of document.querySelectorAll('img')) {
      const src = img.src || img.getAttribute('src') || '';
      const m = src.match(/[?&](?:data|chl|cht)=([^&]+)/i);
      if (m) {
        const decoded = decodeURIComponent(m[1]);
        if (decoded.startsWith('otpauth://')) return decoded;
      }
      // Some sites use data-* on the img itself
      for (const attr of img.attributes) {
        if (attr.value.startsWith('otpauth://')) return attr.value;
      }
    }

    // Check canvas elements rendered by JS QR libraries (totp QRs often go to <canvas>)
    for (const canvas of document.querySelectorAll('canvas')) {
      const uri = this._decodeCanvasQr(canvas);
      if (uri) return uri;
    }

    return null;
  }

  // ── Strategy 2: decode a <canvas> QR code using jsQR (bundled) ──────────────
  _decodeCanvasQr(canvas) {
    try {
      const ctx = canvas.getContext('2d');
      if (!ctx) return null;
      const { width, height } = canvas;
      if (width < 50 || height < 50) return null; // too small to be a QR
      const imageData = ctx.getImageData(0, 0, width, height);
      if (typeof jsQR === 'undefined') return null;
      const result = jsQR(imageData.data, width, height);
      if (result?.data?.startsWith('otpauth://')) return result.data;
    } catch (_) {}
    return null;
  }

  // ── Prompt the user to save ──────────────────────────────────────────────────
  _promptSave(uri) {
    const parsed = this._parseOtpauth(uri);
    if (!parsed) return;

    // Build the save banner (matches our extension's dark theme)
    const banner = document.createElement('div');
    banner.id = 'pm-totp-banner';
    Object.assign(banner.style, {
      position: 'fixed', top: '16px', right: '16px', zIndex: '2147483647',
      background: '#1a1a2e', border: '1px solid #2563EB',
      borderRadius: '12px', padding: '14px 16px', width: '320px',
      boxShadow: '0 8px 32px rgba(0,0,0,0.6)',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
      color: '#e5e5e5', fontSize: '14px',
    });

    banner.innerHTML = `
      <div style="display:flex;align-items:center;gap:10px;margin-bottom:10px;">
        <div style="width:32px;height:32px;border-radius:8px;background:#2563EB;display:flex;align-items:center;justify-content:center;font-size:16px;">🔐</div>
        <div>
          <div style="font-weight:600;font-size:14px;">Save One-Time Password?</div>
          <div style="font-size:11px;color:#9d9d9d;">${parsed.issuer || location.hostname}</div>
        </div>
        <button id="pm-totp-close" style="margin-left:auto;background:none;border:none;color:#9d9d9d;cursor:pointer;font-size:18px;padding:0;">✕</button>
      </div>
      <div style="background:#0d1b2a;border-radius:8px;padding:10px;margin-bottom:12px;">
        <div style="font-size:11px;color:#60A5FA;margin-bottom:4px;">ACCOUNT</div>
        <div style="font-weight:500;">${parsed.account || 'Unknown account'}</div>
        <div style="font-size:11px;color:#9d9d9d;margin-top:4px;">Secret key detected · ${parsed.digits || 6} digits · ${parsed.period || 30}s</div>
      </div>
      <div style="display:flex;gap:8px;">
        <button id="pm-totp-save" style="flex:1;background:#2563EB;color:white;border:none;border-radius:8px;padding:9px;cursor:pointer;font-weight:600;font-size:13px;">
          Save to Vault
        </button>
        <button id="pm-totp-dismiss" style="flex:1;background:#2a2a3e;color:#9d9d9d;border:none;border-radius:8px;padding:9px;cursor:pointer;font-size:13px;">
          Ignore
        </button>
      </div>
    `;

    document.body.appendChild(banner);

    banner.querySelector('#pm-totp-close').onclick = () => banner.remove();
    banner.querySelector('#pm-totp-dismiss').onclick = () => banner.remove();
    banner.querySelector('#pm-totp-save').onclick = async () => {
      await this._saveTotp(parsed, uri);
      banner.remove();
    };
  }

  async _saveTotp(parsed, rawUri) {
    // Ask background to save — it will attach it to the matching vault item
    // (matched by hostname) or create a new entry if no match found
    chrome.runtime.sendMessage({
      action: 'saveTotpSecret',
      otpauthUri: rawUri,
      issuer: parsed.issuer || location.hostname,
      account: parsed.account,
      secret: parsed.secret,
      hostname: location.hostname,
    }, (response) => {
      const ok = response?.success;
      this.cs.showNotification(
        ok
          ? `✅ One-time password saved for ${parsed.issuer || location.hostname}`
          : `❌ Could not save OTP: ${response?.error || 'unknown error'}`
      );
    });
  }

  _parseOtpauth(uri) {
    try {
      // otpauth://totp/Issuer:account?secret=XXX&issuer=XXX&digits=6&period=30
      const url = new URL(uri);
      if (url.protocol !== 'otpauth:') return null;
      const label = decodeURIComponent(url.pathname.replace(/^\/\/totp\//, ''));
      const colonIdx = label.indexOf(':');
      const issuer = colonIdx >= 0 ? label.slice(0, colonIdx) : (url.searchParams.get('issuer') || '');
      const account = colonIdx >= 0 ? label.slice(colonIdx + 1) : label;
      return {
        issuer: url.searchParams.get('issuer') || issuer,
        account,
        secret: url.searchParams.get('secret'),
        digits: parseInt(url.searchParams.get('digits') || '6'),
        period: parseInt(url.searchParams.get('period') || '30'),
      };
    } catch (_) { return null; }
  }
}
