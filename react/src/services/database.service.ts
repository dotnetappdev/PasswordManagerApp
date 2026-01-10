/**
 * Database Service
 * SQLite database management for local storage
 */

import SQLite, { SQLiteDatabase } from 'react-native-sqlite-storage';
import { AppConfig } from '../config/app.config';
import { PasswordItem, Category, Tag, Vault, User } from '../models';

// Enable debugging
SQLite.DEBUG(true);
SQLite.enablePromise(true);

class DatabaseService {
  private db: SQLiteDatabase | null = null;

  /**
   * Initialize and open the database
   */
  async initialize(): Promise<void> {
    try {
      this.db = await SQLite.openDatabase({
        name: AppConfig.database.name,
        location: 'default',
      });
      await this.createTables();
      console.log('Database initialized successfully');
    } catch (error) {
      console.error('Failed to initialize database:', error);
      throw error;
    }
  }

  /**
   * Create database tables
   */
  private async createTables(): Promise<void> {
    if (!this.db) throw new Error('Database not initialized');

    const tables = [
      // Users table
      `CREATE TABLE IF NOT EXISTS Users (
        id TEXT PRIMARY KEY,
        userName TEXT NOT NULL,
        email TEXT NOT NULL UNIQUE,
        phoneNumber TEXT,
        masterPasswordHash TEXT NOT NULL,
        twoFactorEnabled INTEGER DEFAULT 0,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL
      )`,

      // Vaults table
      `CREATE TABLE IF NOT EXISTS Vaults (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        description TEXT,
        userId TEXT NOT NULL,
        isDefault INTEGER DEFAULT 0,
        color TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL,
        FOREIGN KEY (userId) REFERENCES Users(id) ON DELETE CASCADE
      )`,

      // Categories table
      `CREATE TABLE IF NOT EXISTS Categories (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        description TEXT,
        icon TEXT,
        color TEXT,
        userId TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL
      )`,

      // Tags table
      `CREATE TABLE IF NOT EXISTS Tags (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        color TEXT,
        userId TEXT NOT NULL,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL,
        FOREIGN KEY (userId) REFERENCES Users(id) ON DELETE CASCADE
      )`,

      // PasswordItems table
      `CREATE TABLE IF NOT EXISTS PasswordItems (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        notes TEXT,
        isFavorite INTEGER DEFAULT 0,
        userId TEXT NOT NULL,
        vaultId TEXT,
        categoryId TEXT,
        type INTEGER NOT NULL,
        encryptedData TEXT,
        username TEXT,
        password TEXT,
        url TEXT,
        totpSecret TEXT,
        content TEXT,
        cardholderName TEXT,
        cardNumber TEXT,
        expirationMonth INTEGER,
        expirationYear INTEGER,
        cvv TEXT,
        brand TEXT,
        billingAddress TEXT,
        ssid TEXT,
        securityType TEXT,
        isHidden INTEGER DEFAULT 0,
        lastModifiedBy TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL,
        FOREIGN KEY (userId) REFERENCES Users(id) ON DELETE CASCADE,
        FOREIGN KEY (vaultId) REFERENCES Vaults(id) ON DELETE SET NULL,
        FOREIGN KEY (categoryId) REFERENCES Categories(id) ON DELETE SET NULL
      )`,

      // PasswordItemTags junction table
      `CREATE TABLE IF NOT EXISTS PasswordItemTags (
        passwordItemId TEXT NOT NULL,
        tagId TEXT NOT NULL,
        PRIMARY KEY (passwordItemId, tagId),
        FOREIGN KEY (passwordItemId) REFERENCES PasswordItems(id) ON DELETE CASCADE,
        FOREIGN KEY (tagId) REFERENCES Tags(id) ON DELETE CASCADE
      )`,

      // CustomFields table
      `CREATE TABLE IF NOT EXISTS CustomFields (
        id TEXT PRIMARY KEY,
        passwordItemId TEXT NOT NULL,
        name TEXT NOT NULL,
        value TEXT NOT NULL,
        type TEXT NOT NULL,
        isEncrypted INTEGER DEFAULT 1,
        FOREIGN KEY (passwordItemId) REFERENCES PasswordItems(id) ON DELETE CASCADE
      )`,

      // Devices table
      `CREATE TABLE IF NOT EXISTS Devices (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        deviceType TEXT NOT NULL,
        lastSeen TEXT NOT NULL,
        userId TEXT NOT NULL,
        isTrusted INTEGER DEFAULT 0,
        deviceFingerprint TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL,
        FOREIGN KEY (userId) REFERENCES Users(id) ON DELETE CASCADE
      )`,

      // SyncLog table for tracking sync operations
      `CREATE TABLE IF NOT EXISTS SyncLog (
        id TEXT PRIMARY KEY,
        entityType TEXT NOT NULL,
        entityId TEXT NOT NULL,
        operation TEXT NOT NULL,
        timestamp TEXT NOT NULL,
        synced INTEGER DEFAULT 0
      )`,
    ];

    for (const sql of tables) {
      await this.db.executeSql(sql);
    }

    // Create indexes for better performance
    const indexes = [
      'CREATE INDEX IF NOT EXISTS idx_passworditems_userid ON PasswordItems(userId)',
      'CREATE INDEX IF NOT EXISTS idx_passworditems_vaultid ON PasswordItems(vaultId)',
      'CREATE INDEX IF NOT EXISTS idx_passworditems_categoryid ON PasswordItems(categoryId)',
      'CREATE INDEX IF NOT EXISTS idx_vaults_userid ON Vaults(userId)',
      'CREATE INDEX IF NOT EXISTS idx_tags_userid ON Tags(userId)',
    ];

    for (const sql of indexes) {
      await this.db.executeSql(sql);
    }
  }

  /**
   * Execute a SQL query
   */
  async executeSql(sql: string, params: any[] = []): Promise<any> {
    if (!this.db) throw new Error('Database not initialized');
    const [results] = await this.db.executeSql(sql, params);
    return results;
  }

  /**
   * Get database instance
   */
  getDatabase(): SQLiteDatabase {
    if (!this.db) throw new Error('Database not initialized');
    return this.db;
  }

  /**
   * Close the database connection
   */
  async close(): Promise<void> {
    if (this.db) {
      await this.db.close();
      this.db = null;
      console.log('Database closed');
    }
  }

  /**
   * Clear all data (for logout/reset)
   */
  async clearAllData(): Promise<void> {
    if (!this.db) throw new Error('Database not initialized');

    const tables = [
      'CustomFields',
      'PasswordItemTags',
      'PasswordItems',
      'Tags',
      'Categories',
      'Vaults',
      'Devices',
      'SyncLog',
      'Users',
    ];

    for (const table of tables) {
      await this.db.executeSql(`DELETE FROM ${table}`);
    }
    console.log('All data cleared');
  }
}

export default new DatabaseService();
