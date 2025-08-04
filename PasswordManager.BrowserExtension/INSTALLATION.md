# Password Manager Browser Extension Installation Guide

This guide will help you install and set up the Password Manager browser extension to work with your Password Manager App.

## Prerequisites

Before installing the extension, ensure you have:

1. **Password Manager API Running**: The Password Manager API should be running and accessible
   - Default URL: `http://localhost:5000`
   - Ensure it's accessible from your browser
   - Test by visiting `http://localhost:5000/api/passworditems` (should return 401 if not authenticated)

2. **User Account**: You need a valid user account in the Password Manager system

3. **Supported Browser**: 
   - Chrome 88+ (recommended)
   - Microsoft Edge 88+ (Chromium-based, recommended)
   - Firefox 109+

   **Note**: The extension requires modern browsers with Manifest V3 support. Older versions of Edge (Legacy/EdgeHTML) are not supported.

## Installation Steps

### For Chrome

1. **Download the Extension**
   - Navigate to the `PasswordManager.BrowserExtension` folder in the repository
   - This is your extension folder

2. **Enable Developer Mode**
   - Open Chrome and go to `chrome://extensions/`
   - Toggle "Developer mode" on in the top-right corner

3. **Load the Extension**
   - Click "Load unpacked"
   - Select the `PasswordManager.BrowserExtension` folder
   - The extension should appear in your extensions list

4. **Pin the Extension** (Optional but Recommended)
   - Click the puzzle piece icon in the Chrome toolbar
   - Find "Password Manager Extension" and click the pin icon
   - The extension icon will now appear in your toolbar

### For Microsoft Edge

1. **Download the Extension**
   - Navigate to the `PasswordManager.BrowserExtension` folder in the repository
   - This is your extension folder

2. **Enable Developer Mode**
   - Open Microsoft Edge and go to `edge://extensions/`
   - Toggle "Developer mode" on in the left sidebar

3. **Load the Extension**
   - Click "Load unpacked"
   - Select the `PasswordManager.BrowserExtension` folder
   - The extension should appear in your extensions list

4. **Pin the Extension** (Optional but Recommended)
   - Click the puzzle piece icon in the Edge toolbar
   - Find "Password Manager Extension" and click the pin icon
   - The extension icon will now appear in your toolbar

5. **Edge-Specific Settings** (If Needed)
   - If you encounter any issues, go to `edge://settings/privacy`
   - Ensure "Block potentially unwanted apps" is disabled for development
   - Check that the extension has proper permissions in `edge://extensions/`

### For Firefox

1. **Download the Extension**
   - Navigate to the `PasswordManager.BrowserExtension` folder

2. **Load Temporary Add-on**
   - Open Firefox and go to `about:debugging#/runtime/this-firefox`
   - Click "Load Temporary Add-on..."
   - Select the `manifest.json` file from the extension folder

3. **Note**: Firefox temporary add-ons are removed when Firefox closes. For permanent installation, you'll need to package the extension.

## Initial Setup

### 1. Configure API Settings

1. **Open Extension Popup**
   - Click the Password Manager extension icon in your browser toolbar

2. **Go to Settings**
   - On the login screen, click "Settings" at the bottom

3. **Set API URL**
   - Enter your Password Manager API URL (default: `http://localhost:5000`)
   - Click "Test Connection" to verify connectivity
   - Click "Save Settings" if the test is successful

### 2. Login to Your Account

1. **Return to Login**
   - Click the back arrow (←) to return to the login screen

2. **Enter Credentials**
   - Enter your Password Manager username/email and password
   - Click "Login"

3. **Verify Login**
   - You should see the main extension interface with "Credentials" and "Generate" tabs
   - The extension is now ready to use!

## Testing the Extension

### 1. Use the Test Page

1. **Open Test Page**
   - Open `test-page.html` from the extension folder in your browser
   - This page contains various form types for testing

2. **Look for Icons**
   - You should see blue icons (👤 for username, 🔑 for password) next to input fields
   - If icons don't appear, refresh the page

3. **Test Autofill**
   - Click the username icon (👤) to see available credentials
   - Click the password icon (🔑) to see password options
   - Select a credential to test autofill functionality

### 2. Test on Real Websites

1. **Visit Login Pages**
   - Go to any website with login forms (GitHub, Google, etc.)
   - Icons should appear next to username and password fields

2. **Test Password Generation**
   - Click the password icon on a registration form
   - Select "Generate New Password"
   - Customize options in the extension popup

## Troubleshooting

### Extension Not Loading

**Issue**: Extension doesn't appear in Chrome extensions list
- **Solution**: Make sure you selected the correct folder containing `manifest.json`
- **Check**: Verify all required files are present in the extension folder

**Issue**: Extension doesn't appear in Microsoft Edge extensions list
- **Solution**: Make sure you went to `edge://extensions/` (not the old Edge)
- **Check**: Ensure you're using Chromium-based Edge (Edge 88+, not Legacy Edge)
- **Verify**: Developer mode is enabled in the left sidebar
- **Alternative**: Try restarting Edge and loading the extension again

### No Icons Appearing

