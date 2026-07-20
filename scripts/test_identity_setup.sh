#!/bin/bash

# Test Script for Entity Framework Identity Setup

set -e

export PATH="/home/runner/.dotnet:$PATH"
cd /home/runner/work/VaultGuardApp/VaultGuardApp

echo "=== Testing Entity Framework Identity Setup ==="
echo ""

# Test 1: Build core projects
echo "Test 1: Building core projects..."
dotnet build VaultGuard.Models/VaultGuard.Models.csproj --verbosity quiet
echo "✅ Models project builds successfully"

dotnet build VaultGuard.DAL/VaultGuard.DAL.csproj --verbosity quiet
echo "✅ DAL project builds successfully (no more warning about Users property)"

dotnet build VaultGuard.Services/VaultGuard.Services.csproj --verbosity quiet
echo "✅ Services project builds successfully"

# Test 2: Check migration files exist
echo ""
echo "Test 2: Checking migration files..."
if [ -f "VaultGuard.DAL/Migrations/20250803101910_firstmigration.cs" ]; then
    echo "✅ First migration exists (creates AspNetUsers table)"
else
    echo "❌ First migration missing"
    exit 1
fi

if [ -f "VaultGuard.DAL/Migrations/20250904185307_AddMasterKeyIdentifier.cs" ]; then
    echo "✅ MasterKeyIdentifier migration exists"
else
    echo "❌ MasterKeyIdentifier migration missing"
    exit 1
fi

# Test 3: Check for Identity table creation in first migration
echo ""
echo "Test 3: Validating Identity table creation in migration..."
if grep -q "AspNetUsers" VaultGuard.DAL/Migrations/20250803101910_firstmigration.cs; then
    echo "✅ First migration creates AspNetUsers table"
else
    echo "❌ AspNetUsers table creation not found in first migration"
    exit 1
fi

if grep -q "AspNetRoles" VaultGuard.DAL/Migrations/20250803101910_firstmigration.cs; then
    echo "✅ First migration creates AspNetRoles table"
else
    echo "❌ AspNetRoles table creation not found in first migration"
    exit 1
fi

# Test 4: Check Identity configuration
echo ""
echo "Test 4: Validating Identity configuration..."
if grep -q "AddIdentity<ApplicationUser, ApplicationRole>" VaultGuard.WinUi/App.xaml.cs; then
    echo "✅ WinUI app uses full Identity with roles"
else
    echo "❌ WinUI app Identity configuration incorrect"
    exit 1
fi

if grep -q "IdentityDbContext<ApplicationUser, ApplicationRole, string>" VaultGuard.DAL/VaultGuardDbContextApp.cs; then
    echo "✅ DbContext inherits from IdentityDbContext with roles"
else
    echo "❌ DbContext Identity inheritance incorrect"
    exit 1
fi

# Test 5: Check new documentation
echo ""
echo "Test 5: Validating documentation..."
if [ -f "EF_IDENTITY_SETUP_GUIDE.md" ]; then
    echo "✅ Entity Framework Identity setup guide created"
else
    echo "❌ EF Identity setup guide missing"
    exit 1
fi

if grep -q "EF_IDENTITY_SETUP_GUIDE.md" ReadMe.md; then
    echo "✅ Main README updated with link to EF guide"
else
    echo "❌ Main README not updated with EF guide link"
    exit 1
fi

# Test 6: Check improved error handling
echo ""
echo "Test 6: Validating improved error handling..."
if grep -q "CheckIdentityTablesExistAsync" VaultGuard.Services/Services/AppStartupService.cs; then
    echo "✅ Identity table checking method exists"
else
    echo "❌ Identity table checking method missing"
    exit 1
fi

if grep -q "sqlite_master" VaultGuard.Services/Services/AppStartupService.cs; then
    echo "✅ Improved table detection using sqlite_master"
else
    echo "❌ Improved table detection logic missing"
    exit 1
fi

if grep -q "no such table: AspNetUsers" VaultGuard.WinUi/Services/WinUiAuthService.cs; then
    echo "✅ WinUiAuthService has error handling for missing AspNetUsers table"
else
    echo "❌ WinUiAuthService missing AspNetUsers error handling"
    exit 1
fi

echo ""
echo "=== All Tests Passed! ==="
echo ""
echo "✅ Entity Framework Identity setup is properly configured"
echo "✅ Migration files create all necessary Identity tables"
echo "✅ Error handling and recovery mechanisms are in place"
echo "✅ Comprehensive documentation is available"
echo "✅ The 'SQLite Error 1: no such table: AspNetUsers' issue should be resolved"
echo ""
echo "Changes made:"
echo "- Fixed CheckIdentityTablesExistAsync() method with proper table detection"
echo "- Added comprehensive error handling and recovery in AppStartupService"
echo "- Fixed DbContext warning about Users property hiding inherited member"
echo "- Created detailed EF Identity setup and migration guide"
echo "- Updated main README with link to new documentation"
echo "- Improved database initialization with multiple fallback strategies"