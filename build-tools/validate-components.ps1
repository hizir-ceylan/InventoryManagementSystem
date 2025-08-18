#!/usr/bin/env pwsh
# WiX Component Validation Script
# Validates that all Component tags in WiX files have Win64 attribute

param(
    [string]$WxsFile = ""
)

if ([string]::IsNullOrEmpty($WxsFile)) {
    Write-Host "Usage: validate-components.ps1 -WxsFile <path-to-wxs-file>" -ForegroundColor Yellow
    Write-Host "Example: validate-components.ps1 -WxsFile Setup/MSI/ApiFiles.wxs" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $WxsFile)) {
    Write-Host "Error: File not found: $WxsFile" -ForegroundColor Red
    exit 1
}

Write-Host "Validating WiX Component Win64 attributes in: $WxsFile" -ForegroundColor Cyan
Write-Host "=" * 60

$content = Get-Content $WxsFile -Raw

# Remove XML comments before parsing to avoid false positives
$contentWithoutComments = $content -replace '<!--[\s\S]*?-->', ''

$componentMatches = [regex]::Matches($contentWithoutComments, '<Component\s+([^>]*?)>')

$totalComponents = $componentMatches.Count
$win64YesCount = 0
$win64NoCount = 0
$missingWin64 = 0

Write-Host "Found $totalComponents Component tags:" -ForegroundColor Yellow

foreach ($match in $componentMatches) {
    $componentTag = $match.Value
    
    # Extract Component ID for reporting
    $idMatch = [regex]::Match($componentTag, 'Id="([^"]*)"')
    $componentId = if ($idMatch.Success) { $idMatch.Groups[1].Value } else { "Unknown" }
    
    if ($componentTag -match 'Win64="yes"') {
        Write-Host "  ✓ $componentId - Win64='yes'" -ForegroundColor Green
        $win64YesCount++
    } elseif ($componentTag -match 'Win64="no"') {
        Write-Host "  ! $componentId - Win64='no'" -ForegroundColor Yellow
        $win64NoCount++
    } else {
        Write-Host "  ✗ $componentId - Missing Win64 attribute" -ForegroundColor Red
        $missingWin64++
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total Components: $totalComponents"
Write-Host "  Win64='yes': $win64YesCount" -ForegroundColor Green
Write-Host "  Win64='no': $win64NoCount" -ForegroundColor Yellow
Write-Host "  Missing Win64: $missingWin64" -ForegroundColor Red

if ($missingWin64 -eq 0) {
    Write-Host ""
    Write-Host "✓ VALIDATION PASSED: All components have Win64 attribute" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "✗ VALIDATION FAILED: $missingWin64 components missing Win64 attribute" -ForegroundColor Red
    Write-Host "This will cause ICE80 errors during MSI compilation." -ForegroundColor Red
    exit 1
}