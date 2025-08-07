// Çaykur Envanter Yönetim Sistemi - Tea Theme
class InventoryApp {
    constructor() {
        this.apiBaseUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
        this.devices = [];
        this.filteredDevices = [];
        this.changeLogs = [];
        this.filteredChangeLogs = [];
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

        document.getElementById('filter-discovery').addEventListener('change', () => {
            this.filterDevices();
        });

        // Change logs filter event listeners
        document.getElementById('filter-change-type').addEventListener('change', () => {
            this.filterChangeLogs();
        });

        document.getElementById('filter-device').addEventListener('change', () => {
            this.filterChangeLogs();
        });

        document.getElementById('filter-date-from').addEventListener('change', () => {
            this.filterChangeLogs();
        });

        document.getElementById('filter-date-to').addEventListener('change', () => {
            this.filterChangeLogs();
        });
    }

    // Load initial data
    async loadInitialData() {
        try {
            await this.loadDevices();
        } catch (error) {
            console.warn('Initial data load failed, showing empty state:', error);
            this.devices = [];
            this.filteredDevices = [];
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
                // If API fails, use mock data for testing UI improvements
                console.warn('API not available, using mock data for testing:', apiError.message);
                this.devices = this.getMockDevices();
                this.filteredDevices = [...this.devices];
                this.updateStatistics(this.devices, this.devices.filter(d => d.managementType === 1), this.devices.filter(d => d.managementType === 2));
            }

            // Render devices
            this.renderDevices();

            if (showLoading) {
                this.hideLoading();
            }

            this.updateLastUpdateTime();

        } catch (error) {
            // If all else fails, show empty state
            console.warn('All data loading failed, showing empty state:', error);
            this.devices = [];
            this.filteredDevices = [];
            this.renderDevices();
            this.hideLoading();
            this.updateLastUpdateTime();
        }
    }

    // Update statistics cards
    updateStatistics(allDevices, agentDevices, networkDevices) {
        const totalDevices = allDevices?.length || 0;

        // Improved active device detection: include devices that are:
        // 1. Status 0 (Aktif) OR
        // 2. Recently seen (within last 24 hours) regardless of status OR  
        // 3. Status 2 (Bakım) - maintenance devices are often still working
        const now = new Date();
        const twentyFourHoursAgo = new Date(now - 24 * 60 * 60 * 1000);

        const activeDevices = allDevices?.filter(d => {
            // Always consider status 0 as active
            if (d.status === 0) return true;

            // Consider maintenance devices as active if recently seen
            if (d.status === 2 && d.lastSeen) {
                const lastSeen = new Date(d.lastSeen);
                return lastSeen > twentyFourHoursAgo;
            }

            // Consider any device as active if seen in last 24 hours, even if marked inactive
            if (d.lastSeen) {
                const lastSeen = new Date(d.lastSeen);
                return lastSeen > twentyFourHoursAgo;
            }

            return false;
        }).length || 0;

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
        const discoveryFilter = document.getElementById('filter-discovery').value;

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

            // Discovery method filter
            const matchesDiscovery = !discoveryFilter || 
                (discoveryFilter === 'agent' && (device.agentInstalled || device.managementType === 'Agent')) ||
                (discoveryFilter === 'network' && (!device.agentInstalled && (device.managementType === 'NetworkDiscovery' || device.discoveryMethod === 'NetworkDiscovery')));

            return matchesSearch && matchesStatus && matchesType && matchesDiscovery;
        });

        this.renderDevices();
    }

    // Clear all filters
    clearFilters() {
        document.getElementById('search-devices').value = '';
        document.getElementById('filter-status').value = '';
        document.getElementById('filter-type').value = '';
        document.getElementById('filter-discovery').value = '';
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
                <td><span class="text-truncate-mobile">${device.ipAddress || 'Bilinmiyor'}</span></td>
                <td class="hide-mobile"><small>${device.macAddress || 'Bilinmiyor'}</small></td>
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
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.location || 'Bilinmiyor'}</span></td>
                <td class="hide-mobile">
                    <span class="badge ${this.getDiscoveryTypeBadgeClass(device)}">
                        ${this.getDiscoveryTypeText(device)}
                    </span>
                </td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-primary btn-sm" onclick="app.showDeviceDetailPage('${device.id}')" title="Cihaz Detayına Git">
                            <i class="bi bi-info-circle"></i>
                            <span class="d-none d-lg-inline">Cihaz Detayı</span>
                        </button>
                        <button class="btn btn-secondary btn-sm" onclick="app.showDeviceLogs('${device.id}')" title="Cihaz Loglarına Git">
                            <i class="bi bi-clock-history"></i>
                            <span class="d-none d-lg-inline">Loglar</span>
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
                            <span class="badge ${this.getStatusBadgeClass(this.getComputedStatus(device))}">${this.getStatusText(this.getComputedStatus(device))}</span>
                        </div>
                    </div>
                    <div class="device-actions">
                        <button class="btn-danger" onclick="app.confirmDeleteDevice('${device.id}', '${device.name || 'Bilinmeyen Cihaz'}')">
                            <i class="bi bi-trash"></i>
                            Cihazı Sil
                        </button>
                        <button class="btn-secondary" onclick="showPage('devices')">
                            <i class="bi bi-arrow-left"></i>
                            Cihazlar Listesine Dön
                        </button>
                    </div>
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

    // Show device logs - navigate to change logs page with device filter
    async showDeviceLogs(deviceId) {
        try {
            // Get device info first to display in the filter
            let device = this.devices.find(d => d.id == deviceId);
            if (!device) {
                device = await this.apiCall(`device/${deviceId}`);
            }

            // Navigate to change logs page
            this.showPage('change-logs');

            // Set filter to show logs for this specific device
            const deviceFilter = document.getElementById('filter-device');
            if (deviceFilter) {
                // Add device to filter options if not already there
                const existingOption = Array.from(deviceFilter.options).find(option => option.value === deviceId);
                if (!existingOption) {
                    const option = new Option(device.name || 'Bilinmeyen Cihaz', deviceId);
                    deviceFilter.add(option);
                }
                deviceFilter.value = deviceId;

                // Trigger change logs refresh with filter
                this.refreshChangeLogs();
            }
        } catch (error) {
            this.showError('Cihaz logları yüklenirken hata oluştu: ' + error.message);
        }
    }

    // Render device detail content
    renderDeviceDetail(device) {
        return `
            <div class="device-info-group">
                <h6><i class="bi bi-info-circle"></i> Genel Bilgiler</h6>
                <div class="device-info-item">
                    <span class="device-info-label">Cihaz Adı:</span>
                    <span class="device-info-value">${device.name || 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">IP Adresi:</span>
                    <span class="device-info-value">${device.ipAddress || 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">MAC Adresi:</span>
                    <span class="device-info-value">${device.macAddress || 'Bilinmiyor'}</span>
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
                        <span class="badge ${this.getStatusBadgeClass(this.getComputedStatus(device))}">
                            ${this.getStatusText(this.getComputedStatus(device))}
                        </span>
                    </span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Model:</span>
                    <span class="device-info-value">${device.model || 'Bilinmiyor'}</span>
                </div>
                <div class="device-info-item">
                    <span class="device-info-label">Konum:</span>
                    <span class="device-info-value">${device.location || 'Bilinmiyor'}</span>
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

            ${this.renderHardwareInfo(device)}
            ${this.renderSoftwareInfo(device)}
        `;
    }

    // Render hardware information section
    renderHardwareInfo(device) {
        if (!device.hardwareInfo) {
            return `
                <div class="device-info-group">
                    <h6><i class="bi bi-cpu"></i> Donanım Bilgileri</h6>
                    <div class="device-info-item">
                        <span class="device-info-value text-muted">Bu cihaz için donanım bilgisi bulunmuyor</span>
                    </div>
                </div>
            `;
        }

        const hw = device.hardwareInfo;
        return `
            <div class="device-info-group">
                <h6><i class="bi bi-cpu"></i> Donanım Bilgileri</h6>
                
                ${hw.cpu ? `
                    <div class="device-info-item">
                        <span class="device-info-label">İşlemci:</span>
                        <span class="device-info-value">${hw.cpu} ${hw.cpuCores ? `(${hw.cpuCores} çekirdek)` : ''} ${hw.cpuClockMHz ? `@ ${hw.cpuClockMHz}MHz` : ''}</span>
                    </div>
                ` : ''}
                
                ${hw.ramGB ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Bellek:</span>
                        <span class="device-info-value">${hw.ramGB} GB RAM</span>
                    </div>
                ` : ''}
                
                ${hw.ramModules && hw.ramModules.length > 0 ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Bellek Modülleri:</span>
                        <div class="device-info-value">
                            ${hw.ramModules.map(ram => `
                                <div class="hardware-item">
                                    <strong>${ram.slot}:</strong> ${ram.capacityGB}GB ${ram.manufacturer || ''} ${ram.speedMHz ? `@ ${ram.speedMHz}MHz` : ''}
                                </div>
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
                
                ${hw.diskGB ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Depolama:</span>
                        <span class="device-info-value">${hw.diskGB} GB</span>
                    </div>
                ` : ''}
                
                ${hw.disks && hw.disks.length > 0 ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Disk Bilgileri:</span>
                        <div class="device-info-value">
                            ${hw.disks.map(disk => `
                                <div class="hardware-item">
                                    <strong>${disk.deviceId}:</strong> ${disk.totalGB}GB toplam, ${disk.freeGB}GB boş
                                </div>
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
                
                ${hw.gpus && hw.gpus.length > 0 ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Ekran Kartları:</span>
                        <div class="device-info-value">
                            ${hw.gpus.map(gpu => `
                                <div class="hardware-item">
                                    <strong>${gpu.name}</strong> ${gpu.memoryGB ? `(${gpu.memoryGB}GB)` : ''}
                                </div>
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
                
                ${hw.networkAdapters && hw.networkAdapters.length > 0 ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Ağ Adaptörleri:</span>
                        <div class="device-info-value">
                            ${hw.networkAdapters.map(adapter => `
                                <div class="hardware-item">
                                    <strong>${adapter.description}</strong><br>
                                    MAC: ${adapter.macAddress || 'N/A'}, IP: ${adapter.ipAddress || 'N/A'}
                                </div>
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
                
                ${hw.motherboard ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Anakart:</span>
                        <span class="device-info-value">${hw.motherboard} ${hw.motherboardSerial ? `(S/N: ${hw.motherboardSerial})` : ''}</span>
                    </div>
                ` : ''}
                
                ${hw.biosManufacturer ? `
                    <div class="device-info-item">
                        <span class="device-info-label">BIOS:</span>
                        <span class="device-info-value">${hw.biosManufacturer} ${hw.biosVersion || ''}</span>
                    </div>
                ` : ''}
            </div>
        `;
    }

    // Render software information section with pagination
    renderSoftwareInfo(device) {
        if (!device.softwareInfo) {
            return `
                <div class="device-info-group">
                    <h6><i class="bi bi-pc-display"></i> Yazılım Bilgileri</h6>
                    <div class="device-info-item">
                        <span class="device-info-value text-muted">Bu cihaz için yazılım bilgisi bulunmuyor</span>
                    </div>
                </div>
            `;
        }

        const sw = device.softwareInfo;
        const deviceId = device.id || 'modal';

        return `
            <div class="device-info-group">
                <h6><i class="bi bi-pc-display"></i> Yazılım Bilgileri</h6>
                
                ${sw.operatingSystem ? `
                    <div class="device-info-item">
                        <span class="device-info-label">İşletim Sistemi:</span>
                        <span class="device-info-value">${sw.operatingSystem} ${sw.osVersion || ''} ${sw.osArchitecture ? `(${sw.osArchitecture})` : ''}</span>
                    </div>
                ` : ''}
                
                ${sw.registeredUser ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Kayıtlı Kullanıcı:</span>
                        <span class="device-info-value">${sw.registeredUser}</span>
                    </div>
                ` : ''}
                
                ${sw.activeUser ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Aktif Kullanıcı:</span>
                        <span class="device-info-value">${sw.activeUser}</span>
                    </div>
                ` : ''}
                
                ${sw.serialNumber ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Seri Numarası:</span>
                        <span class="device-info-value">${sw.serialNumber}</span>
                    </div>
                ` : ''}
                
                ${sw.users && sw.users.length > 0 ? `
                    <div class="device-info-item">
                        <span class="device-info-label">Kullanıcılar (${sw.users.length}):</span>
                        <div class="device-info-value">
                            <div class="users-list">
                                ${sw.users.slice(0, 10).map(user => `<span class="user-badge">${user}</span>`).join('')}
                                ${sw.users.length > 10 ? `<span class="text-muted">ve ${sw.users.length - 10} diğer...</span>` : ''}
                            </div>
                        </div>
                    </div>
                ` : ''}
                
                ${sw.installedApps && sw.installedApps.length > 0 ? `
                    <div class="device-info-item">
                        <div class="device-info-value">
                            <div class="software-list-container">
                                <!-- Asıl liste -->
                                <div class="software-list" id="software-list-${deviceId}">
                                    ${sw.installedApps.slice(0, 20).map(app => `
                                        <div class="software-item">
                                            <i class="bi bi-app"></i>
                                            <span class="software-name">${app}</span>
                                        </div>
                                    `).join('')}
                                </div>

                                <!-- Toplam sayı, listenin hemen altında -->
                                <div class="software-count">
                                    Toplam yüklü yazılımlar: ${sw.installedApps.length}
                                </div>

                                <!-- “Daha fazla göster” butonu -->
                                ${sw.installedApps.length > 20 ? `
                                    <div class="software-load-more">
                                        <button class="btn-load-more" onclick="app.loadMoreSoftware('${deviceId}', ${sw.installedApps.length})">
                                            <i class="bi bi-chevron-down"></i>
                                            Daha fazla yazılım göster (${sw.installedApps.length - 20} kalan)
                                        </button>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                ` : ''}
            </div>
        `;
    }

    // Load more software items for large software lists
    loadMoreSoftware(deviceId, totalCount) {
        try {
            // Get the device to access its software list
            const device = this.devices.find(d => d.id == deviceId);
            if (!device || !device.softwareInfo || !device.softwareInfo.installedApps) return;

            const container = document.getElementById(`software-list-${deviceId}`);
            const loadMoreContainer = container?.parentElement.querySelector('.software-load-more');
            const loadMoreBtn = loadMoreContainer?.querySelector('.btn-load-more');

            if (!container || !loadMoreBtn) return;

            const currentItems = container.children.length;
            const nextBatch = device.softwareInfo.installedApps.slice(currentItems, currentItems + 20);

            // Add new software items
            nextBatch.forEach(app => {
                const softwareItem = document.createElement('div');
                softwareItem.className = 'software-item';
                softwareItem.innerHTML = `
                    <i class="bi bi-app"></i>
                    <span class="software-name">${app}</span>
                `;
                container.appendChild(softwareItem);
            });

            // Update or remove the load more button
            const remainingItems = device.softwareInfo.installedApps.length - container.children.length;
            if (remainingItems > 0) {
                loadMoreBtn.innerHTML = `
                    <i class="bi bi-chevron-down"></i>
                    Daha fazla yazılım göster (${remainingItems} kalan)
                `;
                // Update the onclick attribute to maintain functionality
                loadMoreBtn.setAttribute('onclick', `app.loadMoreSoftware('${deviceId}', ${device.softwareInfo.installedApps.length})`);
            } else {
                loadMoreContainer.style.display = 'none';
            }
        } catch (error) {
            console.error('Error loading more software:', error);
        }
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
        if (device.agentInstalled || device.managementType === 'Agent' || device.discoveryMethod === 'Agent') {
            return 'badge-success'; // Green for agent-installed devices
        } else if (device.managementType === 'NetworkDiscovery' || device.discoveryMethod === 'NetworkDiscovery') {
            return 'badge-info'; // Blue for network discovered devices
        } else {
            return 'badge-secondary'; // Gray for manual/unknown
        }
    }

    getDiscoveryTypeText(device) {
        if (device.agentInstalled || device.managementType === 'Agent' || device.discoveryMethod === 'Agent') {
            return 'Agent';
        } else if (device.managementType === 'NetworkDiscovery' || device.discoveryMethod === 'NetworkDiscovery') {
            return 'Network';
        } else {
            return 'Manual';
        }
    }

    // Check if device is recently active (within 24 hours)
    isRecentlyActive(device) {
        if (!device.lastSeen) return false;

        const now = new Date();
        const twentyFourHoursAgo = new Date(now - 24 * 60 * 60 * 1000);
        const lastSeen = new Date(device.lastSeen);

        return lastSeen > twentyFourHoursAgo;
    }

    // Check if device is online (seen within 30 minutes)
    // Server sends Turkey time (+3), we need to compare with Turkey time
    isDeviceOnline(device) {
        if (!device.lastSeen) return false;

        // Get current time in Turkey (UTC+3)
        const now = new Date();
        const turkeyOffset = 3 * 60; // Turkey is UTC+3
        const localOffset = now.getTimezoneOffset(); // Local offset in minutes from UTC
        const turkeyTime = new Date(now.getTime() + (localOffset * 60 * 1000) + (turkeyOffset * 60 * 1000));
        
        const thirtyMinutesAgo = new Date(turkeyTime - 30 * 60 * 1000);
        const lastSeen = new Date(device.lastSeen);

        return lastSeen > thirtyMinutesAgo;
    }

    // Get computed device status based on last seen time
    getComputedStatus(device) {
        // If device hasn't been seen in 30 minutes, it's offline (status 1)
        if (!this.isDeviceOnline(device)) {
            return 1; // Pasif/Offline
        }

        // If device was seen recently, it's online (status 0)
        return 0; // Aktif/Online
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
        
        // Convert UTC time to Turkey time (UTC+3)
        // The API returns UTC times, so we need to properly convert to Turkey timezone
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
        
        // Format the date using Turkey timezone
        const formatter = new Intl.DateTimeFormat('tr-TR', options);
        return formatter.format(date);
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

        // Use real network scan via API
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/NetworkScan/trigger-range`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    networkRange: networkRange,
                    timeoutSeconds: parseInt(timeout),
                    portScanType: portScan
                })
            });

            if (response.ok) {
                // Poll for scan results
                await this.pollScanProgress(progressFill, scanStatus, startBtn, resultsTable);
            } else {
                throw new Error(`Network scan failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Network scan error:', error);
            scanStatus.textContent = `Tarama hatası: ${error.message}`;
            this.resetScanButton(startBtn);
        }
    }

    async pollScanProgress(progressFill, scanStatus, startBtn, resultsTable) {
        let progress = 0;

        const pollInterval = setInterval(async () => {
            try {
                const statusResponse = await fetch(`${this.apiBaseUrl}/api/NetworkScan/status`);
                if (statusResponse.ok) {
                    const status = await statusResponse.json();

                    if (status.isScanning) {
                        progress = Math.min(progress + 10, 90); // Simulated progress
                        progressFill.style.width = progress + '%';
                        scanStatus.textContent = `Tarama devam ediyor... ${Math.round(progress)}%`;
                    } else {
                        // Scan completed, get results
                        clearInterval(pollInterval);
                        progressFill.style.width = '100%';
                        scanStatus.textContent = 'Tarama tamamlandı!';

                        // Get discovered devices
                        const devicesResponse = await fetch(`${this.apiBaseUrl}/api/Device/network-discovered`);
                        if (devicesResponse.ok) {
                            const devices = await devicesResponse.json();
                            this.displayScanResults(devices.map(device => ({
                                ip: device.ipAddress,
                                mac: device.macAddress,
                                name: device.name || 'Unknown',
                                status: 'Discovered',
                                ports: device.openPorts && device.openPorts.length > 0 ? device.openPorts.join(', ') : 'None'
                            })));
                        }

                        resultsTable.style.display = 'block';
                        this.resetScanButton(startBtn);
                    }
                } else {
                    // Fallback: assume scan completed after timeout
                    clearInterval(pollInterval);
                    progressFill.style.width = '100%';
                    scanStatus.textContent = 'Tarama tamamlandı!';
                    resultsTable.style.display = 'block';
                    this.resetScanButton(startBtn);
                }
            } catch (error) {
                clearInterval(pollInterval);
                scanStatus.textContent = `Tarama hatası: ${error.message}`;
                this.resetScanButton(startBtn);
            }
        }, 2000); // Poll every 2 seconds
    }

    resetScanButton(startBtn) {
        startBtn.disabled = false;
        startBtn.innerHTML = '<i class="bi bi-play-fill"></i> Taramayı Başlat';
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
        try {
            // Load both change logs and devices for proper device name mapping
            const [changeLogsResponse, devicesResponse] = await Promise.all([
                fetch(`${this.apiBaseUrl}/api/ChangeLog`),
                fetch(`${this.apiBaseUrl}/api/device`)
            ]);

            if (changeLogsResponse.ok && devicesResponse.ok) {
                const changeLogs = await changeLogsResponse.json();
                const devices = await devicesResponse.json();
                
                // Create a device ID to name mapping
                const deviceMap = new Map();
                devices.forEach(device => {
                    deviceMap.set(device.id, device.name);
                });

                this.changeLogs = changeLogs.map(log => ({
                    id: log.id,
                    deviceId: log.deviceId,
                    deviceName: deviceMap.get(log.deviceId) || 'Bilinmeyen Cihaz',
                    changeDate: log.changeDate,
                    changeType: log.changeType,
                    oldValue: log.oldValue,
                    newValue: log.newValue,
                    changedBy: log.changedBy
                }));

                // Populate device filter dropdown
                this.populateDeviceFilter(devices);
            } else {
                console.warn('Could not load change logs or devices from API');
                this.changeLogs = [];
            }
        } catch (error) {
            console.error('Error loading change logs:', error);
            this.changeLogs = [];
        }

        this.filteredChangeLogs = [...this.changeLogs];
        this.renderChangeLogs();
    }

    populateDeviceFilter(devices) {
        const deviceFilter = document.getElementById('filter-device');
        
        // Clear existing options except the first one
        while (deviceFilter.options.length > 1) {
            deviceFilter.remove(1);
        }

        // Add devices as options
        devices.forEach(device => {
            const option = document.createElement('option');
            option.value = device.id;
            option.textContent = device.name;
            deviceFilter.appendChild(option);
        });
    }

    filterChangeLogs() {
        const changeTypeFilter = document.getElementById('filter-change-type').value;
        const deviceFilter = document.getElementById('filter-device').value;
        const dateFromFilter = document.getElementById('filter-date-from').value;
        const dateToFilter = document.getElementById('filter-date-to').value;

        this.filteredChangeLogs = this.changeLogs.filter(log => {
            // Filter by change type
            if (changeTypeFilter && log.changeType !== changeTypeFilter) {
                return false;
            }

            // Filter by device
            if (deviceFilter && log.deviceId !== deviceFilter) {
                return false;
            }

            // Filter by date range
            if (dateFromFilter || dateToFilter) {
                const logDate = new Date(log.changeDate);
                const fromDate = dateFromFilter ? new Date(dateFromFilter) : null;
                const toDate = dateToFilter ? new Date(dateToFilter + 'T23:59:59') : null;

                if (fromDate && logDate < fromDate) {
                    return false;
                }

                if (toDate && logDate > toDate) {
                    return false;
                }
            }

            return true;
        });

        this.renderChangeLogs();
    }

    renderChangeLogs() {
        const tbody = document.getElementById('change-logs-body');
        const noDataDiv = document.getElementById('no-change-logs');

        const logsToShow = this.filteredChangeLogs || this.changeLogs || [];

        if (logsToShow.length === 0) {
            tbody.innerHTML = '';
            noDataDiv.classList.remove('d-none');
            return;
        }

        noDataDiv.classList.add('d-none');
        tbody.innerHTML = logsToShow.map(log => `
            <tr>
                <td>${this.formatDate(log.changeDate)}</td>
                <td><strong>${log.deviceName||'Bilinmeyen Cihaz'}</strong></td>
                <td><span class="badge type-unknown">${log.changeType}</span></td>
                <td class="hide-mobile"><span class="change-value ${log.oldValue?'':'text-muted'}" title="${log.oldValue||'Boş'}">${log.oldValue||'Boş'}</span></td>
                <td class="hide-mobile"><span class="change-value ${log.newValue?'':'text-muted'}" title="${log.newValue||'Boş'}">${log.newValue||'Boş'}</span></td>
                <td class="hide-mobile"><span class="badge badge-secondary">${log.changedBy||'Sistem'}</span></td>
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
        this.filteredChangeLogs = [...this.changeLogs];
        this.renderChangeLogs();
    }

    // Device deletion functions
    confirmDeleteDevice(deviceId, deviceName) {
        const confirmation = confirm(`Cihazı silmek istediğinize emin misiniz?\n\nCihaz: ${deviceName}\n\nBu işlem geri alınamaz ve cihazın tüm bilgileri veritabanından silinecektir.`);
        
        if (confirmation) {
            this.deleteDevice(deviceId, deviceName);
        }
    }

    async deleteDevice(deviceId, deviceName) {
        try {
            this.showLoading();
            
            // Call the API to delete the device
            const response = await fetch(`${this.apiBaseUrl}/api/device/${deviceId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                this.showSuccess(`Cihaz "${deviceName}" başarıyla silindi.`);
                
                // Remove the device from the local array
                this.devices = this.devices.filter(device => device.id !== deviceId);
                this.filteredDevices = this.filteredDevices.filter(device => device.id !== deviceId);
                
                // Update statistics
                this.updateStatistics();
                
                // Navigate back to devices list
                this.showPage('devices');
                
                // Re-render the device table
                this.renderDevices();
            } else {
                const errorData = await response.text();
                throw new Error(errorData || 'Cihaz silinirken bir hata oluştu');
            }
        } catch (error) {
            this.showError('Cihaz silinirken hata oluştu: ' + error.message);
        } finally {
            this.hideLoading();
        }
    }

    // Success message display
    showSuccess(message) {
        // Create or update success alert
        let successAlert = document.getElementById('success-alert');
        if (!successAlert) {
            successAlert = document.createElement('div');
            successAlert.id = 'success-alert';
            successAlert.className = 'success-alert';
            successAlert.innerHTML = `
                <div class="success-content">
                    <i class="bi bi-check-circle"></i>
                    <span id="success-message"></span>
                    <button type="button" class="success-close" onclick="app.hideSuccess()">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `;
            
            // Insert after the error alert
            const errorAlert = document.getElementById('error-alert');
            if (errorAlert && errorAlert.parentNode) {
                errorAlert.parentNode.insertBefore(successAlert, errorAlert.nextSibling);
            } else {
                document.querySelector('.main-content').prepend(successAlert);
            }
        }

        document.getElementById('success-message').textContent = message;
        successAlert.classList.remove('d-none');

        // Auto-hide after 5 seconds
        setTimeout(() => {
            this.hideSuccess();
        }, 5000);
    }

    hideSuccess() {
        const successAlert = document.getElementById('success-alert');
        if (successAlert) {
            successAlert.classList.add('d-none');
        }
    }
}

// Global functions for HTML onclick events
function showPage(pageId) {
    if (app) {
        app.showPage(pageId);
    } else {
        console.warn('App not initialized yet');
    }
}

function hideError() {
    if (app) app.hideError();
}

function refreshDevices() {
    if (app) app.refreshDevices();
}

function clearFilters() {
    if (app) app.clearFilters();
}

function closeModal() {
    if (app) app.closeModal();
}

function showDeviceDetailPage(deviceId) {
    if (app) app.showDeviceDetailPage(deviceId);
}

function openApiDocumentation() {
    const apiUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
    // Swagger UI is now served at the root of the API server
    window.open(apiUrl, '_blank');
}

// Add mock data function to prototype
InventoryApp.prototype.getMockDevices = function () {
    return [
        {
            id: '1',
            name: 'DESKTOP-TEST01',
            ipAddress: '192.168.1.100',
            macAddress: '00:11:22:33:44:55',
            deviceType: 0, // PC
            status: 0, // Active
            model: 'Dell OptiPlex 7090',
            location: 'IT Departmanı',
            lastSeen: new Date(Date.now() - 5 * 60 * 1000).toISOString(), // 5 minutes ago (online)
            managementType: 1, // Agent installed
            discoveryMethod: 1,
            hardwareInfo: {
                cpu: 'Intel Core i7-11700',
                cpuCores: 8,
                cpuClockMHz: 2900,
                ramGB: 16,
                ramModules: [
                    { slot: 'DIMM1', capacityGB: 8, manufacturer: 'Samsung', speedMHz: 3200 },
                    { slot: 'DIMM2', capacityGB: 8, manufacturer: 'Samsung', speedMHz: 3200 }
                ],
                diskGB: 512,
                disks: [
                    { deviceId: 'C:', totalGB: 512, freeGB: 256 }
                ],
                gpus: [
                    { name: 'Intel UHD Graphics 750', memoryGB: 1 },
                    { name: 'NVIDIA GeForce RTX 3060', memoryGB: 12 }
                ],
                networkAdapters: [
                    { description: 'Intel Ethernet Connection', macAddress: '00:11:22:33:44:55', ipAddress: '192.168.1.100' }
                ],
                motherboard: 'Dell Inc. 0K240Y',
                motherboardSerial: 'CN123456789'
            },
            softwareInfo: {
                operatingSystem: 'Windows 11 Pro',
                osVersion: '22H2',
                osArchitecture: 'x64',
                registeredUser: 'Test User',
                installedApps: [
                    'Microsoft Office 365', 'Google Chrome', 'Mozilla Firefox', 'Adobe Acrobat Reader DC',
                    'Visual Studio Code', 'Git for Windows', 'Node.js', 'Python 3.11', 'Docker Desktop',
                    'Zoom', 'Slack', 'Teams', 'Notepad++', 'WinRAR', '7-Zip', 'VLC Media Player',
                    'Spotify', 'Steam', 'Discord', 'WhatsApp', 'Telegram', 'Skype', 'Adobe Photoshop',
                    'Adobe Illustrator', 'AutoCAD', 'MATLAB', 'Android Studio', 'IntelliJ IDEA',
                    'Eclipse IDE', 'Postman', 'Figma', 'Canva', 'OBS Studio', 'Blender'
                ]
            }
        },
        {
            id: '2',
            name: 'LAPTOP-SALES05',
            ipAddress: '192.168.1.150',
            macAddress: '00:AA:BB:CC:DD:EE',
            deviceType: 1, // Laptop
            status: 1, // Inactive
            model: 'HP EliteBook 840',
            location: 'Satış Departmanı',
            lastSeen: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago (offline)
            managementType: 1, // Agent installed
            discoveryMethod: 1,
            hardwareInfo: {
                cpu: 'Intel Core i5-1135G7',
                cpuCores: 4,
                cpuClockMHz: 2400,
                ramGB: 8,
                ramModules: [
                    { slot: 'SO-DIMM1', capacityGB: 8, manufacturer: 'Crucial', speedMHz: 3200 }
                ],
                diskGB: 256,
                disks: [
                    { deviceId: 'C:', totalGB: 256, freeGB: 128 }
                ],
                gpus: [
                    { name: 'Intel Iris Xe Graphics', memoryGB: 1 }
                ]
            },
            softwareInfo: {
                operatingSystem: 'Windows 10 Pro',
                osVersion: '22H2',
                osArchitecture: 'x64',
                registeredUser: 'Sales User',
                installedApps: [
                    'Microsoft Office 365', 'Google Chrome', 'Outlook', 'Excel', 'PowerPoint',
                    'Word', 'Teams', 'Zoom', 'Salesforce', 'CRM Software'
                ]
            }
        },
        {
            id: '3',
            name: 'SERVER-DB01',
            ipAddress: '192.168.1.200',
            macAddress: '00:FF:EE:DD:CC:BB',
            deviceType: 2, // Server
            status: 0, // Active
            model: 'Dell PowerEdge R740',
            location: 'Sunucu Odası',
            lastSeen: new Date(Date.now() - 30 * 1000).toISOString(), // 30 seconds ago (online)
            managementType: 2, // Network discovered
            discoveryMethod: 2,
            hardwareInfo: {
                cpu: 'Intel Xeon Silver 4214R',
                cpuCores: 24,
                cpuClockMHz: 2400,
                ramGB: 64,
                diskGB: 2000
            },
            softwareInfo: {
                operatingSystem: 'Windows Server 2022',
                osVersion: '21H2',
                osArchitecture: 'x64'
            }
        }
    ];
};

// Initialize the application when the page loads
let app;
document.addEventListener('DOMContentLoaded', function () {
    app = new InventoryApp();
});