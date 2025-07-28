# Inventory Management System - Build and Package Script (PowerShell)
# Creates setup.exe installer for Windows deployment

param(
    [switch]$SkipInnoSetup = $false,
    [string]$Configuration = "Release",
    [switch]$SelfContained = $false
)

Write-Host "=====================================================" -ForegroundColor Green
Write-Host "Inventory Management System - Build and Package" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to write colored output
function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    switch ($Status) {
        "SUCCESS" { Write-Host "✓ $Message" -ForegroundColor Green }
        "WARNING" { Write-Host "⚠ $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "✗ $Message" -ForegroundColor Red }
        default   { Write-Host "  $Message" -ForegroundColor White }
    }
}

# Check administrator privileges
if (-not (Test-Administrator)) {
    Write-Status "Not running as administrator. Some operations may fail." "WARNING"
    Write-Status "It's recommended to run this script as administrator." "WARNING"
}

# Check for required tools
Write-Host "`nChecking for required tools..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = & dotnet --version
    Write-Status ".NET SDK found: $dotnetVersion" "SUCCESS"
} catch {
    Write-Status ".NET SDK is required but not found!" "ERROR"
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

# Check for Inno Setup
$innoSetupPath = $null
$innoSetupFound = $false

# Common Inno Setup installation paths
$commonPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
    "C:\Program Files\Inno Setup 5\ISCC.exe"
)

foreach ($path in $commonPaths) {
    if (Test-Path $path) {
        $innoSetupPath = $path
        $innoSetupFound = $true
        break
    }
}

# Also check PATH
if (-not $innoSetupFound) {
    try {
        $null = Get-Command "iscc" -ErrorAction Stop
        $innoSetupPath = "iscc"
        $innoSetupFound = $true
    } catch {
        # Not in PATH
    }
}

if ($innoSetupFound -and -not $SkipInnoSetup) {
    Write-Status "Inno Setup found: $innoSetupPath" "SUCCESS"
} else {
    Write-Status "Inno Setup not found." "WARNING"
    Write-Host "To create setup.exe, please install Inno Setup from: https://jrsoftware.org/isinfo.php"
    Write-Host "Current script will build and prepare files, but won't create setup.exe"
    $SkipInnoSetup = $true
}

Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
$foldersToClean = @("Published", "Setup")
foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force
        Write-Status "Cleaned $folder directory"
    }
}

# Create directories
$null = New-Item -ItemType Directory -Path "Published" -Force
$null = New-Item -ItemType Directory -Path "Published\Api" -Force
$null = New-Item -ItemType Directory -Path "Published\Agent" -Force
$null = New-Item -ItemType Directory -Path "Setup" -Force
Write-Status "Created build directories" "SUCCESS"

# Restore NuGet packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
try {
    & dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Status "NuGet packages restored successfully" "SUCCESS"
    } else {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Failed to restore NuGet packages: $_" "ERROR"
    exit 1
}

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
try {
    & dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Solution built successfully" "SUCCESS"
    } else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Build failed: $_" "ERROR"
    exit 1
}

# Determine publish arguments
$publishArgs = @(
    "--configuration", $Configuration,
    "--no-build"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", "true"
    $publishArgs += "--runtime", "win-x64"
    Write-Status "Building as self-contained (includes .NET runtime)"
} else {
    $publishArgs += "--self-contained", "false"
    Write-Status "Building as framework-dependent (requires .NET runtime on target)"
}

# Publish API
Write-Host "`nPublishing API..." -ForegroundColor Yellow
try {
    $apiArgs = $publishArgs + @("Inventory.Api", "--output", "Published\Api")
    & dotnet publish @apiArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Status "API published successfully" "SUCCESS"
    } else {
        throw "API publish failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Failed to publish API: $_" "ERROR"
    exit 1
}

# Publish Agent
Write-Host "`nPublishing Agent..." -ForegroundColor Yellow
try {
    $agentArgs = $publishArgs + @("Inventory.Agent.Windows", "--output", "Published\Agent")
    & dotnet publish @agentArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Agent published successfully" "SUCCESS"
    } else {
        throw "Agent publish failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Failed to publish Agent: $_" "ERROR"
    exit 1
}

# Create configuration files
Write-Host "`nCreating configuration files..." -ForegroundColor Yellow

# API configuration
$apiConfig = @{
    ConnectionStrings = @{
        DefaultConnection = "Data Source=inventory.db"
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
    AllowedHosts = "*"
    Urls = "http://localhost:5093"
} | ConvertTo-Json -Depth 4

Set-Content -Path "Published\Api\appsettings.json" -Value $apiConfig -Encoding UTF8

# Agent configuration
$agentConfig = @{
    ApiSettings = @{
        BaseUrl = "http://localhost:5093"
        EnableOfflineStorage = $true
        OfflineStoragePath = "C:\Program Files\Inventory Management System\Data\OfflineStorage"
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.Hosting.Lifetime" = "Information"
        }
    }
} | ConvertTo-Json -Depth 4

Set-Content -Path "Published\Agent\appsettings.json" -Value $agentConfig -Encoding UTF8

Write-Status "Configuration files created" "SUCCESS"

# Create setup.exe if Inno Setup is available
if (-not $SkipInnoSetup) {
    Write-Host "`nCreating setup.exe with Inno Setup..." -ForegroundColor Yellow
    try {
        & $innoSetupPath "InventoryManagementSystem.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Setup.exe created successfully" "SUCCESS"
            $setupExists = $true
        } else {
            throw "Inno Setup failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-Status "Failed to create setup.exe: $_" "ERROR"
        Write-Status "The published files are still available in 'Published' folder." "WARNING"
        $setupExists = $false
    }
} else {
    $setupExists = $false
}

# Final status report
Write-Host "`n=====================================================" -ForegroundColor Green
Write-Host "Build and packaging completed!" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

if ($setupExists) {
    Write-Host "`n✓ Setup file created: " -NoNewline -ForegroundColor Green
    Write-Host "Setup\InventoryManagementSystem-Setup.exe" -ForegroundColor Cyan
    Write-Host "✓ Published files: " -NoNewline -ForegroundColor Green
    Write-Host "Published\Api\ and Published\Agent\" -ForegroundColor Cyan
    Write-Host "`nYou can now distribute the setup.exe file to install the system on Windows machines." -ForegroundColor White
} else {
    Write-Host "`n✓ Published files ready in 'Published' folder:" -ForegroundColor Green
    Write-Host "  - API: Published\Api\" -ForegroundColor Cyan
    Write-Host "  - Agent: Published\Agent\" -ForegroundColor Cyan
    
    if ($SkipInnoSetup) {
        Write-Host "`nTo create setup.exe:" -ForegroundColor Yellow
        Write-Host "1. Install Inno Setup from: https://jrsoftware.org/isinfo.php" -ForegroundColor White
        Write-Host "2. Run: " -NoNewline -ForegroundColor White
        Write-Host '"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" InventoryManagementSystem.iss' -ForegroundColor Cyan
        Write-Host "`nThe setup script is ready: InventoryManagementSystem.iss" -ForegroundColor White
    }
}

Write-Host "`nInstallation requirements for target machines:" -ForegroundColor Yellow
Write-Host "  - Windows 10/11 or Windows Server 2016+" -ForegroundColor White
Write-Host "  - Administrator privileges for installation" -ForegroundColor White
if (-not $SelfContained) {
    Write-Host "  - .NET 8 Runtime (will be installed automatically if missing)" -ForegroundColor White
}
Write-Host "  - Port 5093 available (will be configured automatically)" -ForegroundColor White

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")