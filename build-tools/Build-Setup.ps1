param(
    [switch]$SkipInnoSetup = $false,
    [string]$Configuration = "Release",
    [switch]$SelfContained = $false
)

Write-Host "====================================================="
Write-Host "Inventory Management System - Build and Package"
Write-Host "====================================================="

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    switch ($Status) {
        "SUCCESS" { Write-Host "[OK] $Message" }
        "WARNING" { Write-Host "[WARN] $Message" }
        "ERROR"   { Write-Host "[ERROR] $Message" }
        default   { Write-Host "[INFO] $Message" }
    }
}

if (-not (Test-Administrator)) {
    Write-Status "Not running as administrator. Some operations may fail." "WARNING"
    Write-Status "It is recommended to run this script as administrator." "WARNING"
}

Write-Host ""
Write-Host "Checking for required tools..."

try {
    $dotnetVersion = & dotnet --version
    Write-Status ".NET SDK found: $dotnetVersion" "SUCCESS"
} catch {
    Write-Status ".NET SDK is required but not found!" "ERROR"
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

$innoSetupPath = $null
$innoSetupFound = $false
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
if (-not $innoSetupFound) {
    try {
        $null = Get-Command "iscc" -ErrorAction Stop
        $innoSetupPath = "iscc"
        $innoSetupFound = $true
    } catch {}
}
if ($innoSetupFound -and -not $SkipInnoSetup) {
    Write-Status "Inno Setup found: $innoSetupPath" "SUCCESS"
} else {
    Write-Status "Inno Setup not found." "WARNING"
    Write-Host "To create setup.exe, install Inno Setup from: https://jrsoftware.org/isinfo.php"
    $SkipInnoSetup = $true
}

Write-Host ""
Write-Host "Cleaning previous builds..."
$foldersToClean = @("Published", "Setup")
foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force
        Write-Status "Deleted folder: $folder"
    }
}

$null = New-Item -ItemType Directory -Path "Published\Api" -Force
$null = New-Item -ItemType Directory -Path "Published\Agent" -Force
$null = New-Item -ItemType Directory -Path "Setup" -Force
Write-Status "Created build directories" "SUCCESS"

Write-Host ""
Write-Host "Restoring NuGet packages..."
try {
    & dotnet restore ..
    if ($LASTEXITCODE -eq 0) {
        Write-Status "NuGet packages restored successfully" "SUCCESS"
    } else {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Failed to restore NuGet packages: $_" "ERROR"
    exit 1
}

Write-Host ""
Write-Host "Building solution..."
try {
    & dotnet build .. --configuration $Configuration --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Status "Solution built successfully" "SUCCESS"
    } else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Status "Build failed: $_" "ERROR"
    exit 1
}

$publishArgs = @("--configuration", $Configuration, "--no-build")
if ($SelfContained) {
    $publishArgs += "--self-contained", "true", "--runtime", "win-x64"
    Write-Status "Publishing as self-contained"
} else {
    $publishArgs += "--self-contained", "false"
    Write-Status "Publishing as framework-dependent"
}

Write-Host ""
Write-Host "Publishing API..."
try {
    $apiArgs = $publishArgs + @("..\Inventory.Api", "--output", "Published\Api")
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

Write-Host ""
Write-Host "Publishing Agent..."
try {
    $agentArgs = $publishArgs + @("..\Inventory.Agent.Windows", "--output", "Published\Agent")
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

Write-Host ""
Write-Host "Creating configuration files..."

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

$agentConfig = @{
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.Hosting.Lifetime" = "Information"
            "Microsoft.Extensions.Hosting" = "Information"
        }
    }
} | ConvertTo-Json -Depth 4

Set-Content -Path "Published\Agent\appsettings.json" -Value $agentConfig -Encoding UTF8

Write-Status "Configuration files created" "SUCCESS"

$setupExists = $false
if (-not $SkipInnoSetup) {
    Write-Host ""
    Write-Host "Creating setup.exe with Inno Setup..."
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
        Write-Status "You can still use the published files in the Published folder." "WARNING"
    }
}

Write-Host ""
Write-Host "====================================================="
Write-Host "Build and packaging completed"
Write-Host "====================================================="

if ($setupExists) {
    Write-Host ""
    Write-Host "[OK] Setup file created: Setup\InventoryManagementSystem-Setup.exe"
    Write-Host "[OK] Published files: Published\Api\ and Published\Agent\"
} else {
    Write-Host ""
    Write-Host "[OK] Published files ready:"
    Write-Host " - API: Published\Api\"
    Write-Host " - Agent: Published\Agent\"

    if ($SkipInnoSetup) {
        Write-Host ""
        Write-Host "To create setup.exe:"
        Write-Host "1. Install Inno Setup from: https://jrsoftware.org/isinfo.php"
        Write-Host "2. Run:"
        Write-Host "`"C:\Program Files (x86)\Inno Setup 6\ISCC.exe`" InventoryManagementSystem.iss"
    }
}

Write-Host ""
Write-Host "Installation requirements for target machines:"
Write-Host " - Windows 10/11 or Windows Server 2016 or newer"
Write-Host " - Administrator privileges required"
if (-not $SelfContained) {
    Write-Host " - .NET 8 Runtime required (will be installed automatically if missing)"
}
Write-Host " - Port 5093 must be available"

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
