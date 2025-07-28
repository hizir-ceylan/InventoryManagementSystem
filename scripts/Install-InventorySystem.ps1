<#
.SYNOPSIS
    Inventory Management System Kurulum Scripti (.NET yükleme yok, sadece kontrol)
.DESCRIPTION
    .NET 8 SDK'nın manuel olarak kurulu olduğunu varsayar. Sadece kontrol eder.
    Git otomatik yüklenir, .NET 8 SDK ise kullanıcıdan manuel yüklenmesi istenir.
    Tüm adımlar tamamen güncellendi.
#>

param(
    [Parameter(HelpMessage="Installation directory (default: C:\InventoryManagementSystem)")]
    [string]$InstallPath = "C:\InventoryManagementSystem",
    
    [Parameter(HelpMessage="GitHub repository URL")]
    [string]$RepoUrl = "https://github.com/hizir-ceylan/InventoryManagementSystem.git",
    
    [Parameter(HelpMessage="Git branch to checkout (default: main)")]
    [string]$Branch = "main",
    
    [Parameter(HelpMessage="Install as Windows Service")]
    [switch]$InstallAsService = $true,
    
    [Parameter(HelpMessage="Skip dependency checks")]
    [switch]$SkipDependencyCheck = $false,
    
    [Parameter(HelpMessage="Silent installation (no prompts)")]
    [switch]$Silent = $false
)

# Ensure script is running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script requires Administrator privileges. Please run PowerShell as Administrator."
    exit 1
}

# Configuration
$LogFile = Join-Path $env:TEMP "InventorySystem-Install-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$RequiredServices = @("InventoryManagementApi", "InventoryManagementAgent")

# Logging function
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
    Add-Content -Path $LogFile -Value $logMessage
}

# Check if command exists
function Test-Command {
    param([string]$Command)
    try {
        if (Get-Command $Command -ErrorAction Stop) { return $true }
    } catch {
        return $false
    }
}

# Download and install Git if not present
function Install-Git {
    if (Test-Command "git") {
        Write-Log "Git is already installed."
        return
    }
    
    Write-Log "Git not found. Installing Git..."
    try {
        # Download Git installer
        $gitUrl = "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-64-bit.exe"
        $gitInstaller = Join-Path $env:TEMP "Git-Installer.exe"
        
        Write-Log "Downloading Git from $gitUrl"
        Invoke-WebRequest -Uri $gitUrl -OutFile $gitInstaller -UseBasicParsing
        
        Write-Log "Installing Git..."
        Start-Process -FilePath $gitInstaller -ArgumentList "/VERYSILENT", "/NORESTART" -Wait
        
        # Add Git to PATH
        $env:PATH += ";C:\Program Files\Git\bin"
        [Environment]::SetEnvironmentVariable("PATH", $env:PATH, [EnvironmentVariableTarget]::Machine)
        
        Write-Log "Git installation completed."
    } catch {
        Write-Log "Failed to install Git: $($_.Exception.Message)" -Level "ERROR"
        throw
    }
}

# Check .NET 8 SDK (no install, only check)
function Check-DotNet {
    try {
        $dotnetSdks = & dotnet --list-sdks 2>$null
        if ($dotnetSdks -match "^8\.")
        {
            Write-Log ".NET 8 SDK is installed."
            return $true
        } else {
            Write-Log ".NET 8 SDK not found." -Level "ERROR"
            return $false
        }
    } catch {
        Write-Log ".NET not found." -Level "ERROR"
        return $false
    }
}

# Check and install dependencies (only Git, .NET sadece kontrol edilir)
function Install-Dependencies {
    if ($SkipDependencyCheck) {
        Write-Log "Skipping dependency checks as requested."
        return
    }
    
    Write-Log "Checking and installing dependencies..."
    
    # Install Git
    Install-Git
    
    # Check .NET 8 SDK
    if (-not (Check-DotNet)) {
        Write-Log "Lütfen .NET 8 SDK'yı manuel olarak kurun: https://dotnet.microsoft.com/download/dotnet/8.0" -Level "ERROR"
        Write-Host "`n[ERROR] .NET 8 SDK bulunamadı! Lütfen kurulumdan önce https://dotnet.microsoft.com/download/dotnet/8.0 adresinden .NET 8 SDK'yı indirip yükleyin."
        exit 1
    }
    
    Write-Log "Dependency check completed."
}

