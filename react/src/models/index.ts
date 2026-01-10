/**
 * Core Models
 * TypeScript interfaces matching the backend models
 */

// Base Entity
export interface BaseEntity {
  id: string;
  createdAt: string;
  updatedAt: string;
}

// User
export interface User extends BaseEntity {
  userName: string;
  email: string;
  phoneNumber?: string;
  twoFactorEnabled: boolean;
}

// Vault
export interface Vault extends BaseEntity {
  name: string;
  description?: string;
  userId: string;
  isDefault: boolean;
  color?: string;
}

// Category
export interface Category extends BaseEntity {
  name: string;
  description?: string;
  icon?: string;
  color?: string;
  userId?: string;
}

// Tag
export interface Tag extends BaseEntity {
  name: string;
  color?: string;
  userId: string;
}

// Collection
export interface Collection extends BaseEntity {
  name: string;
  description?: string;
  userId: string;
}

// Custom Field
export interface CustomField {
  id?: string;
  name: string;
  value: string;
  type: 'text' | 'password' | 'email' | 'url' | 'number' | 'date' | 'boolean';
  isEncrypted: boolean;
}

// Password Item Types
export enum PasswordItemType {
  Login = 0,
  SecureNote = 1,
  CreditCard = 2,
  WiFi = 3,
  Passkey = 4,
}

// Base Password Item
export interface PasswordItem extends BaseEntity {
  name: string;
  notes?: string;
  isFavorite: boolean;
  userId: string;
  vaultId?: string;
  categoryId?: string;
  tags?: Tag[];
  customFields?: CustomField[];
  type: PasswordItemType;
  encryptedData?: string;
  lastModifiedBy?: string;
}

// Login Item
export interface LoginItem extends PasswordItem {
  username?: string;
  password?: string;
  url?: string;
  totpSecret?: string;
}

// Secure Note Item
export interface SecureNoteItem extends PasswordItem {
  content?: string;
}

// Credit Card Item
export interface CreditCardItem extends PasswordItem {
  cardholderName?: string;
  cardNumber?: string;
  expirationMonth?: number;
  expirationYear?: number;
  cvv?: string;
  brand?: string;
  billingAddress?: string;
}

// WiFi Item
export interface WiFiItem extends PasswordItem {
  ssid?: string;
  password?: string;
  securityType?: string;
  isHidden?: boolean;
}

// Passkey Item
export interface PasskeyItem extends PasswordItem {
  rpId?: string;
  userHandle?: string;
  credentialId?: string;
}

// Device
export interface Device extends BaseEntity {
  name: string;
  deviceType: string;
  lastSeen: string;
  userId: string;
  isTrusted: boolean;
  deviceFingerprint?: string;
}

// API Key
export interface ApiKey extends BaseEntity {
  name: string;
  keyHash: string;
  userId: string;
  expiresAt?: string;
  isActive: boolean;
  lastUsed?: string;
}

// Audit Log
export interface AuditLog extends BaseEntity {
  userId: string;
  action: string;
  entityType?: string;
  entityId?: string;
  details?: string;
  ipAddress?: string;
  deviceId?: string;
}

// Authentication
export interface LoginRequest {
  email: string;
  password: string;
  masterPasswordHash?: string;
  deviceId?: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  masterPassword: string;
  phoneNumber?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  userId: string;
  userName: string;
  email: string;
  expiresAt: string;
}

// Settings
export interface AppSettings {
  mode: 'local' | 'api';
  apiUrl?: string;
  apiKey?: string;
  theme: 'light' | 'dark' | 'system';
  biometricEnabled: boolean;
  autoSync: boolean;
  syncInterval: number;
}

// API Response
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: Record<string, string[]>;
}

export default {
  PasswordItemType,
};
