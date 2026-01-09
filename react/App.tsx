/**
 * Password Manager Mobile App
 * React Native application for secure password management
 */

import React, { useEffect } from 'react';
import { StatusBar } from 'react-native';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import AppNavigator from './src/navigation/AppNavigator';
import { databaseService, notificationService } from './src/services';

const App = (): React.JSX.Element => {
  useEffect(() => {
    initializeApp();
  }, []);

  const initializeApp = async () => {
    try {
      // Initialize database
      await databaseService.initialize();
      console.log('Database initialized');

      // Configure notifications
      notificationService.configure();
      console.log('Notifications configured');

      // Request notification permissions
      await notificationService.requestPermissions();
      console.log('Notification permissions requested');
    } catch (error) {
      console.error('App initialization failed:', error);
    }
  };

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <StatusBar barStyle="dark-content" backgroundColor="#fff" />
      <AppNavigator />
    </GestureHandlerRootView>
  );
};

export default App;
