#!/usr/bin/env pwsh
<#
    Runs the full test suite with Coverlet code coverage (via VSTest's XPlat Code Coverage
    collector, already referenced in LvTest.csproj) and renders an HTML report with
    ReportGenerator (dotnet tool: dotnet-reportgenerator-globaltool).

    Usage:
        pwsh ./scripts/run-coverage.ps1
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resultsDir = Join-Path $repoRoot "TestResults"
$reportDir = Join-Path $repoRoot "CoverageReport"

if (Test-Path $resultsDir) { Remove-Item $resultsDir -Recurse -Force }
if (Test-Path $reportDir) { Remove-Item $reportDir -Recurse -Force }

dotnet test $repoRoot --collect:"XPlat Code Coverage" --results-directory $resultsDir

$coverageFiles = Get-ChildItem -Path $resultsDir -Filter "coverage.cobertura.xml" -Recurse
if (-not $coverageFiles) {
    throw "No coverage files were generated under $resultsDir"
}

reportgenerator `
    "-reports:$resultsDir\**\coverage.cobertura.xml" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;TextSummary"

Get-Content (Join-Path $reportDir "Summary.txt")

Write-Host ""
Write-Host "Full HTML report: $reportDir\index.html"
