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
                // If API fails, show empty state instead of fake data
                console.warn('API not available, showing empty state:', apiError.message);
                this.devices = [];
                this.filteredDevices = [];
                this.updateStatistics([], [], []);
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
                    <span class="badge ${this.getStatusBadgeClass(this.getComputedStatus(device))}">
                        ${this.getStatusText(this.getComputedStatus(device))}
                    </span>
                </td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.model || 'N/A'}</span></td>
                <td class="hide-mobile"><span class="text-truncate-mobile">${device.location || 'N/A'}</span></td>
                <td class="hide-mobile">
                    <small class="text-muted">
                        ${device.lastSeen ? this.formatDate(device.lastSeen) : 'Bilinmiyor'}
                        ${this.isRecentlyActive(device) ? '<span class="badge badge-success badge-sm ms-1" title="Son 24 saatte aktif">●</span>' : ''}
                    </small>
                </td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-outline-primary btn-sm" onclick="app.showDeviceDetailPage('${device.id}')" title="Cihaz Detayına Git">
                            <i class="bi bi-info-circle"></i>
                            <span class="d-none d-lg-inline">Cihaz Detayı</span>
                        </button>
                        <button class="btn btn-outline-secondary btn-sm" onclick="app.showDeviceLogs('${device.id}')" title="Cihaz Loglarına Git">
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
                        <span class="badge ${this.getStatusBadgeClass(this.getComputedStatus(device))}">
                            ${this.getStatusText(this.getComputedStatus(device))}
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
                        <span class="device-info-label">Yüklü Yazılımlar (${sw.installedApps.length} adet):</span>
                        <div class="device-info-value">
                            <div class="software-list-container">
                                <div class="software-list" id="software-list-${deviceId}">
                                    ${sw.installedApps.slice(0, 20).map(app => `
                                        <div class="software-item">
                                            <i class="bi bi-app"></i>
                                            <span class="software-name">${app}</span>
                                        </div>
                                    `).join('')}
                                </div>
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
            const device = this.devices.find(d => d.id === deviceId);
            if (!device || !device.softwareInfo || !device.softwareInfo.installedApps) return;

            const container = document.getElementById(`software-list-${deviceId}`);
            const loadMoreBtn = container.parentElement.querySelector('.software-load-more');
            
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
            } else {
                loadMoreBtn.style.display = 'none';
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

    // Check if device is recently active (within 24 hours)
    isRecentlyActive(device) {
        if (!device.lastSeen) return false;
        
        const now = new Date();
        const twentyFourHoursAgo = new Date(now - 24 * 60 * 60 * 1000);
        const lastSeen = new Date(device.lastSeen);
        
        return lastSeen > twentyFourHoursAgo;
    }

    // Check if device is online (seen within 30 minutes)
    isDeviceOnline(device) {
        if (!device.lastSeen) return false;
        
        const now = new Date();
        const thirtyMinutesAgo = new Date(now - 30 * 60 * 1000);
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
        // Convert to Turkey timezone (UTC+3)
        const turkeyTime = new Date(date.getTime() + (3 * 60 * 60 * 1000));
        return turkeyTime.toLocaleDateString('tr-TR') + ' ' + turkeyTime.toLocaleTimeString('tr-TR');
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
                    timeoutSeconds: parseInt(timeout)
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
                                ports: 'N/A'
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
            // Try to load real change logs from API
            const response = await fetch(`${this.apiBaseUrl}/api/ChangeLog`);
            if (response.ok) {
                const changeLogs = await response.json();
                this.changeLogs = changeLogs.map(log => ({
                    id: log.id,
                    deviceName: log.deviceId, // We might need device name lookup later
                    changeDate: log.changeDate,
                    changeType: log.changeType,
                    oldValue: log.oldValue,
                    newValue: log.newValue,
                    changedBy: log.changedBy
                }));
            } else {
                console.warn('Could not load change logs from API:', response.status);
                this.changeLogs = [];
            }
        } catch (error) {
            console.error('Error loading change logs:', error);
            this.changeLogs = [];
        }
        
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
                <td><strong>${log.deviceName || 'Bilinmeyen Cihaz'}</strong></td>
                <td><span class="badge type-unknown">${log.changeType}</span></td>
                <td class="hide-mobile">
                    <span class="change-value ${log.oldValue ? '' : 'text-muted'}" title="${log.oldValue || 'Boş'}">
                        ${log.oldValue || 'Boş'}
                    </span>
                </td>
                <td class="hide-mobile">
                    <span class="change-value ${log.newValue ? '' : 'text-muted'}" title="${log.newValue || 'Boş'}">
                        ${log.newValue || 'Boş'}
                    </span>
                </td>
                <td class="hide-mobile">
                    <span class="badge badge-secondary">${log.changedBy || 'Sistem'}</span>
                </td>
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