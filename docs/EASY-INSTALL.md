# Inventory Management System - Easy Installation Guide

## 🚀 Quick Installation (Recommended)

### Option 1: One-Click Installer (Easiest)
1. Download the `Quick-Install.bat` file
2. Right-click and select **"Run as administrator"**
3. Follow the prompts
4. Access the system at: http://localhost:5093/swagger

### Option 2: PowerShell Script
1. Open PowerShell as Administrator
2. Run the following command:
```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/hizir-ceylan/InventoryManagementSystem/main/build-tools/Install-InventorySystem.ps1'))
```

### Option 3: Manual Download and Install
1. Download the installer script: `build-tools/Install-InventorySystem.ps1`
2. Open PowerShell as Administrator
3. Navigate to the downloaded script location
4. Run: `.\Install-InventorySystem.ps1`

## 📋 What the Installer Does

✅ **Automatic Dependency Installation**
- Downloads and installs Git (if not present)
- Downloads and installs .NET 8 SDK (if not present)

✅ **Source Code Management**
- Clones the latest code from GitHub
- Builds the solution in Release mode
- Publishes API and Agent applications

✅ **Windows Service Installation**
- Installs API as Windows Service (`InventoryManagementApi`)
- Installs Agent as Windows Service (`InventoryManagementAgent`)
- Configures automatic startup
- Sets proper service dependencies

✅ **User Experience**
- Creates desktop shortcuts
- Provides easy access to API documentation
- Creates uninstall script

## 🔧 Installation Options

### Basic Installation
```powershell
.\Install-InventorySystem.ps1
```

### Custom Installation Path
```powershell
.\Install-InventorySystem.ps1 -InstallPath "D:\MyInventorySystem"
```

### Install Without Services (Portable Mode)
```powershell
.\Install-InventorySystem.ps1 -InstallAsService:$false
```

### Silent Installation
```powershell
.\Install-InventorySystem.ps1 -Silent
```

### Install from Specific Branch
```powershell
.\Install-InventorySystem.ps1 -Branch "development"
```

## 🌐 Multi-Computer Deployment

### For IT Administrators
1. **Download once, deploy many**: Download the installer script to a network share
2. **Group Policy deployment**: Use Group Policy to deploy the batch file
3. **Remote installation**: Use PowerShell remoting for bulk installation

### Network Share Deployment
```powershell
# On network share
\\server\share\InventoryInstall\Quick-Install.bat

# Remote execution
Invoke-Command -ComputerName @("PC001", "PC002", "PC003") -ScriptBlock {
    & "\\server\share\InventoryInstall\Install-InventorySystem.ps1" -Silent
}
```

## 🛠️ Post-Installation

### Verify Installation
1. Check services are running:
   - Services → InventoryManagementApi (should be Running)
   - Services → InventoryManagementAgent (should be Running)

2. Test API access: http://localhost:5093/swagger

3. Check logs:
   - API logs: `C:\InventoryManagementSystem\Published\Api\ApiLogs\`
   - Agent logs: Check Windows Event Viewer

### Accessing the System
- **API Documentation**: http://localhost:5093/swagger
- **Direct API**: http://localhost:5093/api/device
- **Installation Folder**: `C:\InventoryManagementSystem` (default)

## 🗑️ Uninstallation

### Automatic Uninstall
Run the uninstall script created during installation:
```powershell
C:\InventoryManagementSystem\Uninstall.ps1
```

### Manual Uninstall
1. Stop services:
   ```powershell
   Stop-Service -Name "InventoryManagementAgent"
   Stop-Service -Name "InventoryManagementApi"
   ```

2. Remove services:
   ```powershell
   sc.exe delete "InventoryManagementAgent"
   sc.exe delete "InventoryManagementApi"
   ```

3. Delete installation folder:
   ```powershell
   Remove-Item -Path "C:\InventoryManagementSystem" -Recurse -Force
   ```

## 🆘 Troubleshooting

### Common Issues

**"Execution Policy" Error**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**"Access Denied" Error**
- Make sure to run PowerShell as Administrator

**Services Won't Start**
- Check Windows Event Viewer for detailed error messages
- Verify .NET 8 runtime is installed
- Check firewall settings for port 5093

**Can't Access API**
- Verify Windows Firewall allows port 5093
- Check if another service is using port 5093
- Try accessing via localhost: http://localhost:5093/swagger

### Log Files
Installation logs are stored in:
```
%TEMP%\InventorySystem-Install-YYYYMMDD-HHMMSS.log
```

### Get Support
1. Check the main documentation: [Complete Documentation](docs/COMPLETE-DOCUMENTATION.md)
2. Open an issue on GitHub: [Issues](https://github.com/hizir-ceylan/InventoryManagementSystem/issues)
3. Check Windows Event Viewer for service-related errors

## 🔒 Security Notes

- The installer requires Administrator privileges
- Services run under the Local System account by default
- API is accessible on localhost:5093 by default
- Consider firewall configuration for network access

## 📈 Advanced Configuration

After installation, you can modify configuration files:
- **API Config**: `C:\InventoryManagementSystem\Published\Api\appsettings.json`
- **Agent Config**: Via environment variables or service properties

For production deployments, consider:
- Using HTTPS with proper certificates
- Configuring SQL Server instead of SQLite
- Setting up reverse proxy with IIS or Nginx
- Implementing authentication and authorization