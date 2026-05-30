# Screenshots Directory

This directory contains interface screenshots for the Password Manager WinUI application.

## Directory Structure

```
screenshots/
├── README.md                           # This file
├── SCREENSHOT_GUIDE.md                 # Detailed capture instructions
├── winui-login-light.png              # Login screen (light theme)
├── winui-login-dark.png               # Login screen (dark theme)
├── winui-dashboard-light.png          # Main dashboard (light theme)
├── winui-dashboard-dark.png           # Main dashboard (dark theme)
├── winui-password-items-light.png     # Password management (light theme)
├── winui-password-items-dark.png      # Password management (dark theme)
├── winui-add-password-dialog-light.png # Add/edit dialog (light theme)
├── winui-add-password-dialog-dark.png  # Add/edit dialog (dark theme)
├── winui-categories-light.png         # Categories management (light theme)
├── winui-categories-dark.png          # Categories management (dark theme)
├── winui-password-generator-light.png # Password generator (light theme)
├── winui-password-generator-dark.png  # Password generator (dark theme)
├── winui-settings-light.png           # Application settings (light theme)
├── winui-settings-dark.png            # Application settings (dark theme)
├── winui-search-light.png             # Search interface (light theme)
├── winui-search-dark.png              # Search interface (dark theme)
├── winui-context-menu-light.png       # Context menu (light theme)
├── winui-context-menu-dark.png        # Context menu (dark theme)
├── winui-import-light.png             # Import interface (light theme)
└── winui-import-dark.png              # Import interface (dark theme)
```

## Adding Screenshots

To add screenshots when running the WinUI application on Windows:

1. Follow the detailed instructions in [SCREENSHOT_GUIDE.md](SCREENSHOT_GUIDE.md)
2. Capture screenshots for both light and dark themes
3. Use minimum 1920x1080 resolution for high-quality display
4. Save as PNG files using the naming convention above
5. Update the main [screenshots.md](../screenshots.md) file with new images

### Key Requirements:
- **Both Themes**: Capture light and dark theme versions of each interface
- **1Password Styling**: Showcase the professional design elements
- **Full App Frame**: Include title bar, top menus/toolbar, left sidebar, and page content in one capture
- **Navigation Context**: Ensure profile/login context and settings entry are visible where applicable
- **Sample Data**: Use realistic but fake data for demonstrations
- **Consistency**: Maintain same scaling and window sizing across captures

## Guidelines

- **Format**: PNG with transparency where applicable
- **Resolution**: Minimum 1920x1080, recommended 2560x1440 for high-DPI displays
- **Content**: Use sample data only (no real passwords or sensitive information)
- **Coverage**: Avoid cropped CRUD-only screenshots; capture the full application shell
- **Themes**: Capture both light and dark theme versions
- **Consistency**: Same Windows theme and scaling within each theme set
- **Quality**: Clear, readable text and UI elements
- **1Password Style**: Showcase the professional design elements and navigation

### Visual Standards:
- **Light Theme**: Clean white backgrounds (#FAFBFC) with blue accents (#005BFF)
- **Dark Theme**: Professional dark surfaces (#0F0F0F, #1A1A1A) with maintained contrast
- **Typography**: Clear hierarchy and professional presentation
- **Spacing**: Consistent margins and padding following design system
- **Interactions**: Show hover states and selection indicators where appropriate

For detailed instructions, see [SCREENSHOT_GUIDE.md](SCREENSHOT_GUIDE.md) and [screenshots.md](../screenshots.md#how-to-update-screenshots).