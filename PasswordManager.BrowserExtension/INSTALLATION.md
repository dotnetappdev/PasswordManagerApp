# Password Manager Browser Extension - Installation Guide

## Quick Setup for Direct Database Access

This guide will help you complete the setup of the Password Manager browser extension to work with SQLite database files directly.

### Prerequisites

- Password Manager application with a created database file
- Chrome/Chromium-based browser (Chrome 88+, Edge 88+)
- Access to download external libraries

### Step 1: Download Required Libraries

The extension needs SQL.js to read SQLite database files. Download the required files:

```bash
# Navigate to the extension directory
cd PasswordManager.BrowserExtension/lib/

# Download SQL.js library (replace placeholder file)
curl -L https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/sql-wasm.js -o sql-wasm.js

# Download SQL.js WebAssembly module  
curl -L https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/sql-wasm.wasm -o sql-wasm.wasm
```

**Alternative: Manual Download**
1. Visit https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/
2. Download `sql-wasm.js` and `sql-wasm.wasm`
3. Place both files in the `PasswordManager.BrowserExtension/lib/` folder

### Step 2: Install Browser Extension

#### Chrome/Chromium Browsers:
1. Open Chrome and go to `chrome://extensions/`
2. Enable "Developer mode" (toggle in top right)
3. Click "Load unpacked"
4. Select the `PasswordManager.BrowserExtension` folder
5. Extension will appear in your browser toolbar

#### Firefox (Alternative):
1. Open Firefox and go to `about:debugging#/runtime/this-firefox`
2. Click "Load Temporary Add-on"
3. Select the `manifest.json` file from the extension folder

### Step 3: Prepare Your Database

1. **Create Database**: Use the main Password Manager application to create your database
2. **Add Credentials**: Add some login credentials through the main app
3. **Note Database Location**: Remember where your database file is saved (usually `.db` or `.sqlite`)

### Step 4: First Use

1. **Click Extension Icon**: Look for the Password Manager icon in your browser toolbar
2. **Load Database**: 
   - Click "Load Database" on the first screen
   - Select your Password Manager database file (.db/.sqlite/.sal)
   - Click "Load Database" button
3. **Authenticate**:
   - Enter the same email you use in the main app
   - Enter your master password
   - Click "Unlock"

### Step 5: Test Functionality

1. **Visit a Website**: Go to any website with a login form
2. **Look for Icons**: You should see 👤 and 🔑 icons next to username/password fields
3. **Test Autofill**: Click the username icon to see your stored credentials
4. **Select Credential**: Click on a credential to auto-fill the form

## Troubleshooting

### Common Issues

**"Failed to load database"**
- Ensure you downloaded the SQL.js files correctly
- Check that your database file isn't corrupted
- Verify the file has a .db, .sqlite, or .sal extension

**"Authentication failed"**
- Double-check your email address (must match exactly)
- Verify your master password is correct
- Ensure the database was created with the same user

**No icons appear on websites**
- Refresh the page after installing the extension
- Check browser console (F12) for JavaScript errors
- Verify the extension has necessary permissions

**"No credentials found"**
- Ensure you have login items stored for the current website
- Check that the website URL matches what you saved
- Try clicking "View All" to see all credentials

### Developer Console

If issues persist, check the browser console:
1. Press F12 to open developer tools
2. Go to Console tab
3. Look for Password Manager related errors
4. Check for SQL.js loading errors

### Permissions

Ensure the extension has these permissions:
- ✅ `storage` - For database and authentication state
- ✅ `activeTab` - For form interaction
- ✅ `unlimitedStorage` - For large database files
- ✅ `notifications` - For user feedback

## Security Notes

- **Database Files**: Keep your database files secure and backed up
- **Master Password**: Never share your master password
- **Browser Storage**: Extension only stores authentication state, not passwords
- **Local Operation**: All decryption happens locally - no data sent to servers

## File Verification

After setup, verify these files exist:

```
PasswordManager.BrowserExtension/
├── lib/
│   ├── sql-wasm.js              # ✅ Should be ~150KB
│   └── sql-wasm.wasm            # ✅ Should be ~400KB
├── database-service.js          # ✅ Database operations
├── crypto-service.js            # ✅ Encryption/decryption
├── background.js                # ✅ Updated for database access
├── popup.html                   # ✅ Updated UI with database screens
└── manifest.json                # ✅ Updated permissions
```

## Support

If you encounter issues:

1. **Check Console Logs**: Browser developer console shows detailed errors
2. **Verify File Sizes**: SQL.js files should be substantial (not empty placeholder files)
3. **Database Integrity**: Ensure your database file opens in the main Password Manager app
4. **Extension Permissions**: Verify all required permissions are granted

## Success Indicators

You'll know everything is working when:
- ✅ Extension loads without console errors
- ✅ Database file loads successfully  
- ✅ Authentication succeeds with your master password
- ✅ Icons appear next to form fields on websites
- ✅ Clicking icons shows your stored credentials
- ✅ Credentials auto-fill correctly

## Next Steps

Once installed and working:
- **Bookmark Important Sites**: Test the extension on your frequently used websites
- **Generate Passwords**: Use the built-in password generator for new accounts
- **Manage Settings**: Access settings to change database files if needed
- **Stay Secure**: Regularly backup your database file

## Installation Steps

### For Chrome/Edge

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
3. **Reload Extension**: Go to `chrome://extensions/`, find the extension, and click the reload icon
4. **Test**: Verify the extension works correctly after update

The extension will automatically preserve your settings and login status across updates.