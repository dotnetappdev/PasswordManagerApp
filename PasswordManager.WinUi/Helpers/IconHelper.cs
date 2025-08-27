using System.Collections.Generic;

namespace PasswordManager.WinUi.Helpers
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
        public static readonly Dictionary<Models.ItemType, string> TypeToIcon = new()
        {
            { Models.ItemType.Login, "key" },
            { Models.ItemType.CreditCard, "creditcard" },
            { Models.ItemType.SecureNote, "note" },
            { Models.ItemType.WiFi, "wifi" },
            { Models.ItemType.Passkey, "security" }
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

        public static string GetIconForType(Models.ItemType type)
        {
            return TypeToIcon.ContainsKey(type) ? TypeToIcon[type] : "folder";
        }

        public static string GetEmojiForType(Models.ItemType type)
        {
            var iconId = GetIconForType(type);
            return GetEmojiForIcon(iconId);
        }

        public static string GetGlyphForType(Models.ItemType type)
        {
            var iconId = GetIconForType(type);
            return GetGlyphForIcon(iconId);
        }
    }
}