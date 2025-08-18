# WiX LGHT0103 Error Fix

## Problem
MSI build was failing with error:
```
LGHT0103: The system cannot find the file '[SystemFolder]shell32.dll'.
```

## Root Cause
In `InventoryManagementSystem.wxs` line 304, the Icon element was incorrectly referencing a runtime Windows Installer property as a build-time source file:

```xml
<Icon Id="BrowserIcon" SourceFile="[SystemFolder]shell32.dll" />
```

The issue is that:
- `[SystemFolder]` is a Windows Installer property that gets resolved at **runtime**
- `SourceFile` attribute requires a path that exists at **build time**
- WiX Light linker cannot find this file during compilation

## Solution
Replaced the problematic reference with an actual icon file from the repository:

**Before:**
```xml
<Icon Id="BrowserIcon" SourceFile="[SystemFolder]shell32.dll" />
```

**After:**
```xml
<Icon Id="BrowserIcon" SourceFile="../Inventory.WebApp/wwwroot/favicon.ico" />
```

## Why This Works
- `favicon.ico` exists in the repository at build time
- Relative path resolves correctly from `build-tools/` directory
- WiX Light can locate and embed the icon file during compilation
- Runtime behavior remains the same (icon displays in Add/Remove Programs)

## Key Learning
**Runtime properties** like `[SystemFolder]`, `[ProgramFilesFolder]`, etc. should only be used in:
- `Target` attributes (for shortcuts, etc.)
- `WorkingDirectory` attributes 
- Registry values
- Custom action parameters

**Build-time file paths** in `SourceFile` attributes must reference actual files:
- Relative paths from WiX source file location
- Absolute paths on build machine
- Files that exist during compilation

## Testing
The fix was validated by:
1. XML syntax validation (well-formed)
2. File existence check (favicon.ico found)
3. Pattern matching (no problematic runtime properties in SourceFile)
4. Build-time reference verification

This should resolve the LGHT0103 error when running `Build-MSI.ps1` on Windows with WiX 3.14.