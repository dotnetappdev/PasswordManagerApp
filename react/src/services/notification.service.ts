/**
 * Notification Service
 * Handles push notifications and local notifications with best practices
 */

import PushNotification, { Importance } from 'react-native-push-notification';
import { Platform } from 'react-native';

class NotificationService {
  private isConfigured: boolean = false;

  /**
   * Initialize notification service
   */
  configure(): void {
    if (this.isConfigured) return;

    PushNotification.configure({
      // Called when Token is generated (iOS and Android)
      onRegister: (token) => {
        console.log('Notification Token:', token);
      },

      // Called when a remote notification is received
      onNotification: (notification) => {
        console.log('Notification:', notification);
        
        // Handle notification tap
        if (notification.userInteraction) {
          this.handleNotificationTap(notification);
        }
      },

      // Called when Action is clicked (iOS and Android)
      onAction: (notification) => {
        console.log('Action:', notification.action);
      },

      // Should the initial notification be popped automatically (default: true)
      popInitialNotification: true,

      // Requested permissions (iOS)
      permissions: {
        alert: true,
        badge: true,
        sound: true,
      },

      // Request permissions (Android)
      requestPermissions: Platform.OS === 'ios',
    });

    // Create notification channels (Android 8.0+)
    if (Platform.OS === 'android') {
      this.createChannels();
    }

    this.isConfigured = true;
  }

  /**
   * Create notification channels for Android
   */
  private createChannels(): void {
    PushNotification.createChannel(
      {
        channelId: 'password-manager-default',
        channelName: 'Password Manager',
        channelDescription: 'Default notifications',
        playSound: true,
        soundName: 'default',
        importance: Importance.HIGH,
        vibrate: true,
      },
      (created) => console.log(`Channel created: ${created}`)
    );

    PushNotification.createChannel(
      {
        channelId: 'password-manager-security',
        channelName: 'Security Alerts',
        channelDescription: 'Security-related notifications',
        playSound: true,
        soundName: 'default',
        importance: Importance.HIGH,
        vibrate: true,
      },
      (created) => console.log(`Security channel created: ${created}`)
    );

    PushNotification.createChannel(
      {
        channelId: 'password-manager-sync',
        channelName: 'Sync Notifications',
        channelDescription: 'Synchronization status',
        playSound: false,
        importance: Importance.LOW,
        vibrate: false,
      },
      (created) => console.log(`Sync channel created: ${created}`)
    );
  }

  /**
   * Show local notification
   */
  showNotification(
    title: string,
    message: string,
    channelId: string = 'password-manager-default',
    data?: any
  ): void {
    PushNotification.localNotification({
      channelId,
      title,
      message,
      playSound: true,
      soundName: 'default',
      userInfo: data,
      priority: 'high',
      visibility: 'private',
    });
  }

  /**
   * Show security alert notification
   */
  showSecurityAlert(title: string, message: string): void {
    this.showNotification(
      title,
      message,
      'password-manager-security',
      { type: 'security' }
    );
  }

  /**
   * Show sync notification
   */
  showSyncNotification(message: string): void {
    this.showNotification(
      'Sync Status',
      message,
      'password-manager-sync',
      { type: 'sync' }
    );
  }

  /**
   * Schedule notification
   */
  scheduleNotification(
    title: string,
    message: string,
    date: Date,
    channelId: string = 'password-manager-default'
  ): void {
    PushNotification.localNotificationSchedule({
      channelId,
      title,
      message,
      date,
      allowWhileIdle: true,
    });
  }

  /**
   * Cancel all notifications
   */
  cancelAllNotifications(): void {
    PushNotification.cancelAllLocalNotifications();
  }

  /**
   * Get badge count (iOS)
   */
  getBadgeCount(callback: (count: number) => void): void {
    PushNotification.getApplicationIconBadgeNumber(callback);
  }

  /**
   * Set badge count (iOS)
   */
  setBadgeCount(count: number): void {
    PushNotification.setApplicationIconBadgeNumber(count);
  }

  /**
   * Handle notification tap
   */
  private handleNotificationTap(notification: any): void {
    // Implement navigation logic based on notification data
    console.log('Notification tapped:', notification);
    
    if (notification.data) {
      const { type, itemId } = notification.data;
      
      // Navigate to appropriate screen based on notification type
      // This will be handled by navigation service
    }
  }

  /**
   * Request notification permissions
   */
  async requestPermissions(): Promise<boolean> {
    return new Promise((resolve) => {
      PushNotification.checkPermissions((permissions) => {
        if (permissions.alert && permissions.badge && permissions.sound) {
          resolve(true);
        } else {
          PushNotification.requestPermissions().then((result) => {
            resolve(
              result.alert === true &&
              result.badge === true &&
              result.sound === true
            );
          });
        }
      });
    });
  }
}

export default new NotificationService();
