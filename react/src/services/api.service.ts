/**
 * API Service
 * Handles all HTTP requests to the backend API with best practices
 * - Request/response interceptors
 * - Error handling
 * - Retry logic
 * - Request cancellation
 * - Authentication token management
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios';
import NetInfo from '@react-native-community/netinfo';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { AppConfig } from '../config/app.config';
import { ApiResponse } from '../models';

class ApiService {
  private client: AxiosInstance;
  private authToken: string | null = null;
  private isRefreshing: boolean = false;
  private failedQueue: any[] = [];

  constructor() {
    this.client = axios.create({
      baseURL: AppConfig.api.defaultBaseUrl,
      timeout: AppConfig.api.timeout,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  /**
   * Setup request and response interceptors
   */
  private setupInterceptors(): void {
    // Request interceptor
    this.client.interceptors.request.use(
      async (config) => {
        // Check network connectivity
        const netInfo = await NetInfo.fetch();
        if (!netInfo.isConnected) {
          throw new Error('No internet connection');
        }

        // Add authentication token
        if (this.authToken) {
          config.headers.Authorization = `Bearer ${this.authToken}`;
        }

        // Add API key if configured
        const apiKey = await AsyncStorage.getItem(AppConfig.storageKeys.apiKey);
        if (apiKey) {
          config.headers['X-API-Key'] = apiKey;
        }

        console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
        return config;
      },
      (error) => {
        console.error('[API] Request error:', error);
        return Promise.reject(error);
      }
    );

    // Response interceptor
    this.client.interceptors.response.use(
      (response) => {
        console.log(`[API] Response ${response.status}:`, response.config.url);
        return response;
      },
      async (error: AxiosError) => {
        const originalRequest: any = error.config;

        // Handle 401 Unauthorized - try to refresh token
        if (error.response?.status === 401 && !originalRequest._retry) {
          if (this.isRefreshing) {
            return new Promise((resolve, reject) => {
              this.failedQueue.push({ resolve, reject });
            })
              .then((token) => {
                originalRequest.headers.Authorization = `Bearer ${token}`;
                return this.client(originalRequest);
              })
              .catch((err) => {
                return Promise.reject(err);
              });
          }

          originalRequest._retry = true;
          this.isRefreshing = true;

          try {
            const newToken = await this.refreshAuthToken();
            this.authToken = newToken;
            this.processQueue(null, newToken);
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return this.client(originalRequest);
          } catch (refreshError) {
            this.processQueue(refreshError, null);
            // Token refresh failed, user needs to login again
            await this.clearAuth();
            return Promise.reject(refreshError);
          } finally {
            this.isRefreshing = false;
          }
        }

        // Handle network errors
        if (!error.response) {
          console.error('[API] Network error:', error.message);
          throw new Error('Network error. Please check your connection.');
        }

        // Handle other errors
        console.error('[API] Error:', error.response?.status, error.response?.data);
        return Promise.reject(error);
      }
    );
  }

  /**
   * Process queued requests after token refresh
   */
  private processQueue(error: any, token: string | null = null): void {
    this.failedQueue.forEach((promise) => {
      if (error) {
        promise.reject(error);
      } else {
        promise.resolve(token);
      }
    });
    this.failedQueue = [];
  }

  /**
   * Refresh authentication token
   */
  private async refreshAuthToken(): Promise<string> {
    // Implement token refresh logic
    // This should call your refresh token endpoint
    throw new Error('Token refresh not implemented');
  }

  /**
   * Clear authentication data
   */
  private async clearAuth(): Promise<void> {
    this.authToken = null;
    await AsyncStorage.multiRemove([
      AppConfig.storageKeys.sessionToken,
      AppConfig.storageKeys.userId,
    ]);
  }

  /**
   * Set authentication token
   */
  setAuthToken(token: string): void {
    this.authToken = token;
  }

  /**
   * Update base URL
   */
  setBaseUrl(url: string): void {
    this.client.defaults.baseURL = url;
  }

  /**
   * Generic GET request with retry logic
   */
  async get<T>(
    url: string,
    config?: AxiosRequestConfig,
    retries: number = AppConfig.api.retryAttempts
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.get<ApiResponse<T>>(url, config);
      return response.data;
    } catch (error) {
      if (retries > 0 && this.shouldRetry(error as AxiosError)) {
        console.log(`[API] Retrying GET ${url}, ${retries} attempts left`);
        await this.delay(AppConfig.api.retryDelay);
        return this.get<T>(url, config, retries - 1);
      }
      throw this.handleError(error as AxiosError);
    }
  }

  /**
   * Generic POST request
   */
  async post<T>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.post<ApiResponse<T>>(url, data, config);
      return response.data;
    } catch (error) {
      throw this.handleError(error as AxiosError);
    }
  }

  /**
   * Generic PUT request
   */
  async put<T>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.put<ApiResponse<T>>(url, data, config);
      return response.data;
    } catch (error) {
      throw this.handleError(error as AxiosError);
    }
  }

  /**
   * Generic DELETE request
   */
  async delete<T>(
    url: string,
    config?: AxiosRequestConfig
  ): Promise<ApiResponse<T>> {
    try {
      const response = await this.client.delete<ApiResponse<T>>(url, config);
      return response.data;
    } catch (error) {
      throw this.handleError(error as AxiosError);
    }
  }

  /**
   * Check if request should be retried
   */
  private shouldRetry(error: AxiosError): boolean {
    // Retry on network errors or 5xx server errors
    return (
      !error.response ||
      (error.response.status >= 500 && error.response.status < 600)
    );
  }

  /**
   * Delay utility for retry logic
   */
  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Handle and format API errors
   */
  private handleError(error: AxiosError): Error {
    if (error.response) {
      // Server responded with error
      const data: any = error.response.data;
      const message = data?.message || 'An error occurred';
      return new Error(message);
    } else if (error.request) {
      // Request made but no response
      return new Error('No response from server. Please try again.');
    } else {
      // Error setting up request
      return new Error(error.message || 'Request failed');
    }
  }

  /**
   * Check API connectivity
   */
  async checkConnectivity(): Promise<boolean> {
    try {
      await this.get('/health');
      return true;
    } catch {
      return false;
    }
  }
}

export default new ApiService();
