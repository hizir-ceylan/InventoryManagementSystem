// Devices Module
class DeviceManager {
    constructor() {
        this.devices = [];
        this.filteredDevices = [];
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadDevices();
        this.loadStatistics();
        
        // Apply URL filters if present
        setTimeout(() => {
            if (window.navigation) {
                window.navigation.applyUrlFilters();
            }
        }, 100);
    }

    setupEventListeners() {
        // Search input
        const searchInput = document.getElementById('search-devices');
        if (searchInput) {
            searchInput.addEventListener('input', () => this.filterDevices());
        }

        // Filter dropdowns
        const filters = ['filter-status', 'filter-type', 'filter-discovery'];
        filters.forEach(filterId => {
            const element = document.getElementById(filterId);
            if (element) {
                element.addEventListener('change', () => this.filterDevices());
            }
        });
    }

    async loadDevices(showLoading = true) {
        try {
            if (showLoading) window.ui.showLoading();
            
            const devices = await window.api.getDevices();
            this.devices = devices || [];
            this.filterDevices();
            
            if (showLoading) window.ui.hideLoading();
            window.ui.updateLastUpdateTime();
        } catch (error) {
            window.ui.showError('Cihazlar yüklenirken hata oluştu: ' + error.message);
            if (showLoading) window.ui.hideLoading();
        }
    }

    async loadStatistics() {
        try {
            const stats = await window.api.getStatistics();
            window.ui.updateStatistics(stats);
        } catch (error) {
            console.warn('Statistics could not be loaded:', error.message);
        }
    }

    filterDevices() {
        const searchTerm = document.getElementById('search-devices')?.value.toLowerCase() || '';
        const statusFilter = document.getElementById('filter-status')?.value || '';
        const typeFilter = document.getElementById('filter-type')?.value || '';
        const discoveryFilter = document.getElementById('filter-discovery')?.value || '';

        this.filteredDevices = this.devices.filter(device => {
            // Text search
            const searchMatch = !searchTerm || 
                (device.name && device.name.toLowerCase().includes(searchTerm)) ||
                (device.ipAddress && device.ipAddress.toLowerCase().includes(searchTerm)) ||
                (device.macAddress && device.macAddress.toLowerCase().includes(searchTerm));

            // Status filter
            const statusMatch = !statusFilter || device.status?.toString() === statusFilter;

            // Type filter  
            const typeMatch = !typeFilter || device.type?.toString() === typeFilter;

            // Discovery filter
            const discoveryMatch = !discoveryFilter || device.discoveryMethod === discoveryFilter;

            return searchMatch && statusMatch && typeMatch && discoveryMatch;
        });

        this.renderDevices();
    }

    renderDevices() {
        const tableBody = document.getElementById('devices-table-body');
        const noDevicesDiv = document.getElementById('no-devices');

        if (!tableBody) return;

        if (this.filteredDevices.length === 0) {
            tableBody.innerHTML = '';
            window.ui.showNoDataMessage('no-devices');
            return;
        }

        window.ui.hideNoDataMessage('no-devices');

        tableBody.innerHTML = this.filteredDevices.map(device => `
            <tr>
                <td>
                    <div class="device-name">
                        <strong>${device.name || 'Bilinmeyen Cihaz'}</strong>
                        ${device.isVirtual ? '<span class="vm-badge">VM</span>' : ''}
                    </div>
                </td>
                <td>${device.ipAddress || '--'}</td>
                <td class="hide-mobile">${device.macAddress || '--'}</td>
                <td>${window.ui.getDeviceTypeText(device.type)}</td>
                <td>${window.ui.getStatusBadge(device.status)}</td>
                <td class="hide-mobile">${device.model || '--'}</td>
                <td class="hide-mobile">${device.location || '--'}</td>
                <td class="hide-mobile">${window.ui.getDiscoveryMethodText(device.discoveryMethod)}</td>
                <td>${window.ui.createActionButtons(device.id, device.name)}</td>
            </tr>
        `).join('');
    }

    applyFilter(filterType) {
        // Clear existing filters
        this.clearFilters();

        // Apply specific filter
        switch (filterType) {
            case 'active':
                document.getElementById('filter-status').value = '0';
                break;
            case 'updates':
                // This would need additional logic based on device update status
                break;
            case 'virtual':
                document.getElementById('filter-type').value = 'virtual';
                break;
        }

        this.filterDevices();
    }

    clearFilters() {
        const filterElements = ['search-devices', 'filter-status', 'filter-type', 'filter-discovery'];
        filterElements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.value = '';
            }
        });
        this.filterDevices();
    }

    async showAddDeviceModal() {
        // This would open the add device modal
        window.ui.showModal('deviceAddModal');
    }

    async createEmptyDevice() {
        try {
            const deviceData = {
                name: 'Yeni Cihaz',
                ipAddress: '',
                status: 0,
                type: 0
            };

            await window.api.createDevice(deviceData);
            await this.loadDevices();
            await this.loadStatistics();
            window.ui.closeModal();
        } catch (error) {
            window.ui.showError('Cihaz oluşturulurken hata oluştu: ' + error.message);
        }
    }
}

// Global functions for device operations
async function viewDeviceDetails(deviceId) {
    try {
        const device = await window.api.getDevice(deviceId);
        if (device) {
            // Store device data and navigate to device details page
            sessionStorage.setItem('selectedDevice', JSON.stringify(device));
            window.location.href = `/DeviceDetails?id=${deviceId}`;
        }
    } catch (error) {
        window.ui.showError('Cihaz detayları yüklenirken hata oluştu: ' + error.message);
    }
}

async function editDevice(deviceId) {
    try {
        const device = await window.api.getDevice(deviceId);
        if (device) {
            // This would populate and show the edit modal
            window.ui.showModal('deviceEditModal');
            // Additional logic to populate form would go here
        }
    } catch (error) {
        window.ui.showError('Cihaz bilgileri yüklenirken hata oluştu: ' + error.message);
    }
}

async function confirmDeleteDevice(deviceId, deviceName) {
    if (confirm(`"${deviceName}" cihazını silmek istediğinizden emin misiniz?`)) {
        await deleteDevice(deviceId);
    }
}

async function deleteDevice(deviceId) {
    try {
        await window.api.deleteDevice(deviceId);
        if (window.deviceManager) {
            await window.deviceManager.loadDevices();
            await window.deviceManager.loadStatistics();
        }
    } catch (error) {
        window.ui.showError('Cihaz silinirken hata oluştu: ' + error.message);
    }
}

function refreshDevices() {
    if (window.deviceManager) {
        window.deviceManager.loadDevices();
    }
}

function clearFilters() {
    if (window.deviceManager) {
        window.deviceManager.clearFilters();
    }
}

// Initialize device manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.deviceManager = new DeviceManager();
});