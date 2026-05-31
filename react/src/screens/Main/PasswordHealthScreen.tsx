import React, { useState, useEffect } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity, ActivityIndicator
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

// Types
interface HealthItem {
  id: string;
  title: string;
  username?: string;
  lastChanged?: Date;
  hasTotp: boolean;
}

export default function PasswordHealthScreen() {
  const insets = useSafeAreaInsets();
  const [loading, setLoading] = useState(true);
  const [score, setScore] = useState(0);
  const [loginItems, setLoginItems] = useState<HealthItem[]>([]);
  const [needsUpdate, setNeedsUpdate] = useState<HealthItem[]>([]);
  const [has2FA, setHas2FA] = useState<HealthItem[]>([]);
  const [activeTab, setActiveTab] = useState<'needs-update' | 'no-2fa' | 'healthy'>('needs-update');

  useEffect(() => {
    // TODO: Load from local SQLite/API
    setTimeout(() => {
      setScore(72);
      setLoading(false);
    }, 500);
  }, []);

  const scoreColor = score >= 80 ? '#10B981' : score >= 50 ? '#F59E0B' : '#EF4444';

  if (loading) {
    return (
      <View style={[styles.center, { paddingTop: insets.top }]}>
        <ActivityIndicator size="large" color="#7C3AED" />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: insets.bottom + 20 }}>
      {/* Score Card */}
      <View style={styles.scoreCard}>
        <Text style={styles.scoreTitle}>Security Score</Text>
        <View style={[styles.scoreBadge, { borderColor: scoreColor }]}>
          <Text style={[styles.scoreNumber, { color: scoreColor }]}>{score}</Text>
          <Text style={[styles.scoreMax, { color: scoreColor }]}>/100</Text>
        </View>
        <Text style={styles.scoreDesc}>
          {score >= 80 ? '🟢 Your vault is well protected' :
           score >= 50 ? '🟡 Some improvements needed' :
           '🔴 Immediate action recommended'}
        </Text>
      </View>

      {/* Stats Row */}
      <View style={styles.statsRow}>
        {[
          { label: 'Total Logins', value: '42', color: '#7C3AED' },
          { label: 'Need Update', value: '14', color: '#F59E0B' },
          { label: 'Have 2FA', value: '19', color: '#10B981' },
        ].map((stat, i) => (
          <View key={i} style={[styles.statCard, { borderTopColor: stat.color }]}>
            <Text style={[styles.statValue, { color: stat.color }]}>{stat.value}</Text>
            <Text style={styles.statLabel}>{stat.label}</Text>
          </View>
        ))}
      </View>

      {/* Tabs */}
      <View style={styles.tabs}>
        {(['needs-update', 'no-2fa', 'healthy'] as const).map(tab => (
          <TouchableOpacity
            key={tab}
            style={[styles.tab, activeTab === tab && styles.tabActive]}
            onPress={() => setActiveTab(tab)}
          >
            <Text style={[styles.tabText, activeTab === tab && styles.tabTextActive]}>
              {tab === 'needs-update' ? '⚠️ Needs Update' :
               tab === 'no-2fa' ? '🔓 No 2FA' : '✅ Healthy'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Empty state */}
      <View style={styles.emptyState}>
        <Text style={styles.emptyIcon}>
          {activeTab === 'needs-update' ? '⏰' : activeTab === 'no-2fa' ? '🔐' : '✅'}
        </Text>
        <Text style={styles.emptyText}>
          {activeTab === 'needs-update' ? 'Connect your vault to see passwords that need updating' :
           activeTab === 'no-2fa' ? 'Connect your vault to see accounts without 2FA' :
           'Connect your vault to see healthy passwords'}
        </Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0F1117' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#0F1117' },
  scoreCard: {
    margin: 16, padding: 24, backgroundColor: '#1A1D27',
    borderRadius: 16, alignItems: 'center',
  },
  scoreTitle: { color: '#8B90A7', fontSize: 14, marginBottom: 12 },
  scoreBadge: {
    width: 100, height: 100, borderRadius: 50,
    borderWidth: 6, justifyContent: 'center', alignItems: 'center',
    flexDirection: 'row', marginBottom: 12,
  },
  scoreNumber: { fontSize: 36, fontWeight: '700' },
  scoreMax: { fontSize: 16, fontWeight: '400', alignSelf: 'flex-end', marginBottom: 6 },
  scoreDesc: { color: '#F0F2FF', fontSize: 14, textAlign: 'center' },
  statsRow: { flexDirection: 'row', marginHorizontal: 16, gap: 10, marginBottom: 16 },
  statCard: {
    flex: 1, backgroundColor: '#1A1D27', borderRadius: 12,
    padding: 14, borderTopWidth: 3, alignItems: 'center',
  },
  statValue: { fontSize: 24, fontWeight: '700', marginBottom: 4 },
  statLabel: { color: '#8B90A7', fontSize: 12, textAlign: 'center' },
  tabs: { flexDirection: 'row', marginHorizontal: 16, marginBottom: 16, gap: 8 },
  tab: {
    flex: 1, paddingVertical: 8, paddingHorizontal: 4,
    borderRadius: 20, backgroundColor: '#1A1D27', alignItems: 'center',
  },
  tabActive: { backgroundColor: '#7C3AED' },
  tabText: { color: '#8B90A7', fontSize: 11, textAlign: 'center' },
  tabTextActive: { color: '#fff', fontWeight: '600' },
  emptyState: { margin: 32, alignItems: 'center' },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { color: '#8B90A7', fontSize: 14, textAlign: 'center', lineHeight: 20 },
});
