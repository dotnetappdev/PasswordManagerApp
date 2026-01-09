/**
 * Storage Service
 * Handles secure storage of sensitive data using react-native-keychain
 * and AsyncStorage for non-sensitive data
 */

import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Keychain from 'react-native-keychain';
import { AppSettings } from '../models';
import { AppConfig } from '../config/app.config';

class StorageService {
  /**
   * Store sensitive data in Keychain (encrypted)
   */
  async setSecureItem(key: string, value: string): Promise<void> {
    try {
      await Keychain.setGenericPassword(key, value, {
        service: key,
      });
    } catch (error) {
      console.error('Failed to store secure item:', error);
      throw error;
    }
  }

  /**
   * Retrieve sensitive data from Keychain
   */
  async getSecureItem(key: string): Promise<string | null> {
    try {
      const credentials = await Keychain.getGenericPassword({
        service: key,
      });
      if (credentials) {
        return credentials.password;
      }
      return null;
    } catch (error) {
      console.error('Failed to retrieve secure item:', error);
      return null;
    }
  }

  /**
   * Remove sensitive data from Keychain
   */
  async removeSecureItem(key: string): Promise<void> {
    try {
      await Keychain.resetGenericPassword({
        service: key,
      });
    } catch (error) {
      console.error('Failed to remove secure item:', error);
      throw error;
    }
  }

  /**
   * Store non-sensitive data in AsyncStorage
   */
  async setItem(key: string, value: string): Promise<void> {
    try {
      await AsyncStorage.setItem(key, value);
    } catch (error) {
      console.error('Failed to store item:', error);
      throw error;
    }
  }

  /**
   * Retrieve non-sensitive data from AsyncStorage
   */
  async getItem(key: string): Promise<string | null> {
    try {
      return await AsyncStorage.getItem(key);
    } catch (error) {
      console.error('Failed to retrieve item:', error);
      return null;
    }
  }

  /**
   * Remove non-sensitive data from AsyncStorage
   */
  async removeItem(key: string): Promise<void> {
    try {
      await AsyncStorage.removeItem(key);
    } catch (error) {
      console.error('Failed to remove item:', error);
      throw error;
    }
  }

  /**
   * Store object data
   */
  async setObject(key: string, value: any): Promise<void> {
    try {
      const jsonValue = JSON.stringify(value);
      await this.setItem(key, jsonValue);
    } catch (error) {
      console.error('Failed to store object:', error);
      throw error;
    }
  }

  /**
   * Retrieve object data
   */
  async getObject<T>(key: string): Promise<T | null> {
    try {
      const jsonValue = await this.getItem(key);
      return jsonValue != null ? JSON.parse(jsonValue) : null;
    } catch (error) {
      console.error('Failed to retrieve object:', error);
      return null;
    }
  }

  /**
   * Get app settings
   */
  async getSettings(): Promise<AppSettings> {
    const mode = await this.getItem(AppConfig.storageKeys.settingsMode);
    const apiUrl = await this.getItem(AppConfig.storageKeys.apiUrl);
    const apiKey = await this.getSecureItem(AppConfig.storageKeys.apiKey);
    const theme = await this.getItem(AppConfig.storageKeys.theme);
    const biometricEnabled = await this.getItem(
      AppConfig.storageKeys.biometricEnabled
    );

    return {
      mode: (mode as 'local' | 'api') || 'local',
      apiUrl: apiUrl || undefined,
      apiKey: apiKey || undefined,
      theme: (theme as 'light' | 'dark' | 'system') || 'system',
      biometricEnabled: biometricEnabled === 'true',
      autoSync: true,
      syncInterval: 30,
    };
  }

  /**
   * Save app settings
   */
  async saveSettings(settings: AppSettings): Promise<void> {
    await this.setItem(AppConfig.storageKeys.settingsMode, settings.mode);
    
    if (settings.apiUrl) {
      await this.setItem(AppConfig.storageKeys.apiUrl, settings.apiUrl);
    }
    
    if (settings.apiKey) {
      await this.setSecureItem(AppConfig.storageKeys.apiKey, settings.apiKey);
    }
    
    await this.setItem(AppConfig.storageKeys.theme, settings.theme);
    await this.setItem(
      AppConfig.storageKeys.biometricEnabled,
      settings.biometricEnabled.toString()
    );
  }

  /**
   * Clear all storage (for logout)
   */
  async clearAll(): Promise<void> {
    try {
      // Clear AsyncStorage
      await AsyncStorage.clear();
      
      // Clear Keychain items
      const keychainKeys = [
        AppConfig.storageKeys.apiKey,
        AppConfig.storageKeys.sessionToken,
      ];
      
      for (const key of keychainKeys) {
        try {
          await this.removeSecureItem(key);
        } catch (error) {
          // Ignore errors for non-existent keys
        }
      }
      
      console.log('All storage cleared');
    } catch (error) {
      console.error('Failed to clear storage:', error);
      throw error;
    }
  }
}

export default new StorageService();
