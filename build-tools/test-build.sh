#!/bin/bash
# Test script to validate the build and publish process

echo "========================================"
echo "Inventory Management System - Build Test"
echo "========================================"

# Clean up previous builds
echo "Cleaning previous builds..."
rm -rf Published Setup

# Test build process
echo "Testing build process..."
dotnet clean
dotnet restore
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

# Test publish process
echo "Testing publish process..."
mkdir -p Published/Api Published/Agent Setup

dotnet publish ../Inventory.Api --configuration Release --output "Published/Api" --no-build --self-contained false
if [ $? -ne 0 ]; then
    echo "❌ API publish failed!"
    exit 1
fi

dotnet publish ../Inventory.Agent.Windows --configuration Release --output "Published/Agent" --no-build --self-contained false
if [ $? -ne 0 ]; then
    echo "❌ Agent publish failed!"
    exit 1
fi

# Verify published files
echo "Verifying published files..."

API_EXECUTABLE="Published/Api/Inventory.Api"
AGENT_EXECUTABLE="Published/Agent/Inventory.Agent.Windows"

if [ ! -f "$API_EXECUTABLE" ]; then
    echo "❌ API executable not found: $API_EXECUTABLE"
    exit 1
fi

if [ ! -f "$AGENT_EXECUTABLE" ]; then
    echo "❌ Agent executable not found: $AGENT_EXECUTABLE"
    exit 1
fi

echo "✅ API executable found: $API_EXECUTABLE"
echo "✅ Agent executable found: $AGENT_EXECUTABLE"

# Create test configuration files
echo "Creating test configuration files..."

# API configuration
cat > "Published/Api/appsettings.json" << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5093"
}
EOF

# Agent configuration
cat > "Published/Agent/appsettings.json" << 'EOF'
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5093",
    "EnableOfflineStorage": true,
    "OfflineStoragePath": "C:\\Program Files\\Inventory Management System\\Data\\OfflineStorage"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
EOF

echo "✅ Configuration files created"

# Test executable permissions (Linux)
chmod +x "$API_EXECUTABLE"
chmod +x "$AGENT_EXECUTABLE"

echo "✅ Executable permissions set"

# Display file sizes
echo ""
echo "Published file information:"
echo "API directory size: $(du -sh Published/Api | cut -f1)"
echo "Agent directory size: $(du -sh Published/Agent | cut -f1)"
echo "Total size: $(du -sh Published | cut -f1)"

echo ""
echo "✅ All tests passed!"
echo ""
echo "Ready for Windows setup.exe creation!"
echo ""
echo "To create setup.exe on Windows:"
echo "1. Copy this entire folder to a Windows machine"
echo "2. Install Inno Setup from: https://jrsoftware.org/isinfo.php"  
echo "3. Run: Build-Setup.ps1 or Build-Setup.bat"
echo "4. The setup.exe will be created in the Setup folder"

echo ""
echo "Published files are ready in:"
echo "  - API: $(realpath Published/Api)"
echo "  - Agent: $(realpath Published/Agent)"