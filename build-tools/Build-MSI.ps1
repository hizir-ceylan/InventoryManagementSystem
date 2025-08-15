param(
    [string]$Configuration = "Release",
    [switch]$SelfContained = $false,
    [switch]$SkipBuild = $false,
    [string]$WixPath = ""
)

Write-Host "====================================================="
Write-Host "Inventory Management System - MSI Builder"
Write-Host "Enterprise MSI Package Generator using WiX Toolset"
Write-Host "====================================================="

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    switch ($Status) {
        "SUCCESS" { Write-Host "[OK] $Message" -ForegroundColor Green }
        "WARNING" { Write-Host "[WARN] $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "[ERROR] $Message" -ForegroundColor Red }
        default   { Write-Host "[INFO] $Message" -ForegroundColor Cyan }
    }
}

if (-not (Test-Administrator)) {
    Write-Status "Administrator privileges recommended for MSI building." "WARNING"
}

# Check for required tools
Write-Host ""
Write-Host "Checking for required tools..."

# .NET SDK Check
try {
    $dotnetVersion = & dotnet --version
    Write-Status ".NET SDK found: $dotnetVersion" "SUCCESS"
} catch {
    Write-Status ".NET SDK is required but not found!" "ERROR"
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

# WiX Toolset Check
$wixFound = $false
$wixToolsPath = $null

if ($WixPath) {
    $wixToolsPath = $WixPath
    if ((Test-Path "$wixToolsPath\candle.exe") -and (Test-Path "$wixToolsPath\light.exe")) {
        $wixFound = $true
    }
} else {
    # Common WiX installation paths
    $commonWixPaths = @(
        "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin",
        "${env:ProgramFiles}\WiX Toolset v3.11\bin",
        "${env:ProgramFiles(x86)}\WiX Toolset v4.0\bin",
        "${env:ProgramFiles}\WiX Toolset v4.0\bin",
        "${env:USERPROFILE}\.dotnet\tools"  # For dotnet tool install
    )

    foreach ($path in $commonWixPaths) {
        if ((Test-Path "$path\candle.exe") -and (Test-Path "$path\light.exe")) {
            $wixToolsPath = $path
            $wixFound = $true
            break
        }
    }

    # Check if WiX is in PATH
    if (-not $wixFound) {
        try {
            $null = Get-Command "candle" -ErrorAction Stop
            $null = Get-Command "light" -ErrorAction Stop
            $wixToolsPath = ""  # Use PATH
            $wixFound = $true
        } catch {
            # Try to install WiX via dotnet tool
            Write-Status "WiX Toolset not found. Attempting to install via dotnet tool..." "WARNING"
            try {
                & dotnet tool install --global wix
                if ($LASTEXITCODE -eq 0) {
                    $wixToolsPath = ""
                    $wixFound = $true
                    Write-Status "WiX installed successfully via dotnet tool" "SUCCESS"
                } else {
                    throw "Installation failed"
                }
            } catch {
                Write-Status "Failed to install WiX via dotnet tool" "ERROR"
            }
        }
    }
}

if (-not $wixFound) {
    Write-Status "WiX Toolset is required for MSI creation!" "ERROR"
    Write-Host ""
    Write-Host "Installation options:"
    Write-Host "1. Download from: https://wixtoolset.org/releases/"
    Write-Host "2. Install via dotnet tool: dotnet tool install --global wix"
    Write-Host "3. Use Chocolatey: choco install wixtoolset"
    Write-Host "4. Use winget: winget install WiXToolset.WiX"
    exit 1
} else {
    if ($wixToolsPath) {
        Write-Status "WiX Toolset found: $wixToolsPath" "SUCCESS"
    } else {
        Write-Status "WiX Toolset found in PATH" "SUCCESS"
    }
}

# Build the solution first if not skipping
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building solution using existing build script..."
    
    # Use hashtable for proper parameter splatting
    $buildArgs = @{
        Configuration = $Configuration
        SkipInnoSetup = $true
    }
    if ($SelfContained) {
        $buildArgs.SelfContained = $true
    }
    
    try {
        & .\Build-Setup.ps1 @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Build script failed"
        }
        Write-Status "Build completed successfully" "SUCCESS"
    } catch {
        Write-Status "Build failed: $_" "ERROR"
        exit 1
    }
} else {
    Write-Status "Skipping build phase" "WARNING"
}

