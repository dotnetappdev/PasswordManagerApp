using System;

namespace VaultGuard.WPF.Services
{
    /// <summary>
    /// Lightweight app-wide notifications so pages can react to data changes made elsewhere
    /// (e.g. clearing seed data from Settings should refresh the live items / dashboard views).
    /// </summary>
    public static class AppEvents
    {
        /// <summary>Raised after vault data (items, categories, collections, tags) changes in bulk.</summary>
        public static event Action? VaultDataChanged;

        public static void RaiseVaultDataChanged() => VaultDataChanged?.Invoke();
    }
}
