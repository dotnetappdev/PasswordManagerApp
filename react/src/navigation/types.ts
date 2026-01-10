/**
 * Navigation Types
 * Type definitions for React Navigation
 */

import { NavigatorScreenParams } from '@react-navigation/native';
import { PasswordItem } from '../models';

// Root Stack Navigator
export type RootStackParamList = {
  Auth: NavigatorScreenParams<AuthStackParamList>;
  Main: NavigatorScreenParams<MainTabParamList>;
  Settings: undefined;
  ItemDetail: { itemId: string };
  AddEditItem: { itemId?: string; type?: number };
};

// Auth Stack Navigator
export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  BiometricSetup: undefined;
};

// Main Tab Navigator
export type MainTabParamList = {
  Dashboard: undefined;
  Passwords: NavigatorScreenParams<PasswordStackParamList>;
  Categories: undefined;
  Vaults: undefined;
  Profile: undefined;
};

// Password Stack Navigator
export type PasswordStackParamList = {
  PasswordList: undefined;
  PasswordDetail: { itemId: string };
  AddPassword: undefined;
  EditPassword: { itemId: string };
};
