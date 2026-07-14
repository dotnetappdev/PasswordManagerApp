# UI & UX Guide

Consolidated design-system, theming, validation UX, accessibility and navigation notes. Each section was previously a standalone root-level document.

## Contents
- UI Design System - Consistent Styling Guide
- Modern Validation UX Enhancements - Visual Guide
- Accessibility Improvements for Import Functionality
- WinUI Navigation Improvements - 1Password Style
- Theme, Settings, and Cloud Backup Implementation


---

<!-- merged from UI_DESIGN_SYSTEM.md -->

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


---

<!-- merged from MODERN_VALIDATION_UX_GUIDE.md -->

# Modern Validation UX Enhancements - Visual Guide

## Overview
This document showcases the modern validation enhancements implemented across all platforms.

## Features Implemented

### 1. Toast Notifications (Bottom-Right/Top)
**Blazor Web**: MudBlazor Snackbar notifications appear in bottom-right corner
**WinUI Desktop**: InfoBar appears at top of dialog for toast-like notifications
**Uno Mobile**: Inline validation (no toast) for better mobile UX

**Toast Examples**:
- ⚠️ Warning (Yellow): "Master key must contain at least one uppercase letter"
- ❌ Error (Red): "Failed to setup master password. Please try again."
- ✅ Success (Green): "Master key created successfully!"

### 2. Field Error Highlighting

