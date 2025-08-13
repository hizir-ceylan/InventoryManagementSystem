// Change Logs Module
class ChangeLogsManager {
    constructor() {
        this.changeLogs = [];
        this.filteredChangeLogs = [];
        this.devices = [];
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadChangeLogs();
        this.loadDevicesList();
    }

    setupEventListeners() {
        // Filter event listeners
        const filters = ['filter-change-type', 'filter-device', 'filter-date-from', 'filter-date-to'];
        filters.forEach(filterId => {
            const element = document.getElementById(filterId);
            if (element) {
                element.addEventListener('change', () => this.filterChangeLogs());
            }
        });
    }

    async loadChangeLogs() {
        try {
            window.ui.showLoading();
            const logs = await window.api.getChangeLogs();
            this.changeLogs = logs || [];
            this.filterChangeLogs();
            window.ui.hideLoading();
            window.ui.updateLastUpdateTime();
        } catch (error) {
            window.ui.showError('Değişiklik logları yüklenirken hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
    }

    async loadDevicesList() {
        try {
            const devices = await window.api.getDevices();
            this.devices = devices || [];
            this.populateDeviceFilter();
        } catch (error) {
            console.warn('Could not load devices for filter:', error.message);
        }
    }

    populateDeviceFilter() {
        const deviceFilter = document.getElementById('filter-device');
        if (!deviceFilter) return;

        // Clear existing options (except "All Devices")
        while (deviceFilter.children.length > 1) {
            deviceFilter.removeChild(deviceFilter.lastChild);
        }

        // Add device options
        this.devices.forEach(device => {
            const option = document.createElement('option');
            option.value = device.id;
            option.textContent = device.name || 'Bilinmeyen Cihaz';
            deviceFilter.appendChild(option);
        });
    }

    filterChangeLogs() {
        const changeTypeFilter = document.getElementById('filter-change-type')?.value || '';
        const deviceFilter = document.getElementById('filter-device')?.value || '';
        const dateFromFilter = document.getElementById('filter-date-from')?.value || '';
        const dateToFilter = document.getElementById('filter-date-to')?.value || '';

        this.filteredChangeLogs = this.changeLogs.filter(log => {
            // Change type filter
            const typeMatch = !changeTypeFilter || log.changeType === changeTypeFilter;

            // Device filter
            const deviceMatch = !deviceFilter || log.deviceId?.toString() === deviceFilter;

            // Date range filter
            let dateMatch = true;
            if (dateFromFilter || dateToFilter) {
                const logDate = new Date(log.changeDate);
                const fromDate = dateFromFilter ? new Date(dateFromFilter) : null;
                const toDate = dateToFilter ? new Date(dateToFilter + 'T23:59:59') : null;

                dateMatch = (!fromDate || logDate >= fromDate) && (!toDate || logDate <= toDate);
            }

            return typeMatch && deviceMatch && dateMatch;
        });

        this.renderChangeLogs();
    }

    renderChangeLogs() {
        const tableBody = document.getElementById('change-logs-body');
        const noLogsDiv = document.getElementById('no-change-logs');

        if (!tableBody) return;

        if (this.filteredChangeLogs.length === 0) {
            tableBody.innerHTML = '';
            window.ui.showNoDataMessage('no-change-logs');
            return;
        }

        window.ui.hideNoDataMessage('no-change-logs');

        tableBody.innerHTML = this.filteredChangeLogs.map(log => {
            const device = this.devices.find(d => d.id === log.deviceId);
            const deviceName = device ? device.name : 'Bilinmeyen Cihaz';

            return `
                <tr>
                    <td>${window.ui.formatDateTime(log.changeDate)}</td>
                    <td>
                        <a href="device-details.html?id=${log.deviceId}" class="device-link">
                            ${deviceName}
                        </a>
                    </td>
                    <td>${this.getChangeTypeText(log.changeType)}</td>
                    <td class="hide-mobile">${this.formatValue(log.oldValue)}</td>
                    <td class="hide-mobile">${this.formatValue(log.newValue)}</td>
                    <td class="hide-mobile">${log.changedBy || 'Sistem'}</td>
                </tr>
            `;
        }).join('');
    }

    getChangeTypeText(changeType) {
        const typeMap = {
            'Name': 'Cihaz Adı',
            'IpAddress': 'IP Adresi',
            'Status': 'Durum',
            'Location': 'Konum',
            'HardwareInfo': 'Donanım Bilgisi',
            'SoftwareInfo': 'Yazılım Bilgisi',
            'MacAddress': 'MAC Adresi',
            'Type': 'Cihaz Türü',
            'Model': 'Model'
        };

        return typeMap[changeType] || changeType;
    }

    formatValue(value) {
        if (!value || value === 'null' || value === 'undefined') {
            return '--';
        }

        // Truncate long values
        if (value.length > 50) {
            return value.substring(0, 47) + '...';
        }

        return value;
    }

    clearChangeLogFilters() {
        const filterElements = ['filter-change-type', 'filter-device', 'filter-date-from', 'filter-date-to'];
        filterElements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.value = '';
            }
        });
        this.filterChangeLogs();
    }

    async refreshChangeLogs() {
        await this.loadChangeLogs();
    }

    // Show change logs for a specific device
    showDeviceChangeLogs(deviceId) {
        const deviceFilter = document.getElementById('filter-device');
        if (deviceFilter) {
            deviceFilter.value = deviceId;
            this.filterChangeLogs();
        }
    }
}

// Global functions for change logs
function refreshChangeLogs() {
    if (window.changeLogs) {
        window.changeLogs.refreshChangeLogs();
    }
}

// Initialize change logs manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.changeLogs = new ChangeLogsManager();
    
    // Check if we need to show logs for a specific device
    const urlParams = new URLSearchParams(window.location.search);
    const deviceId = urlParams.get('deviceId');
    if (deviceId && window.changeLogs) {
        setTimeout(() => {
            window.changeLogs.showDeviceChangeLogs(deviceId);
        }, 500);
    }
});