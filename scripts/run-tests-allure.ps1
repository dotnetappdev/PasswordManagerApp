<#
.SYNOPSIS
  Runs the test suites and builds (optionally serves) an Allure HTML dashboard from their results.

.DESCRIPTION
  Every test project references JunitXml.TestLogger, so `dotnet test --logger junit` produces a JUnit XML
  file per suite - uniformly across NUnit, xUnit and MSTest/Playwright (no per-framework Allure adapter).
  Allure's bundled JUnit plugin turns those XML files into the dashboard, so all suites appear together.

  Results (JUnit XML) are written straight into ./allure-results, then ./allure-report is generated with the
  Allure CLI (or `npx allure-commandline`, which needs Node + a JRE).

.PARAMETER Serve
  Open a live, auto-refreshing report instead of writing a static site.

.PARAMETER IncludeUi
  Also run the Playwright UI suite (self-hosts the Blazor app + Chromium; slower, needs browsers installed).

.EXAMPLE
  pwsh scripts/run-tests-allure.ps1
  pwsh scripts/run-tests-allure.ps1 -IncludeUi -Serve
#>
param([switch]$Serve, [switch]$IncludeUi)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$results = Join-Path $root 'allure-results'
$report  = Join-Path $root 'allure-report'
Remove-Item -Recurse -Force $results -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $results | Out-Null

# Non-UI suites always run. The UI suite is opt-in (it launches the web app + a browser).
$projects = [System.Collections.ArrayList]@(
  'VaultGuard.BackEnd.Tests/VaultGuard.BackEnd.Tests.csproj',
  'VaultGuard.Tests.QrLogin/VaultGuard.Tests.QrLogin.csproj',
  'VaultGuard.Tests.OTP/VaultGuard.Tests.OTP.csproj'
)
if ($IncludeUi) { [void]$projects.Add('VaultGuard.Tests.Playwright/VaultGuard.Tests.Playwright.csproj') }

foreach ($p in $projects) {
  $name = [System.IO.Path]::GetFileNameWithoutExtension($p)
  $xml  = Join-Path $results "$name.junit.xml"
  Write-Host "Running $name ..." -ForegroundColor Cyan
  # A failing test must not abort the report, so swallow the non-zero exit code.
  & dotnet test $p -v q --nologo --logger "junit;LogFilePath=$xml"
}

$count = (Get-ChildItem $results -Filter *.xml -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "Wrote $count JUnit result file(s) to $results" -ForegroundColor Green

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
