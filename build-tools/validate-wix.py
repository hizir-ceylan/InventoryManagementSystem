#!/usr/bin/env python3
"""
WiX MSI Build Validation Script

This script validates WiX configuration files for common ICE errors
that were reported in the original issue.
"""
import xml.etree.ElementTree as ET
import sys
import os
from pathlib import Path

def check_ice80_errors(root):
    """Check for ICE80: 32BitComponent using 64BitDirectory"""
    errors = []
    
    # Find all components without Win64="yes"
    for component in root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        win64 = component.get('Win64', '').lower()
        if win64 != 'yes':
            comp_id = component.get('Id', 'Unknown')
            directory = component.get('Directory', 'Unknown')
            # Check if it's using a 64-bit directory
            if directory in ['APIFOLDER', 'AGENTFOLDER', 'ProgramFiles64Folder']:
                errors.append(f"ICE80: Component '{comp_id}' missing Win64='yes' attribute for 64-bit directory '{directory}'")
    
    return errors

def check_ice03_errors(root):
    """Check for ICE03: Bad shortcut targets"""
    errors = []
    
    # Find shortcuts with HTTP targets (these should use cmd.exe wrapper)
    for shortcut in root.findall('.//wix:Shortcut', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        target = shortcut.get('Target', '')
        shortcut_id = shortcut.get('Id', 'Unknown')
        if target.startswith('http://') or target.startswith('https://'):
            errors.append(f"ICE03: Shortcut '{shortcut_id}' has invalid HTTP target: {target}")
    
    return errors

def check_ice21_errors(main_root, api_root, agent_root):
    """Check for ICE21: Components not belonging to any Feature"""
    errors = []
    
    # Get all component IDs referenced in Features
    referenced_components = set()
    referenced_groups = set()
    
    for feature in main_root.findall('.//wix:Feature', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        for comp_ref in feature.findall('.//wix:ComponentRef', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            referenced_components.add(comp_ref.get('Id'))
        for group_ref in feature.findall('.//wix:ComponentGroupRef', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            referenced_groups.add(group_ref.get('Id'))
    
    # Get all components referenced in ComponentGroups
    for group in main_root.findall('.//wix:ComponentGroup', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        group_id = group.get('Id')
        if group_id in referenced_groups:
            # Add ComponentRef elements
            for comp_ref in group.findall('.//wix:ComponentRef', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                referenced_components.add(comp_ref.get('Id'))
            # Add Components defined directly in the group
            for component in group.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                referenced_components.add(component.get('Id'))
    
    # Add Heat-generated components from referenced groups
    if 'ApiFilesGroup' in referenced_groups and api_root:
        for group in api_root.findall('.//wix:ComponentGroup[@Id="ApiFilesGroup"]', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            for component in group.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                referenced_components.add(component.get('Id'))
    
    if 'AgentFilesGroup' in referenced_groups and agent_root:
        for group in agent_root.findall('.//wix:ComponentGroup[@Id="AgentFilesGroup"]', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            for component in group.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                referenced_components.add(component.get('Id'))
    
    # Find orphaned components in main file
    for component in main_root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        comp_id = component.get('Id')
        if comp_id not in referenced_components:
            errors.append(f"ICE21: Component '{comp_id}' does not belong to any Feature")
    
    return errors

def check_ice18_errors(root):
    """Check for ICE18: KeyPath is Directory but not in CreateFolders"""
    errors = []
    
    for component in root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        comp_id = component.get('Id', 'Unknown')
        directory = component.get('Directory', '')
        
        # Check if component uses Directory as KeyPath (no files, or explicit Directory KeyPath)
        files = component.findall('.//wix:File', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'})
        create_folders = component.findall('.//wix:CreateFolder', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'})
        registry_values = component.findall('.//wix:RegistryValue', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'})
        
        # If no files and no registry KeyPath, Directory is likely the KeyPath
        has_file_keypath = any(f.get('KeyPath', '').lower() == 'yes' for f in files)
        has_registry_keypath = any(r.get('KeyPath', '').lower() == 'yes' for r in registry_values)
        
        if not has_file_keypath and not has_registry_keypath and len(create_folders) == 0:
            # This component likely uses Directory as KeyPath but has no CreateFolder
            if any(elem.tag.endswith('}ServiceInstall') or elem.tag.endswith('}FirewallException') 
                   for elem in component):
                errors.append(f"ICE18: Component '{comp_id}' uses Directory KeyPath but missing CreateFolder element")
    
    return errors

def check_ice30_errors(main_root, api_root, agent_root):
    """Check for ICE30: Same file installed by multiple components"""
    errors = []
    file_sources = {}  # source -> [component_ids]
    
    # Check main file
    for component in main_root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
        comp_id = component.get('Id', 'Unknown')
        for file_elem in component.findall('.//wix:File', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            source = file_elem.get('Source', '')
            if source:
                # Normalize source path
                normalized = source.replace('\\', '/').lower()
                if normalized not in file_sources:
                    file_sources[normalized] = []
                file_sources[normalized].append(comp_id)
    
    # Check API Heat files
    if api_root:
        for component in api_root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            comp_id = component.get('Id', 'Unknown')
            for file_elem in component.findall('.//wix:File', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                source = file_elem.get('Source', '')
                if source:
                    # Convert Heat variable source to normalized path
                    normalized = source.replace('$(var.ApiSourceDir)', 'published/api').replace('\\', '/').lower()
                    if normalized not in file_sources:
                        file_sources[normalized] = []
                    file_sources[normalized].append(comp_id)
    
    # Check Agent Heat files
    if agent_root:
        for component in agent_root.findall('.//wix:Component', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
            comp_id = component.get('Id', 'Unknown')
            for file_elem in component.findall('.//wix:File', {'wix': 'http://schemas.microsoft.com/wix/2006/wi'}):
                source = file_elem.get('Source', '')
                if source:
                    # Convert Heat variable source to normalized path
                    normalized = source.replace('$(var.AgentSourceDir)', 'published/agent').replace('\\', '/').lower()
                    if normalized not in file_sources:
                        file_sources[normalized] = []
                    file_sources[normalized].append(comp_id)
    
    # Find duplicates
    for source, components in file_sources.items():
        if len(components) > 1:
            errors.append(f"ICE30: File '{source}' is installed by multiple components: {', '.join(components)}")
    
    return errors

def main():
    script_dir = Path(__file__).parent
    main_wxs = script_dir / 'InventoryManagementSystem.wxs'
    api_wxs = script_dir / 'Setup' / 'MSI' / 'ApiFiles.wxs'
    agent_wxs = script_dir / 'Setup' / 'MSI' / 'AgentFiles.wxs'
    
    print("WiX MSI Build Validation")
    print("=" * 50)
    
    # Parse main WiX file
    try:
        main_tree = ET.parse(main_wxs)
        main_root = main_tree.getroot()
        print(f"✓ Parsed main WiX file: {main_wxs}")
    except Exception as e:
        print(f"✗ Failed to parse main WiX file: {e}")
        return 1
    
    # Parse Heat-generated files (optional)
    api_root = None
    agent_root = None
    
    if api_wxs.exists():
        try:
            api_tree = ET.parse(api_wxs)
            api_root = api_tree.getroot()
            print(f"✓ Parsed API Heat file: {api_wxs}")
        except Exception as e:
            print(f"⚠ Could not parse API Heat file: {e}")
    
    if agent_wxs.exists():
        try:
            agent_tree = ET.parse(agent_wxs)
            agent_root = agent_tree.getroot()
            print(f"✓ Parsed Agent Heat file: {agent_wxs}")
        except Exception as e:
            print(f"⚠ Could not parse Agent Heat file: {e}")
    
    print()
    
    # Run all checks
    all_errors = []
    
    print("Checking for ICE80 errors (32-bit components in 64-bit directories)...")
    ice80_errors = check_ice80_errors(main_root)
    if api_root:
        ice80_errors.extend(check_ice80_errors(api_root))
    if agent_root:
        ice80_errors.extend(check_ice80_errors(agent_root))
    all_errors.extend(ice80_errors)
    print(f"  Found {len(ice80_errors)} ICE80 errors")
    
    print("Checking for ICE03 errors (bad shortcut targets)...")
    ice03_errors = check_ice03_errors(main_root)
    all_errors.extend(ice03_errors)
    print(f"  Found {len(ice03_errors)} ICE03 errors")
    
    print("Checking for ICE21 errors (components not in features)...")
    ice21_errors = check_ice21_errors(main_root, api_root, agent_root)
    all_errors.extend(ice21_errors)
    print(f"  Found {len(ice21_errors)} ICE21 errors")
    
    print("Checking for ICE18 errors (missing CreateFolder)...")
    ice18_errors = check_ice18_errors(main_root)
    all_errors.extend(ice18_errors)
    print(f"  Found {len(ice18_errors)} ICE18 errors")
    
    print("Checking for ICE30 errors (duplicate file components)...")
    ice30_errors = check_ice30_errors(main_root, api_root, agent_root)
    all_errors.extend(ice30_errors)
    print(f"  Found {len(ice30_errors)} ICE30 errors")
    
    print()
    print("=" * 50)
    
    if all_errors:
        print(f"VALIDATION FAILED: Found {len(all_errors)} total errors")
        print()
        for error in all_errors:
            print(f"  ✗ {error}")
        return 1
    else:
        print("✓ VALIDATION PASSED: No ICE errors detected")
        print()
        print("The WiX configuration should now build successfully!")
        return 0

if __name__ == '__main__':
    sys.exit(main())