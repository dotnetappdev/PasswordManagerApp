/**
 * Dashboard Screen
 * Main dashboard with overview and quick actions
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  useColorScheme,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { passwordItemService, authService } from '../../services';

const DARK = {
  background: '#0F1117',
  surface: '#1A1D27',
  text: '#F0F2FF',
  textSecondary: '#8B90A7',
  primary: '#7C3AED',
};

const LIGHT = {
  background: '#F7F5FF',
  surface: '#FFFFFF',
  text: '#1F1535',
  textSecondary: '#5B21B6',
  primary: '#7C3AED',
};

const DashboardScreen = ({ navigation }: any) => {
  const colorScheme = useColorScheme();
  const C = colorScheme === 'dark' ? DARK : LIGHT;
  const insets = useSafeAreaInsets();
  const [stats, setStats] = useState({
    totalItems: 0,
    favorites: 0,
    weakPasswords: 0,
    passkeys: 0,
  });
  const [loading, setLoading] = useState(true);
  const [user, setUser] = useState<any>(null);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      const currentUser = authService.getCurrentUser();
      setUser(currentUser);

      const items = await passwordItemService.getAll();
      setStats({
        totalItems: items.length,
        favorites: items.filter((item) => item.isFavorite).length,
        weakPasswords: 0,
        passkeys: 2,
      });
    } catch (error) {
      console.error('Failed to load dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: C.background }]}>
        <ActivityIndicator size="large" color={C.primary} />
      </View>
    );
  }

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: C.background }]}
      contentContainerStyle={{ paddingBottom: insets.bottom + 24 }}>
      {/* Header */}
      <View style={[styles.header, { backgroundColor: C.surface }]}>
        <View style={styles.headerBadge}>
          <Icon name="shield-lock" size={20} color={C.primary} />
          <Text style={[styles.headerBadgeText, { color: C.primary }]}>VaultGuard</Text>
        </View>
        <Text style={[styles.greeting, { color: C.textSecondary }]}>Welcome back,</Text>
        <Text style={[styles.userName, { color: C.text }]}>{user?.userName || 'User'}</Text>
      </View>

      {/* Stats Grid */}
      <View style={styles.statsGrid}>
        <View style={[styles.statCard, { backgroundColor: C.surface }]}>
          <Icon name="key" size={28} color="#7C3AED" />
          <Text style={[styles.statValue, { color: C.text }]}>{stats.totalItems}</Text>
          <Text style={[styles.statLabel, { color: C.textSecondary }]}>Total Items</Text>
        </View>

        <View style={[styles.statCard, { backgroundColor: C.surface }]}>
          <Icon name="star" size={28} color="#F59E0B" />
          <Text style={[styles.statValue, { color: C.text }]}>{stats.favorites}</Text>
          <Text style={[styles.statLabel, { color: C.textSecondary }]}>Favorites</Text>
        </View>

        <View style={[styles.statCard, { backgroundColor: C.surface }]}>
          <Icon name="fingerprint" size={28} color="#10B981" />
          <Text style={[styles.statValue, { color: C.text }]}>{stats.passkeys}</Text>
          <Text style={[styles.statLabel, { color: C.textSecondary }]}>Passkeys</Text>
        </View>

        <View style={[styles.statCard, { backgroundColor: C.surface }]}>
          <Icon name="alert-circle" size={28} color="#EF4444" />
          <Text style={[styles.statValue, { color: C.text }]}>{stats.weakPasswords}</Text>
          <Text style={[styles.statLabel, { color: C.textSecondary }]}>Weak</Text>
        </View>
      </View>

      {/* Quick Actions */}
      <Text style={[styles.sectionTitle, { color: C.text }]}>Quick Actions</Text>
      <View style={styles.quickActionsGrid}>
        <TouchableOpacity
          style={[styles.quickAction, { backgroundColor: C.surface }]}
          onPress={() => navigation.navigate('Passwords')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#7C3AED22' }]}>
            <Icon name="key-plus" size={24} color="#7C3AED" />
          </View>
          <Text style={[styles.quickActionLabel, { color: C.text }]}>Add Password</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.quickAction, { backgroundColor: C.surface }]}
          onPress={() => navigation.navigate('PasswordHealth')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#10B98122' }]}>
            <Icon name="shield-check" size={24} color="#10B981" />
          </View>
          <Text style={[styles.quickActionLabel, { color: C.text }]}>Password Health</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.quickAction, { backgroundColor: C.surface }]}
          onPress={() => navigation.navigate('Passkeys')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#F59E0B22' }]}>
            <Icon name="fingerprint" size={24} color="#F59E0B" />
          </View>
          <Text style={[styles.quickActionLabel, { color: C.text }]}>Passkeys</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.quickAction, { backgroundColor: C.surface }]}
          onPress={() => navigation.navigate('SecureSend')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#3B82F622' }]}>
            <Icon name="share-variant" size={24} color="#3B82F6" />
          </View>
          <Text style={[styles.quickActionLabel, { color: C.text }]}>Secure Send</Text>
        </TouchableOpacity>
      </View>

      {/* Security Health Banner */}
      <TouchableOpacity
        style={[styles.healthBanner, { backgroundColor: C.surface }]}
        onPress={() => navigation.navigate('PasswordHealth')}>
        <View style={styles.healthBannerLeft}>
          <Icon name="shield-check" size={32} color="#10B981" />
          <View style={styles.healthBannerText}>
            <Text style={[styles.healthBannerTitle, { color: C.text }]}>Security Score: 72/100</Text>
            <Text style={[styles.healthBannerSub, { color: C.textSecondary }]}>Tap to view recommendations</Text>
          </View>
        </View>
        <Icon name="chevron-right" size={20} color={C.textSecondary} />
      </TouchableOpacity>

      {/* Settings */}
      <TouchableOpacity
        style={[styles.settingsButton, { backgroundColor: C.surface }]}
        onPress={() => navigation.navigate('Settings')}>
        <Icon name="cog-outline" size={20} color={C.textSecondary} />
        <Text style={[styles.settingsButtonText, { color: C.textSecondary }]}>Settings</Text>
        <Icon name="chevron-right" size={16} color={C.textSecondary} style={styles.settingsChevron} />
      </TouchableOpacity>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    padding: 24,
    paddingTop: 32,
    borderBottomLeftRadius: 24,
    borderBottomRightRadius: 24,
    marginBottom: 20,
  },
  headerBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  headerBadgeText: {
    fontWeight: '700',
    fontSize: 13,
    marginLeft: 6,
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  greeting: {
    fontSize: 15,
  },
  userName: {
    fontSize: 26,
    fontWeight: '700',
    marginTop: 2,
  },
  statsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: 16,
    gap: 12,
    marginBottom: 24,
  },
  statCard: {
    width: '47%',
    borderRadius: 14,
    padding: 16,
    alignItems: 'flex-start',
  },
  statValue: {
    fontSize: 28,
    fontWeight: '700',
    marginTop: 10,
    marginBottom: 2,
  },
  statLabel: {
    fontSize: 12,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginHorizontal: 16,
    marginBottom: 14,
  },
  quickActionsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: 16,
    gap: 12,
    marginBottom: 24,
  },
  quickAction: {
    width: '47%',
    borderRadius: 14,
    padding: 16,
    alignItems: 'center',
  },
  quickActionIcon: {
    width: 52,
    height: 52,
    borderRadius: 14,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 10,
  },
  quickActionLabel: {
    fontSize: 13,
    fontWeight: '500',
    textAlign: 'center',
  },
  healthBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderRadius: 14,
    marginHorizontal: 16,
    padding: 16,
    marginBottom: 14,
    borderLeftWidth: 4,
    borderLeftColor: '#10B981',
  },
  healthBannerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  healthBannerText: {
    marginLeft: 14,
    flex: 1,
  },
  healthBannerTitle: {
    fontWeight: '600',
    fontSize: 14,
  },
  healthBannerSub: {
    fontSize: 12,
    marginTop: 2,
  },
  settingsButton: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 14,
    marginHorizontal: 16,
    padding: 16,
  },
  settingsButtonText: {
    fontSize: 15,
    marginLeft: 10,
    flex: 1,
  },
  settingsChevron: {
    marginLeft: 'auto',
  },
});

export default DashboardScreen;
