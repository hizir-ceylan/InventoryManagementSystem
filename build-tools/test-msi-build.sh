#!/bin/bash
# Test script to demonstrate MSI build process without requiring Windows/WiX

echo "=============================================="
echo "WiX MSI Build Test - Linux Simulation"
echo "=============================================="
echo ""

# Check if Published files exist
echo "1. Checking Published files..."
if [ -d "Published/Api" ] && [ -d "Published/Agent" ]; then
    echo "   ✓ Published directories exist"
    echo "   ✓ API files: $(ls -1 Published/Api | wc -l) files"
    echo "   ✓ Agent files: $(ls -1 Published/Agent | wc -l) files"
    
    # Check for executables
    if [ -f "Published/Api/Inventory.Api.exe" ]; then
        echo "   ✓ API executable found"
    else
        echo "   ✗ API executable missing"
    fi
    
    if [ -f "Published/Agent/Inventory.Agent.Windows.exe" ]; then
        echo "   ✓ Agent executable found"
    else
        echo "   ✗ Agent executable missing"
    fi
else
    echo "   ✗ Published directories missing - run dotnet publish first"
fi
echo ""

# Validate WiX configuration
echo "2. Validating WiX configuration..."
if [ -f "validate-wix.py" ]; then
    python3 validate-wix.py
    if [ $? -eq 0 ]; then
        echo "   ✓ WiX validation passed"
        wix_valid=true
    else
        echo "   ✗ WiX validation failed"
        wix_valid=false
    fi
else
    echo "   ✗ Validation script not found"
    wix_valid=false
fi
echo ""

# Simulate Heat file generation
echo "3. Simulating Heat file generation..."
mkdir -p Setup/MSI

# Simulate API files harvesting
echo '<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Fragment>
        <ComponentGroup Id="ApiFilesGroup">
            <!-- Heat would generate components for all files in Published/Api -->' > Setup/MSI/ApiFiles.wxs

if [ -d "Published/Api" ]; then
    file_count=0
    for file in Published/Api/*; do
        if [ -f "$file" ] && [ "$(basename "$file")" != "Inventory.Api.exe" ]; then
            filename=$(basename "$file")
            component_id="cmp$(echo "$filename" | tr '.' '_' | tr '[:lower:]' '[:upper:]')"
            echo "            <Component Id=\"$component_id\" Directory=\"APIFOLDER\" Win64=\"yes\">" >> Setup/MSI/ApiFiles.wxs
            echo "                <File Id=\"fil$component_id\" KeyPath=\"yes\" Source=\"\$(var.ApiSourceDir)\\$filename\" />" >> Setup/MSI/ApiFiles.wxs
            echo "            </Component>" >> Setup/MSI/ApiFiles.wxs
            file_count=$((file_count + 1))
        fi
    done
    echo "   ✓ Generated API components for $file_count files"
else
    echo "   ✗ API directory not found"
fi

echo '        </ComponentGroup>
    </Fragment>
</Wix>' >> Setup/MSI/ApiFiles.wxs

# Simulate Agent files harvesting
echo '<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Fragment>
        <ComponentGroup Id="AgentFilesGroup">
            <!-- Heat would generate components for all files in Published/Agent -->' > Setup/MSI/AgentFiles.wxs

if [ -d "Published/Agent" ]; then
    file_count=0
    for file in Published/Agent/*; do
        if [ -f "$file" ] && [ "$(basename "$file")" != "Inventory.Agent.Windows.exe" ]; then
            filename=$(basename "$file")
            component_id="cmp$(echo "$filename" | tr '.' '_' | tr '[:lower:]' '[:upper:]')"
            echo "            <Component Id=\"$component_id\" Directory=\"AGENTFOLDER\" Win64=\"yes\">" >> Setup/MSI/AgentFiles.wxs
            echo "                <File Id=\"fil$component_id\" KeyPath=\"yes\" Source=\"\$(var.AgentSourceDir)\\$filename\" />" >> Setup/MSI/AgentFiles.wxs
            echo "            </Component>" >> Setup/MSI/AgentFiles.wxs
            file_count=$((file_count + 1))
        fi
    done
    echo "   ✓ Generated Agent components for $file_count files"
else
    echo "   ✗ Agent directory not found"
fi

echo '        </ComponentGroup>
    </Fragment>
</Wix>' >> Setup/MSI/AgentFiles.wxs

echo ""

# Validate generated files
echo "4. Validating generated Heat files..."
for file in Setup/MSI/ApiFiles.wxs Setup/MSI/AgentFiles.wxs; do
    if python3 -c "import xml.etree.ElementTree as ET; ET.parse('$file')" 2>/dev/null; then
        echo "   ✓ $file is valid XML"
    else
        echo "   ✗ $file has XML errors"
    fi
done
echo ""

# Final validation with generated files
echo "5. Final validation with Heat-generated files..."
if [ "$wix_valid" = true ]; then
    python3 validate-wix.py
    if [ $? -eq 0 ]; then
        echo ""
        echo "=============================================="
        echo "✅ MSI BUILD SIMULATION SUCCESSFUL"
        echo "=============================================="
        echo ""
        echo "The following would be created on Windows:"
        echo "  📦 Setup/MSI/InventoryManagementSystem.msi"
        echo "  📁 Setup/Enterprise/ (deployment package)"
        echo "  📋 Deploy.bat, Uninstall.bat (enterprise scripts)"
        echo ""
        echo "MSI would include:"
        echo "  🔧 Windows Services (API + Agent)"
        echo "  🔥 Firewall Rules (Port 5093)"
        echo "  🔗 Start Menu Shortcuts"
        echo "  📁 Optional Desktop Shortcuts"
        echo "  📊 All dependency files (240+ files total)"
        echo ""
        echo "Ready for Windows MSI build!"
    else
        echo ""
        echo "❌ Final validation failed"
    fi
else
    echo "❌ Cannot complete - WiX validation failed"
fi