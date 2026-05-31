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
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { passwordItemService, authService } from '../../services';

const DashboardScreen = ({ navigation }: any) => {
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
        weakPasswords: 0, // TODO: Implement weak password detection
        passkeys: 2,      // TODO: Load from passkey service
      });
    } catch (error) {
      console.error('Failed to load dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#7C3AED" />
      </View>
    );
  }

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={{ paddingBottom: insets.bottom + 24 }}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerBadge}>
          <Icon name="shield-lock" size={20} color="#7C3AED" />
          <Text style={styles.headerBadgeText}>VaultGuard</Text>
        </View>
        <Text style={styles.greeting}>Welcome back,</Text>
        <Text style={styles.userName}>{user?.userName || 'User'}</Text>
      </View>

      {/* Stats Grid */}
      <View style={styles.statsGrid}>
        <View style={styles.statCard}>
          <Icon name="key" size={28} color="#7C3AED" />
          <Text style={styles.statValue}>{stats.totalItems}</Text>
          <Text style={styles.statLabel}>Total Items</Text>
        </View>

        <View style={styles.statCard}>
          <Icon name="star" size={28} color="#F59E0B" />
          <Text style={styles.statValue}>{stats.favorites}</Text>
          <Text style={styles.statLabel}>Favorites</Text>
        </View>

        <View style={styles.statCard}>
          <Icon name="fingerprint" size={28} color="#10B981" />
          <Text style={styles.statValue}>{stats.passkeys}</Text>
          <Text style={styles.statLabel}>Passkeys</Text>
        </View>

        <View style={styles.statCard}>
          <Icon name="alert-circle" size={28} color="#EF4444" />
          <Text style={styles.statValue}>{stats.weakPasswords}</Text>
          <Text style={styles.statLabel}>Weak</Text>
        </View>
      </View>

      {/* Quick Actions */}
      <Text style={styles.sectionTitle}>Quick Actions</Text>
      <View style={styles.quickActionsGrid}>
        <TouchableOpacity
          style={styles.quickAction}
          onPress={() => navigation.navigate('Passwords')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#7C3AED22' }]}>
            <Icon name="key-plus" size={24} color="#7C3AED" />
          </View>
          <Text style={styles.quickActionLabel}>Add Password</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.quickAction}
          onPress={() => navigation.navigate('PasswordHealth')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#10B98122' }]}>
            <Icon name="shield-check" size={24} color="#10B981" />
          </View>
          <Text style={styles.quickActionLabel}>Password Health</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.quickAction}
          onPress={() => navigation.navigate('Passkeys')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#F59E0B22' }]}>
            <Icon name="fingerprint" size={24} color="#F59E0B" />
          </View>
          <Text style={styles.quickActionLabel}>Passkeys</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.quickAction}
          onPress={() => navigation.navigate('SecureSend')}>
          <View style={[styles.quickActionIcon, { backgroundColor: '#3B82F622' }]}>
            <Icon name="share-variant" size={24} color="#3B82F6" />
          </View>
          <Text style={styles.quickActionLabel}>Secure Send</Text>
        </TouchableOpacity>
      </View>

      {/* Security Health Banner */}
      <TouchableOpacity
        style={styles.healthBanner}
        onPress={() => navigation.navigate('PasswordHealth')}>
        <View style={styles.healthBannerLeft}>
          <Icon name="shield-check" size={32} color="#10B981" />
          <View style={styles.healthBannerText}>
            <Text style={styles.healthBannerTitle}>Security Score: 72/100</Text>
            <Text style={styles.healthBannerSub}>Tap to view recommendations</Text>
          </View>
        </View>
        <Icon name="chevron-right" size={20} color="#8B90A7" />
      </TouchableOpacity>

      {/* Settings */}
      <TouchableOpacity
        style={styles.settingsButton}
        onPress={() => navigation.navigate('Settings')}>
        <Icon name="cog-outline" size={20} color="#8B90A7" />
        <Text style={styles.settingsButtonText}>Settings</Text>
        <Icon name="chevron-right" size={16} color="#8B90A7" style={styles.settingsChevron} />
      </TouchableOpacity>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0F1117',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#0F1117',
  },
  header: {
    backgroundColor: '#1A1D27',
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
    color: '#7C3AED',
    fontWeight: '700',
    fontSize: 13,
    marginLeft: 6,
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  greeting: {
    fontSize: 15,
    color: '#8B90A7',
  },
  userName: {
    fontSize: 26,
    fontWeight: '700',
    color: '#F0F2FF',
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
    backgroundColor: '#1A1D27',
    borderRadius: 14,
    padding: 16,
    alignItems: 'flex-start',
  },
  statValue: {
    fontSize: 28,
    fontWeight: '700',
    color: '#F0F2FF',
    marginTop: 10,
    marginBottom: 2,
  },
  statLabel: {
    fontSize: 12,
    color: '#8B90A7',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#F0F2FF',
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
    backgroundColor: '#1A1D27',
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
    color: '#F0F2FF',
    fontSize: 13,
    fontWeight: '500',
    textAlign: 'center',
  },
  healthBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#1A1D27',
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
    color: '#F0F2FF',
    fontWeight: '600',
    fontSize: 14,
  },
  healthBannerSub: {
    color: '#8B90A7',
    fontSize: 12,
    marginTop: 2,
  },
  settingsButton: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1A1D27',
    borderRadius: 14,
    marginHorizontal: 16,
    padding: 16,
  },
  settingsButtonText: {
    color: '#8B90A7',
    fontSize: 15,
    marginLeft: 10,
    flex: 1,
  },
  settingsChevron: {
    marginLeft: 'auto',
  },
});

export default DashboardScreen;
