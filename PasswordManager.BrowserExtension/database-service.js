// Database service for reading SQLite files directly
class DatabaseService {
  constructor() {
    this.db = null;
    this.isInitialized = false;
  }

  async initialize(databaseFile) {
    try {
      // Read the database file
      const fileBuffer = await this.readFileAsArrayBuffer(databaseFile);
      
      // Initialize SQL.js database
      this.db = new SQL.Database(new Uint8Array(fileBuffer));
      this.isInitialized = true;
      
      console.log('Database initialized successfully');
      return { success: true };
    } catch (error) {
      console.error('Failed to initialize database:', error);
      return { success: false, error: error.message };
    }
  }

  async readFileAsArrayBuffer(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (event) => resolve(event.target.result);
      reader.onerror = (error) => reject(error);
      reader.readAsArrayBuffer(file);
    });
  }

  async getUserSalt(userEmail) {
    if (!this.isInitialized) {
      throw new Error('Database not initialized');
    }

    try {
      const stmt = this.db.prepare('SELECT UserSalt FROM AspNetUsers WHERE Email = ?');
      stmt.bind([userEmail]);
      
      if (stmt.step()) {
        const result = stmt.getAsObject();
        return result.UserSalt ? atob(result.UserSalt) : null;
      }
      
      return null;
    } catch (error) {
      console.error('Error getting user salt:', error);
      throw error;
    }
  }

  async getUserPasswordHash(userEmail) {
    if (!this.isInitialized) {
      throw new Error('Database not initialized');
    }

    try {
      const stmt = this.db.prepare('SELECT PasswordHash FROM AspNetUsers WHERE Email = ?');
      stmt.bind([userEmail]);
      
      if (stmt.step()) {
        const result = stmt.getAsObject();
        return result.PasswordHash;
      }
      
      return null;
    } catch (error) {
      console.error('Error getting password hash:', error);
      throw error;
    }
  }

  async getLoginItems(userId = null, domain = null) {
    if (!this.isInitialized) {
      throw new Error('Database not initialized');
    }

    try {
      let query = `
        SELECT 
          p.Id, p.Title,
          l.Username, l.EncryptedPassword, l.PasswordNonce, l.PasswordAuthTag,
          l.WebsiteUrl, l.Website
        FROM PasswordItems p
        INNER JOIN LoginItems l ON p.Id = l.PasswordItemId
        WHERE p.IsDeleted = 0 AND p.Type = 0
      `;
      
      const params = [];
      
      if (userId) {
        query += ' AND p.UserId = ?';
        params.push(userId);
      }
      
      if (domain) {
        query += ' AND (l.WebsiteUrl LIKE ? OR l.Website LIKE ?)';
        params.push(`%${domain}%`, `%${domain}%`);
      }
      
      const stmt = this.db.prepare(query);
      if (params.length > 0) {
        stmt.bind(params);
      }
      
      const results = [];
      while (stmt.step()) {
        const row = stmt.getAsObject();
        results.push({
          id: row.Id,
          title: row.Title,
          username: row.Username || '',
          encryptedPassword: row.EncryptedPassword,
          passwordNonce: row.PasswordNonce,
          passwordAuthTag: row.PasswordAuthTag,
          websiteUrl: row.WebsiteUrl || row.Website || ''
        });
      }
      
      stmt.free();
      return results;
    } catch (error) {
      console.error('Error getting login items:', error);
      throw error;
    }
  }

  async getPasswordItem(itemId) {
    if (!this.isInitialized) {
      throw new Error('Database not initialized');
    }

    try {
      const query = `
        SELECT 
          p.Id, p.Title,
          l.Username, l.EncryptedPassword, l.PasswordNonce, l.PasswordAuthTag,
          l.WebsiteUrl, l.Website
        FROM PasswordItems p
        INNER JOIN LoginItems l ON p.Id = l.PasswordItemId
        WHERE p.Id = ? AND p.IsDeleted = 0
      `;
      
      const stmt = this.db.prepare(query);
      stmt.bind([itemId]);
      
      if (stmt.step()) {
        const row = stmt.getAsObject();
        stmt.free();
        return {
          id: row.Id,
          title: row.Title,
          username: row.Username || '',
          encryptedPassword: row.EncryptedPassword,
          passwordNonce: row.PasswordNonce,
          passwordAuthTag: row.PasswordAuthTag,
          websiteUrl: row.WebsiteUrl || row.Website || ''
        };
      }
      
      stmt.free();
      return null;
    } catch (error) {
      console.error('Error getting password item:', error);
      throw error;
    }
  }

  close() {
    if (this.db) {
      this.db.close();
      this.db = null;
      this.isInitialized = false;
    }
  }
}