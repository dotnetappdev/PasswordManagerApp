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
            
            // Secondary/Accent Colors
            public const string AccentPink = "#EC4899";
            public const string AccentPinkLight = "#F472B6";
            
            // Status Colors
            public const string SuccessGreen = "#10B981";
            public const string WarningOrange = "#F59E0B";
            public const string ErrorRed = "#EF4444";
            
            // Background Colors - Dark Theme (Default)
            public const string BackgroundDark = "#1A1A1A";
            public const string SurfaceDark = "#262626";
            public const string CardDark = "#262626";
            public const string ElevatedSurfaceDark = "#333333";
            public const string BorderDark = "#404040";
            public const string SidebarDark = "#141414";
            
            // Background Colors - Light Theme
            public const string BackgroundLight = "#F8F9FA";
            public const string SurfaceLight = "#FFFFFF";
            public const string CardLight = "#FFFFFF";
            public const string ElevatedSurfaceLight = "#F6F8FA";
            public const string BorderLight = "#E0E6ED";
            
            // Text Colors - Dark Theme
            public const string TextPrimaryDark = "#FFFFFF";
            public const string TextSecondaryDark = "#B0B0B0";
            public const string TextTertiaryDark = "#808080";
            
            // Text Colors - Light Theme
            public const string TextPrimaryLight = "#1A1A1A";
            public const string TextSecondaryLight = "#6C757D";
            public const string TextTertiaryLight = "#94A3B8";
            
            // Special Purpose Colors
            public const string NewItemButton = "#C08FDA";
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
            
            // Font Families
            public const string FontFamilyDefault = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
            public const string FontFamilyMonospace = "'Courier New', Courier, monospace";
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
