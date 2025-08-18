#!/usr/bin/env pwsh
# WiX MSI Build Health Check
# Comprehensive validation of WiX configuration to prevent ICE80 errors

Write-Host "WiX MSI Build Health Check" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

$errorCount = 0
$warningCount = 0

# Check 1: Main WiX file validation
Write-Host ""
Write-Host "1. Validating main WiX file..." -ForegroundColor Yellow
try {
    $result = & pwsh build-tools/validate-components.ps1 -WxsFile build-tools/InventoryManagementSystem.wxs
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Main WiX file passed validation" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Main WiX file failed validation" -ForegroundColor Red
        $errorCount++
    }
} catch {
    Write-Host "   ✗ Error validating main WiX file: $_" -ForegroundColor Red
    $errorCount++
}

# Check 2: Build script validation
Write-Host ""
Write-Host "2. Checking Build-MSI.ps1 configuration..." -ForegroundColor Yellow

$buildScript = Get-Content "build-tools/Build-MSI.ps1" -Raw

# Check for new regex pattern
if ($buildScript -match 'Post-process.*Win64.*attribute' -and $buildScript -match 'Component\s+\(\[\^>\]\*\?\)>') {
    Write-Host "   ✓ New Win64 attribute logic found" -ForegroundColor Green
} else {
    Write-Host "   ! Win64 attribute logic may need verification" -ForegroundColor Yellow
    $warningCount++
}

# Check for Heat tool usage
if ($buildScript -match 'heatExe.*dir.*Published') {
    Write-Host "   ✓ Heat tool configuration found" -ForegroundColor Green
} else {
    Write-Host "   ! Heat tool configuration may need verification" -ForegroundColor Yellow
    $warningCount++
}

# Check for post-processing
if ($buildScript -match 'Post-process.*files.*Win64') {
    Write-Host "   ✓ Post-processing for Win64 attributes found" -ForegroundColor Green
} else {
    Write-Host "   ✗ Post-processing for Win64 attributes missing" -ForegroundColor Red
    $errorCount++
}

# Check 3: Required directories
Write-Host ""
Write-Host "3. Checking required directories..." -ForegroundColor Yellow

$requiredDirs = @("Published", "Published/Api", "Published/Agent")
foreach ($dir in $requiredDirs) {
    if (Test-Path $dir) {
        Write-Host "   ✓ Directory exists: $dir" -ForegroundColor Green
    } else {
        Write-Host "   ! Directory missing: $dir (required for MSI build)" -ForegroundColor Yellow
        $warningCount++
    }
}

# Check 4: Icon file validation
Write-Host ""
Write-Host "4. Checking icon file..." -ForegroundColor Yellow

if (Test-Path "../Inventory.WebApp/wwwroot/favicon.ico") {
    Write-Host "   ✓ Icon file found" -ForegroundColor Green
} else {
    Write-Host "   ! Icon file missing (may cause LGHT0103 error)" -ForegroundColor Yellow
    $warningCount++
}

# Summary
Write-Host ""
Write-Host "=" * 50
Write-Host "HEALTH CHECK SUMMARY" -ForegroundColor Cyan
Write-Host "Errors: $errorCount" -ForegroundColor $(if ($errorCount -eq 0) { "Green" } else { "Red" })
Write-Host "Warnings: $warningCount" -ForegroundColor $(if ($warningCount -eq 0) { "Green" } else { "Yellow" })

if ($errorCount -eq 0 -and $warningCount -eq 0) {
    Write-Host ""
    Write-Host "✓ ALL CHECKS PASSED - MSI build should succeed!" -ForegroundColor Green
    Write-Host "The ICE80 errors should be resolved." -ForegroundColor Green
    exit 0
} elseif ($errorCount -eq 0) {
    Write-Host ""
    Write-Host "⚠ BUILD SHOULD WORK with $warningCount warnings" -ForegroundColor Yellow
    Write-Host "The ICE80 errors should be resolved, but some optional components are missing." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host ""
    Write-Host "✗ CRITICAL ISSUES FOUND - MSI build may fail" -ForegroundColor Red
    Write-Host "Please fix the $errorCount errors before building." -ForegroundColor Red
    exit 1
}