# Clone repository
function Get-Repository {
    Write-Log "Cloning repository from $RepoUrl"
    
    if (Test-Path $InstallPath) {
        if (-not $Silent) {
            $response = Read-Host "Directory $InstallPath already exists. Do you want to remove it? (y/N)"
            if ($response -ne 'y' -and $response -ne 'Y') {
                Write-Log "Installation cancelled by user." -Level "WARN"
                exit 1
            }
        }
        Write-Log "Removing existing directory $InstallPath"
        Remove-Item -Path $InstallPath -Recurse -Force
    }
    
    try {
        & git clone --branch $Branch $RepoUrl $InstallPath
        if ($LASTEXITCODE -ne 0) {
            throw "Git clone failed with exit code $LASTEXITCODE"
        }
        Write-Log "Repository cloned successfully to $InstallPath"
    } catch {
        Write-Log "Failed to clone repository: $($_.Exception.Message)" -Level "ERROR"
        throw
    }
}

# Build the solution
function Build-Solution {
    Write-Log "Building the solution..."
    
    Push-Location $InstallPath
    try {
        # Restore packages
        Write-Log "Restoring NuGet packages..."
        & dotnet restore
        if ($LASTEXITCODE -ne 0) {
            throw "Package restore failed with exit code $LASTEXITCODE"
        }
        
        # Build solution
        Write-Log "Building solution in Release mode..."
        & dotnet build --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        
        # Publish API
        Write-Log "Publishing API..."
        & dotnet publish Inventory.Api --configuration Release --output "Published\Api" --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "API publish failed with exit code $LASTEXITCODE"
        }
        
        # Publish Agent
        Write-Log "Publishing Agent..."
        & dotnet publish Inventory.Agent.Windows --configuration Release --output "Published\Agent" --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Agent publish failed with exit code $LASTEXITCODE"
        }
        
        Write-Log "Build completed successfully."
    } catch {
        Write-Log "Build failed: $($_.Exception.Message)" -Level "ERROR"
        throw
    } finally {
        Pop-Location
    }
}

# Install Windows Services
function Install-WindowsServices {
    if (-not $InstallAsService) {
        Write-Log "Skipping Windows Service installation as requested."
        return
    }
    
    Write-Log "Installing Windows Services..."
    
    try {
        # Stop existing services if they exist
        foreach ($serviceName in $RequiredServices) {
            $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
            if ($service) {
                Write-Log "Stopping existing service: $serviceName"
                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                & sc.exe delete $serviceName
            }
        }
        
        # Install API Service
        $apiPath = Join-Path $InstallPath "Published\Api\Inventory.Api.exe"
        Write-Log "Installing API service from $apiPath"
        & sc.exe create "InventoryManagementApi" binPath= "`"$apiPath`" --service" start= auto DisplayName= "Inventory Management API" depend= ""
        
        # Install Agent Service  
        $agentPath = Join-Path $InstallPath "Published\Agent\Inventory.Agent.Windows.exe"
        Write-Log "Installing Agent service from $agentPath"
        & sc.exe create "InventoryManagementAgent" binPath= "`"$agentPath`" --service" start= auto DisplayName= "Inventory Management Agent" depend= "InventoryManagementApi"
        
        # Start services
        Write-Log "Starting API service..."
        Start-Service -Name "InventoryManagementApi"
        Start-Sleep -Seconds 5
        
        Write-Log "Starting Agent service..."
        Start-Service -Name "InventoryManagementAgent"
        
        Write-Log "Windows Services installed and started successfully."
    } catch {
        Write-Log "Failed to install Windows Services: $($_.Exception.Message)" -Level "ERROR"
        throw
    }
}

