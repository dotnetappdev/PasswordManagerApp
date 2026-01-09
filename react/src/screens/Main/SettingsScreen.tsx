/**
 * Settings Screen
 * App configuration screen with database mode selection
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
  Switch,
  ActivityIndicator,
} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { storageService, apiService, authService } from '../../services';
import { AppSettings } from '../../models';

const SettingsScreen = ({ navigation }: any) => {
  const [settings, setSettings] = useState<AppSettings>({
    mode: 'local',
    theme: 'system',
    biometricEnabled: false,
    autoSync: true,
    syncInterval: 30,
  });
  const [apiUrl, setApiUrl] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testingConnection, setTestingConnection] = useState(false);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      const loadedSettings = await storageService.getSettings();
      setSettings(loadedSettings);
      setApiUrl(loadedSettings.apiUrl || '');
      setApiKey(loadedSettings.apiKey || '');
    } catch (error) {
      console.error('Failed to load settings:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    // Validation
    if (settings.mode === 'api') {
      if (!apiUrl) {
        Alert.alert('Error', 'Please enter API URL');
        return;
      }
      if (!apiKey) {
        Alert.alert('Error', 'Please enter API Key');
        return;
      }
    }

    setSaving(true);
    try {
      const updatedSettings: AppSettings = {
        ...settings,
        apiUrl: apiUrl || undefined,
        apiKey: apiKey || undefined,
      };

      await storageService.saveSettings(updatedSettings);

      // Update API service if in API mode
      if (settings.mode === 'api') {
        apiService.setBaseUrl(apiUrl);
      }

      Alert.alert('Success', 'Settings saved successfully');
      navigation.goBack();
    } catch (error: any) {
      Alert.alert('Error', error.message || 'Failed to save settings');
    } finally {
      setSaving(false);
    }
  };

  const handleTestConnection = async () => {
    if (!apiUrl) {
      Alert.alert('Error', 'Please enter API URL');
      return;
    }

    setTestingConnection(true);
    try {
      apiService.setBaseUrl(apiUrl);
      const isConnected = await apiService.checkConnectivity();

      if (isConnected) {
        Alert.alert('Success', 'Connection test successful!');
      } else {
        Alert.alert('Error', 'Failed to connect to API');
      }
    } catch (error: any) {
      Alert.alert('Connection Error', error.message);
    } finally {
      setTestingConnection(false);
    }
  };

  const handleEnableBiometric = async () => {
    if (!settings.biometricEnabled) {
      const success = await authService.setupBiometric();
      if (success) {
        setSettings({ ...settings, biometricEnabled: true });
        Alert.alert('Success', 'Biometric authentication enabled');
      } else {
        Alert.alert('Error', 'Failed to setup biometric authentication');
      }
    } else {
      setSettings({ ...settings, biometricEnabled: false });
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#007AFF" />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      <View style={styles.content}>
        {/* Database Mode Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Database Mode</Text>
          <Text style={styles.sectionDescription}>
            Choose how to store your password data
          </Text>

          <TouchableOpacity
            style={[
              styles.optionCard,
              settings.mode === 'local' && styles.optionCardSelected,
            ]}
            onPress={() => setSettings({ ...settings, mode: 'local' })}>
            <View style={styles.optionHeader}>
              <Icon
                name="database"
                size={24}
                color={settings.mode === 'local' ? '#007AFF' : '#666'}
              />
              <Text
                style={[
                  styles.optionTitle,
                  settings.mode === 'local' && styles.optionTitleSelected,
                ]}>
                Local SQLite Database
              </Text>
              {settings.mode === 'local' && (
                <Icon name="check-circle" size={24} color="#007AFF" />
              )}
            </View>
            <Text style={styles.optionDescription}>
              Store data locally on device. Works offline. No synchronization.
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.optionCard,
              settings.mode === 'api' && styles.optionCardSelected,
            ]}
            onPress={() => setSettings({ ...settings, mode: 'api' })}>
            <View style={styles.optionHeader}>
              <Icon
                name="cloud-sync"
                size={24}
                color={settings.mode === 'api' ? '#007AFF' : '#666'}
              />
              <Text
                style={[
                  styles.optionTitle,
                  settings.mode === 'api' && styles.optionTitleSelected,
                ]}>
                API with Cloud Sync
              </Text>
              {settings.mode === 'api' && (
                <Icon name="check-circle" size={24} color="#007AFF" />
              )}
            </View>
            <Text style={styles.optionDescription}>
              Connect to Password Manager API. Sync across devices. Requires internet.
            </Text>
          </TouchableOpacity>
        </View>

        {/* API Configuration (only shown when API mode is selected) */}
        {settings.mode === 'api' && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>API Configuration</Text>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>API URL</Text>
              <TextInput
                style={styles.input}
                placeholder="https://api.example.com"
                value={apiUrl}
                onChangeText={setApiUrl}
                autoCapitalize="none"
                keyboardType="url"
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>API Key</Text>
              <TextInput
                style={styles.input}
                placeholder="Enter your API key"
                value={apiKey}
                onChangeText={setApiKey}
                autoCapitalize="none"
                secureTextEntry
              />
            </View>

            <TouchableOpacity
              style={styles.testButton}
              onPress={handleTestConnection}
              disabled={testingConnection}>
              {testingConnection ? (
                <ActivityIndicator color="#007AFF" size="small" />
              ) : (
                <>
                  <Icon name="connection" size={20} color="#007AFF" />
                  <Text style={styles.testButtonText}>Test Connection</Text>
                </>
              )}
            </TouchableOpacity>
          </View>
        )}

        {/* Security Settings */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Security</Text>

          <View style={styles.settingRow}>
            <View style={styles.settingInfo}>
              <Icon name="fingerprint" size={24} color="#666" />
              <View style={styles.settingText}>
                <Text style={styles.settingLabel}>Biometric Authentication</Text>
                <Text style={styles.settingDescription}>
                  Use fingerprint or face recognition
                </Text>
              </View>
            </View>
            <Switch
              value={settings.biometricEnabled}
              onValueChange={handleEnableBiometric}
              trackColor={{ false: '#767577', true: '#007AFF' }}
            />
          </View>
        </View>

        {/* Appearance Settings */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Appearance</Text>

          <View style={styles.themeOptions}>
            {['light', 'dark', 'system'].map((theme) => (
              <TouchableOpacity
                key={theme}
                style={[
                  styles.themeOption,
                  settings.theme === theme && styles.themeOptionSelected,
                ]}
                onPress={() =>
                  setSettings({ ...settings, theme: theme as any })
                }>
                <Icon
                  name={
                    theme === 'light'
                      ? 'white-balance-sunny'
                      : theme === 'dark'
                      ? 'moon-waning-crescent'
                      : 'theme-light-dark'
                  }
                  size={24}
                  color={settings.theme === theme ? '#007AFF' : '#666'}
                />
                <Text
                  style={[
                    styles.themeLabel,
                    settings.theme === theme && styles.themeLabelSelected,
                  ]}>
                  {theme.charAt(0).toUpperCase() + theme.slice(1)}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        {/* Sync Settings */}
        {settings.mode === 'api' && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Synchronization</Text>

            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Icon name="sync" size={24} color="#666" />
                <View style={styles.settingText}>
                  <Text style={styles.settingLabel}>Auto Sync</Text>
                  <Text style={styles.settingDescription}>
                    Automatically sync data
                  </Text>
                </View>
              </View>
              <Switch
                value={settings.autoSync}
                onValueChange={(value) =>
                  setSettings({ ...settings, autoSync: value })
                }
                trackColor={{ false: '#767577', true: '#007AFF' }}
              />
            </View>
          </View>
        )}

        {/* Save Button */}
        <TouchableOpacity
          style={styles.saveButton}
          onPress={handleSave}
          disabled={saving}>
          {saving ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.saveButtonText}>Save Settings</Text>
          )}
        </TouchableOpacity>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  content: {
    padding: 16,
  },
  section: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000',
    marginBottom: 8,
  },
  sectionDescription: {
    fontSize: 14,
    color: '#666',
    marginBottom: 16,
  },
  optionCard: {
    borderWidth: 2,
    borderColor: '#e0e0e0',
    borderRadius: 8,
    padding: 16,
    marginBottom: 12,
  },
  optionCardSelected: {
    borderColor: '#007AFF',
    backgroundColor: '#f0f8ff',
  },
  optionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  optionTitle: {
    flex: 1,
    fontSize: 16,
    fontWeight: '600',
    color: '#000',
    marginLeft: 12,
  },
  optionTitleSelected: {
    color: '#007AFF',
  },
  optionDescription: {
    fontSize: 13,
    color: '#666',
    marginLeft: 36,
  },
  inputGroup: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    color: '#000',
    marginBottom: 8,
  },
  input: {
    height: 50,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    paddingHorizontal: 12,
    fontSize: 16,
    backgroundColor: '#f9f9f9',
  },
  testButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: 44,
    borderWidth: 1,
    borderColor: '#007AFF',
    borderRadius: 8,
    marginTop: 8,
  },
  testButtonText: {
    color: '#007AFF',
    fontSize: 15,
    fontWeight: '600',
    marginLeft: 8,
  },
  settingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 8,
  },
  settingInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  settingText: {
    marginLeft: 12,
    flex: 1,
  },
  settingLabel: {
    fontSize: 16,
    fontWeight: '500',
    color: '#000',
  },
  settingDescription: {
    fontSize: 13,
    color: '#666',
    marginTop: 2,
  },
  themeOptions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  themeOption: {
    flex: 1,
    alignItems: 'center',
    padding: 16,
    borderWidth: 2,
    borderColor: '#e0e0e0',
    borderRadius: 8,
    marginHorizontal: 4,
  },
  themeOptionSelected: {
    borderColor: '#007AFF',
    backgroundColor: '#f0f8ff',
  },
  themeLabel: {
    fontSize: 13,
    color: '#666',
    marginTop: 8,
  },
  themeLabelSelected: {
    color: '#007AFF',
    fontWeight: '600',
  },
  saveButton: {
    backgroundColor: '#007AFF',
    height: 50,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 8,
    marginBottom: 32,
  },
  saveButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default SettingsScreen;
