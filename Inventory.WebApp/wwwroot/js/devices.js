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

            // Handle empty responses (like 204 No Content from PUT/DELETE requests)
            const contentLength = response.headers.get('content-length');
            if (contentLength === '0' || response.status === 204) {
                return { success: true };
            }

            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }

            // Return success for non-JSON responses
            return { success: true };
        } catch (error) {
            console.error('API call failed:', error);
            throw error;
        }
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
        const device = this.devices.find(d => d.id == deviceId);
        if (!device) {
            this.showError('Cihaz bulunamadı');
            return;
        }

        // Store device data in session storage for device details page
        sessionStorage.setItem('selectedDevice', JSON.stringify(device));
        
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

    // Mock data for development with comprehensive hardware/software info
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
                barcodeNumber: 'IT-DESK-001',
                notes: 'Ana geliştirme bilgisayarı. Özel yazılım yüklü.',
                lastSeen: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
                createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days ago
                managementType: 1,
                discoveryMethod: 2,
                agentInstalled: true,
                hardwareInfo: {
                    cpu: 'Intel Core i7-11700',
                    cpuCores: 8,
                    cpuClockMHz: 2900,
                    ramGB: 32,
                    ramModules: [
                        { slot: 'DIMM1', capacityGB: 16, manufacturer: 'Samsung', speedMHz: 3200 },
                        { slot: 'DIMM2', capacityGB: 16, manufacturer: 'Samsung', speedMHz: 3200 }
                    ],
                    diskGB: 1024,
                    disks: [
                        { deviceId: 'C:', totalGB: 512, freeGB: 256 },
                        { deviceId: 'D:', totalGB: 512, freeGB: 400 }
                    ],
                    gpus: [
                        { name: 'Intel UHD Graphics 750', memoryGB: 1 },
                        { name: 'NVIDIA GeForce RTX 3060', memoryGB: 12 }
                    ],
                    networkAdapters: [
                        { description: 'Intel Ethernet Connection I219-LM', macAddress: '00:11:22:33:44:55', ipAddress: '192.168.101.100' },
                        { description: 'Intel Wi-Fi 6 AX201', macAddress: '00:11:22:33:44:56', ipAddress: 'N/A' }
                    ],
                    motherboard: 'Dell Inc. 0K240Y',
                    motherboardSerial: 'CN123456789',
                    biosManufacturer: 'Dell Inc.',
                    biosVersion: '2.18.0'
                },
                softwareInfo: {
                    operatingSystem: 'Windows 11 Pro',
                    osVersion: '22H2',
                    osArchitecture: 'x64',
                    registeredUser: 'Ahmet Yılmaz',
                    activeUser: 'ayilmaz',
                    serialNumber: 'WIN11-PRO-12345',
                    users: ['ayilmaz', 'admin', 'guest', 'test_user', 'developer'],
                    installedApps: [
                        'Microsoft Office 365', 'Google Chrome', 'Mozilla Firefox', 'Adobe Acrobat Reader DC',
                        'Visual Studio 2022', 'Visual Studio Code', 'Git for Windows', 'Node.js', 'Python 3.11',
                        'Docker Desktop', 'Postman', 'FileZilla', 'WinRAR', '7-Zip', 'Notepad++',
                        'VLC Media Player', 'Adobe Photoshop 2023', 'Adobe Illustrator 2023', 'Figma',
                        'Zoom', 'Microsoft Teams', 'Slack', 'Discord', 'Spotify', 'Steam',
                        'AutoCAD 2023', 'SolidWorks 2023', 'MATLAB R2023a', 'IntelliJ IDEA',
                        'Eclipse IDE', 'Android Studio', 'Unity 3D', 'Blender', 'OBS Studio',
                        'Wireshark', 'PuTTY', 'WinSCP', 'TeamViewer', 'AnyDesk', 'VirtualBox',
                        'VMware Workstation', 'Hyper-V', 'SQL Server Management Studio', 'MySQL Workbench'
                    ]
                }
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
                barcodeNumber: 'SALES-LAP-005',
                notes: 'Satış temsilcisi laptop\'ı. Mobil kullanım için.',
                lastSeen: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
                createdAt: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000).toISOString(), // 15 days ago
                managementType: 1,
                discoveryMethod: 2,
                agentInstalled: false,
                hardwareInfo: {
                    cpu: 'Intel Core i5-1135G7',
                    cpuCores: 4,
                    cpuClockMHz: 2400,
                    ramGB: 16,
                    ramModules: [
                        { slot: 'SO-DIMM1', capacityGB: 16, manufacturer: 'Crucial', speedMHz: 3200 }
                    ],
                    diskGB: 512,
                    disks: [
                        { deviceId: 'C:', totalGB: 512, freeGB: 128 }
                    ],
                    gpus: [
                        { name: 'Intel Iris Xe Graphics', memoryGB: 1 }
                    ],
                    networkAdapters: [
                        { description: 'Intel Wi-Fi 6 AX201', macAddress: '00:AA:BB:CC:DD:EE', ipAddress: '192.168.102.150' }
                    ],
                    motherboard: 'HP 8846',
                    motherboardSerial: 'HP123456789',
                    biosManufacturer: 'HP',
                    biosVersion: 'U74 Ver. 01.15.00'
                },
                softwareInfo: {
                    operatingSystem: 'Windows 10 Pro',
                    osVersion: '22H2',
                    osArchitecture: 'x64',
                    registeredUser: 'Fatma Kaya',
                    activeUser: 'fkaya',
                    serialNumber: 'WIN10-PRO-67890',
                    users: ['fkaya', 'admin', 'guest'],
                    installedApps: [
                        'Microsoft Office 365', 'Google Chrome', 'Outlook', 'Excel', 'PowerPoint', 'Word',
                        'Microsoft Teams', 'Zoom', 'Adobe Acrobat Reader DC', 'Salesforce Desktop',
                        'CRM Software', 'QuickBooks', 'Slack', 'Skype for Business', 'OneDrive',
                        'Dropbox', 'VLC Media Player', 'WinRAR', 'TeamViewer', 'AnyDesk'
                    ]
                }
            },
            {
                id: '3',
                name: 'SERVER-DB01',
                ipAddress: '192.168.100.200',
                macAddress: '00:FF:EE:DD:CC:BB',
                deviceType: 3, // Server
                status: 0, // Active
                model: 'Dell PowerEdge R740',
                location: 'Sunucu Odası',
                barcodeNumber: 'SRV-DB-001',
                notes: 'Ana veritabanı sunucusu. Kritik sistem.',
                lastSeen: new Date(Date.now() - 30 * 1000).toISOString(), // 30 seconds ago
                createdAt: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString(), // 90 days ago
                managementType: 2,
                discoveryMethod: 1,
                agentInstalled: false,
                hardwareInfo: {
                    cpu: 'Intel Xeon Silver 4214R',
                    cpuCores: 24,
                    cpuClockMHz: 2400,
                    ramGB: 128,
                    ramModules: [
                        { slot: 'DIMM1', capacityGB: 32, manufacturer: 'Samsung', speedMHz: 2933 },
                        { slot: 'DIMM2', capacityGB: 32, manufacturer: 'Samsung', speedMHz: 2933 },
                        { slot: 'DIMM3', capacityGB: 32, manufacturer: 'Samsung', speedMHz: 2933 },
                        { slot: 'DIMM4', capacityGB: 32, manufacturer: 'Samsung', speedMHz: 2933 }
                    ],
                    diskGB: 4000,
                    disks: [
                        { deviceId: 'C:', totalGB: 500, freeGB: 200 },
                        { deviceId: 'D:', totalGB: 1750, freeGB: 800 },
                        { deviceId: 'E:', totalGB: 1750, freeGB: 1500 }
                    ],
                    networkAdapters: [
                        { description: 'Intel Ethernet Server Adapter I350-T4', macAddress: '00:FF:EE:DD:CC:BB', ipAddress: '192.168.100.200' },
                        { description: 'Intel Ethernet Server Adapter I350-T4 #2', macAddress: '00:FF:EE:DD:CC:BC', ipAddress: '192.168.100.201' }
                    ],
                    motherboard: 'Dell Inc. 0C4Y3R',
                    motherboardSerial: 'SRV123456789',
                    biosManufacturer: 'Dell Inc.',
                    biosVersion: '2.15.0'
                },
                softwareInfo: {
                    operatingSystem: 'Windows Server 2022',
                    osVersion: 'Standard',
                    osArchitecture: 'x64',
                    registeredUser: 'System Administrator',
                    activeUser: 'SYSTEM',
                    serialNumber: 'WINSRV-2022-001',
                    users: ['Administrator', 'SYSTEM', 'NETWORK SERVICE', 'LOCAL SERVICE', 'srvadmin'],
                    installedApps: [
                        'Microsoft SQL Server 2022', 'SQL Server Management Studio', 'IIS 10.0',
                        'Microsoft .NET Framework 4.8', '.NET 6.0 Runtime', '.NET 7.0 Runtime',
                        'Windows PowerShell 5.1', 'PowerShell 7', 'Remote Desktop Services',
                        'Hyper-V', 'Windows Server Backup', 'System Center Operations Manager Agent',
                        'Microsoft Defender Antivirus', 'Windows Admin Center', 'Failover Clustering'
                    ]
                }
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

    // Device edit functionality
    editDevice(deviceId) {
        const device = this.devices.find(d => d.id === deviceId);
        if (!device) {
            this.showError('Cihaz bulunamadı');
            return;
        }

        this.currentEditDevice = { ...device }; // Clone the device for editing
        this.showEditModal(device);
    }

    showEditModal(device) {
        const editContent = document.getElementById('device-edit-content');
        editContent.innerHTML = `
            <div class="device-edit-form">
                <div class="form-row">
                    <div class="form-group">
                        <label for="edit-device-name">Cihaz Adı:</label>
                        <input type="text" id="edit-device-name" class="form-control" value="${device.name || ''}" maxlength="200">
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
                        <label for="edit-device-model">Model:</label>
                        <input type="text" id="edit-device-model" class="form-control" value="${device.model || ''}" maxlength="200">
                    </div>
                    <div class="form-group">
                        <label for="edit-device-location">Konum:</label>
                        <input type="text" id="edit-device-location" class="form-control" value="${device.location || ''}" maxlength="200">
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label for="edit-device-barcode">Barkod Numarası:</label>
                        <input type="text" id="edit-device-barcode" class="form-control" value="${device.barcodeNumber || ''}" maxlength="100" placeholder="Manuel olarak girilecek">
                    </div>
                    <div class="form-group">
                        <label for="edit-device-status">Durum:</label>
                        <select id="edit-device-status" class="form-control">
                            <option value="0" ${device.status === 0 ? 'selected' : ''}>Aktif</option>
                            <option value="1" ${device.status === 1 ? 'selected' : ''}>Pasif</option>
                            <option value="2" ${device.status === 2 ? 'selected' : ''}>Bakım</option>
                            <option value="3" ${device.status === 3 ? 'selected' : ''}>Arızalı</option>
                        </select>
                    </div>
                </div>
                <div class="form-group">
                    <label for="edit-device-notes">Notlar:</label>
                    <textarea id="edit-device-notes" class="form-control" rows="3" maxlength="1000" placeholder="Cihaz hakkında notlar...">${device.notes || ''}</textarea>
                </div>
                <div class="form-info">
                    <small class="text-muted">
                        <i class="bi bi-info-circle"></i>
                        Barkod numarası ve notlar sadece manuel olarak değiştirilebilir ve otomatik işlemlerden etkilenmez.
                    </small>
                </div>
            </div>
        `;

        const modal = document.getElementById('deviceEditModal');
        modal.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    closeEditModal() {
        const modal = document.getElementById('deviceEditModal');
        modal.classList.remove('show');
        document.body.style.overflow = 'auto';
        this.currentEditDevice = null;
    }

    async saveDeviceChanges() {
        if (!this.currentEditDevice) {
            this.showError('Düzenlenecek cihaz bulunamadı');
            return;
        }

        try {
            // Get form values
            const name = document.getElementById('edit-device-name').value.trim();
            const deviceType = parseInt(document.getElementById('edit-device-type').value);
            const model = document.getElementById('edit-device-model').value.trim();
            const location = document.getElementById('edit-device-location').value.trim();
            const barcodeNumber = document.getElementById('edit-device-barcode').value.trim();
            const status = parseInt(document.getElementById('edit-device-status').value);
            const notes = document.getElementById('edit-device-notes').value.trim();

            // Prepare update data
            const updateData = {
                id: this.currentEditDevice.id,
                name: name || null,
                deviceType: deviceType,
                model: model || null,
                location: location || null,
                barcodeNumber: barcodeNumber || null,
                status: status,
                notes: notes || null
            };

            // Call API to update device
            const response = await this.apiCall(`device/${this.currentEditDevice.id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updateData)
            });

            if (response.success !== false) {
                this.showSuccess('Cihaz başarıyla güncellendi');
                this.closeEditModal();
                
                // Update local device data
                const deviceIndex = this.devices.findIndex(d => d.id === this.currentEditDevice.id);
                if (deviceIndex !== -1) {
                    this.devices[deviceIndex] = { ...this.devices[deviceIndex], ...updateData };
                    this.filterDevices(); // Refresh the table
                }
            } else {
                throw new Error(response.message || 'Cihaz güncellenirken hata oluştu');
            }

        } catch (error) {
            this.showError('Cihaz güncellenirken hata oluştu: ' + error.message);
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