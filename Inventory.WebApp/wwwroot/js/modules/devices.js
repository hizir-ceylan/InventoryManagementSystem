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
            // Show the edit modal
            window.ui.showModal('deviceEditModal');
            // Populate the edit form using the same logic as device-details module
            if (window.deviceDetails && window.deviceDetails.populateEditForm) {
                window.deviceDetails.populateEditForm(device);
            } else {
                // Fallback form population
                populateEditForm(device);
            }
        }
    } catch (error) {
        window.ui.showError('Cihaz bilgileri yüklenirken hata oluştu: ' + error.message);
    }
}

function populateEditForm(device) {
    const editContent = document.getElementById('device-edit-content');
    if (editContent) {
        editContent.innerHTML = `
            <div class="device-edit-form">
                <div class="form-row">
                    <div class="form-group">
                        <label for="edit-device-name">Cihaz Adı: <span class="text-danger">*</span></label>
                        <input type="text" id="edit-device-name" class="form-control" value="${device.deviceName || ''}" required>
                    </div>
                    <div class="form-group">
                        <label for="edit-device-type">Cihaz Türü:</label>
                        <select id="edit-device-type" class="form-control">
                            <option value="0" ${device.deviceType === 0 ? 'selected' : ''}>Bilinmiyor</option>
                            <option value="1" ${device.deviceType === 1 ? 'selected' : ''}>Laptop</option>
                            <option value="2" ${device.deviceType === 2 ? 'selected' : ''}>Masaüstü</option>
                            <option value="3" ${device.deviceType === 3 ? 'selected' : ''}>Sunucu</option>
                            <option value="4" ${device.deviceType === 4 ? 'selected' : ''}>Yazıcı</option>
                            <option value="5" ${device.deviceType === 5 ? 'selected' : ''}>Tarayıcı</option>
                            <option value="6" ${device.deviceType === 6 ? 'selected' : ''}>Kamera</option>
                            <option value="7" ${device.deviceType === 7 ? 'selected' : ''}>IP Telefon</option>
                            <option value="8" ${device.deviceType === 8 ? 'selected' : ''}>Ağ Cihazı</option>
                            <option value="9" ${device.deviceType === 9 ? 'selected' : ''}>Router</option>
                            <option value="10" ${device.deviceType === 10 ? 'selected' : ''}>Switch</option>
                            <option value="11" ${device.deviceType === 11 ? 'selected' : ''}>Access Point</option>
                            <option value="12" ${device.deviceType === 12 ? 'selected' : ''}>Depolama</option>
                            <option value="13" ${device.deviceType === 13 ? 'selected' : ''}>Tablet</option>
                            <option value="14" ${device.deviceType === 14 ? 'selected' : ''}>Akıllı Telefon</option>
                            <option value="15" ${device.deviceType === 15 ? 'selected' : ''}>Akıllı TV</option>
                            <option value="16" ${device.deviceType === 16 ? 'selected' : ''}>Projektör/Ekran</option>
                            <option value="17" ${device.deviceType === 17 ? 'selected' : ''}>Diğer</option>
                        </select>
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label for="edit-ip-address">IP Adresi:</label>
                        <input type="text" id="edit-ip-address" class="form-control" value="${device.ipAddress || ''}" placeholder="Örn: 192.168.1.100">
                    </div>
                    <div class="form-group">
                        <label for="edit-location">Konum:</label>
                        <input type="text" id="edit-location" class="form-control" value="${device.location || ''}" placeholder="Örn: İkinci Kat - Muhasebe">
                    </div>
                </div>
                <div class="form-group">
                    <label for="edit-notes">Notlar:</label>
                    <textarea id="edit-notes" class="form-control" rows="3" placeholder="Cihazla ilgili notlar...">${device.notes || ''}</textarea>
                </div>
                <input type="hidden" id="edit-device-id" value="${device.id}">
            </div>
        `;
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