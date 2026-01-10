/**
 * Vaults Screen
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

const VaultsScreen = () => {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>Vaults Screen</Text>
      <Text style={styles.subtext}>Coming soon...</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f5f5f5',
  },
  text: {
    fontSize: 20,
    fontWeight: '600',
    color: '#000',
  },
  subtext: {
    fontSize: 14,
    color: '#666',
    marginTop: 8,
  },
});

export default VaultsScreen;
