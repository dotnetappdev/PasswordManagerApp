<#
.SYNOPSIS
  Runs the test suites, aggregates Allure results and builds (optionally serves) the HTML dashboard.

.DESCRIPTION
  Allure-instrumented suites (currently VaultGuard.BackEnd.Tests - all NUnit fixtures carry [AllureNUnit])
  write per-test JSON into their output allure-results folder. This script runs the tests, collects every
  allure-results folder into a single ./allure-results, then generates ./allure-report via the Allure CLI
  (falls back to `npx allure-commandline`, which needs Node + Java).

.PARAMETER Serve
  After generating, open the live report in a browser instead of writing a static site.

.PARAMETER IncludeBlazorUi
  Also run VaultGuard.Tests.Playwright's assertion suite and fold its .trx into the same dashboard -
  matching what run-tests.yml's publish-allure-report job does in CI. Off by default: this suite self-
  hosts a whole VaultGuard.Web instance, installs a real browser, and takes several minutes, which isn't
  what you want for a quick "did my NUnit fixture wire up right" check.

.EXAMPLE
  pwsh scripts/run-tests-allure.ps1
  pwsh scripts/run-tests-allure.ps1 -Serve
  pwsh scripts/run-tests-allure.ps1 -IncludeBlazorUi
#>
param([switch]$Serve, [switch]$IncludeBlazorUi)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$results = Join-Path $root 'allure-results'
$report  = Join-Path $root 'allure-report'
Remove-Item -Recurse -Force $results -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $results | Out-Null

# Allure-instrumented test projects. Add more here as suites gain the Allure adapter.
$projects = @(
  'VaultGuard.BackEnd.Tests/VaultGuard.BackEnd.Tests.csproj'
)

foreach ($p in $projects) {
  Write-Host "Running $p ..." -ForegroundColor Cyan
  # Don't stop the whole run on a failing test - we still want the report.
  dotnet test $p -v q --nologo
}

# Collect every produced allure-results folder (in project output dirs) into the aggregate directory.
Get-ChildItem -Recurse -Directory -Filter 'allure-results' |
  Where-Object { $_.FullName -ne $results } |
  ForEach-Object {
    Get-ChildItem $_.FullName -File | Copy-Item -Destination $results -Force
  }

if ($IncludeBlazorUi) {
  Write-Host "Running VaultGuard.Tests.Playwright (assertion suite, excludes ScreenshotCaptureTests) ..." -ForegroundColor Cyan
  $playwrightProject = 'VaultGuard.Tests.Playwright/VaultGuard.Tests.Playwright.csproj'
  $playwrightTrxDir = Join-Path $root 'blazor-ui-test-results'
  dotnet build $playwrightProject -c Debug --nologo -v q
  pwsh (Join-Path $root 'VaultGuard.Tests.Playwright/bin/Debug/net10.0/playwright.ps1') install --with-deps chromium
  $env:HEADED = '0'
  # Allure 2's own trx-plugin reads this .trx directly - no [Allure*] adapter exists for MSTest (see
  # docs/TESTING.md's Allure dashboard section), so it's copied straight into $results as-is rather than
  # converted to the NUnit suites' per-test JSON format.
  dotnet test $playwrightProject --no-build -c Debug --filter "FullyQualifiedName!~ScreenshotCaptureTests" --results-directory $playwrightTrxDir --logger "trx;LogFileName=blazor-ui-tests.trx"
  Get-ChildItem -Recurse -Path $playwrightTrxDir -Filter 'blazor-ui-tests.trx' -ErrorAction SilentlyContinue |
    Copy-Item -Destination $results -Force
}

$count = (Get-ChildItem $results -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Aggregated $count Allure result files into $results" -ForegroundColor Green

function Invoke-Allure([string[]]$AllureArgs) {
  if (Get-Command allure -ErrorAction SilentlyContinue) { & allure @AllureArgs }
  else { & npx -y allure-commandline @AllureArgs }
}

if ($Serve) {
  Invoke-Allure @('serve', $results)
} else {
  Invoke-Allure @('generate', $results, '-o', $report, '--clean')
  Write-Host "Report: $report/index.html" -ForegroundColor Green
}