# Verify published files exist and handle executable naming
Write-Host ""
Write-Host "Verifying published files..."

$requiredPaths = @(
    "Published\Api\Inventory.Api.exe",
    "Published\Agent\Inventory.Agent.Windows.exe"
)

# Check if .exe files exist, if not, check for runtime files without .exe extension
$apiExeExists = Test-Path "Published\Api\Inventory.Api.exe"
$agentExeExists = Test-Path "Published\Agent\Inventory.Agent.Windows.exe"

if (-not $apiExeExists) {
    if (Test-Path "Published\Api\Inventory.Api") {
        Write-Status "Creating Windows executable symlink for API..." "WARNING"
        Copy-Item "Published\Api\Inventory.Api" "Published\Api\Inventory.Api.exe" -Force
    } else {
        Write-Status "API executable not found. Rebuilding with Windows runtime..." "WARNING"
        # Rebuild with Windows runtime if needed
        try {
            & dotnet publish "..\Inventory.Api" --configuration $Configuration --runtime win-x64 --self-contained false --output "Published\Api"
            if ($LASTEXITCODE -ne 0) {
                throw "API rebuild failed"
            }
        } catch {
            Write-Status "Failed to rebuild API with Windows runtime: $_" "ERROR"
            exit 1
        }
    }
}

if (-not $agentExeExists) {
    if (Test-Path "Published\Agent\Inventory.Agent.Windows") {
        Write-Status "Creating Windows executable symlink for Agent..." "WARNING"
        Copy-Item "Published\Agent\Inventory.Agent.Windows" "Published\Agent\Inventory.Agent.Windows.exe" -Force
    } else {
        Write-Status "Agent executable not found. Rebuilding with Windows runtime..." "WARNING"
        # Rebuild with Windows runtime if needed
        try {
            & dotnet publish "..\Inventory.Agent.Windows" --configuration $Configuration --runtime win-x64 --self-contained false --output "Published\Agent"
            if ($LASTEXITCODE -ne 0) {
                throw "Agent rebuild failed"
            }
        } catch {
            Write-Status "Failed to rebuild Agent with Windows runtime: $_" "ERROR"
            exit 1
        }
    }
}

# Final verification
foreach ($path in $requiredPaths) {
    if (-not (Test-Path $path)) {
        Write-Status "Required file not found: $path" "ERROR"
        Write-Status "MSI build requires Windows executable files" "ERROR"
        exit 1
    }
}

Write-Status "All required files found" "SUCCESS"

# Create MSI output directory
$msiOutputDir = "Setup\MSI"
if (-not (Test-Path $msiOutputDir)) {
    $null = New-Item -ItemType Directory -Path $msiOutputDir -Force
}

# WiX compilation step
Write-Host ""
Write-Host "Harvesting published files with Heat..."

$candleExe = if ($wixToolsPath) { "$wixToolsPath\candle.exe" } else { "candle" }
$lightExe = if ($wixToolsPath) { "$wixToolsPath\light.exe" } else { "light" }
$heatExe = if ($wixToolsPath) { "$wixToolsPath\heat.exe" } else { "heat" }

