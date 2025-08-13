// API Module for making HTTP requests
class ApiManager {
    constructor() {
        this.baseUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
        this.defaultTimeout = 30000; // 30 seconds
    }

    // Make API call with error handling
    async apiCall(endpoint, options = {}) {
        const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}/${endpoint}`;
        
        const defaultOptions = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            timeout: this.defaultTimeout
        };

        const mergedOptions = { ...defaultOptions, ...options };
        
        // Handle timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), mergedOptions.timeout);
        mergedOptions.signal = controller.signal;

        try {
            const response = await fetch(url, mergedOptions);
            clearTimeout(timeoutId);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            } else {
                return await response.text();
            }
        } catch (error) {
            clearTimeout(timeoutId);
            if (error.name === 'AbortError') {
                throw new Error('Request timed out');
            }
            throw error;
        }
    }

    // Device API methods
    async getDevices() {
        return await this.apiCall('api/device');
    }

    async getDevice(id) {
        return await this.apiCall(`api/device/${id}`);
    }

    async createDevice(deviceData) {
        return await this.apiCall('api/device', {
            method: 'POST',
            body: JSON.stringify(deviceData)
        });
    }

    async updateDevice(id, deviceData) {
        return await this.apiCall(`api/device/${id}`, {
            method: 'PUT',
            body: JSON.stringify(deviceData)
        });
    }

    async deleteDevice(id) {
        return await this.apiCall(`api/device/${id}`, {
            method: 'DELETE'
        });
    }

    // Change logs API methods
    async getChangeLogs() {
        return await this.apiCall('api/device/change-logs');
    }

    async getDeviceChangeLogs(deviceId) {
        return await this.apiCall(`api/device/${deviceId}/change-logs`);
    }

    // Network scan API methods
    async startNetworkScan(networkRange, timeout = 5, portScan = 'common') {
        return await this.apiCall('api/network/scan', {
            method: 'POST',
            body: JSON.stringify({
                networkRange,
                timeout,
                portScan
            })
        });
    }

    async getNetworkScanStatus(scanId) {
        return await this.apiCall(`api/network/scan/${scanId}/status`);
    }

    async getNetworkScanResults(scanId) {
        return await this.apiCall(`api/network/scan/${scanId}/results`);
    }

    // VMware API methods
    async getVMwareStatus() {
        return await this.apiCall('api/vmware/status');
    }

    async getVirtualMachines() {
        return await this.apiCall('api/vmware/virtual-machines');
    }

    async syncVirtualMachines() {
        return await this.apiCall('api/vmware/sync', {
            method: 'POST'
        });
    }

    async testVMwareConnection() {
        return await this.apiCall('api/vmware/test-connection', {
            method: 'POST'
        });
    }

    async updateVMwareConfiguration(config) {
        return await this.apiCall('api/vmware/configuration', {
            method: 'PUT',
            body: JSON.stringify(config)
        });
    }

    // Statistics API methods
    async getStatistics() {
        return await this.apiCall('api/device/statistics');
    }

    // Health check
    async healthCheck() {
        return await this.apiCall('api/health');
    }
}

// Initialize API manager
window.api = new ApiManager();