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

.EXAMPLE
  pwsh scripts/run-tests-allure.ps1
  pwsh scripts/run-tests-allure.ps1 -Serve
#>
param([switch]$Serve)

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
