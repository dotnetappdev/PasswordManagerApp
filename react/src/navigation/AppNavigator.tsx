/**
 * App Navigator
 * Main navigation configuration
 */

import React, { useEffect, useState } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';

import { RootStackParamList, AuthStackParamList, MainTabParamList } from './types';
import { authService } from '../services';

// Auth screens
import LoginScreen from '../screens/Auth/LoginScreen';
import RegisterScreen from '../screens/Auth/RegisterScreen';

// Main screens
import DashboardScreen from '../screens/Main/DashboardScreen';
import PasswordListScreen from '../screens/Main/PasswordListScreen';
import SettingsScreen from '../screens/Main/SettingsScreen';
import CategoriesScreen from '../screens/Main/CategoriesScreen';
import VaultsScreen from '../screens/Main/VaultsScreen';
import ProfileScreen from '../screens/Main/ProfileScreen';
import PasswordHealthScreen from '../screens/Main/PasswordHealthScreen';
import PasskeysScreen from '../screens/Main/PasskeysScreen';
import SecureSendScreen from '../screens/Main/SecureSendScreen';

const RootStack = createNativeStackNavigator<RootStackParamList>();
const AuthStack = createNativeStackNavigator<AuthStackParamList>();
const MainTab = createBottomTabNavigator<MainTabParamList>();

const TAB_BAR_STYLE = {
  backgroundColor: '#1A1D27',
  borderTopColor: '#2A2D3A',
  borderTopWidth: 1,
};

/**
 * Auth Stack Navigator
 */
const AuthNavigator = () => {
  return (
    <AuthStack.Navigator
      screenOptions={{
        headerShown: false,
      }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
      <AuthStack.Screen name="Register" component={RegisterScreen} />
    </AuthStack.Navigator>
  );
};

/**
 * Main Tab Navigator
 */
const MainNavigator = () => {
  return (
    <MainTab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName: string;

          switch (route.name) {
            case 'Dashboard':
              iconName = focused ? 'view-dashboard' : 'view-dashboard-outline';
              break;
            case 'Passwords':
              iconName = focused ? 'key' : 'key-outline';
              break;
            case 'Categories':
              iconName = focused ? 'folder' : 'folder-outline';
              break;
            case 'Vaults':
              iconName = focused ? 'lock' : 'lock-outline';
              break;
            case 'Profile':
              iconName = focused ? 'account' : 'account-outline';
              break;
            case 'PasswordHealth':
              iconName = focused ? 'shield-check' : 'shield-check-outline';
              break;
            case 'Passkeys':
              iconName = focused ? 'fingerprint' : 'fingerprint';
              break;
            case 'SecureSend':
              iconName = focused ? 'share-variant' : 'share-variant-outline';
              break;
            default:
              iconName = 'circle';
          }

          return <Icon name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: '#7C3AED',
        tabBarInactiveTintColor: '#8B90A7',
        tabBarStyle: TAB_BAR_STYLE,
        tabBarLabelStyle: { fontSize: 11 },
        headerShown: true,
        headerStyle: { backgroundColor: '#1A1D27' },
        headerTintColor: '#F0F2FF',
        headerTitleStyle: { fontWeight: '600' },
      })}>
      <MainTab.Screen
        name="Dashboard"
        component={DashboardScreen}
        options={{ title: 'Dashboard' }}
      />
      <MainTab.Screen
        name="Passwords"
        component={PasswordListScreen}
        options={{ title: 'Passwords' }}
      />
      <MainTab.Screen
        name="PasswordHealth"
        component={PasswordHealthScreen}
        options={{ title: 'Health', tabBarLabel: 'Health' }}
      />
      <MainTab.Screen
        name="Passkeys"
        component={PasskeysScreen}
        options={{ title: 'Passkeys' }}
      />
      <MainTab.Screen
        name="SecureSend"
        component={SecureSendScreen}
        options={{ title: 'Send', tabBarLabel: 'Send' }}
      />
      <MainTab.Screen
        name="Categories"
        component={CategoriesScreen}
        options={{ title: 'Categories', tabBarItemStyle: { display: 'none' } }}
      />
      <MainTab.Screen
        name="Vaults"
        component={VaultsScreen}
        options={{ title: 'Vaults', tabBarItemStyle: { display: 'none' } }}
      />
      <MainTab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{ title: 'Profile' }}
      />
    </MainTab.Navigator>
  );
};

/**
 * App Navigator
 */
const AppNavigator = () => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const authenticated = await authService.isAuthenticated();
      setIsAuthenticated(authenticated);
    } catch (error) {
      console.error('Auth check failed:', error);
      setIsAuthenticated(false);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return null; // Or a loading screen
  }

  return (
    <NavigationContainer>
      <RootStack.Navigator screenOptions={{ headerShown: false }}>
        {isAuthenticated ? (
          <RootStack.Screen name="Main" component={MainNavigator} />
        ) : (
          <RootStack.Screen name="Auth" component={AuthNavigator} />
        )}
        <RootStack.Screen
          name="Settings"
          component={SettingsScreen}
          options={{
            headerShown: true,
            title: 'Settings',
            presentation: 'modal',
            headerStyle: { backgroundColor: '#1A1D27' },
            headerTintColor: '#F0F2FF',
          }}
        />
      </RootStack.Navigator>
    </NavigationContainer>
  );
};

export default AppNavigator;
