# Settings File Configuration

The Password Manager browser extension now supports loading database configuration from a settings file. This allows users to pre-configure their database location and preferences without manually selecting the database file each time.

## Settings File Format

Create a JSON file with the following structure:

```json
{
  "databasePath": "/path/to/your/password-database.db",
  "databaseName": "My Password Database",
  "autoRememberLocation": true
}
```

### Required Fields

- **`databasePath`** (string, required): The full path to your Password Manager database file. This serves as a reference for where your database is located.

### Optional Fields

- **`databaseName`** (string, optional): A friendly name for your database. Defaults to "Database from settings" if not provided.
- **`autoRememberLocation`** (boolean, optional): Whether to remember this database location for future sessions. Defaults to false.

## How to Use

1. **Create Settings File**: Create a JSON file (e.g., `password-manager-settings.json`) with your database configuration.

2. **Load in Extension**: 
   - Open the Password Manager extension
   - Select "Load from settings file" option
   - Choose your settings JSON file
   - The extension will save your preferences and guide you to select your database file

3. **Database Selection**: After loading settings, you'll still need to manually select your database file due to browser security restrictions, but the extension will:
   - Remember your configured path
   - Show you the expected location
   - Store your preferences for future reference

## Example Settings Files

### Basic Configuration
```json
{
  "databasePath": "C:\\Users\\YourName\\Documents\\PasswordManager\\passwords.db",
  "databaseName": "Personal Passwords"
}
```

### Advanced Configuration
```json
{
  "databasePath": "/home/user/Documents/password-manager/work-passwords.db",
  "databaseName": "Work Password Database",
  "autoRememberLocation": true
}
```

### Multiple Database Setup
You can create different settings files for different databases:

**personal-settings.json:**
```json
{
  "databasePath": "/Users/yourname/personal-passwords.db",
  "databaseName": "Personal Passwords"
}
```

**work-settings.json:**
```json
{
  "databasePath": "/Users/yourname/work-passwords.db", 
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