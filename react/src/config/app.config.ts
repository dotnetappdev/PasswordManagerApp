/**
 * Application Configuration
 * Central configuration for the Password Manager mobile app
 */

export const AppConfig = {
  // App Information
  appName: 'Password Manager',
  version: '1.0.0',
  
  // API Configuration
  api: {
    defaultBaseUrl: 'http://localhost:5000/api',
    timeout: 30000, // 30 seconds
    retryAttempts: 3,
    retryDelay: 1000, // 1 second
  },
  
  // Database Configuration
  database: {
    name: 'passwordmanager.db',
    version: 1,
    displayName: 'Password Manager Database',
    size: 10 * 1024 * 1024, // 10MB
  },
  
  // Encryption Configuration (matching backend settings)
  encryption: {
    keyDerivationIterations: 600000, // PBKDF2 iterations
    authHashIterations: 600000,
    algorithm: 'AES-256-GCM',
  },
  
  // Security Settings
  security: {
    sessionTimeout: 15 * 60 * 1000, // 15 minutes in milliseconds
    maxLoginAttempts: 5,
    lockoutDuration: 15 * 60 * 1000, // 15 minutes
    passwordMinLength: 8,
    requireBiometricAuth: false,
  },
  
  // Sync Settings
  sync: {
    enableAutoSync: true,
    syncIntervalMinutes: 30,
    conflictResolution: 'LastWriteWins',
  },
  
  // Storage Keys
  storageKeys: {
    settingsMode: '@settings:mode',
    apiUrl: '@settings:apiUrl',
    apiKey: '@settings:apiKey',
    userId: '@auth:userId',
    deviceId: '@device:id',
    theme: '@settings:theme',
    biometricEnabled: '@settings:biometricEnabled',
    sessionToken: '@auth:sessionToken',
  },
  
  // Database Modes
  databaseModes: {
    LOCAL: 'local',
    API: 'api',
  },
  
  // Theme
  themes: {
    LIGHT: 'light',
    DARK: 'dark',
    SYSTEM: 'system',
  },
};

export default AppConfig;