try {
    # Generate component definitions for API files
    Write-Status "Harvesting API files..."
    $heatApiArgs = @(
        "dir", "Published\Api",
        "-cg", "ApiFilesGroup",
        "-gg", "-sf", "-srd", "-sreg",
        "-dr", "APIFOLDER",
        "-var", "var.ApiSourceDir",
        "-xf", "Published\Api\Inventory.Api.exe",
        "-out", "$msiOutputDir\ApiFiles.wxs"
    )
    & $heatExe @heatApiArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Heat harvesting of API files failed with exit code $LASTEXITCODE"
    }
    
    # Generate component definitions for Agent files  
    Write-Status "Harvesting Agent files..."
    $heatAgentArgs = @(
        "dir", "Published\Agent",
        "-cg", "AgentFilesGroup", 
        "-gg", "-sf", "-srd", "-sreg",
        "-dr", "AGENTFOLDER",
        "-var", "var.AgentSourceDir",
        "-xf", "Published\Agent\Inventory.Agent.Windows.exe",
        "-out", "$msiOutputDir\AgentFiles.wxs"
    )
    & $heatAgentArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Heat harvesting of Agent files failed with exit code $LASTEXITCODE"
    }
    
    Write-Status "File harvesting successful" "SUCCESS"
    
    Write-Host ""
    Write-Host "Compiling WiX source files..."
    
    # Compile main .wxs file and harvested files to .wixobj
    $candleArgs = @(
        "InventoryManagementSystem.wxs",
        "$msiOutputDir\ApiFiles.wxs",
        "$msiOutputDir\AgentFiles.wxs",
        "-out", "$msiOutputDir\",
        "-ext", "WixUtilExtension",
        "-ext", "WixFirewallExtension",
        "-dApiSourceDir=Published\Api",
        "-dAgentSourceDir=Published\Agent"
    )
    
    Write-Status "Running candle.exe (WiX compiler)..."
    & $candleExe @candleArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Candle compilation failed with exit code $LASTEXITCODE"
    }
    
    Write-Status "WiX compilation successful" "SUCCESS"
    
    # Link .wixobj files to .msi
    $lightArgs = @(
        "$msiOutputDir\InventoryManagementSystem.wixobj",
        "$msiOutputDir\ApiFiles.wixobj", 
        "$msiOutputDir\AgentFiles.wixobj",
        "-out", "$msiOutputDir\InventoryManagementSystem.msi",
        "-ext", "WixUtilExtension",
        "-ext", "WixFirewallExtension",
        "-ext", "WixUIExtension"
    )
    
    Write-Status "Running light.exe (WiX linker)..."
    & $lightExe @lightArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Light linking failed with exit code $LASTEXITCODE"
    }
    
    Write-Status "MSI creation successful" "SUCCESS"
    
} catch {
    Write-Status "WiX compilation failed: $_" "ERROR"
    
    # Check for common issues
    if (Test-Path "$msiOutputDir\InventoryManagementSystem.wixobj") {
        Write-Status "WiX object file created but linking failed" "WARNING"
        Write-Status "Check that all referenced files exist in Published directory" "WARNING"
    }
    
    exit 1
}

# Verify MSI was created
$msiPath = "$msiOutputDir\InventoryManagementSystem.msi"
if (Test-Path $msiPath) {
    $msiInfo = Get-Item $msiPath
    Write-Status "MSI created successfully: $($msiInfo.FullName)" "SUCCESS"
    Write-Status "MSI size: $([math]::Round($msiInfo.Length / 1MB, 2)) MB" "SUCCESS"
} else {
    Write-Status "MSI file was not created" "ERROR"
    exit 1
}

# Create enterprise deployment package
Write-Host ""
Write-Host "Creating enterprise deployment package..."

$enterpriseDir = "Setup\Enterprise"
if (Test-Path $enterpriseDir) {
    Remove-Item -Path $enterpriseDir -Recurse -Force
}
$null = New-Item -ItemType Directory -Path $enterpriseDir -Force

# Copy MSI
Copy-Item $msiPath "$enterpriseDir\"

# Create deployment scripts
$deployScript = @"
@echo off
REM Enterprise Deployment Script for Inventory Management System
REM Run this script as Administrator

echo ===================================================
echo Inventory Management System - Enterprise Deployment
echo ===================================================

net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo ERROR: This script must be run as Administrator
    pause
    exit /b 1
)

echo Installing Inventory Management System...
msiexec /i "InventoryManagementSystem.msi" /quiet /l*v install.log

if %errorLevel% EQ 0 (
    echo SUCCESS: Installation completed
    echo Log file: install.log
    echo API Service will be available at: http://localhost:5093
    echo Agent Service will start automatically
) else (
    echo ERROR: Installation failed with code %errorLevel%
    echo Check install.log for details
)

pause
"@

Set-Content -Path "$enterpriseDir\Deploy.bat" -Value $deployScript -Encoding UTF8

$uninstallScript = @"
@echo off
REM Enterprise Uninstall Script for Inventory Management System

echo ===================================================
echo Inventory Management System - Enterprise Uninstall
echo ===================================================

net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo ERROR: This script must be run as Administrator
    pause
    exit /b 1
)

echo Uninstalling Inventory Management System...
msiexec /x "InventoryManagementSystem.msi" /quiet /l*v uninstall.log

if %errorLevel% EQ 0 (
    echo SUCCESS: Uninstallation completed
    echo Log file: uninstall.log
) else (
    echo ERROR: Uninstallation failed with code %errorLevel%
    echo Check uninstall.log for details
)

