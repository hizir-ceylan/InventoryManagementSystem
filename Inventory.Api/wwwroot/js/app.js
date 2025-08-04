// Çaykur Envanter Yönetim Sistemi - Tea Theme
class InventoryApp {
    constructor() {
        this.apiBaseUrl = '';
        this.devices = [];
        this.filteredDevices = [];
        this.currentPage = 'devices';
        
        this.init();
    }

    // Initialize the application
    init() {
        this.setupEventListeners();
        this.loadInitialData();
        this.setupMobileMenu();
        
        // Set up auto-refresh every 30 seconds
        setInterval(() => {
            if (this.currentPage === 'devices') {
                this.loadDevices(false); // Silent refresh
            }
        }, 30000);
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
    }

    // Load initial data
    async loadInitialData() {
        await this.loadDevices();
        this.updateLastUpdateTime();
    }

    // Show loading indicator
    showLoading() {
        document.getElementById('loading').classList.remove('d-none');
    }

    // Hide loading indicator
    hideLoading() {
        document.getElementById('loading').classList.add('d-none');
    }

    // Show error message
    showError(message) {
        document.getElementById('error-message').textContent = message;
        document.getElementById('error-alert').classList.remove('d-none');
        this.hideLoading();
    }

    // Hide error message
    hideError() {
        document.getElementById('error-alert').classList.add('d-none');
    }

