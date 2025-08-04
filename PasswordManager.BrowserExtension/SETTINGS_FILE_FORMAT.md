# Settings File Configuration

The Password Manager browser extension now supports loading database configuration from a settings file. This allows users to pre-configure their database location and preferences without manually selecting the database file each time.

## Settings File Format

Create a JSON file with the following structure:

### Direct Database Access Mode
```json
{
  "databasePath": "/path/to/your/password-database.db",
  "databaseName": "My Password Database",
  "autoRememberLocation": true
}
```

### API Server Mode
```json
{
  "apiUrl": "https://your-password-manager-api.com",
  "apiKey": "your-api-key-here",
  "databaseName": "My Password Database",
  "autoRememberLocation": true
}
```

### Configuration Options

**For Direct Database Access:**
- **`databasePath`** (string, required): The full path to your Password Manager database file. This serves as a reference for where your database is located.

**For API Server Mode:**
- **`apiUrl`** (string, required): The base URL of your Password Manager API server (e.g., "https://api.yourpasswordmanager.com" or "http://localhost:5000").
- **`apiKey`** (string, required): Your API authentication key for accessing the Password Manager API.

**Common Optional Fields:**
- **`databaseName`** (string, optional): A friendly name for your database/configuration. Defaults to "Database from settings" if not provided.
- **`autoRememberLocation`** (boolean, optional): Whether to remember this configuration for future sessions. Defaults to false.

## How to Use

1. **Create Settings File**: Create a JSON file (e.g., `password-manager-settings.json`) with your database configuration.

2. **Load in Extension**: 
   - Open the Password Manager extension
   - Select "Load from settings file" option
   - Choose your settings JSON file
   - **Direct Database Mode**: The extension will save your preferences and guide you to select your database file
   - **API Server Mode**: The extension will configure API access and allow you to authenticate directly

3. **Authentication**: 
   - **Direct Database Mode**: After loading settings, you'll still need to manually select your database file due to browser security restrictions, but the extension will remember your configured path and show you the expected location
   - **API Server Mode**: Enter your email and master password to authenticate directly with the API server

## Example Settings Files

### Direct Database Access
**Basic Configuration:**
```json
{
  "databasePath": "C:\\Users\\YourName\\Documents\\PasswordManager\\passwords.db",
  "databaseName": "Personal Passwords"
}
```

**Advanced Configuration:**
```json
{
  "databasePath": "/home/user/Documents/password-manager/work-passwords.db",
  "databaseName": "Work Password Database",
  "autoRememberLocation": true
}
```

### API Server Access
**Local API Server:**
```json
{
  "apiUrl": "http://localhost:5000",
  "apiKey": "your-local-api-key",
  "databaseName": "Local Password Server"
}
```

**Remote API Server:**
```json
{
  "apiUrl": "https://your-password-api.example.com",
  "apiKey": "your-secure-api-key-here",
  "databaseName": "Remote Password Database",
  "autoRememberLocation": true
}
```

### Multiple Database Setup
You can create different settings files for different databases:

**personal-settings.json (Direct Database):**
```json
{
  "databasePath": "/Users/yourname/personal-passwords.db",
  "databaseName": "Personal Passwords"
}
```

**work-settings.json (API Server):**
```json
{
  "apiUrl": "https://work-password-api.company.com",
  "apiKey": "work-api-key-here",
  "databaseName": "Work Passwords"
}
```

## Security Notes

- Settings files should be stored securely as they contain path information about your databases
- The extension does not store any passwords or sensitive data in the settings file
- Due to browser security restrictions, you must still manually select your database file each session
- Settings are stored locally in the browser's extension storage

## Troubleshooting

### Invalid JSON Format
If you receive an "Invalid JSON format" error:
- Ensure your JSON file is properly formatted
- Use a JSON validator to check syntax
- Make sure all strings are enclosed in double quotes
- Verify there are no trailing commas

### Missing databasePath
If you receive a "Settings file must contain databasePath field" error:
- Ensure your settings file includes the required `databasePath` field
- Check that the field name is spelled correctly (case-sensitive)

### File Selection Issues
- Make sure your settings file has a `.json` or `.config` extension
- Verify the file is accessible and not corrupted
- Try creating a new settings file with minimal configuration

## Benefits

- **Streamlined Setup**: Pre-configure database locations for faster access
- **Multiple Databases**: Easy switching between different database configurations
- **Team Sharing**: Share settings files (without sensitive data) with team members
- **Backup Configuration**: Keep your database location preferences backed up