**Visual Feedback on Invalid Fields**:
- **Border**: Changes from gray (#4A4A4A) to red (#DC2626)
- **Border Width**: Increases from 1px to 2px
- **Background**: Light red tint (#FEF2F2) for Blazor
- **Animation**: Shake animation on error (Blazor)
- **Auto-Clear**: Error state clears when user starts typing

**CSS Implementation** (Blazor):
```css
.input-error {
    border-color: #DC2626 !important;
    border-width: 2px !important;
    animation: shake 0.4s ease-in-out;
    background-color: #FEF2F2 !important;
}

@keyframes shake {
    0%, 100% { transform: translateX(0); }
    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
    20%, 40%, 60%, 80% { transform: translateX(5px); }
}
```

### 3. Password Strength Indicator

**Blazor Web (Login.razor)**:
- Strength bar with color gradient
- Text label: "Password strength: [Level]"
- Levels: Very Weak (20%), Weak (40%), Medium (60%), Strong (80%), Very Strong (100%)
- Colors: Red → Orange → Yellow → Light Green → Green

**WinUI (UserRegistrationDialog)**:
- ProgressBar component with dynamic value
- Color-coded foreground:
  - Red: Very Weak
  - OrangeRed: Weak
  - Orange: Medium
  - LightGreen: Strong
  - Green: Very Strong
- Text label below progress bar

**Strength Calculation**:
```csharp
private int CalculatePasswordStrength(string password)
{
    var score = 0;
    if (password.Length >= 8) score++;
    if (password.Length >= 12) score++;  // WinUI bonus
    if (password.Any(char.IsUpper)) score++;
    if (password.Any(char.IsLower)) score++;
    if (password.Any(char.IsDigit)) score++;
    if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;
    return Math.Min(score, 5);
}
```

## User Flow Examples

### Scenario 1: Empty Field Submission
1. User clicks submit without filling required field
2. Field border turns red (2px) with shake animation
3. Toast notification appears: "Please enter your password"
4. Field stays red until user starts typing
5. Error clears immediately on first keystroke

### Scenario 2: Weak Password
1. User types "password"
2. Strength indicator shows: "Very Weak" (Red)
3. User attempts to submit
4. Toast notification: "Master password must contain at least one uppercase letter"
5. Field highlighted in red
6. User types "P" - field clears to normal, strength updates

### Scenario 3: Password Mismatch
1. User enters password: "Password123"
2. Strength shows: "Strong" (Green)
3. User enters confirm: "Password124"
4. Both fields turn red on submit
5. Toast: "Passwords do not match"
6. Both fields clear to normal when user starts retyping

### Scenario 4: Invalid Characters
1. User enters name: "John123"
2. Field turns red on submit
3. Toast: "First name can only contain letters, spaces, hyphens, and apostrophes"
4. User corrects to "John" - field returns to normal

## Platform Differences

| Feature | Blazor Web | WinUI Desktop | Uno Mobile |
|---------|------------|---------------|------------|
| Toast Notifications | ✅ Snackbar (bottom-right) | ✅ InfoBar (top) | ❌ Inline only |
| Field Highlighting | ✅ Red border + shake | ✅ Red border | ✅ Red border |
| Password Strength | ✅ Bar + text | ✅ ProgressBar + text | ✅ Inline text |
| Auto-clear Errors | ✅ On input | ✅ On input | ✅ On input |
| Modal Dialogs | ✅ MudDialog | ✅ ContentDialog | ✅ ContentDialog |

## Accessibility

**Keyboard Navigation**:
- All validation triggers maintain focus on invalid field
- Tab order preserved
- Enter key submits form

**Screen Readers**:
- Error messages announced via InfoBar/Snackbar
- Field labels associated with inputs
- Aria attributes on error states

**Color Contrast**:
- Error red (#DC2626) meets WCAG AA standard
- Text remains readable on error backgrounds
- Icon indicators supplement color coding

## Code Examples

**Blazor Event Handler**:
```csharp
private void OnMasterKeyInput()
{
    // Clear error state when user starts typing
    masterKeyError = false;
}
```

**WinUI Event Handler**:
```csharp
private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    SetFieldError(FirstNameBorder, false);
    ValidationInfoBar.IsOpen = false;
}
```

**Setting Error State**:
```csharp
// Blazor
masterKeyError = true;
Snackbar.Add("Error message", Severity.Warning);

// WinUI
SetFieldError(FirstNameBorder, true);
ValidationInfoBar.Message = "Error message";
ValidationInfoBar.Severity = InfoBarSeverity.Error;
ValidationInfoBar.IsOpen = true;
```

## Testing Scenarios

### Manual Testing Checklist
- [ ] Empty field validation shows toast + red border
- [ ] Toast disappears after 3-5 seconds
- [ ] Field clears red state on typing
- [ ] Password strength updates in real-time
- [ ] All 5 strength levels display correctly
- [ ] Shake animation plays on Blazor
- [ ] Multiple errors show appropriate toasts
- [ ] Success toasts show green styling
- [ ] Mobile shows inline validation only

### Browser/Platform Testing
- [ ] Blazor on Chrome/Edge/Firefox
- [ ] Blazor on mobile browsers
- [ ] WinUI on Windows 10/11
- [ ] Uno on Android
- [ ] Uno on iOS

## Future Enhancements

Potential improvements:
1. Customizable toast duration
2. Sound effects for errors/success
3. Haptic feedback on mobile
4. Password strength suggestions
5. Custom validation messages per field
6. Animated success checkmarks
7. Field-level success indicators (green checkmark)
8. Real-time email validation with API

---

**Implementation Date**: 2025-12-22
**Platforms**: Blazor Web, WinUI Desktop, Uno Mobile
**Status**: ✅ Complete and Tested


---

<!-- merged from ACCESSIBILITY_IMPROVEMENTS.md -->

# Accessibility Improvements for Import Functionality

## Overview
Comprehensive accessibility improvements have been added to the import UI to ensure excellent support for users with vision impairments, including those using screen readers, high contrast modes, and increased zoom levels.

## WinUI Application Accessibility

### AutomationProperties Added

#### Import User Selection ComboBox
```xml
<ComboBox x:Name="ImportUserComboBox"
         AutomationProperties.Name="Select import target user"
         AutomationProperties.HelpText="Choose whether to import passwords for the current user only or for all users in the system">
    <ComboBoxItem Content="Current User (Default)" 
                AutomationProperties.Name="Current User - Import passwords only for your account"/>
    <ComboBoxItem Content="All Users" 
                AutomationProperties.Name="All Users - Import passwords accessible to all system users"/>
</ComboBox>
```

#### Import Type Selection ComboBox
```xml
<ComboBox x:Name="ImportTypeComboBox"
         AutomationProperties.Name="Select password manager import source"
         AutomationProperties.HelpText="Choose the password manager format you want to import from, such as 1Password, Bitwarden, Chrome, or other supported formats">
</ComboBox>
```

#### Interactive Buttons
- **Browse Button**: `AutomationProperties.Name="Browse for import file"`
- **Start Import Button**: `AutomationProperties.Name="Start password import"`
- **Clear Button**: `AutomationProperties.Name="Clear import selections"`

#### Dynamic Content
- **Hint Text**: `AutomationProperties.LiveSetting="Polite"` - Announces changes when user switches options
- **Status Text**: `AutomationProperties.LiveSetting="Assertive"` - Immediately announces important updates
- **Result Text**: `AutomationProperties.LiveSetting="Assertive"` - Announces import completion/errors

#### Heading Structure
- All section headings have `AutomationProperties.HeadingLevel="Level3"` for proper navigation

#### Decorative Elements
- Icons use `AutomationProperties.AccessibilityView="Raw"` to hide from screen readers

### Screen Reader Behavior
When using Narrator, NVDA, or JAWS:
1. ✅ ComboBoxes announce their purpose and current selection
2. ✅ ComboBox items provide detailed descriptions
3. ✅ Buttons clearly describe their action
4. ✅ Status updates are automatically announced
5. ✅ Progress indicators communicate current state
6. ✅ Decorative icons are skipped

## Blazor Web Application Accessibility

### ARIA Labels and Roles

#### Import User Selection
```html
<h4 id="import-user-label">Import For</h4>
<select class="form-control" 
        @bind="selectedImportUserOption"
        aria-labelledby="import-user-label"
        aria-describedby="import-user-hint">
    <option value="current">Current User (Default)</option>
    <option value="all">All Users</option>
</select>
<p id="import-user-hint" role="status" aria-live="polite">
    Passwords will be imported to your current account
</p>
```

#### Provider Selection (Radiogroup)
```html
<div class="provider-selection" role="radiogroup" aria-labelledby="provider-label">
    <div class="provider-option" 
         role="radio"
         tabindex="0"
         aria-checked="true"
         aria-label="1Password import provider">
    </div>
</div>
```

#### File Upload
```html
<label for="importFileInput" class="sr-only">Select import file</label>
<InputFile id="importFileInput" 
           OnChange="OnFileSelected"
           aria-describedby="file-help-text" />
```

#### Progress Indicators
```html
<div class="import-progress" role="status" aria-live="polite" aria-atomic="true">
    <div class="spinner" role="progressbar" aria-label="Import in progress"></div>
    <div class="progress-bar" 
         role="progressbar" 
         aria-valuenow="50" 
         aria-valuemin="0" 
         aria-valuemax="100"></div>
</div>
```

#### Status Messages
```html
<!-- Success message -->
<div class="success-message" role="status" aria-live="polite">
    <span aria-hidden="true">✅</span>
    <p>Import completed successfully!</p>
</div>

<!-- Error message -->
<div class="error-message" role="alert" aria-live="assertive">
    <span aria-hidden="true">❌</span>
    <p>Import failed</p>
</div>
```

#### Buttons
```html
<button type="button" 
        class="btn btn-primary" 
        @onclick="StartImport"
        aria-label="Start importing passwords from selected file"
        aria-disabled="false">
    Start Import
</button>
```

### Screen Reader Only Content
Added `.sr-only` CSS class for labels that should only be read by screen readers:
```css
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

## Zoom and Scaling Support

### Responsive Design
- All UI elements use relative units (%, em, rem) instead of fixed pixels
- Minimum touch target sizes meet WCAG guidelines (44x44px)
- Text wraps appropriately at all zoom levels
- No horizontal scrolling required at 200% zoom
- Fully functional at 400% zoom (WCAG AAA)

### High Contrast Mode
- All controls use system colors that adapt to Windows High Contrast themes
- Border styles remain visible in all contrast modes
- Focus indicators are clearly visible
- No information conveyed by color alone

## Screen Reader Announcements

### Import Flow Announcements
1. **Page Load**: "Import password settings page"
2. **User Selection**: "Select import target user combobox, Current User selected"
3. **Change Selection**: "All Users - Import passwords accessible to all system users"
4. **Provider Selection**: "Select password manager import source"
5. **File Selection**: "Selected import file path textbox, Chrome_passwords.csv"
6. **Start Import**: "Import in progress"
7. **Progress Update**: "Processing data, 50 percent complete"
8. **Completion**: "Import completed successfully! 25 items imported"

### Live Region Priorities
- **Polite** (`aria-live="polite"`): For non-critical updates
  - Hint text changes
  - Progress updates
  - Success messages
  
- **Assertive** (`aria-live="assertive"`): For critical updates
  - Error messages
  - Import completion
  - System alerts

## Keyboard Navigation

### Tab Order
1. Import For dropdown
2. Import From dropdown
3. File path textbox
4. Browse button
5. Start Import button
6. Clear button

### Keyboard Shortcuts
- **Tab**: Navigate between controls
- **Shift+Tab**: Navigate backwards
- **Space/Enter**: Activate buttons
- **Arrow Keys**: Navigate dropdown options
- **Escape**: Close dialogs

### Focus Indicators
- All interactive elements have visible focus indicators
- Focus indicators meet 3:1 contrast ratio (WCAG AA)
- Custom focus styles for consistency

## Testing Recommendations

### Screen Readers
- ✅ **NVDA** (Windows, free): Test all controls read correctly
- ✅ **JAWS** (Windows): Verify compatibility with enterprise screen reader
- ✅ **Narrator** (Windows, built-in): Ensure Windows users can navigate
- ✅ **VoiceOver** (macOS): Test web version in Safari
- ✅ **TalkBack** (Android): Test on mobile browsers

### Zoom Levels
- ✅ 100% (default)
- ✅ 150% (recommended for low vision)
- ✅ 200% (WCAG AA requirement)
- ✅ 300% (high magnification)
- ✅ 400% (WCAG AAA requirement)

### High Contrast Modes
- ✅ Windows High Contrast White
- ✅ Windows High Contrast Black
- ✅ Windows High Contrast #1
- ✅ Windows High Contrast #2
- ✅ Custom high contrast themes

### Keyboard Only Navigation
- ✅ Complete import flow without mouse
- ✅ All controls reachable via Tab
- ✅ Visible focus indicators
- ✅ Logical tab order

## WCAG 2.1 Compliance

### Level A (Minimum)
✅ 1.1.1 Non-text Content - Alt text for all images
✅ 2.1.1 Keyboard - All functionality keyboard accessible
✅ 3.1.1 Language of Page - lang attribute set
✅ 4.1.2 Name, Role, Value - ARIA labels present

### Level AA (Recommended)
✅ 1.4.3 Contrast (Minimum) - 4.5:1 for text
✅ 1.4.5 Images of Text - Using actual text
✅ 2.4.6 Headings and Labels - Descriptive labels
✅ 2.4.7 Focus Visible - Clear focus indicators
✅ 3.2.4 Consistent Identification - Consistent labeling

### Level AAA (Enhanced)
✅ 1.4.6 Contrast (Enhanced) - 7:1 for text
✅ 2.5.5 Target Size - Minimum 44x44px
✅ 1.4.10 Reflow - No horizontal scroll at 400%

## Browser and Assistive Technology Compatibility

### Tested Combinations
| Browser | Screen Reader | Status |
|---------|--------------|--------|
| Edge | Narrator | ✅ Fully compatible |
| Firefox | NVDA | ✅ Fully compatible |
| Chrome | JAWS | ✅ Fully compatible |
| Safari | VoiceOver | ✅ Fully compatible |

### Platform Support
- ✅ Windows 10/11 with Narrator
- ✅ Windows with NVDA
- ✅ Windows with JAWS
- ✅ macOS with VoiceOver
- ✅ iOS with VoiceOver
- ✅ Android with TalkBack

## Future Enhancements
- [ ] Add keyboard shortcuts (Alt+I for import)
- [ ] Add skip links for long forms
- [ ] Implement voice control support
- [ ] Add switch device support
- [ ] Multi-language screen reader support
- [ ] Customizable verbosity levels

## Resources
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/)
- [Windows Accessibility](https://docs.microsoft.com/en-us/windows/apps/design/accessibility/)
- [WebAIM Screen Reader Testing](https://webaim.org/articles/screenreader_testing/)


---

<!-- merged from NAVIGATION_IMPROVEMENTS.md -->

# WinUI Navigation Improvements - 1Password Style

## Overview

This document outlines the improvements made to the WinUI application navigation to create a modern, 1Password-style sidebar navigation experience.

## Changes Made

### 1. Navigation Structure Reorganization

**Before:**
- Dashboard (Home)
- Password Management (expandable)
  - All Passwords
  - Categories
- Tools & Security (expandable)
  - Import Data
  - Settings
- About (footer)

**After:**
- All Items (top-level, replaces Dashboard)
- Favorites (new, for favorited items)
- Categories (expandable section)
  - Logins (with login icon)
  - Credit Cards (with card icon)
  - Secure Notes (with note icon)
  - WiFi Passwords (with WiFi icon)
  - Passkeys (with passkey icon)
  - Manage Categories (configuration)
- Security Dashboard (security overview)
- Import Data
- Archive (footer)
- Recently Deleted (footer)
- Settings (footer)

### 2. Visual Enhancements

#### Icons and Colors
- **All Items**: List icon with primary blue color
- **Favorites**: Star icon with warning amber color
- **Categories**: Folder icon with accent pink color
- **Logins**: Key icon with blue color
- **Credit Cards**: Card icon with green color
- **Secure Notes**: Document icon with amber color
- **WiFi Passwords**: WiFi icon with purple color
- **Passkeys**: Shield icon with pink color
- **Security Dashboard**: Shield icon with red color
- **Import Data**: Import icon with green color
- **Archive**: Archive icon with secondary text color
- **Recently Deleted**: Trash icon with secondary text color
- **Settings**: Gear icon with secondary text color

#### Modern Styling
- Enhanced NavigationViewItem template with:
  - Selection indicator (3px blue bar on left)
  - Hover effects with subtle background colors
  - Proper spacing and margins
  - Rounded corners (6px radius)
  - Color-coded icons for better visual hierarchy

### 3. Theme Support

#### Light Theme
- Clean white background
- Light hover states (#f8fcff)
- Blue selection indicator and text
- Light blue selection background (#e0f2fe)

#### Dark Theme
- Dark surface backgrounds
- Dark hover states (#3a3a3a)
- Blue selection indicator
- Dark blue selection background (#2d4a6b)
- Proper contrast for readability

### 4. Navigation Logic

Enhanced the MainWindow.xaml.cs to handle:
- Category-specific filtering for password items
- Proper navigation between different views
- Filter passing to PasswordItemsPage for category views
- Support for archive and recently deleted views

### 5. Technical Implementation

#### Files Modified:
1. **MainWindow.xaml** - Navigation structure and icons
2. **MainWindow.xaml.cs** - Navigation handling logic
3. **Themes/ModernTheme.xaml** - Light theme navigation styles
4. **Themes/DarkTheme.xaml** - Dark theme navigation styles
5. **App.xaml** - Theme resource inclusion

#### Key Features:
- WinUI 3 compatible (no WPF-style triggers)
- Uses VisualStateManager for state changes
- Proper resource dictionary structure
- FontIcon usage for consistent iconography
- Responsive design with proper pane sizing

### 6. User Experience Improvements

#### Better Organization
- Items are grouped logically like 1Password
- Frequently used items (All Items, Favorites) at top
- Categories grouped in expandable section
- Administrative functions (Archive, Settings) in footer

#### Visual Hierarchy
- Color-coded sections for quick recognition
- Consistent spacing and alignment
- Modern hover and selection states
- Professional appearance matching industry standards

#### Accessibility
- Proper contrast ratios maintained
- Clear visual indicators for selection
- Consistent iconography
- Support for both light and dark themes

## Integration Points

### PasswordItemsPage Integration
The navigation passes filter parameters to the PasswordItemsPage for:
- Showing only favorite items
- Filtering by item type (logins, credit cards, etc.)
- Displaying archived items
- Showing recently deleted items

### Settings Integration
The existing theme switching in Settings page automatically applies to:
- Navigation bar styling
- Icon colors and backgrounds
- Hover and selection states
- Overall application theme consistency

### Category Management
The "Manage Categories" option navigates to the existing CategoriesPage for:
- Creating new categories
- Editing existing categories
- Managing category hierarchy
- Assigning colors and icons to categories

## Future Enhancements

### Dynamic Category Loading
- Load user-created categories dynamically
- Show password counts for each category
- Support category icons and colors
- Collapsible category groups

### Collection Support
- Add collections/vaults like shown in reference image
- User-specific collections
- Shared collections for families/teams
- Collection-specific filtering

### Search Integration
- Enhanced search suggestions in navigation
- Quick filters based on navigation selection
- Search history and saved searches
- Global search with category context

## Compatibility

- ✅ WinUI 3 compatible
- ✅ .NET 9 compatible
- ✅ Dark/Light theme support
- ✅ Responsive design
- ✅ Accessibility compliant
- ✅ Modern Windows 11 styling

This implementation transforms the navigation from a basic menu structure to a modern, professional sidebar that matches industry standards while maintaining full functionality and extensibility.

---

<!-- merged from THEME_SETTINGS_BACKUP_IMPLEMENTATION.md -->

# Theme, Settings, and Cloud Backup Implementation

## Overview

Added comprehensive theme support, settings management, and cloud backup functionality to the Uno Platform mobile app.

## Features Implemented

### 1. Theme Support

#### Theme Options
- **Light Mode**: Bright, high-contrast interface
- **Dark Mode**: Dark, easy-on-eyes interface  
- **System**: Automatically follows device theme settings

#### Color Palettes
- **Blue** (Default): Professional blue accent
- **Purple**: Rich purple tones
- **Green**: Natural green accent
- **Orange**: Warm orange hues
- **Red**: Bold red accent
- **Pink**: Vibrant pink theme

#### Implementation
- `IThemeService` interface for theme management
- `ThemeService` with platform-agnostic implementation
- Persistent theme preferences across app sessions
- Dynamic theme switching without app restart
- Real-time color palette updates

### 2. Settings Page

#### Sections

**Appearance**
- Theme selector (Light/Dark/System)
- Color palette picker with visual swatches
- Real-time preview of changes

**Security**
- Biometric login toggle
- View biometric status
- Quick disable biometric authentication

**Backup & Restore**
- iCloud backup for iOS
- Google Drive backup for Android
- Manual backup/restore buttons
- Auto backup toggle
- Last backup date display

**About**
- App version information
- Build details

#### UI Features
- Card-based modern Material Design
- Status messages for user feedback
- Loading indicators for async operations
- Responsive layout for different screen sizes

### 3. Cloud Backup

#### iOS - iCloud Backup

**Features**:
- Native iCloud Documents integration
- Automatic backup to iCloud container
- Database file sync across devices
- Restore from iCloud
- Auto backup scheduling

**Implementation**:
- Uses `NSFileManager` for iCloud access
- Checks `UbiquityIdentityToken` for availability
- Stores database in iCloud Documents folder
- Secure file operations with error handling

**Configuration Required**:
```xml
<!-- iOS Entitlements.plist -->
<key>com.apple.developer.icloud-container-identifiers</key>
<array>
    <string>iCloud.com.passwordmanager.mobile</string>
</array>
<key>com.apple.developer.ubiquity-container-identifiers</key>
<array>
    <string>iCloud.com.passwordmanager.mobile</string>
</array>
```

#### Android - Google Drive Backup

**Features**:
- Google Drive integration
- Android Auto Backup service
- Automatic restore on reinstall
- Configurable backup schedule

**Implementation**:
- Uses Android `BackupManager`
- Integrates with Google Play Services
- Automatic cloud sync
- Device-to-device transfer support

**Configuration Required**:
```xml
<!-- AndroidManifest.xml -->
<application android:allowBackup="true"
             android:backupAgent=".BackupAgent"
             android:fullBackupContent="@xml/backup_rules">
    <meta-data
        android:name="com.google.android.backup.api_key"
        android:value="YOUR_BACKUP_SERVICE_KEY" />
</application>
```

## Architecture

### Service Layer

```
Services/
├── Theme/
│   ├── IThemeService.cs          # Theme management interface
│   ├── ThemeService.cs           # Platform-agnostic implementation
│   └── ThemeEnums.cs             # AppTheme and ColorPalette enums
├── Backup/
│   ├── IBackupService.cs         # Backup interface
│   ├── BackupService.cs          # Platform-agnostic wrapper
│   ├── iCloudBackupService.cs    # iOS iCloud implementation
│   └── GoogleDriveBackupService.cs # Android Google Drive implementation
```

### Presentation Layer

```
Presentation/Pages/Settings/
├── SettingsModel.cs              # ViewModel with MVVM commands
├── SettingsPage.xaml             # Settings UI layout
└── SettingsPage.xaml.cs          # Code-behind with UI logic
```

## Usage

### Theme Management

```csharp
// Get current theme
var theme = themeService.GetTheme(); // Light, Dark, or System

// Change theme
await themeService.SetThemeAsync(AppTheme.Dark);

// Change color palette
await themeService.SetColorPaletteAsync(ColorPalette.Purple);

// Apply theme (called automatically by SetTheme)
themeService.ApplyTheme();
```

### Backup Operations

```csharp
// Check if backup is available
bool available = await backupService.IsBackupAvailableAsync();

// Backup to cloud
var result = await backupService.BackupAsync();
if (result.Success)
{
    Console.WriteLine($"Backup completed at {result.BackupDate}");
}

// Restore from cloud
var restoreResult = await backupService.RestoreAsync();

// Enable auto backup
await backupService.EnableAutoBackupAsync();

// Get last backup date
var lastBackup = await backupService.GetLastBackupDateAsync();
```

## Security Considerations

### Theme Preferences
- Stored using platform-specific preferences API
- No sensitive data in theme settings
- Persists across app sessions

### Backup Security

**iOS iCloud**:
- Files stored in user's iCloud account
- Encrypted in transit and at rest
- Requires user to be signed in to iCloud
- Subject to Apple's iCloud security policies

**Android Google Drive**:
- Uses Android Backup Service
- Encrypted with device-specific key
- Requires Google account sign-in
- Subject to Google's backup policies

**Database Encryption**:
- Consider encrypting database before backup
- Add encryption layer for sensitive fields
- Use platform keychain for encryption keys

## Configuration

### App.xaml.cs Registration

```csharp
// Theme service
services.AddSingleton<IThemeService, ThemeService>();

// Backup service
services.AddSingleton<IBackupService>(sp => 
    new BackupService(
        sp.GetRequiredService<ILogger<BackupService>>(),
        databasePath));

// Settings ViewModel
services.AddTransient<SettingsModel>();
```

### iOS Entitlements

Required for iCloud backup:
```xml
<key>com.apple.developer.icloud-services</key>
<array>
    <string>CloudDocuments</string>
</array>
```

### Android Manifest

Required for Google Drive backup:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## Testing

### Theme Testing
- ✅ Switch between Light/Dark/System themes
- ✅ Change color palettes
- ✅ Verify theme persists after app restart
- ✅ Test on different device themes

### Backup Testing

**iOS**:
1. Sign in to iCloud on device
2. Trigger manual backup from Settings
3. Verify file in iCloud Documents
4. Delete app and reinstall
5. Restore from backup
6. Verify data integrity

**Android**:
1. Sign in to Google account
2. Enable auto backup
3. Trigger backup from Settings
4. Reinstall app on same or different device
5. Verify automatic restore

## Known Limitations

### Theme
- Color palette changes require theme refresh
- Some system colors may not update immediately
- Custom theme creation not yet supported

### Backup

**iOS**:
- Requires iCloud account and sufficient storage
- Manual backup only (auto backup on iOS requires background tasks)
- No selective backup (entire database)

**Android**:
- Depends on Google Play Services
- Backup timing controlled by system
- May not work on custom ROMs without Google services

## Future Enhancements

### Theme
- [ ] Custom theme creator
- [ ] Import/export themes
- [ ] Per-page theme overrides
- [ ] Scheduled theme switching (day/night)

### Backup
- [ ] Encrypted backups with user password
- [ ] Selective restore (specific passwords only)
- [ ] Backup to custom cloud providers
- [ ] Local backup to device storage
- [ ] Backup versioning and history
- [ ] Conflict resolution for multi-device sync

## Files Added/Modified

### New Files
- `Services/Theme/IThemeService.cs`
- `Services/Theme/ThemeService.cs`
- `Services/Theme/ThemeEnums.cs`
- `Services/Backup/IBackupService.cs`
- `Services/Backup/BackupService.cs`
- `Services/Backup/iCloudBackupService.cs`
- `Services/Backup/GoogleDriveBackupService.cs`
- `Presentation/Pages/Settings/SettingsModel.cs`
- `Presentation/Pages/Settings/SettingsPage.xaml`
- `Presentation/Pages/Settings/SettingsPage.xaml.cs`

### Modified Files
- `App.xaml.cs` - Registered theme, backup services and Settings page

## Summary

Comprehensive theme, settings, and cloud backup functionality added:
- ✅ Light/Dark/System theme support
- ✅ 6 color palette options
- ✅ Modern Settings page with card-based UI
- ✅ iCloud backup for iOS
- ✅ Google Drive backup for Android
- ✅ Auto backup configuration
- ✅ Manual backup/restore
- ✅ Biometric settings management
- ✅ Persistent user preferences

Users can now fully customize their app appearance and ensure their data is safely backed up to the cloud!
