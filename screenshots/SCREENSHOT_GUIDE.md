# WinUI Screenshot Creation Guide

This guide describes how to capture professional screenshots of the Vault Guard WinUI application that showcase its 1Password-style design and functionality.

## Prerequisites

- Windows 10 version 19041+ or Windows 11
- .NET 9.0 SDK installed
- Visual Studio 2022 with WinUI workload
- Vault Guard WinUI application built and running

## Screenshot Specifications

### Technical Requirements
- **Resolution**: Minimum 1920x1080, recommended 2560x1440 for high-DPI displays
- **Format**: PNG with transparency where applicable
- **Quality**: Lossless compression, clear readable text
- **Content**: Use sample data only (no real passwords)
- **Consistency**: Same Windows theme and scaling across all screenshots

### Full-Screen Documentation Standard
- Capture the complete app shell (title bar, menu/toolbar, sidebar, and active page content).
- Do not submit cropped form-only screenshots for documentation.
- Keep profile/login context and settings access visible in representative screens.
- Maintain a modern vault-style layout consistent with 1Password/LastPass-inspired navigation.

### Sample Data Setup

Before capturing screenshots, ensure the application contains realistic sample data:

```
Sample Categories:
- Work (Blue icon)
- Personal (Green icon) 
- Social Media (Purple icon)
- Banking (Red icon)
- Travel (Orange icon)

Sample Password Items:
- GitHub (Work category)
- Gmail (Personal category)
- Facebook (Social Media category)
- Chase Bank (Banking category)
- Airbnb (Travel category)
- Microsoft Azure (Work category)
- iCloud (Personal category)
- LinkedIn (Social Media category)
```

## Light Theme Screenshots

### 1. Login Screen (`winui-login-light.png`)
**Description**: Clean authentication interface with Windows 11 styling
**Key Elements to Show**:
- Split-screen layout with app branding on left
- Login form on right with master password field
- "Remember me" checkbox
- Modern Windows 11 Fluent Design elements
- Clean white background (#FAFBFC)
- Blue accent colors (#005BFF)

### 2. Main Dashboard (`winui-dashboard-light.png`)
**Description**: Central hub with 1Password-style navigation and overview
**Key Elements to Show**:
- Left sidebar navigation with categories
- Main content area showing password items
- Top toolbar with search and action buttons
- Windows native styling with light theme colors
- Proper spacing and typography matching 1Password

### 3. Password Items Management (`winui-password-items-light.png`)
**Description**: Comprehensive password management interface
**Key Elements to Show**:
- Three-column layout (categories, items list, item details)
- Search functionality in action
- Item type icons and visual indicators
- Modern card-based design
- Context menu operations available

### 4. Add/Edit Password Dialog (`winui-add-password-dialog-light.png`)
**Description**: Modal dialog for creating/editing password entries
**Key Elements to Show**:
- Clean form layout with proper field spacing
- Password strength indicator
- Category selection dropdown
- Save/Cancel buttons with proper styling
- Modern dialog design with blur background

### 5. Password Generator (`winui-password-generator-light.png`)
**Description**: Advanced password generation interface
**Key Elements to Show**:
- Password length slider
- Character set options (uppercase, lowercase, numbers, symbols)
- Generated password preview
- Copy to clipboard functionality
- Real-time strength indicator

### 6. Settings Page (`winui-settings-light.png`)
**Description**: Comprehensive application settings
**Key Elements to Show**:
- Organized settings sections
- Theme toggle (showing light mode selected)
- Security options
- Import/export settings
- Modern card-based layout

## Dark Theme Screenshots

### 1. Login Screen (`winui-login-dark.png`)
**Description**: Same as light theme but with dark styling
**Key Elements to Show**:
- Dark background (#0F0F0F)
- Proper contrast for text readability
- Same layout as light theme
- Blue accents maintained for consistency

### 2. Main Dashboard (`winui-dashboard-dark.png`)
**Description**: Dashboard with dark theme applied
**Key Elements to Show**:
- Dark navigation sidebar (#1A1A1A)
- Dark content background (#2A2A2A)
- Proper text contrast (#FFFFFF, #B0B0B0)
- Maintained visual hierarchy

### 3. Password Items Management (`winui-password-items-dark.png`)
**Description**: Password management in dark theme
**Key Elements to Show**:
- Dark three-column layout
- Proper hover states for dark theme
- Maintained readability and contrast
- Dark card backgrounds (#2A2A2A)

### 4. Add/Edit Password Dialog (`winui-add-password-dialog-dark.png`)
**Description**: Modal dialog with dark theme styling
**Key Elements to Show**:
- Dark form backgrounds
- Proper input field styling
- Maintained accessibility and contrast
- Dark blur overlay

### 5. Password Generator (`winui-password-generator-dark.png`)
**Description**: Password generator with dark theme
**Key Elements to Show**:
- Dark background with proper contrast
- Readable form elements
- Same functionality as light theme
- Consistent visual design

### 6. Settings Page (`winui-settings-dark.png`)
**Description**: Settings with dark theme applied
**Key Elements to Show**:
- Theme toggle (showing dark mode selected)
- Dark card backgrounds
- Proper text contrast
- Maintained organization and layout

## Before/After Comparison Features

### Navigation Improvements
**Before**: Basic navigation structure
**After**: 1Password-style navigation with:
- Color-coded section icons
- Hover states with subtle animations
- Selection indicators (3px blue bar)
- Organized grouping (All Items, Favorites, Categories, Archive)
- Modern typography and spacing

### Visual Design Enhancements
**Before**: Standard WinUI controls
**After**: 1Password-inspired styling with:
- Rounded corners (8px radius)
- Enhanced color palette
- Professional card-based layouts
- Improved typography hierarchy
- Consistent spacing and margins

### User Experience Improvements
**Before**: Basic password management
**After**: Professional UX with:
- Three-column responsive layout
- Real-time search and filtering
- Context menus for quick actions
- Keyboard shortcuts
- Professional form design

## Capture Instructions

1. **Set up the environment**:
   - Use Windows 11 with light/dark theme as needed
   - Set display scaling to 100% for consistency
   - Close unnecessary applications
   - Use clean desktop background

2. **Prepare the application**:
   - Load sample data as specified above
   - Ensure application is maximized
   - Clear any temporary states or dialogs

3. **Capture process**:
   - Use Windows + Shift + S for built-in screenshot tool
   - Capture full window including title bar, top actions, and sidebar navigation
   - Save as PNG format
   - Use descriptive filenames as specified

4. **Post-processing**:
   - Ensure consistent sizing across screenshots
   - Verify text is readable at various sizes
   - Check that colors match the theme specifications
   - Remove any sensitive information

## Quality Checklist

- [ ] All text is clearly readable
- [ ] Colors match the specified theme palette
- [ ] Screenshots are consistent in size and quality
- [ ] Sample data is realistic but not sensitive
- [ ] UI elements are properly aligned and sized
- [ ] Hover states and interactions are captured appropriately
- [ ] Both light and dark themes are represented
- [ ] File naming convention is followed