    // API call wrapper
    async apiCall(endpoint, options = {}) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/${endpoint}`, {
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
        } catch (error) {
            console.error('API call failed:', error);
            throw error;
        }
    }

    // Load devices from API
    async loadDevices(showLoading = true) {
        try {
            if (showLoading) {
                this.showLoading();
            }
            this.hideError();

            // Load all devices
            const [allDevices, agentDevices, networkDevices] = await Promise.all([
                this.apiCall('device'),
                this.apiCall('device/agent-installed'),
                this.apiCall('device/network-discovered')
            ]);

            this.devices = allDevices || [];
            this.filteredDevices = [...this.devices];

            // Update statistics
            this.updateStatistics(allDevices, agentDevices, networkDevices);
            
            // Render devices
            this.renderDevices();
            
            if (showLoading) {
                this.hideLoading();
            }
            
            this.updateLastUpdateTime();

        } catch (error) {
            this.showError('Cihazlar yüklenirken hata oluştu: ' + error.message);
        }
    }

    // Update statistics cards
    updateStatistics(allDevices, agentDevices, networkDevices) {
        const totalDevices = allDevices?.length || 0;
        const activeDevices = allDevices?.filter(d => d.status === 0).length || 0;
        const agentDevicesCount = agentDevices?.length || 0;
        const networkDevicesCount = networkDevices?.length || 0;

        document.getElementById('total-devices').textContent = totalDevices;
        document.getElementById('active-devices').textContent = activeDevices;
        document.getElementById('agent-devices').textContent = agentDevicesCount;
        document.getElementById('network-devices').textContent = networkDevicesCount;
    }

    // Filter devices based on search and filters
    filterDevices() {
        const searchTerm = document.getElementById('search-devices').value.toLowerCase();
        const statusFilter = document.getElementById('filter-status').value;
        const typeFilter = document.getElementById('filter-type').value;

        this.filteredDevices = this.devices.filter(device => {
            // Search filter
            const matchesSearch = !searchTerm || 
                device.name?.toLowerCase().includes(searchTerm) ||
                device.ipAddress?.toLowerCase().includes(searchTerm) ||
                device.macAddress?.toLowerCase().includes(searchTerm) ||
                device.model?.toLowerCase().includes(searchTerm) ||
                device.location?.toLowerCase().includes(searchTerm);

            // Status filter
            const matchesStatus = !statusFilter || device.status.toString() === statusFilter;

            // Type filter
            const matchesType = !typeFilter || device.deviceType.toString() === typeFilter;

            return matchesSearch && matchesStatus && matchesType;
        });

        this.renderDevices();
    }

    // Clear all filters
    clearFilters() {
        document.getElementById('search-devices').value = '';
        document.getElementById('filter-status').value = '';
        document.getElementById('filter-type').value = '';
        this.filterDevices();
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
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <i class="bi ${this.getDeviceIcon(device.deviceType)} me-2"></i>
                        <span class="text-truncate-mobile">${device.name || 'N/A'}</span>
                    </div>
                </td>
                <td><span class="text-truncate-mobile">${device.ipAddress || 'N/A'}</span></td>
                <td class="d-none-mobile"><small>${device.macAddress || 'N/A'}</small></td>
                <td>
                    <span class="badge ${this.getDeviceTypeBadgeClass(device.deviceType)}">
                        ${this.getDeviceTypeText(device.deviceType)}
                    </span>
                </td>
                <td>
                    <span class="badge ${this.getStatusBadgeClass(device.status)}">
                        ${this.getStatusText(device.status)}
                    </span>
                </td>
                <td class="d-none-mobile"><span class="text-truncate-mobile">${device.model || 'N/A'}</span></td>
                <td class="d-none-mobile"><span class="text-truncate-mobile">${device.location || 'N/A'}</span></td>
                <td class="d-none-mobile">
                    <small class="text-muted">
                        ${device.lastSeen ? this.formatDate(device.lastSeen) : 'Bilinmiyor'}
                    </small>
                </td>
                <td>
                    <div class="btn-group">
                        <button class="btn btn-outline-primary btn-sm" onclick="app.showDeviceDetail(${device.id})" title="Detay Görüntüle">
                            <i class="bi bi-eye"></i>
                            <span class="d-none d-md-inline">Detay</span>
                        </button>
                        <button class="btn btn-outline-success btn-sm" onclick="app.showDeviceDetailPage(${device.id})" title="Detay Sayfasında Aç">
                            <i class="bi bi-box-arrow-up-right"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    // Show device detail modal
    async showDeviceDetail(deviceId) {
        try {
            this.showLoading();
            
            // Get device details with related data
            const device = await this.apiCall(`device/${deviceId}`);
            
            const modalContent = document.getElementById('device-detail-content');
            modalContent.innerHTML = this.renderDeviceDetail(device);
            
            // Show modal
            this.showModal();
            
            this.hideLoading();
        } catch (error) {
            this.showError('Cihaz detayları yüklenirken hata oluştu: ' + error.message);
        }
    }

    // Show modal
    showModal() {
        const modal = document.getElementById('deviceDetailModal');
        modal.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    // Close modal
    closeModal() {
        const modal = document.getElementById('deviceDetailModal');
        modal.classList.remove('show');
        document.body.style.overflow = 'auto';
    }

    // Show device detail in dedicated page
    async showDeviceDetailPage(deviceId) {
        try {
            this.showLoading();
            
            // Get device details with related data
            const device = await this.apiCall(`device/${deviceId}`);
            
            // Update device details page content
            const detailsContent = document.getElementById('device-details-content');
            detailsContent.innerHTML = `
                <div class="device-details-header">
                    <div class="device-header-info">
                        <div class="device-title">
                            <i class="bi ${this.getDeviceIcon(device.deviceType)}"></i>
                            <h3>${device.name || 'Bilinmeyen Cihaz'}</h3>
                            <span class="badge ${this.getDeviceTypeBadgeClass(device.deviceType)}">${this.getDeviceTypeText(device.deviceType)}</span>
                        </div>
                        <div class="device-status">
                            <span class="badge ${this.getStatusBadgeClass(device.status)}">${this.getStatusText(device.status)}</span>
                        </div>
                    </div>
                    <button class="btn-secondary" onclick="showPage('devices')">
                        <i class="bi bi-arrow-left"></i>
                        Cihazlar Listesine Dön
                    </button>
                </div>
                ${this.renderDeviceDetail(device)}
            `;
            
            // Show device details page
            this.showPage('device-details');
            
            this.hideLoading();
        } catch (error) {
            this.showError('Cihaz detayları yüklenirken hata oluştu: ' + error.message);
        }
    }

    // Render device detail content
    renderDeviceDetail(device) {
        return `
            <div class="device-info-group">
                <h6><i class="bi bi-info-circle"></i> Genel Bilgiler</h6>
                <div class="device-info-item">
                    <span class="device-info-label">Cihaz Adı:</span>
                    <span class="device-info-value">${device.name || 'N/A'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">IP Adresi:</span>
                    <span class="device-info-value">${device.ipAddress || 'N/A'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">MAC Adresi:</span>
                    <span class="device-info-value">${device.macAddress || 'N/A'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Cihaz Türü:</span>
                    <span class="device-info-value">
                        <span class="badge ${this.getDeviceTypeBadgeClass(device.deviceType)}">
                            ${this.getDeviceTypeText(device.deviceType)}
                        </span>
                    </span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Durum:</span>
                    <span class="device-info-value">
                        <span class="badge ${this.getStatusBadgeClass(device.status)}">
                            ${this.getStatusText(device.status)}
                        </span>
                    </span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Model:</span>
                    <span class="device-info-value">${device.model || 'N/A'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Konum:</span>
                    <span class="device-info-value">${device.location || 'N/A'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Yönetim Türü:</span>
                    <span class="device-info-value">${this.getManagementTypeText(device.managementType)}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Keşif Yöntemi:</span>
                    <span class="device-info-value">${this.getDiscoveryMethodText(device.discoveryMethod)}</span>
                </div>
            </div>

            <div class="device-info-group">
                <h6><i class="bi bi-calendar"></i> Zaman Bilgileri</h6>
                <div class="device-info-item">
                    <span class="device-info-label">İlk Görülme:</span>
                    <span class="device-info-value">${device.firstSeen ? this.formatDate(device.firstSeen) : 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Son Görülme:</span>
                    <span class="device-info-value">${device.lastSeen ? this.formatDate(device.lastSeen) : 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Oluşturulma:</span>
                    <span class="device-info-value">${device.createdAt ? this.formatDate(device.createdAt) : 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Son Güncelleme:</span>
                    <span class="device-info-value">${device.updatedAt ? this.formatDate(device.updatedAt) : 'Bilinmiyor'}</span>
                </div>
            </div>

            ${device.hardwareInfo && device.hardwareInfo.length > 0 ? `
                <div class="device-info-group">
                    <h6><i class="bi bi-cpu"></i> Donanım Bilgileri</h6>
                    ${device.hardwareInfo.map(hw => `
                        <div class="device-info-item">
                            <span class="device-info-label">${hw.componentType || 'Bilinmeyen'}:</span>
                            <span class="device-info-value">${hw.componentName || 'N/A'}</span>
                        </div>
                    `).join('')}
                </div>
            ` : ''}

            ${device.softwareInfo && device.softwareInfo.length > 0 ? `
                <div class="device-info-group">
                    <h6><i class="bi bi-software-123"></i> Yazılım Bilgileri</h6>
                    ${device.softwareInfo.slice(0, 10).map(sw => `
                        <div class="device-info-item">
                            <span class="device-info-label">${sw.name || 'Bilinmeyen'}:</span>
                            <span class="device-info-value">${sw.version || 'N/A'}</span>
                        </div>
                    `).join('')}
                    ${device.softwareInfo.length > 10 ? `<p class="text-muted small mt-2">ve ${device.softwareInfo.length - 10} yazılım daha...</p>` : ''}
                </div>
            ` : ''}
        `;
    }

    // Show different pages
    showPage(pageId) {
        // Hide all pages
        document.querySelectorAll('.page').forEach(page => {
            page.classList.remove('active');
            page.classList.add('d-none');
        });

        // Show selected page
        const targetPage = document.getElementById(pageId + '-page');
        if (targetPage) {
            targetPage.classList.remove('d-none');
            setTimeout(() => {
                targetPage.classList.add('active');
            }, 10);
        }

        // Update navigation
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });
        
        // Find the clicked nav link and make it active
        const navLinks = document.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            if (link.getAttribute('onclick') && link.getAttribute('onclick').includes(pageId)) {
                link.classList.add('active');
            }
        });

        this.currentPage = pageId;

        // Load page specific data
        if (pageId === 'devices') {
            this.loadDevices();
        }
    }

    // Refresh devices
    refreshDevices() {
        this.loadDevices();
    }

    // Update last update time
    updateLastUpdateTime() {
        const now = new Date();
        document.getElementById('last-update').textContent = `Son güncelleme: ${this.formatDate(now)}`;
    }

    // Utility functions
    getDeviceIcon(deviceType) {
        const icons = {
            0: 'bi-pc-display',      // PC
            1: 'bi-laptop',          // Laptop
            2: 'bi-server',          // Server
            3: 'bi-printer',         // Printer
            4: 'bi-router',          // Network Device
            5: 'bi-phone',           // Mobile Device
            6: 'bi-question-circle'  // Unknown
        };
        return icons[deviceType] || icons[6];
    }

    getDeviceTypeBadgeClass(deviceType) {
        const classes = {
            0: 'type-pc text-white',           // PC
            1: 'type-laptop text-white',       // Laptop
            2: 'type-server text-white',       // Server
            3: 'type-printer text-white',      // Printer
            4: 'type-network text-white',      // Network Device
            5: 'type-mobile text-white',       // Mobile Device
            6: 'type-unknown text-white'       // Unknown
        };
        return classes[deviceType] || classes[6];
    }

    getDeviceTypeText(deviceType) {
        const types = {
            0: 'PC',
            1: 'Laptop',
            2: 'Server',
            3: 'Printer',
            4: 'Network Device',
            5: 'Mobile Device',
            6: 'Unknown'
        };
        return types[deviceType] || 'Unknown';
    }

    getStatusBadgeClass(status) {
        const classes = {
            0: 'status-active text-white',      // Active
            1: 'status-inactive text-white',    // Inactive
            2: 'status-maintenance text-dark',  // Maintenance
            3: 'status-broken text-white'       // Broken
        };
        return classes[status] || classes[1];
    }

    getStatusText(status) {
        const statuses = {
            0: 'Aktif',
            1: 'Pasif',
            2: 'Bakım',
            3: 'Arızalı'
        };
        return statuses[status] || 'Bilinmiyor';
    }

    getManagementTypeText(managementType) {
        const types = {
            0: 'Yönetilmeyen',
            1: 'Agent Kurulu',
            2: 'Ağ Keşfi'
        };
        return types[managementType] || 'Bilinmiyor';
    }

    getDiscoveryMethodText(discoveryMethod) {
        const methods = {
            0: 'Bilinmiyor',
            1: 'Ping',
            2: 'Port Tarama',
            3: 'SNMP',
            4: 'Agent Kaydı'
        };
        return methods[discoveryMethod] || 'Bilinmiyor';
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR');
    }
}

// Global functions for HTML onclick events
function showPage(pageId) {
    app.showPage(pageId);
}

function hideError() {
    app.hideError();
}

function refreshDevices() {
    app.refreshDevices();
}

function clearFilters() {
    app.clearFilters();
}

function closeModal() {
    app.closeModal();
}

function showDeviceDetailPage(deviceId) {
    app.showDeviceDetailPage(deviceId);
}

// Initialize the application when the page loads
let app;
document.addEventListener('DOMContentLoaded', function() {
    app = new InventoryApp();
});