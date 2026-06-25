# UI Design System - Consistent Styling Guide

## Overview

This document describes the standardized UI design system used across all VaultGuard platforms:
- **WinUI Desktop App** (XAML)
- **Uno Platform** (iOS, Android, WebAssembly - XAML)
- **Blazor Web App** (Razor/MudBlazor)
- **Browser Extension** (HTML/CSS/JavaScript)

All platforms reference the centralized constants defined in `VaultGuard.Models/UI/UIConstants.cs`.

## Color Palette

### Primary Colors
- **Primary Blue**: `#005BFF` - Main brand color, used for primary buttons and key UI elements
- **Primary Blue Light**: `#3B82F6` - Lighter variant for hover states
- **Primary Blue Dark**: `#0041CC` - Darker variant for pressed states

### Secondary/Accent Colors
- **Accent Pink**: `#EC4899` - Secondary accent color
- **Accent Pink Light**: `#F472B6` - Lighter pink variant

### Status Colors
- **Success Green**: `#10B981` - Success messages, completed actions
- **Warning Orange**: `#F59E0B` - Warnings, caution messages
- **Error Red**: `#EF4444` - Error messages, destructive actions

### Background Colors

#### Dark Theme (Default)
- **Background**: `#1A1A1A` - Main app background
- **Surface**: `#262626` - Card/panel surfaces
- **Card**: `#262626` - Individual card backgrounds
- **Elevated Surface**: `#333333` - Elevated elements (modals, popups)
- **Border**: `#404040` - Border colors
- **Sidebar**: `#141414` - Navigation sidebar background

#### Light Theme
- **Background**: `#F8F9FA` - Main app background
- **Surface**: `#FFFFFF` - Card/panel surfaces
- **Card**: `#FFFFFF` - Individual card backgrounds
- **Elevated Surface**: `#F6F8FA` - Elevated elements
- **Border**: `#E0E6ED` - Border colors

### Text Colors

#### Dark Theme
- **Primary**: `#FFFFFF` - Main text
- **Secondary**: `#B0B0B0` - Secondary text, labels
- **Tertiary**: `#808080` - Placeholder text, disabled text

#### Light Theme
- **Primary**: `#1A1A1A` - Main text
- **Secondary**: `#6C757D` - Secondary text, labels
- **Tertiary**: `#94A3B8` - Placeholder text, disabled text

### Special Purpose
- **New Item Button**: `#C08FDA` - Special purple color for "New Item" actions

## Spacing System

Use consistent spacing values throughout the UI:

- **XSmall**: 4px
- **Small**: 8px
- **Medium**: 12px
- **Large**: 16px
- **XLarge**: 20px
- **XXLarge**: 24px
- **Huge**: 32px
- **XHuge**: 48px

## Border Radius

Maintain consistent corner rounding:

- **Small**: 4px - Input fields
- **Medium**: 6px - Small buttons, badges
- **Large**: 8px - Standard buttons, cards
- **XLarge**: 12px - Large cards, panels
- **XXLarge**: 16px - Modals, dialogs
- **Round**: 20px - Fully rounded elements (avatars)

## Typography

### Font Sizes
- **Small**: 12px - Captions, helper text
- **Body**: 14px - Standard body text
- **Medium**: 15px - Emphasized body text
- **Large**: 16px - Large body text
- **Heading 6**: 18px
- **Heading 5**: 20px
- **Heading 4**: 24px
- **Heading 3**: 28px
- **Heading 2**: 32px
- **Heading 1**: 36px

### Font Family
- **Default**: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif
- **Monospace**: 'Courier New', Courier, monospace (for passwords, codes)

## Component Standards

### Buttons

#### Sizes
- **Small**: Height 28px, Padding 4px 8px
- **Medium**: Height 36px, Padding 8px 16px (default)
- **Large**: Height 48px, Padding 12px 24px

