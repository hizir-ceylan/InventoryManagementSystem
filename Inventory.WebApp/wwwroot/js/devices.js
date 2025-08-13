// Devices Page Specific JavaScript
class DevicesPageApp {
    constructor() {
        this.apiBaseUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
        this.devices = [];
        this.filteredDevices = [];
        this.currentEditDevice = null;

        this.init();
    }

    // Initialize the application
    init() {
        this.setupEventListeners();
        this.loadInitialData();
        this.setupMobileMenu();

        // Set up auto-refresh every 30 seconds
        setInterval(() => {
            this.loadDevices(false); // Silent refresh
        }, window.INVENTORY_CONFIG?.AUTO_REFRESH_INTERVAL || 30000);

        // Set up device status update every 5 minutes
        setInterval(() => {
            this.updateDeviceStatuses();
        }, 5 * 60 * 1000); // 5 minutes
    }

    // Setup mobile menu
    setupMobileMenu() {
        const toggle = document.getElementById('navbar-toggle');
        const menu = document.getElementById('navbar-menu');

        if (toggle && menu) {
            toggle.addEventListener('click', () => {
                menu.classList.toggle('show');
            });

            // Close menu when clicking on links
            menu.addEventListener('click', (e) => {
                if (e.target.classList.contains('nav-link')) {
                    menu.classList.remove('show');
                }
            });

            // Close menu when clicking outside
            document.addEventListener('click', (e) => {
                if (!toggle.contains(e.target) && !menu.contains(e.target)) {
                    menu.classList.remove('show');
                }
            });
        }
    }

    // Setup event listeners
    setupEventListeners() {
        // Search functionality
        document.getElementById('search-devices').addEventListener('input', (e) => {
            this.filterDevices();
        });

        // Filter dropdowns
        document.getElementById('filter-status').addEventListener('change', () => {
            this.filterDevices();
        });

        document.getElementById('filter-type').addEventListener('change', () => {
            this.filterDevices();
        });

        document.getElementById('filter-discovery').addEventListener('change', () => {
            this.filterDevices();
        });
    }

    // Load initial data
    async loadInitialData() {
        try {
            await this.loadDevices();
        } catch (error) {
            this.showError('Veri yüklenirken hata oluştu: ' + error.message);
        }
        this.updateLastUpdateTime();
    }

    // Load devices from API
    async loadDevices(showLoading = true) {
        try {
            if (showLoading) this.showLoading();
            
            const allDevices = await this.apiCall('device');
            this.devices = allDevices || [];
            
            if (this.devices && this.devices.length > 0) {
                // Update statistics
                this.updateStatistics(this.devices);
                this.filterDevices();
            } else {
                // Use mock data if API returns empty or fails
                this.devices = this.getMockDevices();
                this.updateStatistics(this.devices);
                this.filterDevices();
            }

            if (showLoading) this.hideLoading();
        } catch (error) {
            console.warn('API call failed, using mock data:', error);
            this.devices = this.getMockDevices();
            this.updateStatistics(this.devices);
            this.filterDevices();
            if (showLoading) this.hideLoading();
        }
        this.updateLastUpdateTime();
    }

    // API call helper
    async apiCall(endpoint, options = {}) {
        const url = `${this.apiBaseUrl}/api/${endpoint}`;
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    }

    // Update statistics cards
    updateStatistics(devices) {
        const totalDevices = devices.length;
        const activeDevices = devices.filter(d => this.getComputedStatus(d) === 0).length;
        
        document.getElementById('total-devices').textContent = totalDevices;
        document.getElementById('active-devices').textContent = activeDevices;
        
        // Load update statistics asynchronously
        this.loadUpdateStatistics();
    }