# Create desktop shortcuts
function Create-Shortcuts {
    Write-Log "Creating desktop shortcuts..."
    
    try {
        $shell = New-Object -ComObject WScript.Shell
        $desktop = [Environment]::GetFolderPath("Desktop")
        
        # API Swagger shortcut
        $swaggerShortcut = $shell.CreateShortcut("$desktop\Inventory API (Swagger).lnk")
        $swaggerShortcut.TargetPath = "http://localhost:5093/swagger"
        $swaggerShortcut.Description = "Inventory Management System API Documentation"
        $swaggerShortcut.Save()
        
        # Installation folder shortcut
        $folderShortcut = $shell.CreateShortcut("$desktop\Inventory System Folder.lnk")
        $folderShortcut.TargetPath = $InstallPath
        $folderShortcut.Description = "Inventory Management System Installation Folder"
        $folderShortcut.Save()
        
        Write-Log "Desktop shortcuts created successfully."
    } catch {
        Write-Log "Failed to create shortcuts: $($_.Exception.Message)" -Level "WARN"
    }
}

# Create uninstall script
function Create-UninstallScript {
    Write-Log "Creating uninstall script..."
    
    $uninstallScript = @"
# Inventory Management System - Uninstall Script
# Run as Administrator

Write-Host "Uninstalling Inventory Management System..."

# Stop and remove services
foreach (`$service in @("InventoryManagementAgent", "InventoryManagementApi")) {
    `$svc = Get-Service -Name `$service -ErrorAction SilentlyContinue
    if (`$svc) {
        Write-Host "Stopping service: `$service"
        Stop-Service -Name `$service -Force -ErrorAction SilentlyContinue
        & sc.exe delete `$service
    }
}

# Remove installation directory
if (Test-Path "$InstallPath") {
    Write-Host "Removing installation directory: $InstallPath"
    Remove-Item -Path "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue
}

# Remove desktop shortcuts
`$desktop = [Environment]::GetFolderPath("Desktop")
Remove-Item -Path "`$desktop\Inventory API (Swagger).lnk" -ErrorAction SilentlyContinue
Remove-Item -Path "`$desktop\Inventory System Folder.lnk" -ErrorAction SilentlyContinue

Write-Host "Uninstallation completed."
"@

    $uninstallPath = Join-Path $InstallPath "Uninstall.ps1"
    Set-Content -Path $uninstallPath -Value $uninstallScript
    Write-Log "Uninstall script created at $uninstallPath"
}

# Main installation process
function Start-Installation {
    Write-Log "=============================================="
    Write-Log "Inventory Management System - Installation"
    Write-Log "=============================================="
    Write-Log "Installation path: $InstallPath"
    Write-Log "Repository: $RepoUrl"
    Write-Log "Branch: $Branch"
    Write-Log "Install as Service: $InstallAsService"
    Write-Log "Log file: $LogFile"
    Write-Log "=============================================="
    
    try {
        # Step 1: Install dependencies (only Git, .NET sadece kontrol)
        Install-Dependencies
        
        # Step 2: Clone repository
        Get-Repository
        
        # Step 3: Build solution
        Build-Solution
        
        # Step 4: Install Windows Services
        Install-WindowsServices
        
        # Step 5: Create shortcuts
        Create-Shortcuts
        
        # Step 6: Create uninstall script
        Create-UninstallScript
        
        Write-Log "=============================================="
        Write-Log "Installation completed successfully!"
        Write-Log "=============================================="
        Write-Log "API URL: http://localhost:5093"
        Write-Log "Swagger UI: http://localhost:5093/swagger"
        Write-Log "Installation folder: $InstallPath"
        Write-Log "Log file: $LogFile"
        Write-Log "=============================================="
        
        if (-not $Silent) {
            Write-Host "`nInstallation completed! Press any key to exit..."
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
        
    } catch {
        Write-Log "Installation failed: $($_.Exception.Message)" -Level "ERROR"
        Write-Log "Check the log file for details: $LogFile" -Level "ERROR"
        exit 1
    }
}

# Start the installation
Start-Installation