**Issue**: Icons don't show up on login forms
- **Solution 1**: Refresh the webpage after installing the extension
- **Solution 2**: Check browser console for JavaScript errors
- **Solution 3**: Ensure the website doesn't block content scripts (check CSP headers)

### Connection Issues

**Issue**: "Connection failed" or login errors
- **Solution 1**: Verify API URL in extension settings
- **Solution 2**: Check if Password Manager API is running (`http://localhost:5000`)
- **Solution 3**: Check browser console for CORS or network errors
- **Solution 4**: Ensure the API accepts requests from `chrome-extension://` origins

**Edge-Specific Connection Issues**:
- **Solution 1**: Check Edge security settings - go to `edge://settings/privacy` and ensure strict tracking prevention isn't blocking API calls
- **Solution 2**: Verify that Enhanced Security mode isn't interfering with local connections
- **Solution 3**: Try adding `extension-scheme://` to API CORS settings if needed
- **Solution 4**: Ensure Windows Defender SmartScreen isn't blocking the extension

### Authentication Problems

**Issue**: Login fails with correct credentials
- **Solution 1**: Try the API endpoint directly in browser
- **Solution 2**: Check API logs for authentication errors
- **Solution 3**: Verify the login endpoint URL (should be `/api/authentication/login`)

### Icons Interfering with Website

**Issue**: Extension icons overlap with website elements
- **Solution**: The extension uses high z-index values to avoid conflicts
- **Report**: If you find specific websites with conflicts, report them for fixes

## Using the Extension

### Basic Autofill

1. **Navigate to Login Page**: Visit any website with login forms
2. **Click Username Icon** (👤): Shows matching credentials for the current domain
3. **Select Credential**: Click on a credential to autofill both username and password
4. **Submit Form**: Continue with your normal login process

### Password Generation

1. **Click Password Icon** (🔑): Shows password options
2. **Select "Generate New Password"**: Opens password generator
3. **Customize Options**: Adjust length, character types in extension popup
4. **Generate and Fill**: Password is generated and filled into the field

### Managing Credentials

1. **Open Extension Popup**: Click the extension icon
2. **Browse Credentials**: Use the Credentials tab to view and search
3. **Search**: Type in the search box to filter credentials
4. **Fill Manually**: Click any credential to fill it into the current tab

## Security Notes

- **No Local Storage**: Extension doesn't store passwords locally
- **API Communication**: All data comes from your secure Password Manager API
- **Encryption**: Uses the same security model as your main app
- **Permissions**: Extension only accesses current tab and storage

## Advanced Configuration

### Custom API Endpoints

If your Password Manager API uses different endpoints:

1. Modify `background.js` in the extension folder
2. Update the API endpoint URLs:
   - Login: `/api/authentication/login`
   - Get Items: `/api/passworditems`
   - Health Check: `/api/passworditems` (for connection test)

### HTTPS Configuration

For production use with HTTPS:

1. Update API URL in extension settings to use `https://`
2. Ensure your Password Manager API has valid SSL certificates
3. Update `host_permissions` in `manifest.json` if needed

### Cross-Origin Issues

If you encounter CORS issues:

1. Configure your Password Manager API to allow extension origins
2. Add `chrome-extension://*` to allowed origins in API CORS settings

## Getting Help

If you encounter issues:

1. **Check Browser Console**: Press F12 and look for error messages
2. **Verify API Status**: Ensure Password Manager API is running and accessible
3. **Test Connection**: Use the "Test Connection" button in extension settings
4. **Check Permissions**: Ensure the extension has necessary permissions

## Updating the Extension

To update the extension:

1. **Download New Version**: Get the updated extension files
2. **Replace Files**: Replace files in your extension folder
3. **Reload Extension**: 
   - **Chrome**: Go to `chrome://extensions/`, find the extension, and click the reload icon
   - **Microsoft Edge**: Go to `edge://extensions/`, find the extension, and click the reload icon
   - **Firefox**: Remove and re-add the temporary add-on
4. **Test**: Verify the extension works correctly after update

The extension will automatically preserve your settings and login status across updates.

## Microsoft Edge Specific Notes

### Edge Store Deployment (Future)

When ready for production deployment to the Microsoft Edge Add-ons store:

1. **Package Extension**: Create a ZIP file of the extension folder
2. **Developer Account**: Register for a Microsoft Edge Developer account
3. **Submission**: Upload to https://partner.microsoft.com/en-us/dashboard/microsoftedge
4. **Review Process**: Microsoft will review the extension (typically 7-14 days)
5. **Publication**: Once approved, users can install from the Edge Add-ons store

### Edge-Specific Features

The extension takes advantage of Edge-specific features when available:

- **Enhanced Security**: Works with Edge's enhanced security features
- **Privacy Controls**: Respects Edge's tracking prevention settings
- **Performance**: Optimized for Edge's Chromium engine
- **Integration**: Follows Edge's UI patterns and user experience guidelines

### Edge Enterprise Deployment

For enterprise environments using Microsoft Edge:

1. **Group Policy**: Can be deployed via Group Policy for managed devices
2. **Microsoft Intune**: Compatible with Intune application deployment
3. **Registry Keys**: Supports Edge extension registry configuration
4. **Silent Installation**: Can be installed without user interaction in managed environments