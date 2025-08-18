#!/usr/bin/env pwsh
# Demonstration of Win64 attribute fix for Heat-generated components
# This simulates the issue and shows how the fix resolves it

Write-Host "WiX Heat Component Win64 Attribute Fix Demonstration" -ForegroundColor Cyan
Write-Host "=" * 55

# Simulate typical Heat-generated content (without Win64 attributes)
$simulatedHeatOutput = @'
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Fragment>
    <DirectoryRef Id="APIFOLDER">
      <Component Id="cmpC7B3629F5054567350AEA9AC5CCC840E" Guid="{C7B36290-5054-5673-50AE-A9AC5CCC840E}">
        <File Id="filC7B3629F5054567350AEA9AC5CCC840E" KeyPath="yes" Source="$(var.ApiSourceDir)\Microsoft.AspNetCore.dll" />
      </Component>
      <Component Id="cmp55EC3450EEF0C8C875072369E9EBE227" Guid="{55EC3450-EEF0-C8C8-7507-2369E9EBE227}">
        <File Id="fil55EC3450EEF0C8C875072369E9EBE227" KeyPath="yes" Source="$(var.ApiSourceDir)\System.Text.Json.dll" />
      </Component>
      <Component Guid="{1CB2507E-9320-DC5B-354A-85A1A284C24E}" Id="cmp1CB2507E9320DC5B354A85A1A284C24E">
        <File Id="fil1CB2507E9320DC5B354A85A1A284C24E" KeyPath="yes" Source="$(var.ApiSourceDir)\Newtonsoft.Json.dll" />
      </Component>
    </DirectoryRef>
  </Fragment>
</Wix>
'@

Write-Host "1. BEFORE FIX - Typical Heat-generated content:" -ForegroundColor Red
Write-Host $simulatedHeatOutput
Write-Host ""

# Apply the old regex (problematic)
$oldFixContent = $simulatedHeatOutput
$oldFixContent = $oldFixContent -replace '<Component Id="([^"]+)" Guid="([^"]+)">', '<Component Id="$1" Guid="$2" Win64="yes">'

Write-Host "2. OLD REGEX FIX (problematic):" -ForegroundColor Yellow
Write-Host $oldFixContent
Write-Host ""

# Count how many components have Win64 with old fix
$oldComponentMatches = [regex]::Matches($oldFixContent, '<Component\s+([^>]*?)>')
$oldWin64Count = 0
foreach ($match in $oldComponentMatches) {
    if ($match.Value -match 'Win64="yes"') {
        $oldWin64Count++
    }
}

Write-Host "Old fix results: $oldWin64Count out of $($oldComponentMatches.Count) components have Win64='yes'" -ForegroundColor Yellow

# Apply the new fix (improved)
$newFixContent = $simulatedHeatOutput
if ($newFixContent -notmatch 'Win64=') {
    # No Win64 attributes found, add to all Component tags
    $newFixContent = $newFixContent -replace '<Component\s+([^>]*?)>', '<Component $1 Win64="yes">'
} else {
    # Some Win64 attributes exist, be more selective
    $newFixContent = $newFixContent -replace '<Component\s+(?![^>]*Win64=)([^>]*?)>', '<Component $1 Win64="yes">'
}

Write-Host "3. NEW IMPROVED FIX:" -ForegroundColor Green
Write-Host $newFixContent
Write-Host ""

# Count how many components have Win64 with new fix
$newComponentMatches = [regex]::Matches($newFixContent, '<Component\s+([^>]*?)>')
$newWin64Count = 0
foreach ($match in $newComponentMatches) {
    if ($match.Value -match 'Win64="yes"') {
        $newWin64Count++
    }
}

Write-Host "New fix results: $newWin64Count out of $($newComponentMatches.Count) components have Win64='yes'" -ForegroundColor Green

Write-Host ""
Write-Host "COMPARISON SUMMARY:" -ForegroundColor Cyan
Write-Host "  Total Components: $($newComponentMatches.Count)"
Write-Host "  Old Fix Success: $oldWin64Count components" -ForegroundColor Yellow
Write-Host "  New Fix Success: $newWin64Count components" -ForegroundColor Green

if ($newWin64Count -eq $newComponentMatches.Count) {
    Write-Host ""
    Write-Host "✓ SUCCESS: New fix correctly handles all component variations!" -ForegroundColor Green
    Write-Host "This will resolve the ICE80 errors in the MSI build." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✗ ISSUE: New fix still has problems" -ForegroundColor Red
}