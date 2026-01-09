/**
 * Authentication Service
 * Manages user authentication, registration, and session management
 */

import ReactNativeBiometrics from 'react-native-biometrics';
import DeviceInfo from 'react-native-device-info';
import { AppConfig } from '../config/app.config';
import { LoginRequest, RegisterRequest, AuthResponse, User } from '../models';
import apiService from './api.service';
import databaseService from './database.service';
import encryptionService from './encryption.service';
import storageService from './storage.service';

class AuthService {
  private currentUser: User | null = null;
  private masterKey: string | null = null;

  /**
   * Register a new user
   */
  async register(request: RegisterRequest): Promise<AuthResponse> {
    try {
      // Get settings to determine mode
      const settings = await storageService.getSettings();

      if (settings.mode === 'api') {
        // API mode - register via API
        return await this.registerViaApi(request);
      } else {
        // Local mode - register in local database
        return await this.registerLocally(request);
      }
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  }

  /**
   * Register via API
   */
  private async registerViaApi(request: RegisterRequest): Promise<AuthResponse> {
    const response = await apiService.post<AuthResponse>('/auth/register', request);
    
    if (response.success && response.data) {
      await this.handleAuthSuccess(response.data, request.masterPassword);
      return response.data;
    }
    
    throw new Error(response.message || 'Registration failed');
  }

  /**
   * Register locally
   */
  private async registerLocally(request: RegisterRequest): Promise<AuthResponse> {
    // Generate master key
    const { masterKey, salt } = encryptionService.generateMasterKey(
      request.masterPassword,
      request.email
    );

    // Hash password for authentication
    const passwordHash = encryptionService.hashPassword(
      request.password,
      salt,
      AppConfig.encryption.authHashIterations
    );

    // Generate user ID
    const userId = this.generateId();
    const now = new Date().toISOString();

    // Insert user into database
    await databaseService.executeSql(
      `INSERT INTO Users (id, userName, email, phoneNumber, masterPasswordHash, twoFactorEnabled, createdAt, updatedAt)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [userId, request.userName, request.email, request.phoneNumber || null, passwordHash, 0, now, now]
    );

    // Create default vault
    const vaultId = this.generateId();
    await databaseService.executeSql(
      `INSERT INTO Vaults (id, name, description, userId, isDefault, createdAt, updatedAt)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [vaultId, 'Personal', 'Default vault', userId, 1, now, now]
    );

    this.currentUser = {
      id: userId,
      userName: request.userName,
      email: request.email,
      phoneNumber: request.phoneNumber,
      twoFactorEnabled: false,
      createdAt: now,
      updatedAt: now,
    };

    this.masterKey = masterKey;

    // Store session
    await storageService.setItem(AppConfig.storageKeys.userId, userId);
    await storageService.setSecureItem('masterKey', masterKey);

    return {
      token: 'local',
      userId: userId,
      userName: request.userName,
      email: request.email,
      expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    };
  }

  /**
   * Login user
   */
  async login(request: LoginRequest): Promise<AuthResponse> {
    try {
      const settings = await storageService.getSettings();

      if (settings.mode === 'api') {
        return await this.loginViaApi(request);
      } else {
        return await this.loginLocally(request);
      }
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  }

  /**
   * Login via API
   */
  private async loginViaApi(request: LoginRequest): Promise<AuthResponse> {
    const deviceId = await this.getDeviceId();
    const response = await apiService.post<AuthResponse>('/auth/login', {
      ...request,
      deviceId,
    });
    
    if (response.success && response.data) {
      await this.handleAuthSuccess(response.data, request.password);
      return response.data;
    }
    
    throw new Error(response.message || 'Login failed');
  }

  /**
   * Login locally
   */
  private async loginLocally(request: LoginRequest): Promise<AuthResponse> {
    // Query user from database
    const result = await databaseService.executeSql(
      'SELECT * FROM Users WHERE email = ?',
      [request.email]
    );

    if (result.rows.length === 0) {
      throw new Error('Invalid email or password');
    }

    const user = result.rows.item(0);

    // Verify password
    // Note: In production, you'd need to store the salt with the user
    const salt = user.masterPasswordHash.substring(0, 64); // Example: extract salt
    const passwordHash = encryptionService.hashPassword(
      request.password,
      salt,
      AppConfig.encryption.authHashIterations
    );

    if (passwordHash !== user.masterPasswordHash) {
      throw new Error('Invalid email or password');
    }

    // Derive master key
    const masterKey = encryptionService.deriveKey(
      request.password,
      salt + user.email.toLowerCase(),
      AppConfig.encryption.keyDerivationIterations
    );

    this.currentUser = {
      id: user.id,
      userName: user.userName,
      email: user.email,
      phoneNumber: user.phoneNumber,
      twoFactorEnabled: user.twoFactorEnabled === 1,
      createdAt: user.createdAt,
      updatedAt: user.updatedAt,
    };

    this.masterKey = masterKey;

    // Store session
    await storageService.setItem(AppConfig.storageKeys.userId, user.id);
    await storageService.setSecureItem('masterKey', masterKey);

    return {
      token: 'local',
      userId: user.id,
      userName: user.userName,
      email: user.email,
      expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    };
  }

  /**
   * Handle successful authentication
   */
  private async handleAuthSuccess(authResponse: AuthResponse, masterPassword: string): Promise<void> {
    // Store auth token
    apiService.setAuthToken(authResponse.token);
    await storageService.setSecureItem(AppConfig.storageKeys.sessionToken, authResponse.token);
    await storageService.setItem(AppConfig.storageKeys.userId, authResponse.userId);

    // Derive master key from master password
    this.masterKey = encryptionService.deriveKey(
      masterPassword,
      authResponse.email.toLowerCase(),
      AppConfig.encryption.keyDerivationIterations
    );
    await storageService.setSecureItem('masterKey', this.masterKey);
  }

  /**
   * Logout user
   */
  async logout(): Promise<void> {
    try {
      this.currentUser = null;
      this.masterKey = null;

      // Clear storage
      await storageService.clearAll();

      // Clear database if in local mode
      const settings = await storageService.getSettings();
      if (settings.mode === 'local') {
        await databaseService.clearAllData();
      }

      console.log('Logout successful');
    } catch (error) {
      console.error('Logout failed:', error);
      throw error;
    }
  }

  /**
   * Check if user is authenticated
   */
  async isAuthenticated(): Promise<boolean> {
    const userId = await storageService.getItem(AppConfig.storageKeys.userId);
    const masterKey = await storageService.getSecureItem('masterKey');
    return userId !== null && masterKey !== null;
  }

  /**
   * Get current user
   */
  getCurrentUser(): User | null {
    return this.currentUser;
  }

  /**
   * Get master key for encryption/decryption
   */
  getMasterKey(): string | null {
    return this.masterKey;
  }

  /**
   * Setup biometric authentication
   */
  async setupBiometric(): Promise<boolean> {
    try {
      const rnBiometrics = new ReactNativeBiometrics();
      const { available } = await rnBiometrics.isSensorAvailable();
      
      if (available) {
        const { success } = await rnBiometrics.simplePrompt({
          promptMessage: 'Confirm fingerprint',
        });
        
        if (success) {
          await storageService.setItem(AppConfig.storageKeys.biometricEnabled, 'true');
          return true;
        }
      }
      
      return false;
    } catch (error) {
      console.error('Biometric setup failed:', error);
      return false;
    }
  }

  /**
   * Authenticate with biometrics
   */
  async authenticateWithBiometric(): Promise<boolean> {
    try {
      const rnBiometrics = new ReactNativeBiometrics();
      const { success } = await rnBiometrics.simplePrompt({
        promptMessage: 'Authenticate to access Password Manager',
      });
      
      return success;
    } catch (error) {
      console.error('Biometric authentication failed:', error);
      return false;
    }
  }

  /**
   * Get device ID
   */
  private async getDeviceId(): Promise<string> {
    let deviceId = await storageService.getItem(AppConfig.storageKeys.deviceId);
    
    if (!deviceId) {
      deviceId = await DeviceInfo.getUniqueId();
      await storageService.setItem(AppConfig.storageKeys.deviceId, deviceId);
    }
    
    return deviceId;
  }

  /**
   * Generate unique ID
   */
  private generateId(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}

export default new AuthService();
