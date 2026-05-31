import React, { useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

interface PasskeyItem {
  id: string;
  name: string;
  website: string;
  device: string;
  addedAt: Date;
  isBackedUp: boolean;
}

const MOCK_PASSKEYS: PasskeyItem[] = [
  { id: '1', name: 'iPhone 15 Pro', website: 'google.com', device: 'iPhone', addedAt: new Date(), isBackedUp: true },
  { id: '2', name: 'MacBook Touch ID', website: 'github.com', device: 'Mac', addedAt: new Date(), isBackedUp: true },
];

export default function PasskeysScreen() {
  const insets = useSafeAreaInsets();
  const [passkeys, setPasskeys] = useState<PasskeyItem[]>(MOCK_PASSKEYS);

  const handleDelete = (id: string) => {
    Alert.alert('Delete Passkey', 'Are you sure you want to remove this passkey?', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete', style: 'destructive',
        onPress: () => setPasskeys(p => p.filter(pk => pk.id !== id))
      },
    ]);
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: insets.bottom + 20 }}>
      {/* Info Banner */}
      <View style={styles.infoBanner}>
        <Text style={styles.infoIcon}>🔑</Text>
        <View style={styles.infoText}>
          <Text style={styles.infoTitle}>Passkeys — The Future of Auth</Text>
          <Text style={styles.infoDesc}>
            Passkeys use biometrics (Face ID, fingerprint) instead of passwords.
            They're phishing-resistant and work across all your devices.
          </Text>
        </View>
      </View>

      {/* Stats */}
      <View style={styles.statsRow}>
        <View style={styles.statCard}>
          <Text style={styles.statValue}>{passkeys.length}</Text>
          <Text style={styles.statLabel}>Total Passkeys</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statValue}>{passkeys.filter(p => p.isBackedUp).length}</Text>
          <Text style={styles.statLabel}>Backed Up</Text>
        </View>
      </View>

      {/* Passkey List */}
      <Text style={styles.sectionTitle}>My Passkeys</Text>
      {passkeys.map(pk => (
        <View key={pk.id} style={styles.passkeyCard}>
          <View style={styles.passkeyIcon}>
            <Text style={{ fontSize: 24 }}>
              {pk.device === 'iPhone' ? '📱' : pk.device === 'Mac' ? '💻' : '🔑'}
            </Text>
          </View>
          <View style={styles.passkeyInfo}>
            <Text style={styles.passkeyName}>{pk.name}</Text>
            <Text style={styles.passkeyWebsite}>{pk.website}</Text>
            <Text style={styles.passkeyDate}>
              {pk.isBackedUp ? '☁️ iCloud backed up' : '📱 Device only'}
            </Text>
          </View>
          <TouchableOpacity onPress={() => handleDelete(pk.id)} style={styles.deleteBtn}>
            <Text style={styles.deleteBtnText}>✕</Text>
          </TouchableOpacity>
        </View>
      ))}

      {/* Register New */}
      <TouchableOpacity style={styles.addButton}
        onPress={() => Alert.alert('Register Passkey', 'Open VaultGuard in your browser to register a new passkey for this device.')}>
        <Text style={styles.addButtonText}>+ Register New Passkey</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0F1117' },
  infoBanner: {
    flexDirection: 'row', margin: 16,
    backgroundColor: '#1A1D27', borderRadius: 12,
    padding: 16, borderLeftWidth: 4, borderLeftColor: '#7C3AED',
  },
  infoIcon: { fontSize: 28, marginRight: 12 },
  infoText: { flex: 1 },
  infoTitle: { color: '#F0F2FF', fontWeight: '600', fontSize: 14, marginBottom: 4 },
  infoDesc: { color: '#8B90A7', fontSize: 12, lineHeight: 18 },
  statsRow: { flexDirection: 'row', marginHorizontal: 16, gap: 12, marginBottom: 16 },
  statCard: {
    flex: 1, backgroundColor: '#1A1D27', borderRadius: 12,
    padding: 16, alignItems: 'center',
  },
  statValue: { color: '#7C3AED', fontSize: 28, fontWeight: '700', marginBottom: 4 },
  statLabel: { color: '#8B90A7', fontSize: 12 },
  sectionTitle: { color: '#F0F2FF', fontWeight: '600', fontSize: 16, marginHorizontal: 16, marginBottom: 12 },
  passkeyCard: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: '#1A1D27', marginHorizontal: 16, marginBottom: 10,
    borderRadius: 12, padding: 16,
  },
  passkeyIcon: {
    width: 48, height: 48, backgroundColor: '#0F1117',
    borderRadius: 12, justifyContent: 'center', alignItems: 'center', marginRight: 12,
  },
  passkeyInfo: { flex: 1 },
  passkeyName: { color: '#F0F2FF', fontWeight: '600', fontSize: 15 },
  passkeyWebsite: { color: '#8B90A7', fontSize: 13, marginTop: 2 },
  passkeyDate: { color: '#7C3AED', fontSize: 12, marginTop: 4 },
  deleteBtn: { padding: 8 },
  deleteBtnText: { color: '#EF4444', fontSize: 18, fontWeight: '700' },
  addButton: {
    margin: 16, backgroundColor: '#7C3AED', borderRadius: 12,
    padding: 16, alignItems: 'center',
  },
  addButtonText: { color: '#fff', fontWeight: '600', fontSize: 15 },
});
