# PasswordManager Playwright UI Tests

This project contains comprehensive UI tests for the PasswordManager WinUI application using Microsoft Playwright.

## Overview

The test suite covers CRUD (Create, Read, Update, Delete) operations for all major forms and dialogs in the PasswordManager application:

- **Password Items**: Create, view, edit, and delete password entries
- **Categories**: Manage password categories 
- **Tags**: Create and manage tags for organization
- **User Management**: Admin functionality for managing users

## Test Structure

### Test Classes

1. **PlaywrightTestBase**: Base class providing common setup, teardown, and utility methods
2. **PasswordItemCrudTests**: Tests for password item CRUD operations
3. **CategoryAndTagCrudTests**: Tests for category and tag management
4. **UserManagementCrudTests**: Tests for admin user management functionality

### Key Features

- **Screenshot Capture**: Automatic screenshots at key points and on test failures
- **Test Tracing**: Full trace capture for debugging failed tests
- **Validation Testing**: Form validation and error handling tests
- **Cross-browser Support**: Configured for multiple browser testing

## Prerequisites

1. **Windows Environment**: The WinUI application requires Windows to run
2. **.NET 9.0 SDK**: Required for building and running tests
3. **Built WinUI Application**: The PasswordManager.WinUi project must be built successfully
4. **Playwright Browsers**: Installed automatically on first run

## Setup Instructions

### 1. Install Playwright Browsers

```bash
# Install Playwright browsers (done automatically in tests)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

### 2. Build the WinUI Application

```bash
# Build the WinUI application first
dotnet build PasswordManager.WinUi/PasswordManager.WinUi.csproj --configuration Release
```

### 3. Install Test Dependencies

```bash
# Restore test project dependencies
dotnet restore PasswordManager.Tests.Playwright/PasswordManager.Tests.Playwright.csproj
```

## Running Tests

### Run All Tests

```bash
# Run all Playwright tests
dotnet test PasswordManager.Tests.Playwright/PasswordManager.Tests.Playwright.csproj
```

### Run Specific Test Class

```bash
# Run only password item tests
dotnet test PasswordManager.Tests.Playwright/PasswordManager.Tests.Playwright.csproj --filter "TestCategory=PasswordItemCrudTests"

# Run only user management tests  
dotnet test PasswordManager.Tests.Playwright/PasswordManager.Tests.Playwright.csproj --filter "TestCategory=UserManagementCrudTests"
```

### Run with Visual Studio Test Explorer

1. Open the solution in Visual Studio 2022
2. Build the solution
3. Open Test Explorer (Test → Test Explorer)
4. Run tests individually or in groups

## Test Configuration

The tests are configured via `playwright.config.json`:

- **Headless Mode**: Disabled by default for debugging
- **Screenshots**: Captured on failure and at key test points
- **Video Recording**: Enabled for failed tests
- **Trace Recording**: Full traces for debugging
- **Retries**: 2 retries for flaky tests
- **Timeout**: 30 seconds per test

## Test Data and Screenshots

### Screenshot Organization

Screenshots are automatically saved in the following structure:

```
TestResults/
├── Screenshots/
│   ├── PasswordItemCrudTests_CreatePasswordItem_app_startup_20240101_120000.png
│   ├── PasswordItemCrudTests_CreatePasswordItem_form_filled_20240101_120001.png
│   └── ...
├── Traces/
│   ├── PasswordItemCrudTests_CreatePasswordItem.zip
│   └── ...
└── Videos/
    ├── PasswordItemCrudTests_CreatePasswordItem.webm
    └── ...
```

### Test Artifacts

- **Screenshots**: Taken at key points and on failures
- **Videos**: Recorded for failed tests
- **Traces**: Complete interaction traces for debugging
- **HTML Reports**: Comprehensive test reports with artifacts

## Test Selectors and Automation IDs

The tests use `data-testid` attributes for reliable element selection. When implementing the WinUI application, ensure the following test IDs are added:

### Password Item Form (`AddPasswordDialog.xaml`)
- `add-password-button`: Main add password button
- `add-password-dialog`: The add password dialog
- `title-textbox`: Password item title input
- `category-combobox`: Category selection dropdown
- `type-combobox`: Item type selection dropdown
- `username-textbox`: Username input
- `password-textbox`: Password input
- `website-textbox`: Website URL input
- `save-button`: Save button
- `cancel-button`: Cancel button

### Category Management
- `categories-nav-link`: Navigation to categories page
- `add-category-button`: Add new category button
- `category-dialog`: Category creation/edit dialog
- `category-name-textbox`: Category name input
- `category-description-textbox`: Category description input
- `category-color-picker`: Color selection control
- `save-category-button`: Save category button

### User Management (Admin)
- `admin-nav-link`: Navigation to admin section
- `user-management-link`: User management page link
- `add-user-button`: Add new user button
- `create-user-dialog`: User creation dialog
- `email-textbox`: User email input
- `password-textbox`: User password input
- `first-name-textbox`: First name input
- `create-user-button`: Create user submit button

## Troubleshooting

### Common Issues

1. **Application Not Found**
   - Ensure the WinUI application is built successfully
   - Check the application path in `PlaywrightTestBase.GetApplicationPath()`
   - Verify the target framework and output path

2. **Selector Not Found**
   - Add missing `data-testid` attributes to WinUI controls
   - Use AutomationProperties.AutomationId for WinUI elements
   - Check element visibility and timing

3. **Test Timeouts**
   - Increase timeout values in configuration
   - Add explicit waits for slow operations
   - Check for application startup delays

4. **Screenshots Missing**
   - Ensure TestResults directory has write permissions
   - Check screenshot path configuration
   - Verify test cleanup is running properly

### Debugging Tips

1. **Run Tests in Non-Headless Mode**: Set `headless: false` in config
2. **Use Playwright Inspector**: Add `await page.PauseAsync()` in tests
3. **Check Traces**: Review generated trace files for detailed execution
4. **Enable Verbose Logging**: Add logging to test methods

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: UI Tests

on: [push, pull_request]

jobs:
  ui-tests:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Build WinUI App
      run: dotnet build PasswordManager.WinUi/PasswordManager.WinUi.csproj --configuration Release
    
    - name: Run Playwright Tests
      run: dotnet test PasswordManager.Tests.Playwright/PasswordManager.Tests.Playwright.csproj --logger trx --results-directory TestResults
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults/
```

## Contributing

When adding new tests:

1. **Follow Naming Conventions**: Use descriptive test method names
2. **Add Screenshots**: Include screenshots at key test points
3. **Handle Exceptions**: Wrap test logic in try-catch with failure screenshots
4. **Document Selectors**: Add new test IDs to this README
5. **Test Validation**: Include both positive and negative test cases

## Related Documentation

- [WinUI Project README](../PasswordManager.WinUi/README.md)
- [Main Project README](../README.md)
- [Development Guide](../DEVELOPMENT.md)
- [Microsoft Playwright Documentation](https://playwright.dev/dotnet/)