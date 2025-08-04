// API Service for Password Manager browser extension
// Handles HTTP requests to Password Manager API server

class ApiService {
  constructor() {
    this.baseUrl = '';
    this.apiKey = '';
    this.isConfigured = false;
  }

  configure(apiUrl, apiKey) {
    this.baseUrl = apiUrl.replace(/\/$/, ''); // Remove trailing slash
    this.apiKey = apiKey;
    this.isConfigured = true;
  }

  async makeRequest(endpoint, options = {}) {
    if (!this.isConfigured) {
      throw new Error('API service not configured. Please provide API URL and key.');
    }

    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.apiKey}`,
      ...options.headers
    };

    const requestOptions = {
      method: options.method || 'GET',
      headers,
      ...options
    };

    if (options.body && typeof options.body === 'object') {
      requestOptions.body = JSON.stringify(options.body);
    }

    try {
      const response = await fetch(url, requestOptions);
      
      if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        
        try {
          const errorJson = JSON.parse(errorText);
          errorMessage = errorJson.message || errorJson.error || errorMessage;
        } catch {
          // If not JSON, use the raw text if available
          if (errorText) {
            errorMessage = errorText;
          }
        }
        
        throw new Error(errorMessage);
      }

      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        return await response.json();
      } else {
        return await response.text();
      }
    } catch (error) {
      if (error.name === 'TypeError' && error.message.includes('fetch')) {
        throw new Error(`Cannot connect to API server at ${this.baseUrl}. Please check the URL and ensure the server is running.`);
      }
      throw error;
    }
  }

  // Authentication endpoints
  async authenticate(email, masterPassword) {
    const response = await this.makeRequest('/api/auth/login', {
      method: 'POST',
      body: {
        email,
        masterPassword
      }
    });

    return response;
  }

  async verifyToken() {
    const response = await this.makeRequest('/api/auth/verify', {
      method: 'GET'
    });

    return response;
  }

  // Credential endpoints
  async getCredentials(domain = null) {
    let endpoint = '/api/passwords';
    if (domain) {
      endpoint += `?domain=${encodeURIComponent(domain)}`;
    }

    const response = await this.makeRequest(endpoint, {
      method: 'GET'
    });

    return response;
  }

  async getCredentialById(id) {
    const response = await this.makeRequest(`/api/passwords/${id}`, {
      method: 'GET'
    });

    return response;
  }

  // User management endpoints
  async getUserProfile() {
    const response = await this.makeRequest('/api/user/profile', {
      method: 'GET'
    });

    return response;
  }

  // Health check
  async healthCheck() {
    const response = await this.makeRequest('/api/health', {
      method: 'GET'
    });

    return response;
  }

  // Utility methods
  isReady() {
    return this.isConfigured;
  }

  getBaseUrl() {
    return this.baseUrl;
  }

  clearConfiguration() {
    this.baseUrl = '';
    this.apiKey = '';
    this.isConfigured = false;
  }
}