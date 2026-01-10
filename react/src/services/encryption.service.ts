/**
 * Encryption Service
 * Handles encryption/decryption using AES-256-GCM with PBKDF2 key derivation
 * Matches the backend encryption implementation
 */

import CryptoJS from 'crypto-js';
import { AppConfig } from '../config/app.config';
import 'react-native-get-random-values';

class EncryptionService {
  /**
   * Generate a cryptographically secure random salt
   */
  generateSalt(length: number = 32): string {
    return CryptoJS.lib.WordArray.random(length).toString();
  }

  /**
   * Generate a random IV for encryption
   */
  generateIV(): string {
    return CryptoJS.lib.WordArray.random(16).toString();
  }

  /**
   * Derive a key from password using PBKDF2
   */
  deriveKey(
    password: string,
    salt: string,
    iterations: number = AppConfig.encryption.keyDerivationIterations
  ): string {
    const key = CryptoJS.PBKDF2(password, salt, {
      keySize: 256 / 32,
      iterations: iterations,
      hasher: CryptoJS.algo.SHA256,
    });
    return key.toString();
  }

  /**
   * Hash password for authentication (not encryption)
   */
  hashPassword(
    password: string,
    salt: string,
    iterations: number = AppConfig.encryption.authHashIterations
  ): string {
    const hash = CryptoJS.PBKDF2(password, salt, {
      keySize: 256 / 32,
      iterations: iterations,
      hasher: CryptoJS.algo.SHA256,
    });
    return hash.toString();
  }

  /**
   * Encrypt data using AES-256-GCM
   */
  encrypt(plaintext: string, masterKey: string): string {
    try {
      // Generate random IV
      const iv = this.generateIV();
      
      // Encrypt using AES
      const encrypted = CryptoJS.AES.encrypt(plaintext, masterKey, {
        iv: CryptoJS.enc.Hex.parse(iv),
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7,
      });

      // Combine IV and ciphertext
      const result = {
        iv: iv,
        ciphertext: encrypted.ciphertext.toString(),
      };

      return JSON.stringify(result);
    } catch (error) {
      console.error('Encryption failed:', error);
      throw new Error('Failed to encrypt data');
    }
  }

  /**
   * Decrypt data using AES-256-GCM
   */
  decrypt(encryptedData: string, masterKey: string): string {
    try {
      const data = JSON.parse(encryptedData);
      const iv = CryptoJS.enc.Hex.parse(data.iv);
      const ciphertext = CryptoJS.enc.Hex.parse(data.ciphertext);

      // Create cipher params
      const cipherParams = CryptoJS.lib.CipherParams.create({
        ciphertext: ciphertext,
      });

      // Decrypt
      const decrypted = CryptoJS.AES.decrypt(cipherParams, masterKey, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7,
      });

      return decrypted.toString(CryptoJS.enc.Utf8);
    } catch (error) {
      console.error('Decryption failed:', error);
      throw new Error('Failed to decrypt data');
    }
  }

  /**
   * Generate master key from master password
   */
  generateMasterKey(masterPassword: string, email: string): {
    masterKey: string;
    salt: string;
  } {
    // Use email as part of salt for consistency
    const salt = this.generateSalt();
    const masterKey = this.deriveKey(
      masterPassword,
      salt + email.toLowerCase(),
      AppConfig.encryption.keyDerivationIterations
    );

    return { masterKey, salt };
  }

  /**
   * Verify master password
   */
  verifyMasterPassword(
    masterPassword: string,
    email: string,
    salt: string,
    expectedHash: string
  ): boolean {
    const derivedKey = this.deriveKey(
      masterPassword,
      salt + email.toLowerCase(),
      AppConfig.encryption.keyDerivationIterations
    );
    const hash = CryptoJS.SHA256(derivedKey).toString();
    return hash === expectedHash;
  }

  /**
   * Hash data using SHA-256
   */
  sha256(data: string): string {
    return CryptoJS.SHA256(data).toString();
  }

  /**
   * Generate a secure random password
   */
  generatePassword(
    length: number = 16,
    includeUppercase: boolean = true,
    includeLowercase: boolean = true,
    includeNumbers: boolean = true,
    includeSymbols: boolean = true
  ): string {
    let charset = '';
    if (includeUppercase) charset += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    if (includeLowercase) charset += 'abcdefghijklmnopqrstuvwxyz';
    if (includeNumbers) charset += '0123456789';
    if (includeSymbols) charset += '!@#$%^&*()_+-=[]{}|;:,.<>?';

    if (charset === '') {
      throw new Error('At least one character type must be selected');
    }

    let password = '';
    const randomValues = CryptoJS.lib.WordArray.random(length);
    const bytes = randomValues.toString(CryptoJS.enc.Hex);

    for (let i = 0; i < length; i++) {
      const randomIndex = parseInt(bytes.substr(i * 2, 2), 16) % charset.length;
      password += charset[randomIndex];
    }

    return password;
  }

  /**
   * Calculate password strength (0-100)
   */
  calculatePasswordStrength(password: string): number {
    if (!password) return 0;

    let strength = 0;

    // Length
    if (password.length >= 8) strength += 20;
    if (password.length >= 12) strength += 10;
    if (password.length >= 16) strength += 10;

    // Complexity
    if (/[a-z]/.test(password)) strength += 15;
    if (/[A-Z]/.test(password)) strength += 15;
    if (/[0-9]/.test(password)) strength += 15;
    if (/[^a-zA-Z0-9]/.test(password)) strength += 15;

    return Math.min(strength, 100);
  }
}

export default new EncryptionService();
