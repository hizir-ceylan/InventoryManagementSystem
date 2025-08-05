// Çaykur Envanter Yönetim Sistemi - Tea Theme
class InventoryApp {
    constructor() {
        this.apiBaseUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
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
        }, window.INVENTORY_CONFIG?.AUTO_REFRESH_INTERVAL || 30000);
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
        try {
            await this.loadDevices();
        } catch (error) {
            console.log('Initial data load failed, using mock data');
            this.loadMockDevices();
            this.renderDevices();
        }
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

    // Load devices from API or use mock data
    async loadDevices(showLoading = true) {
        try {
            if (showLoading) {
                this.showLoading();
            }
            this.hideError();

            // Try to load from API first
            try {
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
                
            } catch (apiError) {
                // If API fails, use mock data for demo purposes
                console.log('API not available, using mock data for demo');
                this.loadMockDevices();
            }
            
            // Render devices
            this.renderDevices();
            
            if (showLoading) {
                this.hideLoading();
            }
            
            this.updateLastUpdateTime();

        } catch (error) {
            // If all else fails, load mock data
            console.log('Loading mock data as fallback');
            this.loadMockDevices();
            this.renderDevices();
            this.hideLoading();
            this.updateLastUpdateTime();
        }
    }

    // Load mock devices for demo purposes
    loadMockDevices() {
        this.devices = [
            {
                id: 1,
                name: 'DEV-LAPTOP-001',
                ipAddress: '192.168.1.100',
                macAddress: '00:1B:44:11:3A:B7',
                deviceType: 1, // Laptop
                status: 0,
                model: 'Dell XPS 13',
                location: 'IT Departmanı',
                managementType: 1,
                discoveryMethod: 4,
                firstSeen: '2024-01-15T09:00:00Z',
                lastSeen: '2024-08-04T12:00:00Z',
                createdAt: '2024-01-15T09:00:00Z',
                updatedAt: '2024-08-04T12:00:00Z',
                hardwareInfo: [
                    { componentType: 'CPU', componentName: 'Intel Core i7-11370H' },
                    { componentType: 'RAM', componentName: '16 GB DDR4' },
                    { componentType: 'Storage', componentName: '512 GB SSD' }
                ],
                softwareInfo: [
                    { name: 'Windows 11 Pro', version: '22H2' },
                    { name: 'Google Chrome', version: '119.0.6045.105' },
                    { name: 'Microsoft Office', version: '2021' }
                ]
            },
            {
                id: 2,
                name: 'SRV-DATABASE-01',
                ipAddress: '192.168.1.10',
                macAddress: '00:1B:44:11:3A:C8',
                deviceType: 3, // Server
                status: 0,
                model: 'Dell PowerEdge R740',
                location: 'Server Odası',
                managementType: 1,
                discoveryMethod: 4,
                firstSeen: '2024-01-01T00:00:00Z',
                lastSeen: '2024-08-04T12:00:00Z',
                createdAt: '2024-01-01T00:00:00Z',
                updatedAt: '2024-08-04T12:00:00Z',
                hardwareInfo: [
                    { componentType: 'CPU', componentName: 'Intel Xeon Silver 4214' },
                    { componentType: 'RAM', componentName: '64 GB DDR4' },
                    { componentType: 'Storage', componentName: '2 TB HDD RAID' }
                ],
                softwareInfo: [
                    { name: 'Windows Server 2019', version: 'Standard' },
                    { name: 'SQL Server 2019', version: 'Enterprise' }
                ]
            },
            {
                id: 3,
                name: 'WRK-PC-005',
                ipAddress: '192.168.1.105',
                macAddress: '00:1B:44:11:3A:D9',
                deviceType: 2, // Desktop
                status: 0,
                model: 'HP EliteDesk 800',
                location: 'Muhasebe',
                managementType: 0,
                discoveryMethod: 1,
                firstSeen: '2024-02-10T08:30:00Z',
                lastSeen: '2024-08-04T11:45:00Z',
                createdAt: '2024-02-10T08:30:00Z',
                updatedAt: '2024-08-04T11:45:00Z',
                hardwareInfo: [
                    { componentType: 'CPU', componentName: 'Intel Core i5-10500' },
                    { componentType: 'RAM', componentName: '8 GB DDR4' },
                    { componentType: 'Storage', componentName: '256 GB SSD' }
                ],
                softwareInfo: [
                    { name: 'Windows 10 Pro', version: '21H2' },
                    { name: 'Microsoft Office', version: '2019' }
                ]
            },
            {
                id: 4,
                name: 'PRINT-HP-001',
                ipAddress: '192.168.1.200',
                macAddress: '00:1B:44:11:3A:EA',
                deviceType: 4, // Printer
                status: 0,
                model: 'HP LaserJet Pro M404dn',
                location: 'Genel Ofis',
                managementType: 0,
                discoveryMethod: 2,
                firstSeen: '2024-03-01T10:00:00Z',
                lastSeen: '2024-08-04T12:00:00Z',
                createdAt: '2024-03-01T10:00:00Z',
                updatedAt: '2024-08-04T12:00:00Z',
                hardwareInfo: [],
                softwareInfo: []
            }
        ];
        
        this.filteredDevices = [...this.devices];
        
        // Update statistics with mock data
        this.updateStatistics(this.devices, [this.devices[0], this.devices[1]], [this.devices[2], this.devices[3]]);
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
                <td class="hide-mobile"><small>${device.macAddress || 'N/A'}</small></td>
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
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.model || 'N/A'}</span></td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.location || 'N/A'}</span></td>
                <td class="hide-mobile">
                    <small class="text-muted">
                        ${device.lastSeen ? this.formatDate(device.lastSeen) : 'Bilinmiyor'}
                    </small>
                </td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-outline-primary btn-sm" onclick="app.showDeviceDetail('${device.id}')" title="Detay Görüntüle">
                            <i class="bi bi-eye"></i>
                            <span class="d-none d-md-inline">Detay</span>
                        </button>
                        <button class="btn btn-outline-success btn-sm" onclick="app.showDeviceDetailPage('${device.id}')" title="Detay Sayfasında Aç">
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
            
            // Try to get device details from API first, fallback to mock data
            let device;
            try {
                device = await this.apiCall(`device/${deviceId}`);
            } catch (apiError) {
                // Fallback to mock data if API is not available
                device = this.devices.find(d => d.id == deviceId);
                if (!device) {
                    throw new Error('Cihaz bulunamadı');
                }
            }
            
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
            
            // Try to get device details from API first, fallback to mock data
            let device;
            try {
                device = await this.apiCall(`device/${deviceId}`);
            } catch (apiError) {
                // Fallback to mock data if API is not available
                device = this.devices.find(d => d.id == deviceId);
                if (!device) {
                    throw new Error('Cihaz bulunamadı');
                }
            }
            
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
        
        // Find the nav link that corresponds to this page and make it active
        const navMapping = {
            'devices': 'Cihazlar',
            'device-details': 'Cihaz Detayları',
            'network-scan': 'Ağ Taraması',
            'change-logs': 'Değişiklik Logları'
        };
        
        if (navMapping[pageId]) {
            const navLinks = document.querySelectorAll('.nav-link');
            navLinks.forEach(link => {
                const linkText = link.querySelector('span');
                if (linkText && linkText.textContent.trim() === navMapping[pageId]) {
                    link.classList.add('active');
                }
            });
        }

        this.currentPage = pageId;

        // Load page specific data
        if (pageId === 'devices') {
            this.loadDevices();
        } else if (pageId === 'change-logs') {
            this.loadChangeLogs();
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
            0: 'bi-question-circle',  // Unknown
            1: 'bi-laptop',          // Laptop
            2: 'bi-pc-display',      // Desktop
            3: 'bi-server',          // Server
            4: 'bi-printer',         // Printer
            5: 'bi-scanner',         // Scanner
            6: 'bi-camera',          // Camera
            7: 'bi-telephone',       // IP Phone
            8: 'bi-router',          // Network Device
            9: 'bi-router',          // Router
            10: 'bi-diagram-3',      // Switch
            11: 'bi-wifi',           // Access Point
            12: 'bi-hdd',            // Storage
            13: 'bi-tablet',         // Tablet
            14: 'bi-phone',          // Smartphone
            15: 'bi-tv',             // Smart TV
            16: 'bi-projector',      // Projector/Display
            17: 'bi-gear'            // Other
        };
        return icons[deviceType] || icons[0];
    }

    getDeviceTypeBadgeClass(deviceType) {
        const classes = {
            0: 'type-unknown text-white',       // Unknown
            1: 'type-laptop text-white',        // Laptop
            2: 'type-pc text-white',           // Desktop
            3: 'type-server text-white',        // Server
            4: 'type-printer text-white',       // Printer
            5: 'type-scanner text-white',       // Scanner
            6: 'type-camera text-white',        // Camera
            7: 'type-phone text-white',         // IP Phone
            8: 'type-network text-white',       // Network Device
            9: 'type-router text-white',        // Router
            10: 'type-switch text-white',       // Switch
            11: 'type-wifi text-white',         // Access Point
            12: 'type-storage text-white',      // Storage
            13: 'type-tablet text-white',       // Tablet
            14: 'type-mobile text-white',       // Smartphone
            15: 'type-tv text-white',           // Smart TV
            16: 'type-projector text-white',    // Projector/Display
            17: 'type-other text-white'         // Other
        };
        return classes[deviceType] || classes[0];
    }

    getDeviceTypeText(deviceType) {
        const types = {
            0: 'Unknown',
            1: 'Laptop',
            2: 'Desktop',
            3: 'Server',
            4: 'Printer',
            5: 'Scanner',
            6: 'Camera',
            7: 'IP Phone',
            8: 'Network Device',
            9: 'Router',
            10: 'Switch',
            11: 'Access Point',
            12: 'Storage',
            13: 'Tablet',
            14: 'Smartphone',
            15: 'Smart TV',
            16: 'Projector/Display',
            17: 'Other'
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

    // Network Scan functionality
    async startNetworkScan() {
        const networkRange = document.getElementById('network-range').value;
        const timeout = document.getElementById('scan-timeout').value;
        const portScan = document.getElementById('port-scan').value;
        
        const scanResults = document.getElementById('scan-results');
        const scanProgress = document.getElementById('scan-progress');
        const resultsTable = document.getElementById('results-table');
        const progressFill = document.getElementById('progress-fill');
        const scanStatus = document.getElementById('scan-status');
        const startBtn = document.getElementById('start-scan-btn');
        
        // Show results section
        scanResults.style.display = 'block';
        resultsTable.style.display = 'none';
        startBtn.disabled = true;
        startBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Taranıyor...';
        
        // Simulate network scan
        const mockResults = [
            { ip: '192.168.1.1', mac: '00:14:22:01:23:45', name: 'Router', status: 'Online', ports: '22,80,443' },
            { ip: '192.168.1.10', mac: '00:1B:44:11:3A:C8', name: 'SRV-DATABASE-01', status: 'Online', ports: '22,1433,3389' },
            { ip: '192.168.1.100', mac: '00:1B:44:11:3A:B7', name: 'DEV-LAPTOP-001', status: 'Online', ports: '135,445' },
            { ip: '192.168.1.105', mac: '00:1B:44:11:3A:D9', name: 'WRK-PC-005', status: 'Online', ports: '135,445' },
            { ip: '192.168.1.200', mac: '00:1B:44:11:3A:EA', name: 'PRINT-HP-001', status: 'Online', ports: '9100,80' }
        ];
        
        let progress = 0;
        const totalSteps = 254; // Simulating scanning 254 IPs
        
        const scanInterval = setInterval(() => {
            progress += Math.random() * 10;
            if (progress > 100) progress = 100;
            
            progressFill.style.width = progress + '%';
            scanStatus.textContent = `Tarama devam ediyor... ${Math.round(progress)}%`;
            
            if (progress >= 100) {
                clearInterval(scanInterval);
                this.displayScanResults(mockResults);
                scanStatus.textContent = 'Tarama tamamlandı!';
                resultsTable.style.display = 'block';
                startBtn.disabled = false;
                startBtn.innerHTML = '<i class="bi bi-play-fill"></i> Taramayı Başlat';
            }
        }, 100);
    }
    
    displayScanResults(results) {
        const tbody = document.getElementById('scan-results-body');
        tbody.innerHTML = results.map(result => `
            <tr>
                <td>${result.ip}</td>
                <td>${result.mac}</td>
                <td>${result.name}</td>
                <td><span class="badge status-active">${result.status}</span></td>
                <td><small>${result.ports}</small></td>
                <td>
                    <button class="btn btn-sm btn-outline-primary" onclick="app.addToInventory('${result.ip}', '${result.mac}', '${result.name}')">
                        <i class="bi bi-plus-circle"></i> Envantere Ekle
                    </button>
                </td>
            </tr>
        `).join('');
    }
    
    async addToInventory(ip, mac, name) {
        alert(`${name} (${ip}) envantere eklendi!`);
        // Here you would make an API call to add the device
    }

    // Change Logs functionality
    async loadChangeLogs() {
        // Mock change logs data
        const mockChangeLogs = [
            {
                id: 1,
                deviceName: 'DEV-LAPTOP-001',
                changeDate: '2024-08-04T11:30:00Z',
                changeType: 'Location',
                oldValue: 'Geliştirme',
                newValue: 'IT Departmanı',
                changedBy: 'Agent'
            },
            {
                id: 2,
                deviceName: 'SRV-DATABASE-01',
                changeDate: '2024-08-04T10:15:00Z',
                changeType: 'Status',
                oldValue: 'Bakım',
                newValue: 'Aktif',
                changedBy: 'Admin'
            },
            {
                id: 3,
                deviceName: 'WRK-PC-005',
                changeDate: '2024-08-04T09:45:00Z',
                changeType: 'RAM',
                oldValue: '4 GB',
                newValue: '8 GB',
                changedBy: 'Agent'
            }
        ];
        
        this.changeLogs = mockChangeLogs;
        this.renderChangeLogs();
    }
    
    renderChangeLogs() {
        const tbody = document.getElementById('change-logs-body');
        const noDataDiv = document.getElementById('no-change-logs');
        
        if (!this.changeLogs || this.changeLogs.length === 0) {
            tbody.innerHTML = '';
            noDataDiv.classList.remove('d-none');
            return;
        }
        
        noDataDiv.classList.add('d-none');
        tbody.innerHTML = this.changeLogs.map(log => `
            <tr>
                <td>${this.formatDate(log.changeDate)}</td>
                <td>${log.deviceName}</td>
                <td><span class="badge type-unknown">${log.changeType}</span></td>
                <td>${log.oldValue}</td>
                <td>${log.newValue}</td>
                <td>${log.changedBy}</td>
            </tr>
        `).join('');
    }
    
    refreshChangeLogs() {
        this.loadChangeLogs();
    }
    
    clearChangeLogFilters() {
        document.getElementById('filter-change-type').value = '';
        document.getElementById('filter-device').value = '';
        document.getElementById('filter-date-from').value = '';
        document.getElementById('filter-date-to').value = '';
        this.renderChangeLogs();
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

function openApiDocumentation() {
    const apiUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
    // Swagger UI is now served at the root of the API server
    window.open(apiUrl, '_blank');
}

// Initialize the application when the page loads
let app;
document.addEventListener('DOMContentLoaded', function() {
    app = new InventoryApp();
});