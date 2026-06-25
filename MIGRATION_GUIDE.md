# Vault Guard Migration Guide

This guide helps you migrate your passwords from other password managers and browsers to Vault Guard App.

## 📋 Table of Contents
- [Supported Import Sources](#supported-import-sources)
- [Step-by-Step Migration Instructions](#step-by-step-migration-instructions)
- [Tips for a Smooth Migration](#tips-for-a-smooth-migration)
- [Troubleshooting](#troubleshooting)

## 🔄 Supported Import Sources

### Vault Guards
- **1Password** - CSV and 1PUX formats
- **Bitwarden** - CSV format
- **LastPass** - CSV format with folder preservation
- **Dashlane** - CSV format with categories
- **KeePass** - CSV format with groups

### Web Browsers
- **Google Chrome** - Password CSV export
- **Microsoft Edge** - Password CSV export
- **Mozilla Firefox** - Logins CSV export
- **Apple Safari** - Password CSV export

## 📖 Step-by-Step Migration Instructions

### From 1Password

1. **Export from 1Password**:
   - Open 1Password and log in
   - Go to **File** → **Export** → **All Items**
   - Choose either:
     - **1PUX** format (encrypted archive - recommended)
     - **CSV** format (unencrypted - less secure but simpler)
   - Save the file to a secure location

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Navigate to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **1Password** as the source
   - Choose your exported file
   - Review the preview (first 5 items)
   - Click **Import** to complete

3. **Verify Import**:
   - Check that all passwords were imported
   - Verify folders/collections are preserved
   - Test a few logins to ensure they work

4. **Secure Cleanup**:
   - Delete the exported CSV/1PUX file
   - Empty your trash/recycle bin

### From Bitwarden

1. **Export from Bitwarden**:
   - Log into Bitwarden (web vault or desktop app)
   - Go to **Tools** → **Export Vault**
   - Select **File Format**: `.csv`
   - Click **Export Vault**
   - Save the CSV file securely

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Bitwarden** as the source
   - Browse to your CSV file
   - Preview the items
   - Click **Import**

3. **Post-Import**:
   - Verify all items imported correctly
   - Check that folders are preserved
   - Securely delete the exported CSV file

### From LastPass

1. **Export from LastPass**:
   - Log into LastPass (browser extension or web vault)
   - Click **Advanced Options** (⚙️ icon)
   - Select **Advanced** → **Export**
   - Your passwords will open in a new tab in CSV format
   - Save this as a CSV file (Ctrl+S or Cmd+S)

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Navigate to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **LastPass** as the source
   - Choose your CSV file
   - Review the preview
   - Click **Import**

3. **Note**: LastPass folders will be imported as collections

### From Dashlane

1. **Export from Dashlane**:
   - Open Dashlane desktop app
   - Go to **Settings** → **Export Data**
   - Choose **Unsecured archive (readable) in CSV**
   - Click **Export to CSV**
   - Save the file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Dashlane**
   - Browse to your CSV file
   - Preview and import

3. **Verify**: Categories from Dashlane become collections in Vault Guard

### From KeePass

1. **Export from KeePass**:
   - Open KeePass
   - Go to **File** → **Export**
   - Select **Generic CSV Exporter** (1.xx format)
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **KeePass**
   - Choose your CSV file
   - Review and import

3. **Note**: KeePass groups are preserved as collections

### From Google Chrome

1. **Export from Chrome**:
   - Open Chrome
   - Go to **Settings** (⚙️)
   - Click **Autofill and passwords** → **Google Vault Guard**
   - Click the ⚙️ (Settings) icon
   - Select **Export passwords**
   - Authenticate if prompted
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Google Chrome**
   - Choose your CSV file
   - Import the passwords

3. **Security**: Delete the CSV file immediately after import

### From Microsoft Edge

1. **Export from Edge**:
   - Open Microsoft Edge
   - Go to **Settings** → **Profiles** → **Passwords**
   - Click the three dots (⋯) next to **Saved passwords**
   - Select **Export passwords**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Follow the same process as Chrome import
   - Select **Microsoft Edge** as the source

### From Mozilla Firefox

1. **Export from Firefox**:
   - Open Firefox
   - Type `about:logins` in the address bar
   - Click the three dots menu (⋮)
   - Select **Export Logins**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Mozilla Firefox**
   - Choose your CSV file
   - Import

3. **Note**: Firefox timestamps are preserved during import

### From Apple Safari

1. **Export from Safari** (macOS):
   - Open Safari
   - Go to **Settings/Preferences**
   - Click **Passwords**
   - Authenticate with your Mac password
   - Click the three dots (⋯) menu
   - Select **Export Passwords**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Apple Safari**
   - Choose your CSV file
   - Review and import

## 💡 Tips for a Smooth Migration

### Before You Start
1. **Backup First**: Ensure your current password manager has a backup
2. **Clean Up**: Remove duplicate or unused passwords before exporting
3. **Update Weak Passwords**: Use this as an opportunity to strengthen weak passwords
4. **Organize**: Review your folder/collection structure

### During Migration
1. **Preview First**: Always review the import preview before committing
2. **Small Batches**: If you have many passwords, consider importing in smaller batches
3. **Verify Format**: Make sure the export file is in CSV format
4. **Check Encoding**: Ensure the CSV file is UTF-8 encoded

### After Migration
1. **Verify Everything**: Check that all items imported correctly
2. **Test Logins**: Test several logins to ensure passwords work
3. **Delete Exports**: Securely delete all exported CSV files
4. **Keep Both Active**: Keep your old password manager active for a few weeks as backup
5. **Update Browser Extension**: Install the Vault Guard browser extension
6. **Update Mobile Apps**: Install the mobile app on your devices

## 🔒 Security Best Practices

1. **Export File Security**:
   - Never email CSV files to yourself
   - Delete export files immediately after import
   - Don't save exports to cloud storage
   - Use encrypted USB drives for transfers if needed

2. **Verification**:
   - Check all critical passwords imported correctly
   - Verify 2FA codes if imported (from Safari)
   - Test password autofill in browser

3. **Cleanup**:
   - Clear browser downloads
   - Empty recycle bin
   - Run secure file deletion tools if concerned

## 🔧 Troubleshooting

### Import Fails

**Problem**: Import fails with error message

**Solutions**:
- Verify the CSV file isn't corrupted (open in text editor)
- Check that the file format matches the selected source
- Ensure the CSV is properly formatted (no missing columns)
- Try importing a smaller subset first

### Missing Passwords

**Problem**: Some passwords didn't import

**Solutions**:
- Check if they were in a special vault/folder
- Verify they were included in the export
- Try exporting and importing again
- Check import logs for skipped items

### Wrong Format Detection

**Problem**: The import tool doesn't recognize your file

**Solutions**:
- Verify you selected the correct source
- Check the file extension is `.csv`
- Open the CSV in a text editor and verify the header row
- Ensure the CSV uses UTF-8 encoding

### Duplicate Items

**Problem**: Some items were imported multiple times

**Solutions**:
- Use the duplicate detection feature
- Manually review and delete duplicates
- Re-import with "Skip Duplicates" option if available

### Special Characters Issues

**Problem**: Passwords with special characters are corrupted

**Solutions**:
- Ensure the CSV file is UTF-8 encoded
- Check that special characters are properly escaped
- Try re-exporting with different encoding options

## 📞 Getting Help

If you encounter issues not covered in this guide:

1. Check the [main README](ReadMe.md) for general documentation
2. Review the [User Guide](USER_GUIDE.md) for detailed usage instructions
3. Visit [GitHub Issues](https://github.com/dotnetappdev/VaultGuardApp/issues) to report problems
4. Join our community discussions for migration tips

## 🎯 Success Checklist

After migration, ensure you've completed:

- [ ] All passwords imported successfully
- [ ] Folders/collections are organized correctly
- [ ] Test logins work from Vault Guard
- [ ] Browser extension is installed and working
- [ ] Mobile apps are set up
- [ ] Export files are securely deleted
- [ ] Old password manager account is secure
- [ ] Master password is strong and unique

---

**Welcome to Vault Guard! 🎉**

You've successfully migrated your passwords. Enjoy secure, cross-platform password management with enterprise-grade encryption.
