using System.Collections.Generic;
using VaultGuard.Models;

namespace VaultGuard.WinUi.Helpers
{
    public static class IconHelper
    {
        // Maps icon identifier to emoji for display
        public static readonly Dictionary<string, string> IconToEmoji = new()
        {
            { "folder", "📁" },
            { "key", "🔑" },
            { "creditcard", "💳" },
            { "note", "📝" },
            { "wifi", "📶" },
            { "security", "🔒" },
            { "star", "⭐" },
            { "list", "📋" },
            { "login", "🔑" },
            { "password", "🔒" }
        };

        // Maps icon identifier to FontIcon glyph for UI
        public static readonly Dictionary<string, string> IconToGlyph = new()
        {
            { "folder", "\uE8FD" },  // Folder glyph
            { "key", "\uE192" },     // Key glyph
            { "creditcard", "\uE8C7" }, // Credit card glyph
            { "note", "\uE70F" },    // Note glyph
            { "wifi", "\uE701" },    // WiFi glyph
            { "security", "\uE8D7" }, // Security glyph
            { "star", "\uE734" },    // Star glyph
            { "list", "\uE8A5" },    // List glyph
            { "login", "\uE192" },   // Login (key) glyph
            { "password", "\uE8D7" } // Password (security) glyph
        };

        // Maps ItemType to icon identifier
        public static readonly Dictionary<ItemType, string> TypeToIcon = new()
        {
            { ItemType.Login, "key" },
            { ItemType.CreditCard, "creditcard" },
            { ItemType.SecureNote, "note" },
            { ItemType.WiFi, "wifi" },
            { ItemType.Passkey, "security" }
        };

        public static string GetEmojiForIcon(string? iconId)
        {
            if (string.IsNullOrEmpty(iconId))
                return IconToEmoji["folder"];
            
            return IconToEmoji.ContainsKey(iconId) ? IconToEmoji[iconId] : IconToEmoji["folder"];
        }

        public static string GetGlyphForIcon(string? iconId)
        {
            if (string.IsNullOrEmpty(iconId))
                return IconToGlyph["folder"];
            
            return IconToGlyph.ContainsKey(iconId) ? IconToGlyph[iconId] : IconToGlyph["folder"];
        }

        public static string GetIconForType(ItemType type)
        {
            return TypeToIcon.ContainsKey(type) ? TypeToIcon[type] : "folder";
        }

        public static string GetEmojiForType(ItemType type)
        {
            var iconId = GetIconForType(type);
            return GetEmojiForIcon(iconId);
        }

        public static string GetGlyphForType(ItemType type)
        {
            var iconId = GetIconForType(type);
            return GetGlyphForIcon(iconId);
        }
    }
}