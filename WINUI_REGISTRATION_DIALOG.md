# WinUI User Registration Dialog - Visual Documentation

## Overview
The new UserRegistrationDialog provides a comprehensive interface for creating new user accounts with proper cryptographic setup and role-based access control.

## Dialog Layout

### Header
- **Title**: "Create New User Account"
- **Primary Button**: "Create Account" (blue, enabled when form is valid)
- **Secondary Button**: "Cancel" (gray)

### Content Sections

#### 1. Personal Information (Dark gray container)
```
┌─ Personal Information ────────────────────────────┐
│                                                   │
│  First Name: [Text input field]                   │
│  Last Name:  [Text input field]                   │
│  Email:      [Text input field]                   │
│                                                   │
└───────────────────────────────────────────────────┘
```

#### 2. Account Security (Dark gray container)
```
┌─ Account Security ────────────────────────────────┐
│                                                   │
│  Master Password:         [Password field]        │
│  Confirm Master Password: [Password field]        │
│  Password Hint (Optional): [Text field]          │
│                                                   │
└───────────────────────────────────────────────────┘
```

#### 3. Account Role (Dark gray container)
```
┌─ Account Role ────────────────────────────────────┐
│                                                   │
│  [Toggle Switch] Administrator Account            │
│  ○ OFF          Full access to all features       │
│                                                   │
│  User Role: [Dropdown: Standard User ▼]          │
│                                                   │
│  ℹ️ Standard users can manage their own passwords │
│    and access all features.                      │
│                                                   │
└───────────────────────────────────────────────────┘
```

## Role-Based Behavior

### When Admin Toggle is ON:
- Role dropdown is hidden
- Info message shows: "Administrators have full access to all features and can manage other users."

### Role Options (when admin toggle is OFF):
1. **Parent User**: "Parent users can manage child accounts and have full access to their own passwords."
2. **Standard User**: "Standard users can manage their own passwords and access all features." (Default)
3. **Child User**: "Child users have restricted access and are managed by parent users."

## Access Control Features

### Admin Creation Rights:
- ✅ **Admin Users**: Can create any type of account including other admins
- ✅ **Parent Users**: Can create standard and child accounts (no admin toggle shown)
- ✅ **Standard Users**: Can create standard and child accounts (no admin toggle shown)
- ❌ **Child Users**: Cannot create accounts at all (dialog shows error and disables creation)

### Security Features:
- **Password Validation**: Minimum 8 characters, requires uppercase, lowercase, and numbers
- **Email Validation**: Basic format checking
- **Cryptographic Setup**: Automatically generates UserSalt, MasterPasswordHash, and MasterKeyIdentifier
- **Input Sanitization**: All fields are validated before submission

## Error Handling
```
┌─ Error Message (Red container) ───────────────────┐
│  ⚠️ Passwords do not match. Please try again.     │
└───────────────────────────────────────────────────┘
```

## Color Scheme
- **Background**: Dark gray (#404040)
- **Containers**: Darker gray (#2D2D2D)
- **Input Fields**: Medium gray (#3A3A3A) with gray borders (#4A4A4A)
- **Text**: White for labels, light gray (#B0B0B0) for hints
- **Primary Button**: Blue (#4A90E2)
- **Toggle Switch**: System default (blue when on)
- **Error Container**: Dark red (#5D2A2A) with red border (#DC2626)
- **Info Container**: Dark blue (#1E3A5F) with blue border (#4A90E2)

## Integration
- **Access**: Triggered by "Create New Profile" button on login screen
- **Auto-login**: Optionally logs in the new user automatically after creation
- **Profile List**: New user appears in the profile selection list

This dialog ensures proper security setup for all new users while providing an intuitive interface that respects role-based permissions.