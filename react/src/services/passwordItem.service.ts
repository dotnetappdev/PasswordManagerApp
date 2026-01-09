/**
 * Password Item Service
 * Manages password items in local database or via API
 */

import { PasswordItem, LoginItem, PasswordItemType } from '../models';
import apiService from './api.service';
import databaseService from './database.service';
import encryptionService from './encryption.service';
import storageService from './storage.service';
import authService from './auth.service';

class PasswordItemService {
  /**
   * Get all password items for current user
   */
  async getAll(vaultId?: string): Promise<PasswordItem[]> {
    const settings = await storageService.getSettings();
    
    if (settings.mode === 'api') {
      return await this.getAllFromApi(vaultId);
    } else {
      return await this.getAllFromDatabase(vaultId);
    }
  }

  /**
   * Get all items from API
   */
  private async getAllFromApi(vaultId?: string): Promise<PasswordItem[]> {
    const url = vaultId ? `/passworditems?vaultId=${vaultId}` : '/passworditems';
    const response = await apiService.get<PasswordItem[]>(url);
    
    if (response.success && response.data) {
      // Decrypt items
      const masterKey = authService.getMasterKey();
      if (masterKey) {
        return response.data.map(item => this.decryptItem(item, masterKey));
      }
      return response.data;
    }
    
    return [];
  }

  /**
   * Get all items from local database
   */
  private async getAllFromDatabase(vaultId?: string): Promise<PasswordItem[]> {
    const userId = await storageService.getItem('@auth:userId');
    if (!userId) throw new Error('User not authenticated');

    let query = 'SELECT * FROM PasswordItems WHERE userId = ?';
    const params: any[] = [userId];

    if (vaultId) {
      query += ' AND vaultId = ?';
      params.push(vaultId);
    }

    query += ' ORDER BY name ASC';

    const result = await databaseService.executeSql(query, params);
    const items: PasswordItem[] = [];

    for (let i = 0; i < result.rows.length; i++) {
      const row = result.rows.item(i);
      items.push(this.mapRowToItem(row));
    }

    // Decrypt items
    const masterKey = authService.getMasterKey();
    if (masterKey) {
      return items.map(item => this.decryptItem(item, masterKey));
    }

    return items;
  }

  /**
   * Get item by ID
   */
  async getById(id: string): Promise<PasswordItem | null> {
    const settings = await storageService.getSettings();
    
    if (settings.mode === 'api') {
      return await this.getByIdFromApi(id);
    } else {
      return await this.getByIdFromDatabase(id);
    }
  }

  /**
   * Get item from API
   */
  private async getByIdFromApi(id: string): Promise<PasswordItem | null> {
    const response = await apiService.get<PasswordItem>(`/passworditems/${id}`);
    
    if (response.success && response.data) {
      const masterKey = authService.getMasterKey();
      if (masterKey) {
        return this.decryptItem(response.data, masterKey);
      }
      return response.data;
    }
    
    return null;
  }

  /**
   * Get item from database
   */
  private async getByIdFromDatabase(id: string): Promise<PasswordItem | null> {
    const result = await databaseService.executeSql(
      'SELECT * FROM PasswordItems WHERE id = ?',
      [id]
    );

    if (result.rows.length === 0) return null;

    const item = this.mapRowToItem(result.rows.item(0));
    const masterKey = authService.getMasterKey();
    
    if (masterKey) {
      return this.decryptItem(item, masterKey);
    }

    return item;
  }

  /**
   * Create new password item
   */
  async create(item: Partial<PasswordItem>): Promise<PasswordItem> {
    const settings = await storageService.getSettings();
    
    // Encrypt sensitive fields
    const masterKey = authService.getMasterKey();
    if (!masterKey) throw new Error('Master key not available');
    
    const encryptedItem = this.encryptItem(item as PasswordItem, masterKey);
    
    if (settings.mode === 'api') {
      return await this.createViaApi(encryptedItem);
    } else {
      return await this.createInDatabase(encryptedItem);
    }
  }

  /**
   * Create item via API
   */
  private async createViaApi(item: PasswordItem): Promise<PasswordItem> {
    const response = await apiService.post<PasswordItem>('/passworditems', item);
    
    if (response.success && response.data) {
      return response.data;
    }
    
    throw new Error(response.message || 'Failed to create item');
  }

