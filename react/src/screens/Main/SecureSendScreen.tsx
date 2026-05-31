import React, { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

interface SendItem {
  id: string;
  name: string;
  type: string;
  expiry: string;
  views: string;
  active: boolean;
}

export default function SecureSendScreen() {
  const insets = useSafeAreaInsets();
  const [sends, setSends] = useState<SendItem[]>([
    { id: '1', name: 'WiFi Password', type: 'Text', expiry: '7 days', views: '2/5', active: true },
    { id: '2', name: 'Recovery Code', type: 'Text', expiry: 'Expired', views: '5/5', active: false },
  ]);

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: insets.bottom + 20 }}>
      <View style={styles.header}>
        <Text style={styles.title}>Secure Send</Text>
        <TouchableOpacity style={styles.createBtn}
          onPress={() => Alert.alert('Create Send', 'Feature available in the web app. Open VaultGuard in your browser to create a secure send link.')}>
          <Text style={styles.createBtnText}>+ Create</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.infoBanner}>
        <Text style={styles.infoText}>
          🔗 Create one-time encrypted links to share passwords or text securely. Links expire automatically.
        </Text>
      </View>

      {sends.map(send => (
        <View key={send.id} style={[styles.sendCard, !send.active && styles.sendCardExpired]}>
          <View style={styles.sendIcon}>
            <Text style={{ fontSize: 22 }}>{send.type === 'Text' ? '📝' : '🔑'}</Text>
          </View>
          <View style={styles.sendInfo}>
            <Text style={styles.sendName}>{send.name}</Text>
            <Text style={styles.sendMeta}>{send.type} · {send.views} views · {send.expiry}</Text>
          </View>
          <View style={[styles.statusBadge, { backgroundColor: send.active ? '#10B98133' : '#EF444433' }]}>
            <Text style={{ color: send.active ? '#10B981' : '#EF4444', fontSize: 12, fontWeight: '600' }}>
              {send.active ? 'Active' : 'Expired'}
            </Text>
          </View>
        </View>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0F1117' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', margin: 16, marginBottom: 8 },
  title: { color: '#F0F2FF', fontSize: 20, fontWeight: '700' },
  createBtn: { backgroundColor: '#7C3AED', paddingHorizontal: 16, paddingVertical: 8, borderRadius: 20 },
  createBtnText: { color: '#fff', fontWeight: '600' },
  infoBanner: { marginHorizontal: 16, marginBottom: 16, backgroundColor: '#1A1D27', borderRadius: 12, padding: 14 },
  infoText: { color: '#8B90A7', fontSize: 13, lineHeight: 20 },
  sendCard: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#1A1D27', marginHorizontal: 16, marginBottom: 10, borderRadius: 12, padding: 14 },
  sendCardExpired: { opacity: 0.6 },
  sendIcon: { width: 44, height: 44, backgroundColor: '#0F1117', borderRadius: 10, justifyContent: 'center', alignItems: 'center', marginRight: 12 },
  sendInfo: { flex: 1 },
  sendName: { color: '#F0F2FF', fontWeight: '600', fontSize: 14 },
  sendMeta: { color: '#8B90A7', fontSize: 12, marginTop: 2 },
  statusBadge: { paddingHorizontal: 10, paddingVertical: 4, borderRadius: 12 },
});
