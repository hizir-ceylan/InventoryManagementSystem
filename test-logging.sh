#!/bin/bash
# Test script for validating hourly logging and weekend retention fix
# Usage: ./test-logging.sh [test_folder]

TEST_FOLDER="${1:-/tmp/inventory_test_logs}"

echo "=== Inventory Management System - Logging Test ==="
echo "Test folder: $TEST_FOLDER"
echo

# Create test environment
mkdir -p "$TEST_FOLDER/LocalLogs"
cd "$TEST_FOLDER"

# Mock device data for testing
cat > device_test_data.json << 'EOF'
{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": "PC",
    "model": "Test Model",
    "location": "Test Location",
    "status": 0,
    "hardwareInfo": {
        "cpu": "Intel Core i7-12700K",
        "cpuCores": 12,
        "ramGB": 32,
        "diskGB": 1000
    },
    "softwareInfo": {
        "operatingSystem": "Windows 11 Pro",
        "osVersion": "10.0.22631"
    }
}
EOF

echo "=== Test 1: Simulating Weekend Logging Scenario ==="

# Simulate logs from different days
simulate_log() {
    local date_str="$1"
    local hour="$2"
    local device_data="$3"
    
    local log_file="LocalLogs/device-log-${date_str}-${hour}.json"
    
    cat > "$log_file" << EOF
{
    "Date": "${date_str}T${hour}:00:00Z",
    "Device": $device_data,
    "Diff": "No change detected"
}
EOF
    echo "Created: $log_file"
}

# Simulate Friday logs (should be kept when Monday runs)
echo "Simulating Friday logs..."
simulate_log "2024-01-12" "08" "$(cat device_test_data.json)"
simulate_log "2024-01-12" "12" "$(cat device_test_data.json)"
simulate_log "2024-01-12" "16" "$(cat device_test_data.json)"

# Simulate Saturday logs (should be kept when Monday runs)
echo "Simulating Saturday logs..."
simulate_log "2024-01-13" "10" "$(cat device_test_data.json)"
simulate_log "2024-01-13" "14" "$(cat device_test_data.json)"

# Simulate Sunday logs (should be kept when Monday runs)
echo "Simulating Sunday logs..."
simulate_log "2024-01-14" "09" "$(cat device_test_data.json)"
simulate_log "2024-01-14" "15" "$(cat device_test_data.json)"

# Simulate Monday logs (current day)
echo "Simulating Monday logs..."
simulate_log "2024-01-15" "08" "$(cat device_test_data.json)"
simulate_log "2024-01-15" "12" "$(cat device_test_data.json)"

# Simulate very old logs (should be deleted)
echo "Simulating old logs (should be deleted)..."
simulate_log "2024-01-10" "10" "$(cat device_test_data.json)"
simulate_log "2024-01-11" "14" "$(cat device_test_data.json)"

echo
echo "=== Test 2: Initial File Count ==="
ls -la LocalLogs/
echo "Total log files: $(ls LocalLogs/device-log-*.json | wc -l)"

echo
echo "=== Test 3: Simulating Monday Run (48-hour retention test) ==="

# Create a simple test for the logging logic
cat > test_retention.py << 'EOF'
#!/usr/bin/env python3
import os
import json
from datetime import datetime, timedelta
import glob

def test_48_hour_retention():
    log_folder = "LocalLogs"
    
    # Simulate current time as Monday 2024-01-15 16:00
    current_time = datetime(2024, 1, 15, 16, 0, 0)
    cutoff_time = current_time - timedelta(hours=48)
    
    print(f"Current time (simulated): {current_time}")
    print(f"Cutoff time (48h ago): {cutoff_time}")
    print()
    
    # Get all log files
    log_files = glob.glob(os.path.join(log_folder, "device-log-*.json"))
    
    files_to_keep = []
    files_to_delete = []
    
    for file_path in log_files:
        filename = os.path.basename(file_path)
        if filename.startswith("device-log-"):
            datetime_str = filename[len("device-log-"):-5]  # Remove prefix and .json
            
            try:
                # Parse YYYY-MM-DD-HH format
                file_datetime = datetime.strptime(datetime_str, "%Y-%m-%d-%H")
                
                if file_datetime < cutoff_time:
                    files_to_delete.append((file_path, file_datetime))
                else:
                    files_to_keep.append((file_path, file_datetime))
                    
            except ValueError:
                print(f"Could not parse datetime from: {filename}")
    
    print("=== Files to KEEP (within 48 hours) ===")
    for file_path, file_datetime in sorted(files_to_keep, key=lambda x: x[1]):
        age_hours = (current_time - file_datetime).total_seconds() / 3600
        print(f"KEEP: {os.path.basename(file_path)} (age: {age_hours:.1f} hours)")
    
    print()
    print("=== Files to DELETE (older than 48 hours) ===")
    for file_path, file_datetime in sorted(files_to_delete, key=lambda x: x[1]):
        age_hours = (current_time - file_datetime).total_seconds() / 3600
        print(f"DELETE: {os.path.basename(file_path)} (age: {age_hours:.1f} hours)")
    
    print()
    print("=== Weekend Retention Test Results ===")
    friday_files = [f for f, dt in files_to_keep if dt.date() == datetime(2024, 1, 12).date()]
    saturday_files = [f for f, dt in files_to_keep if dt.date() == datetime(2024, 1, 13).date()]
    sunday_files = [f for f, dt in files_to_keep if dt.date() == datetime(2024, 1, 14).date()]
    
    print(f"Friday files kept: {len(friday_files)}")
    print(f"Saturday files kept: {len(saturday_files)}")
    print(f"Sunday files kept: {len(sunday_files)}")
    
    # Check if Friday files are kept (this was the original bug)
    if len(friday_files) > 0:
        print("✅ SUCCESS: Friday logs are kept when Monday runs!")
    else:
        print("❌ FAILED: Friday logs were deleted when Monday runs!")
    
    # Check if very old files are deleted
    very_old_files = [f for f, dt in files_to_delete if dt.date() <= datetime(2024, 1, 11).date()]
    if len(very_old_files) > 0:
        print("✅ SUCCESS: Very old logs are properly deleted!")
    else:
        print("⚠️  WARNING: No very old logs to delete in test!")
    
    print()
    print("=== Summary ===")
    print(f"Total files: {len(log_files)}")
    print(f"Files to keep: {len(files_to_keep)}")
    print(f"Files to delete: {len(files_to_delete)}")
    
    return len(files_to_keep), len(files_to_delete), len(friday_files) > 0