  /**
   * Create item in database
   */
  private async createInDatabase(item: PasswordItem): Promise<PasswordItem> {
    const userId = await storageService.getItem('@auth:userId');
    if (!userId) throw new Error('User not authenticated');

    const id = this.generateId();
    const now = new Date().toISOString();

    await databaseService.executeSql(
      `INSERT INTO PasswordItems (
        id, name, notes, isFavorite, userId, vaultId, categoryId, type,
        username, password, url, totpSecret, content,
        createdAt, updatedAt
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        id,
        item.name,
        item.notes || null,
        item.isFavorite ? 1 : 0,
        userId,
        item.vaultId || null,
        item.categoryId || null,
        item.type,
        (item as LoginItem).username || null,
        (item as LoginItem).password || null,
        (item as LoginItem).url || null,
        (item as LoginItem).totpSecret || null,
        (item as any).content || null,
        now,
        now,
      ]
    );

    return {
      ...item,
      id,
      userId,
      createdAt: now,
      updatedAt: now,
    } as PasswordItem;
  }

  /**
   * Update password item
   */
  async update(id: string, item: Partial<PasswordItem>): Promise<PasswordItem> {
    const settings = await storageService.getSettings();
    
    const masterKey = authService.getMasterKey();
    if (!masterKey) throw new Error('Master key not available');
    
    const encryptedItem = this.encryptItem(item as PasswordItem, masterKey);
    
    if (settings.mode === 'api') {
      return await this.updateViaApi(id, encryptedItem);
    } else {
      return await this.updateInDatabase(id, encryptedItem);
    }
  }

  /**
   * Update via API
   */
  private async updateViaApi(id: string, item: PasswordItem): Promise<PasswordItem> {
    const response = await apiService.put<PasswordItem>(`/passworditems/${id}`, item);
    
    if (response.success && response.data) {
      return response.data;
    }
    
    throw new Error(response.message || 'Failed to update item');
  }

  /**
   * Update in database
   */
  private async updateInDatabase(id: string, item: Partial<PasswordItem>): Promise<PasswordItem> {
    const now = new Date().toISOString();

    await databaseService.executeSql(
      `UPDATE PasswordItems SET
        name = ?, notes = ?, isFavorite = ?, categoryId = ?,
        username = ?, password = ?, url = ?, totpSecret = ?,
        updatedAt = ?
      WHERE id = ?`,
      [
        item.name,
        item.notes || null,
        item.isFavorite ? 1 : 0,
        item.categoryId || null,
        (item as LoginItem).username || null,
        (item as LoginItem).password || null,
        (item as LoginItem).url || null,
        (item as LoginItem).totpSecret || null,
        now,
        id,
      ]
    );

    return await this.getByIdFromDatabase(id) as PasswordItem;
  }

  /**
   * Delete password item
   */
  async delete(id: string): Promise<void> {
    const settings = await storageService.getSettings();
    
    if (settings.mode === 'api') {
      await this.deleteViaApi(id);
    } else {
      await this.deleteFromDatabase(id);
    }
  }

  /**
   * Delete via API
   */
  private async deleteViaApi(id: string): Promise<void> {
    await apiService.delete(`/passworditems/${id}`);
  }

  /**
   * Delete from database
   */
  private async deleteFromDatabase(id: string): Promise<void> {
    await databaseService.executeSql('DELETE FROM PasswordItems WHERE id = ?', [id]);
  }

  /**
   * Search password items
   */
  async search(query: string): Promise<PasswordItem[]> {
    const allItems = await this.getAll();
    const lowerQuery = query.toLowerCase();
    
    return allItems.filter(item =>
      item.name.toLowerCase().includes(lowerQuery) ||
      (item.notes && item.notes.toLowerCase().includes(lowerQuery)) ||
      ((item as LoginItem).username && (item as LoginItem).username!.toLowerCase().includes(lowerQuery))
    );
  }

  /**
   * Encrypt password item
   */
  private encryptItem(item: PasswordItem, masterKey: string): PasswordItem {
    const encryptedItem = { ...item };

    if ((item as LoginItem).password) {
      (encryptedItem as LoginItem).password = encryptionService.encrypt(
        (item as LoginItem).password!,
        masterKey
      );
    }

    if ((item as LoginItem).totpSecret) {
      (encryptedItem as LoginItem).totpSecret = encryptionService.encrypt(
        (item as LoginItem).totpSecret!,
        masterKey
      );
    }

    if (item.notes) {
      encryptedItem.notes = encryptionService.encrypt(item.notes, masterKey);
    }

    return encryptedItem;
  }

  /**
   * Decrypt password item
   */
  private decryptItem(item: PasswordItem, masterKey: string): PasswordItem {
    const decryptedItem = { ...item };

    try {
      if ((item as LoginItem).password) {
        (decryptedItem as LoginItem).password = encryptionService.decrypt(
          (item as LoginItem).password!,
          masterKey
        );
      }

      if ((item as LoginItem).totpSecret) {
        (decryptedItem as LoginItem).totpSecret = encryptionService.decrypt(
          (item as LoginItem).totpSecret!,
          masterKey
        );
      }

      if (item.notes) {
        decryptedItem.notes = encryptionService.decrypt(item.notes, masterKey);
      }
    } catch (error) {
      console.error('Failed to decrypt item:', error);
    }

    return decryptedItem;
  }

  /**
   * Map database row to PasswordItem
   */
  private mapRowToItem(row: any): PasswordItem {
    return {
      id: row.id,
      name: row.name,
      notes: row.notes,
      isFavorite: row.isFavorite === 1,
      userId: row.userId,
      vaultId: row.vaultId,
      categoryId: row.categoryId,
      type: row.type,
      username: row.username,
      password: row.password,
      url: row.url,
      totpSecret: row.totpSecret,
      content: row.content,
      createdAt: row.createdAt,
      updatedAt: row.updatedAt,
    } as PasswordItem;
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

export default new PasswordItemService();
