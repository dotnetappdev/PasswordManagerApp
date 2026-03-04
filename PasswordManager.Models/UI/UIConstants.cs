namespace PasswordManager.Models.UI
{
    /// <summary>
    /// Centralized UI constants for consistent design across all platforms
    /// (WinUI, Uno, Blazor, iOS, Android, Browser Extension)
    /// </summary>
    public static class UIConstants
    {
        /// <summary>
        /// Color palette for consistent theming
        /// </summary>
        public static class Colors
        {
            // Primary Colors
            public const string PrimaryBlue = "#005BFF";
            public const string PrimaryBlueLight = "#3B82F6";
            public const string PrimaryBlueDark = "#0041CC";

            // Secondary/Accent Colors (violet – updated from pink)
            public const string AccentViolet = "#7C3AED";
            public const string AccentVioletLight = "#A78BFA";
            // Legacy aliases kept for backward compatibility (previously pink, now violet)
            [System.Obsolete("Use AccentViolet instead.")]
            public const string AccentPink = "#7C3AED";
            [System.Obsolete("Use AccentVioletLight instead.")]
            public const string AccentPinkLight = "#A78BFA";

            // Status Colors
            public const string SuccessGreen = "#10B981";
            public const string WarningOrange = "#F59E0B";
            public const string ErrorRed = "#EF4444";
            public const string InfoBlue = "#3B82F6";

            // Background Colors - Dark Theme (deepened for richer feel)
            public const string BackgroundDark = "#0F1117";
            public const string SurfaceDark = "#1A1D27";
            public const string CardDark = "#1A1D27";
            public const string ElevatedSurfaceDark = "#232635";
            public const string OverlayDark = "#2C2F42";
            public const string BorderDark = "#FFFFFF14";   // rgba(255,255,255,0.08)
            public const string SidebarDark = "#0B0D14";

            // Background Colors - Light Theme
            public const string BackgroundLight = "#F0F2FF";
            public const string SurfaceLight = "#FFFFFF";
            public const string CardLight = "#FFFFFF";
            public const string ElevatedSurfaceLight = "#F5F7FF";
            public const string BorderLight = "rgba(0,0,0,0.08)";

            // Text Colors - Dark Theme
            public const string TextPrimaryDark = "#F0F2FF";
            public const string TextSecondaryDark = "#8B90A7";
            public const string TextTertiaryDark = "#5A5F78";

            // Text Colors - Light Theme
            public const string TextPrimaryLight = "#0F1117";
            public const string TextSecondaryLight = "#5A5F78";
            public const string TextTertiaryLight = "#8B90A7";

            // Special Purpose Colors
            public const string NewItemButton = "#005BFF";
        }

        /// <summary>
        /// Spacing constants for consistent layouts
        /// </summary>
        public static class Spacing
        {
            public const int XSmall = 4;
            public const int Small = 8;
            public const int Medium = 12;
            public const int Large = 16;
            public const int XLarge = 20;
            public const int XXLarge = 24;
            public const int Huge = 32;
            public const int XHuge = 48;
        }

        /// <summary>
        /// Border radius constants for consistent rounded corners
        /// </summary>
        public static class BorderRadius
        {
            public const int Small = 4;
            public const int Medium = 6;
            public const int Large = 8;
            public const int XLarge = 12;
            public const int XXLarge = 16;
            public const int Round = 20;
        }

        /// <summary>
        /// Typography constants
        /// </summary>
        public static class Typography
        {
            // Font Sizes
            public const int FontSizeSmall = 12;
            public const int FontSizeBody = 14;
            public const int FontSizeMedium = 15;
            public const int FontSizeLarge = 16;
            public const int FontSizeHeading6 = 18;
            public const int FontSizeHeading5 = 20;
            public const int FontSizeHeading4 = 24;
            public const int FontSizeHeading3 = 28;
            public const int FontSizeHeading2 = 32;
            public const int FontSizeHeading1 = 36;

            // Font Families (Inter first for professional SaaS look)
            public const string FontFamilyDefault = "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
            public const string FontFamilyMonospace = "'JetBrains Mono', 'Fira Code', 'Courier New', Courier, monospace";
        }

        /// <summary>
        /// Button size constants
        /// </summary>
        public static class ButtonSizes
        {
            public const int HeightSmall = 28;
            public const int HeightMedium = 36;
            public const int HeightLarge = 48;

            public const int PaddingHorizontalSmall = 8;
            public const int PaddingHorizontalMedium = 16;
            public const int PaddingHorizontalLarge = 24;

            public const int PaddingVerticalSmall = 4;
            public const int PaddingVerticalMedium = 8;
            public const int PaddingVerticalLarge = 12;
        }

        /// <summary>
        /// Input field constants
        /// </summary>
        public static class InputFields
        {
            public const int HeightDefault = 48;
            public const int HeightCompact = 36;
            public const int PaddingHorizontal = 12;
            public const int PaddingVertical = 10;
        }

        /// <summary>
        /// Shadow/Elevation constants (for CSS)
        /// </summary>
        public static class Shadows
        {
            public const string Small = "0 2px 4px rgba(0,0,0,0.1)";
            public const string Medium = "0 4px 12px rgba(0,0,0,0.15)";
            public const string Large = "0 8px 24px rgba(0,0,0,0.2)";
        }

        /// <summary>
        /// Icon sizes
        /// </summary>
        public static class IconSizes
        {
            public const int Small = 16;
            public const int Medium = 20;
            public const int Large = 24;
            public const int XLarge = 32;
            public const int XXLarge = 48;
            public const int Huge = 60;
        }

        /// <summary>
        /// Animation/Transition durations (in milliseconds)
        /// </summary>
        public static class Animations
        {
            public const int Fast = 150;
            public const int Normal = 200;
            public const int Slow = 300;
        }

        /// <summary>
        /// Z-Index layering constants
        /// </summary>
        public static class ZIndex
        {
            public const int Base = 0;
            public const int Dropdown = 1000;
            public const int Sticky = 1020;
            public const int Fixed = 1030;
            public const int ModalBackdrop = 1040;
            public const int Modal = 1050;
            public const int Popover = 1060;
            public const int Tooltip = 1070;
        }

        /// <summary>
        /// Browser extension specific constants
        /// </summary>
        public static class BrowserExtension
        {
            public const int PopupWidth = 350;
            public const int PopupMinHeight = 400;
            public const int PopupMaxHeight = 600;
            public const int DropdownMaxHeight = 300;
            public const int InlineDropdownMaxHeight = 250;
        }
    }
}