pause
"@

Set-Content -Path "$enterpriseDir\Uninstall.bat" -Value $uninstallScript -Encoding UTF8

# Create deployment instructions
$instructions = @"
# Inventory Management System - Enterprise Deployment Guide

## Prerequisites
- Windows 10/11 or Windows Server 2016 or newer (64-bit)
- Administrator privileges
- .NET 8 Runtime (will be installed automatically if missing)
- Port 5093 available

## Installation Methods

### Method 1: Interactive Installation
1. Run the MSI file: `InventoryManagementSystem.msi`
2. Follow the installation wizard
3. Choose installation directory
4. Optionally create desktop shortcut

### Method 2: Silent Installation (Recommended for Enterprise)
1. Open Command Prompt as Administrator
2. Run: `Deploy.bat`
3. Installation will complete silently with logging

### Method 3: Manual Silent Installation
```cmd
msiexec /i "InventoryManagementSystem.msi" /quiet /l*v install.log
```

## Post-Installation

### Services
The following Windows services will be installed and started:
- **InventoryApiService**: Main API service (port 5093)
- **InventoryAgentService**: Local device monitoring agent

### Verification
1. Open web browser to: http://localhost:5093/swagger
2. Verify API documentation is accessible
3. Check services are running:
   ```cmd
   sc query InventoryApiService
   sc query InventoryAgentService
   ```

### Firewall
- Firewall rule will be automatically created for port 5093
- Rule name: "Inventory Management API"
- Scope: Local subnet

## Configuration

### API Configuration
Location: `%ProgramFiles%\Inventory Management System\Api\appsettings.json`

### Agent Configuration  
Location: `%ProgramFiles%\Inventory Management System\Agent\appsettings.json`

### Database
- Default: SQLite database in Data folder
- Can be configured for SQL Server in production

## Uninstallation

### Method 1: Control Panel
1. Go to Control Panel > Programs and Features
2. Find "Inventory Management System"
3. Click Uninstall

### Method 2: Silent Uninstallation
1. Open Command Prompt as Administrator
2. Run: `Uninstall.bat`

### Method 3: Manual Silent Uninstallation
```cmd
msiexec /x "InventoryManagementSystem.msi" /quiet /l*v uninstall.log
```

## Group Policy Deployment

For domain environments:
1. Copy MSI to network share
2. Create Group Policy Object (GPO)
3. Computer Configuration > Software Settings > Software Installation
4. Add InventoryManagementSystem.msi as new package
5. Configure deployment options (Assigned/Published)

## Troubleshooting

### Common Issues
- **Port 5093 in use**: Stop conflicting service or change port in appsettings.json
- **Service start failure**: Check .NET 8 runtime installation
- **Firewall blocking**: Manually add firewall rule for port 5093

### Log Locations
- Installation: `install.log` (created during installation)
- Application: `%ProgramFiles%\Inventory Management System\Logs\`
- Windows Event Log: Applications and Services Logs > Inventory Management System

### Support
- Project: https://github.com/hizir-ceylan/InventoryManagementSystem
- Issues: https://github.com/hizir-ceylan/InventoryManagementSystem/issues
"@

Set-Content -Path "$enterpriseDir\README.md" -Value $instructions -Encoding UTF8

Write-Status "Enterprise deployment package created: $enterpriseDir" "SUCCESS"

# Summary
Write-Host ""
Write-Host "====================================================="
Write-Host "MSI Build Completed Successfully"
Write-Host "====================================================="
Write-Host ""
Write-Host "[OK] MSI Package: $msiPath"
Write-Host "[OK] Enterprise Package: $enterpriseDir"
Write-Host "[OK] Deployment Scripts: Deploy.bat, Uninstall.bat"
Write-Host "[OK] Documentation: README.md"
Write-Host ""
Write-Host "Installation Features:"
Write-Host " - Windows Services (API + Agent)"
Write-Host " - Firewall Rules (Port 5093)" 
Write-Host " - Start Menu Shortcuts"
Write-Host " - Optional Desktop Shortcut"
Write-Host " - Automatic .NET 8 Runtime Check"
Write-Host " - Enterprise Silent Deployment Support"
Write-Host ""
Write-Host "Note: Web app excluded as requested (test mode)"
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")