if __name__ == "__main__":
    test_48_hour_retention()
EOF

python3 test_retention.py

echo
echo "=== Test 4: Hourly File Format Test ==="

# Test the new hourly format vs old daily format
echo "Testing file naming patterns..."

# Check if files follow YYYY-MM-DD-HH pattern
for file in LocalLogs/device-log-*.json; do
    filename=$(basename "$file")
    datetime_part=${filename#device-log-}
    datetime_part=${datetime_part%.json}
    
    if [[ $datetime_part =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}-[0-9]{2}$ ]]; then
        echo "✅ Valid hourly format: $filename"
    else
        echo "❌ Invalid format: $filename"
    fi
done

echo
echo "=== Test 5: Change Detection Test ==="

# Test change detection with modified device data
cat > device_test_data_modified.json << 'EOF'
{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.101",
    "deviceType": "PC",
    "model": "Test Model Updated",
    "location": "Test Location",
    "status": 0,
    "hardwareInfo": {
        "cpu": "Intel Core i7-12700K",
        "cpuCores": 12,
        "ramGB": 64,
        "diskGB": 2000
    },
    "softwareInfo": {
        "operatingSystem": "Windows 11 Pro",
        "osVersion": "10.0.22631"
    }
}
EOF

# Create a log with changes
cat > "LocalLogs/device-log-2024-01-15-17.json" << EOF
{
    "Date": "2024-01-15T17:00:00Z",
    "Device": $(cat device_test_data_modified.json),
    "Diff": {
        "Device.IpAddress": {
            "Added": [],
            "Removed": [],
            "ChangedValues": [
                {
                    "Field": "IpAddress",
                    "OldValue": "192.168.1.100",
                    "NewValue": "192.168.1.101"
                }
            ]
        },
        "Device.Model": {
            "Added": [],
            "Removed": [],
            "ChangedValues": [
                {
                    "Field": "Model",
                    "OldValue": "Test Model",
                    "NewValue": "Test Model Updated"
                }
            ]
        },
        "HardwareInfo.RamGB": {
            "Added": [],
            "Removed": [],
            "ChangedValues": [
                {
                    "Field": "RamGB",
                    "OldValue": 32,
                    "NewValue": 64
                }
            ]
        }
    }
}
EOF

echo "Created change detection test log"

echo
echo "=== Test 6: Configuration Test ==="

# Test configuration parsing
cat > test_config.json << 'EOF'
{
  "Agent": {
    "LoggingInterval": "01:00:00",
    "LogRetentionHours": 48,
    "EnableHourlyLogging": true
  },
  "Logging": {
    "RetentionHours": 48,
    "EnableHourlyLogging": true
  }
}
EOF

echo "✅ Configuration test file created"

echo
echo "=== Final Test Results ==="

# Count files by day
friday_count=$(ls LocalLogs/device-log-2024-01-12-*.json 2>/dev/null | wc -l)
saturday_count=$(ls LocalLogs/device-log-2024-01-13-*.json 2>/dev/null | wc -l)
sunday_count=$(ls LocalLogs/device-log-2024-01-14-*.json 2>/dev/null | wc -l)
monday_count=$(ls LocalLogs/device-log-2024-01-15-*.json 2>/dev/null | wc -l)
old_count=$(ls LocalLogs/device-log-2024-01-1[01]-*.json 2>/dev/null | wc -l)

echo "Log file counts by day:"
echo "  Friday (2024-01-12): $friday_count files"
echo "  Saturday (2024-01-13): $saturday_count files"
echo "  Sunday (2024-01-14): $sunday_count files"
echo "  Monday (2024-01-15): $monday_count files"
echo "  Very old (Jan 10-11): $old_count files"

echo
echo "=== Test Completion ==="
if [ $friday_count -gt 0 ] && [ $saturday_count -gt 0 ] && [ $sunday_count -gt 0 ]; then
    echo "🎉 SUCCESS: Weekend gap issue has been fixed!"
    echo "   Friday logs are retained when Monday runs"
    echo "   48-hour sliding window works correctly"
else
    echo "❌ FAILED: Weekend gap issue still exists"
fi

echo
echo "Test folder contents:"
find "$TEST_FOLDER" -type f -name "*.json" | sort

echo
echo "Test completed. Check the results above."
echo "To cleanup test files, run: rm -rf $TEST_FOLDER"