    // Load update statistics
    async loadUpdateStatistics() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/Update/statistics`);
            if (response.ok) {
                const stats = await response.json();
                const availableUpdates = stats.availableCount || 0;
                document.getElementById('update-devices').textContent = availableUpdates;
            } else {
                // Fallback to count devices with available updates
                const updatesResponse = await fetch(`${this.apiBaseUrl}/api/Update/available`);
                if (updatesResponse.ok) {
                    const updates = await updatesResponse.json();
                    const deviceIds = new Set(updates.map(u => u.deviceId));
                    document.getElementById('update-devices').textContent = deviceIds.size;
                } else {
                    document.getElementById('update-devices').textContent = '0';
                }
            }
        } catch (error) {
            console.warn('Could not load update statistics:', error);
            document.getElementById('update-devices').textContent = '0';
        }
    }

    // Filter devices based on search and filters
    filterDevices() {
        const searchTerm = document.getElementById('search-devices').value.toLowerCase();
        const statusFilter = document.getElementById('filter-status').value;
        const typeFilter = document.getElementById('filter-type').value;
        const discoveryFilter = document.getElementById('filter-discovery').value;

        this.filteredDevices = this.devices.filter(device => {
            // Search filter
            const matchesSearch = !searchTerm ||
                device.name?.toLowerCase().includes(searchTerm) ||
                device.ipAddress?.toLowerCase().includes(searchTerm) ||
                device.macAddress?.toLowerCase().includes(searchTerm) ||
                device.model?.toLowerCase().includes(searchTerm) ||
                device.location?.toLowerCase().includes(searchTerm);

            // Status filter - use computed status for consistency with display
            const computedStatus = this.getComputedStatus(device);
            const matchesStatus = !statusFilter || computedStatus.toString() === statusFilter;

            // Type filter
            const matchesType = !typeFilter || device.deviceType.toString() === typeFilter;

            // Discovery method filter
            const matchesDiscovery = !discoveryFilter || 
                (discoveryFilter === 'agent' && (device.agentInstalled || device.managementType === 1 || device.discoveryMethod === 2)) ||
                (discoveryFilter === 'network' && (!device.agentInstalled && (device.managementType === 2 || device.discoveryMethod === 1)));

            return matchesSearch && matchesStatus && matchesType && matchesDiscovery;
        });

        this.renderDevices();
    }

    // Render devices table
    renderDevices() {
        const tbody = document.getElementById('devices-table-body');
        const noDataDiv = document.getElementById('no-devices');

        if (this.filteredDevices.length === 0) {
            tbody.innerHTML = '';
            noDataDiv.classList.remove('d-none');
            return;
        }

        noDataDiv.classList.add('d-none');
        
        tbody.innerHTML = this.filteredDevices.map(device => `
            <tr onclick="window.location.href='/DeviceDetails?id=${device.id}';" style="cursor: pointer;">
                <td><span class="text-truncate-mobile">${device.name || 'Bilinmiyor'}</span></td>
                <td><span class="text-truncate-mobile">${device.ipAddress || 'Bilinmiyor'}</span></td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.macAddress || 'Bilinmiyor'}</span></td>
                <td>
                    <span class="badge ${this.getDeviceTypeBadgeClass(device.deviceType)}">
                        ${this.getDeviceTypeText(device.deviceType)}
                    </span>
                </td>
                <td>
                    <span class="badge ${this.getStatusBadgeClass(this.getComputedStatus(device))}">
                        ${this.getStatusText(this.getComputedStatus(device))}
                    </span>
                </td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.model || 'Bilinmiyor'}</span></td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${this.getLocationDisplay(device)}</span></td>
                <td class="hide-mobile">
                    <span class="badge ${this.getDiscoveryTypeBadgeClass(device)}">
                        ${this.getDiscoveryTypeText(device)}
                    </span>
                </td>
                <td onclick="event.stopPropagation();">
                    <div class="btn-group">
                        <button class="btn-sm btn-outline-primary" onclick="app.showDeviceModal('${device.id}')" title="Detayları Görüntüle">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button class="btn-sm btn-outline-secondary" onclick="app.editDevice('${device.id}')" title="Düzenle">
                            <i class="bi bi-pencil"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    // Get computed device status
    getComputedStatus(device) {
        if (!device.lastSeen) {
            return 1; // Offline - device has never been seen
        }
        
        const now = new Date();
        const twentyFourHoursAgo = new Date(now - 24 * 60 * 60 * 1000);
        const lastSeen = new Date(device.lastSeen);
        
        if (lastSeen < twentyFourHoursAgo) {
            return 1; // Offline - haven't seen device in 24 hours
        }
        
        return device.status; // Return the original status if recently seen
    }

    // UI Helper methods
    showLoading() {
        document.getElementById('loading').classList.remove('d-none');
    }

    hideLoading() {
        document.getElementById('loading').classList.add('d-none');
    }

    showError(message) {
        document.getElementById('error-message').textContent = message;
        document.getElementById('error-alert').classList.remove('d-none');
    }

    hideError() {
        document.getElementById('error-alert').classList.add('d-none');
    }

    updateLastUpdateTime() {
        const now = new Date();
        const updateText = `Son güncelleme: ${this.formatDate(now)}`;

        // Update both desktop and mobile status indicators
        const desktopElement = document.getElementById('last-update');
        const mobileElement = document.getElementById('last-update-mobile');
        
        if (desktopElement) {
            desktopElement.textContent = updateText;
        }
        if (mobileElement) {
            mobileElement.textContent = updateText;
        }
    }

    // Formatting and display helper methods
    formatDate(dateString) {
        const date = new Date(dateString);
        
        // Convert UTC time to Turkey time (UTC+3)
        const options = {
            timeZone: 'Europe/Istanbul',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        };
        
        const formatter = new Intl.DateTimeFormat('tr-TR', options);
        return formatter.format(date);
    }

    // Floor mapping functionality
    getFloorFromIP(ipAddress) {
        if (!ipAddress) return null;
        
        const ipParts = ipAddress.split('.');
        if (ipParts.length !== 4) return null;
        
        // Check if it's in the building network range (192.168.x.x)
        if (ipParts[0] === '192' && ipParts[1] === '168') {
            const subnet = parseInt(ipParts[2]);
            
            switch (subnet) {
                case 100: return 'Zemin Kat';
                case 101: return '1. Kat';
                case 102: return '2. Kat';
                case 103: return '3. Kat';
                case 104: return '4. Kat';
                case 105: return '5. Kat';
                case 106: return '6. Kat';
                case 107: return '7. Kat';
                case 108: return '8. Kat';
                case 109: return '9. Kat';
                default: return null;
            }
        }
        
        return null;
    }

    // Enhanced location display with floor mapping
    getLocationDisplay(device) {
        const floor = this.getFloorFromIP(device.ipAddress);
        const location = device.location || '';
        
        if (floor && location) {
            return `${floor} - ${location}`;
        } else if (floor) {
            return floor;
        } else if (location) {
            return location;
        } else {
            return 'Bilinmiyor';
        }
    }

    // Device type helper methods
    getDeviceTypeBadgeClass(deviceType) {
        const classes = {
            0: 'type-unknown text-white',      // Unknown
            1: 'type-laptop text-white',       // Laptop  
            2: 'type-desktop text-white',      // Desktop
            3: 'type-server text-white',       // Server
            4: 'type-printer text-white',      // Printer
            5: 'type-scanner text-white',      // Scanner
            6: 'type-camera text-white',       // Camera
            7: 'type-phone text-white',        // IP Phone
            8: 'type-network text-white',      // Network Device
            9: 'type-router text-white',       // Router
            10: 'type-switch text-white',      // Switch
            11: 'type-ap text-white',          // Access Point
            12: 'type-storage text-white',     // Storage
            13: 'type-tablet text-white',      // Tablet
            14: 'type-mobile text-white',      // Smartphone
            15: 'type-tv text-white',          // Smart TV
            16: 'type-projector text-white',   // Projector/Display
            17: 'type-other text-white'        // Other
        };
        return classes[deviceType] || classes[0];
    }

    getDeviceTypeText(deviceType) {
        const types = {
            0: 'Bilinmiyor',
            1: 'Laptop',
            2: 'Masaüstü',
            3: 'Sunucu',
            4: 'Yazıcı',
            5: 'Tarayıcı',
            6: 'Kamera',
            7: 'IP Telefon',
            8: 'Ağ Cihazı',
            9: 'Router',
            10: 'Switch',
            11: 'Access Point',
            12: 'Depolama',
            13: 'Tablet',
            14: 'Akıllı Telefon',
            15: 'Akıllı TV',
            16: 'Projektör/Ekran',
            17: 'Diğer'
        };
        return types[deviceType] || 'Bilinmiyor';
    }

    getStatusBadgeClass(status) {
        const classes = {
            0: 'status-active text-white',      // Online (green)
            1: 'status-inactive text-white',    // Offline (red)
            2: 'status-maintenance text-dark',  // Maintenance (yellow)
            3: 'status-broken text-white'       // Broken (dark red)
        };
        return classes[status] || classes[1];
    }

    getStatusText(status) {
        const statuses = {
            0: 'Çevrimiçi', // Online
            1: 'Çevrimdışı', // Offline
            2: 'Bakım',     // Maintenance
            3: 'Arızalı'    // Broken
        };
        return statuses[status] || 'Bilinmiyor';
    }

    getDiscoveryTypeBadgeClass(device) {
        if (device.agentInstalled || device.managementType === 1 || device.discoveryMethod === 2) {
            return 'badge-success'; // Green for agent-installed devices
        } else if (device.managementType === 2 || device.discoveryMethod === 1 || device.discoveryMethod === 3) {
            return 'badge-info'; // Blue for network discovered devices (including manual network discovery)
        } else {
            return 'badge-secondary'; // Gray for unknown
        }
    }

    getDiscoveryTypeText(device) {
        if (device.agentInstalled || device.managementType === 1 || device.discoveryMethod === 2) {
            return 'Ajan';
        } else if (device.managementType === 2 || device.discoveryMethod === 1) {
            return 'Ağ Keşfi';
        } else if (device.discoveryMethod === 3) {
            return 'Manuel';
        } else {
            return 'Bilinmiyor';
        }
    }

    // Modal functionality
    showDeviceModal(deviceId) {
        const device = this.devices.find(d => d.id === deviceId);
        if (!device) {
            this.showError('Cihaz bulunamadı');
            return;
        }

        // Redirect to device details page
        window.location.href = `/DeviceDetails?id=${deviceId}`;
    }

    // Device edit functionality would be here...
    // For now, let's redirect to devices page since this is getting complex

    // Refresh functionality
    refreshDevices() {
        this.loadDevices();
    }

    // Clear filters
    clearFilters() {
        document.getElementById('search-devices').value = '';
        document.getElementById('filter-status').value = '';
        document.getElementById('filter-type').value = '';
        document.getElementById('filter-discovery').value = '';
        this.filterDevices();
    }

    // Mock data for development
    getMockDevices() {
        return [
            {
                id: '1',
                name: 'DESKTOP-TEST01',
                ipAddress: '192.168.101.100',
                macAddress: '00:11:22:33:44:55',
                deviceType: 2, // Desktop
                status: 0, // Active
                model: 'Dell OptiPlex 7090',
                location: 'IT Departmanı',
                lastSeen: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
                managementType: 1,
                discoveryMethod: 2,
                agentInstalled: true
            },
            {
                id: '2',
                name: 'LAPTOP-SALES05',
                ipAddress: '192.168.102.150',
                macAddress: '00:AA:BB:CC:DD:EE',
                deviceType: 1, // Laptop
                status: 1, // Inactive
                model: 'HP EliteBook 840',
                location: 'Satış Departmanı',
                lastSeen: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
                managementType: 1,
                discoveryMethod: 2,
                agentInstalled: false
            }
        ];
    }

    // Add device functionality - simplified for now
    showAddDeviceModal() {
        // Redirect to a dedicated add device page or implement modal here
        alert('Boş cihaz ekleme özelliği yakında eklenecek!');
    }

    // Update device statuses
    async updateDeviceStatuses() {
        try {
            const response = await this.apiCall('device/update-statuses', {
                method: 'POST'
            });
            
            if (response.success && response.updatedCount > 0) {
                console.log(`Updated ${response.updatedCount} device statuses`);
                this.loadDevices(false); // Silent refresh
            }
        } catch (error) {
            console.warn('Failed to update device statuses:', error);
        }
    }
}

// Global functions for compatibility
function hideError() {
    if (app) app.hideError();
}

function refreshDevices() {
    if (app) app.refreshDevices();
}

function clearFilters() {
    if (app) app.clearFilters();
}

function openApiDocumentation() {
    const apiUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
    window.open(apiUrl, '_blank');
}

// Initialize the app when the page loads
let app;
document.addEventListener('DOMContentLoaded', function() {
    app = new DevicesPageApp();
});