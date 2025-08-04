// Crypto service for local encryption/decryption operations
// JavaScript implementation of the C# PasswordCryptoService
class CryptoService {
  constructor() {
    this.MASTER_KEY_ITERATIONS = 600000; // OWASP 2024 recommendation
    this.AUTH_HASH_ITERATIONS = 600000;
    this.MASTER_KEY_LENGTH = 32; // 256 bits for AES
    this.NONCE_LENGTH = 12; // 96 bits for GCM
    this.AUTH_TAG_LENGTH = 16; // 128 bits for GCM
  }

  /**
   * Derives a key from password using PBKDF2
   */
  async deriveKey(password, salt, iterations, keyLength) {
    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);
    
    // Import password as key material
    const keyMaterial = await crypto.subtle.importKey(
      'raw',
      passwordBuffer,
      { name: 'PBKDF2' },
      false,
      ['deriveKey', 'deriveBits']
    );
    
    // Derive key using PBKDF2
    const derivedKey = await crypto.subtle.deriveBits(
      {
        name: 'PBKDF2',
        salt: salt,
        iterations: iterations,
        hash: 'SHA-256'
      },
      keyMaterial,
      keyLength * 8 // bits
    );
    
    return new Uint8Array(derivedKey);
  }

  /**
   * Derives master key from master password and user salt
   */
  async deriveMasterKey(masterPassword, userSaltBase64) {
    const userSalt = this.base64ToUint8Array(userSaltBase64);
    return await this.deriveKey(masterPassword, userSalt, this.MASTER_KEY_ITERATIONS, this.MASTER_KEY_LENGTH);
  }

  /**
   * Creates authentication hash for master password verification
   */
  async createAuthHash(masterKey, masterPassword) {
    const encoder = new TextEncoder();
    
    // Convert master key to base64 then to bytes for salt
    const masterKeyBase64 = this.uint8ArrayToBase64(masterKey);
    const masterKeyBytes = encoder.encode(masterKeyBase64);
    const masterPasswordBytes = encoder.encode(masterPassword);
    
    // Combine master key + master password as salt
    const authSalt = new Uint8Array(masterKeyBytes.length + masterPasswordBytes.length);
    authSalt.set(masterKeyBytes, 0);
    authSalt.set(masterPasswordBytes, masterKeyBytes.length);
    
    // Hash with single iteration (Bitwarden approach)
    const hashBytes = await this.deriveKey(masterKeyBase64, authSalt, 1, this.MASTER_KEY_LENGTH);
    return this.uint8ArrayToBase64(hashBytes);
  }

  /**
   * Verifies master password against stored hash
   */
  async verifyMasterPassword(masterPassword, userSaltBase64, storedHash) {
    try {
      const masterKey = await this.deriveMasterKey(masterPassword, userSaltBase64);
      const computedHash = await this.createAuthHash(masterKey, masterPassword);
      
      // Clear master key from memory
      masterKey.fill(0);
      
      return computedHash === storedHash;
    } catch (error) {
      console.error('Error verifying master password:', error);
      return false;
    }
  }

  /**
   * Decrypts password using AES-256-GCM
   */
  async decryptPassword(encryptedPasswordData, masterKey) {
    try {
      const ciphertext = this.base64ToUint8Array(encryptedPasswordData.encryptedPassword);
      const nonce = this.base64ToUint8Array(encryptedPasswordData.passwordNonce);
      const authTag = this.base64ToUint8Array(encryptedPasswordData.passwordAuthTag);
      
      // Combine ciphertext and auth tag for WebCrypto API
      const encryptedData = new Uint8Array(ciphertext.length + authTag.length);
      encryptedData.set(ciphertext);
      encryptedData.set(authTag, ciphertext.length);
      
      // Import master key for AES-GCM
      const cryptoKey = await crypto.subtle.importKey(
        'raw',
        masterKey,
        { name: 'AES-GCM', length: 256 },
        false,
        ['decrypt']
      );
      
      // Decrypt the data
      const decryptedBuffer = await crypto.subtle.decrypt(
        {
          name: 'AES-GCM',
          iv: nonce,
          tagLength: 128 // 16 bytes * 8 bits
        },
        cryptoKey,
        encryptedData
      );
      
      // Convert to string
      const decoder = new TextDecoder();
      return decoder.decode(decryptedBuffer);
    } catch (error) {
      console.error('Error decrypting password:', error);
      throw new Error('Failed to decrypt password');
    }
  }

  /**
   * Encrypts password using AES-256-GCM
   */
  async encryptPassword(password, masterKey) {
    try {
      const encoder = new TextEncoder();
      const passwordBytes = encoder.encode(password);
      
      // Generate random nonce
      const nonce = crypto.getRandomValues(new Uint8Array(this.NONCE_LENGTH));
      
      // Import master key for AES-GCM
      const cryptoKey = await crypto.subtle.importKey(
        'raw',
        masterKey,
        { name: 'AES-GCM', length: 256 },
        false,
        ['encrypt']
      );
      
      // Encrypt the password
      const encryptedBuffer = await crypto.subtle.encrypt(
        {
          name: 'AES-GCM',
          iv: nonce,
          tagLength: 128 // 16 bytes * 8 bits
        },
        cryptoKey,
        passwordBytes
      );
      
      const encryptedArray = new Uint8Array(encryptedBuffer);
      const ciphertext = encryptedArray.slice(0, -this.AUTH_TAG_LENGTH);
      const authTag = encryptedArray.slice(-this.AUTH_TAG_LENGTH);
      
      return {
        encryptedPassword: this.uint8ArrayToBase64(ciphertext),
        passwordNonce: this.uint8ArrayToBase64(nonce),
        passwordAuthTag: this.uint8ArrayToBase64(authTag)
      };
    } catch (error) {
      console.error('Error encrypting password:', error);
      throw new Error('Failed to encrypt password');
    }
  }

  /**
   * Generates a cryptographically secure random salt
   */
  generateSalt(length = 32) {
    return crypto.getRandomValues(new Uint8Array(length));
  }

  /**
   * Utility: Convert base64 to Uint8Array
   */
  base64ToUint8Array(base64) {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes;
  }

  /**
   * Utility: Convert Uint8Array to base64
   */
  uint8ArrayToBase64(bytes) {
    let binaryString = '';
    for (let i = 0; i < bytes.length; i++) {
      binaryString += String.fromCharCode(bytes[i]);
    }
    return btoa(binaryString);
  }

  /**
   * Securely clear array from memory
   */
  clearArray(array) {
    if (array && array.fill) {
      array.fill(0);
    }
  }
}