#### Types
- **Primary**: Primary blue background, white text
- **Secondary**: Secondary gray background, white text
- **Icon**: Transparent background, icon only, 36px × 36px
- **Outlined**: Transparent background with border

### Input Fields
- **Default Height**: 48px
- **Compact Height**: 36px
- **Horizontal Padding**: 12px
- **Vertical Padding**: 10px

### Shadows/Elevation
- **Small**: `0 2px 4px rgba(0,0,0,0.1)` - Subtle elevation
- **Medium**: `0 4px 12px rgba(0,0,0,0.15)` - Standard elevation
- **Large**: `0 8px 24px rgba(0,0,0,0.2)` - High elevation (modals)

### Icons
- **Small**: 16px
- **Medium**: 20px
- **Large**: 24px
- **XLarge**: 32px
- **XXLarge**: 48px
- **Huge**: 60px

## Animation/Transitions

Standard durations for smooth animations:
- **Fast**: 150ms - Quick feedback
- **Normal**: 200ms - Standard transitions
- **Slow**: 300ms - Emphasis animations

## Platform-Specific Implementation

### WinUI (Windows Desktop)
- Theme files: `VaultGuard.WinUi/Themes/ModernTheme.xaml`
- Uses XAML Color resources and StaticResource references
- Supports gradient brushes for enhanced visuals

### Uno Platform (iOS, Android, WebAssembly)
- Theme file: `VaultGuard.Uno/Styles/ColorPaletteOverride.xaml`
- Uses Material Design 3 color system with theme dictionaries
- Separate Light and Dark theme definitions

### Blazor Web App
- Uses MudBlazor component library
- Theme service: `VaultGuard.Components.Shared/Services/ThemeService.cs`
- Colors applied via MudBlazor theme configuration
- Shared CSS in component-specific .razor.css files

### Browser Extension
- CSS file: `VaultGuard.BrowserExtension/popup.css`
- Uses CSS custom properties (variables) for easy theme application
- Example:
  ```css
  :root {
    --primary-blue: #005BFF;
    --spacing-md: 12px;
  }
  ```

## Browser Extension Enhancements

### 1Password-Style Inline Password List

When users focus on a username or password field on a web form, the browser extension automatically shows an inline dropdown with saved passwords for that domain:

#### Features:
- **Automatic Display**: Triggers on field focus
- **Domain Filtering**: Only shows passwords for current domain
- **Clean UI**: Matches main app styling with consistent colors
- **Quick Fill**: Click to autofill credentials
- **Smart Positioning**: Appears below the focused field
- **Click Outside to Close**: Automatically dismisses when clicking elsewhere

#### Visual Design:
- Uses consistent colors from UIConstants
- Border radius: 6px
- Box shadow: `0 4px 12px rgba(0,0,0,0.15)`
- Max height: 250px with scroll
- White background with hover effects

## Best Practices

1. **Always use the defined color constants** - Never hardcode color values
2. **Use the spacing system** - Maintain visual rhythm
3. **Follow the border radius guidelines** - Consistency in rounding
4. **Use appropriate font sizes** - Maintain type hierarchy
5. **Apply shadows correctly** - Match elevation to importance
6. **Respect animation durations** - Don't make things too fast or slow
7. **Test on all platforms** - Ensure visual consistency

## Testing Checklist

- [ ] Colors match across WinUI, Uno, and Blazor
- [ ] Spacing is consistent in all layouts
- [ ] Buttons have uniform sizing and styling
- [ ] Input fields maintain consistent height
- [ ] Shadows/elevations match design system
- [ ] Typography scales appropriately
- [ ] Browser extension matches main app styling
- [ ] Inline password list works on login forms
- [ ] Dark/Light themes work correctly

## Future Enhancements

- [ ] Add support for custom themes
- [ ] Implement accent color picker
- [ ] Add high contrast mode
- [ ] Support reduced motion preferences
- [ ] Add more status colors (info, neutral)
