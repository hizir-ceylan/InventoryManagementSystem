// Device Details Module
class DeviceDetailsManager {
    constructor() {
        this.currentDevice = null;
        this.init();
    }

    init() {
        this.loadDeviceFromStorage();
        this.checkUrlParams();
    }

    loadDeviceFromStorage() {
        const deviceData = sessionStorage.getItem('selectedDevice');
        if (deviceData) {
            try {
                this.currentDevice = JSON.parse(deviceData);
                this.renderDeviceDetails();
            } catch (error) {
                console.error('Error parsing device data:', error);
            }
        }
    }

    checkUrlParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const deviceId = urlParams.get('id');
        
        // Only load from API if we don't have device data in session storage
        if (deviceId && !this.currentDevice) {
            this.loadDeviceById(deviceId);
        }
    }

    async loadDeviceById(deviceId) {
        try {
            window.ui.showLoading();
            
            // Try to get device from API first
            try {
                const device = await window.api.getDevice(deviceId);
                this.currentDevice = device;
                this.renderDeviceDetails();
                window.ui.hideLoading();
                return;
            } catch (apiError) {
                console.warn('API call failed, trying fallback methods:', apiError);
            }
            
            // Fallback: Try to find device in any loaded devices data
            if (window.app && window.app.devices) {
                const device = window.app.devices.find(d => d.id == deviceId);
                if (device) {
                    this.currentDevice = device;
                    this.renderDeviceDetails();
                    window.ui.hideLoading();
                    return;
                }
            }
            
            // If all fails, show fallback message
            this.showFallbackMessage(deviceId);
            window.ui.hideLoading();
            
        } catch (error) {
            window.ui.showError('Cihaz detayları yüklenirken hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
    }

    showFallbackMessage(deviceId) {
        const contentDiv = document.getElementById('device-details-content');
        if (!contentDiv) return;
        
        contentDiv.innerHTML = `
            <div class="device-details-placeholder">
                <div class="placeholder-content">
                    <i class="bi bi-exclamation-triangle"></i>
                    <h3>Cihaz Verisi Bulunamadı</h3>
                    <p>ID: ${deviceId} olan cihazın detayları yüklenemedi. API sunucusu çevrimdışı olabilir.</p>
                    <div class="placeholder-actions">
                        <a href="/Devices" class="btn-primary">
                            <i class="bi bi-list-ul"></i>
                            Cihazlar Listesine Git
                        </a>
                        <button class="btn-secondary" onclick="window.location.reload()">
                            <i class="bi bi-arrow-clockwise"></i>
                            Sayfayı Yenile
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    renderDeviceDetails() {
        const contentDiv = document.getElementById('device-details-content');
        if (!contentDiv || !this.currentDevice) return;

        const device = this.currentDevice;

        contentDiv.innerHTML = `
            <div class="device-detail-header">
                <div class="device-title">
                    <h3>${device.name || 'Bilinmeyen Cihaz'}</h3>
                    ${device.isVirtual ? '<span class="vm-badge">VM</span>' : ''}
                    ${this.getStatusBadge(device.status)}
                </div>
                <div class="device-actions">
                    <button class="btn-warning" onclick="window.deviceDetails?.editDevice?.('${device.id}')">
                        <i class="bi bi-pencil"></i>
                        Düzenle
                    </button>
                    <button class="btn-info" onclick="window.location.href='/ChangeLogs?deviceId=${device.id}'">
                        <i class="bi bi-journal-text"></i>
                        Değişiklik Logları
                    </button>
                    <button class="btn-danger" onclick="window.deviceDetails?.confirmDeleteDevice?.('${device.id}', '${device.name}')">
                        <i class="bi bi-trash"></i>
                        Cihazı Sil
                    </button>
                    <button class="btn-secondary" onclick="window.location.href='/Devices'">
                        <i class="bi bi-arrow-left"></i>
                        Cihazlar Listesine Dön
                    </button>
                </div>
            </div>

            <div class="device-detail-content">
                <div class="detail-section">
                    <h5><i class="bi bi-info-circle"></i> Genel Bilgiler</h5>
                    <div class="detail-grid">
                        <div class="detail-item">
                            <strong>Cihaz Adı:</strong>
                            <span>${device.name || '--'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>IP Adresi:</strong>
                            <span>${device.ipAddress || '--'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>MAC Adresi:</strong>
                            <span>${device.macAddress || '--'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Tür:</strong>
                            <span>${this.getDeviceTypeText(device.deviceType)}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Model:</strong>
                            <span>${device.model || '--'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Konum:</strong>
                            <span>${device.location || '--'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Keşif Yöntemi:</strong>
                            <span>${this.getDiscoveryMethodText(device.discoveryMethod)}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Son Görülme:</strong>
                            <span>${this.formatDateTime(device.lastSeen)}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Barkod Numarası:</strong>
                            <span>${device.barcodeNumber || 'Girilmemiş'}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Oluşturulma:</strong>
                            <span>${this.formatDateTime(device.createdAt)}</span>
                        </div>
                    </div>
                </div>

                ${this.renderHardwareInfo(device)}
                ${this.renderSoftwareInfo(device)}
                ${this.renderDeviceUpdates(device)}
            </div>
        `;
        
        // Load device updates after rendering
        setTimeout(() => this.loadDeviceUpdates(device.id), 500);
    }

    renderHardwareInfo(device) {
        if (!device.hardwareInfo) return '';

        const hw = device.hardwareInfo;
        let content = `
            <div class="detail-section">
                <h5><i class="bi bi-cpu"></i> Donanım Bilgileri</h5>
                <div class="detail-grid">
        `;

        if (hw.cpu) {
            content += `
                <div class="detail-item">
                    <strong>İşlemci:</strong>
                    <span>${hw.cpu}${hw.cpuCores ? ` (${hw.cpuCores} çekirdek)` : ''}${hw.cpuClockMHz ? ` @ ${hw.cpuClockMHz}MHz` : ''}</span>
                </div>
            `;
        }

        if (hw.ramGB) {
            content += `
                <div class="detail-item">
                    <strong>Bellek:</strong>
                    <span>${hw.ramGB} GB RAM</span>
                </div>
            `;
        }

        if (hw.diskGB) {
            content += `
                <div class="detail-item">
                    <strong>Depolama:</strong>
                    <span>${hw.diskGB} GB</span>
                </div>
            `;
        }

        if (hw.motherboard) {
            content += `
                <div class="detail-item">
                    <strong>Anakart:</strong>
                    <span>${hw.motherboard}${hw.motherboardSerial ? ` (S/N: ${hw.motherboardSerial})` : ''}</span>
                </div>
            `;
        }

        if (hw.biosManufacturer || hw.biosVersion) {
            content += `
                <div class="detail-item">
                    <strong>BIOS:</strong>
                    <span>${hw.biosManufacturer || ''} ${hw.biosVersion || ''}</span>
                </div>
            `;
        }

        content += `</div>`;

        // Add detailed hardware info if available
        if (hw.ramModules && hw.ramModules.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Bellek Modülleri</h6>
                    <div class="hardware-list">
                        ${hw.ramModules.map(ram => `
                            <div class="hardware-item">
                                <strong>${ram.slot}:</strong> ${ram.capacityGB}GB ${ram.manufacturer || ''} ${ram.speedMHz ? `@ ${ram.speedMHz}MHz` : ''}
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        if (hw.disks && hw.disks.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Disk Bilgileri</h6>
                    <div class="hardware-list">
                        ${hw.disks.map(disk => `
                            <div class="hardware-item">
                                <strong>${disk.deviceId}:</strong> ${disk.totalGB}GB toplam, ${disk.freeGB}GB boş
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        if (hw.gpus && hw.gpus.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Ekran Kartları</h6>
                    <div class="hardware-list">
                        ${hw.gpus.map(gpu => `
                            <div class="hardware-item">
                                <strong>${gpu.name}</strong> ${gpu.memoryGB ? `(${gpu.memoryGB}GB)` : ''}
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        if (hw.networkAdapters && hw.networkAdapters.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Ağ Adaptörleri</h6>
                    <div class="hardware-list">
                        ${hw.networkAdapters.map(adapter => `
                            <div class="hardware-item">
                                <strong>${adapter.description}</strong><br>
                                <small>MAC: ${adapter.macAddress || 'N/A'}, IP: ${adapter.ipAddress || 'N/A'}</small>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
        }

        content += `</div>`;

        return content;
    }

    renderSoftwareInfo(device) {
        if (!device.softwareInfo) return '';

        const sw = device.softwareInfo;
        let content = `
            <div class="detail-section">
                <h5><i class="bi bi-laptop"></i> Yazılım Bilgileri</h5>
                <div class="detail-grid">
        `;

        if (sw.operatingSystem) {
            content += `
                <div class="detail-item">
                    <strong>İşletim Sistemi:</strong>
                    <span>${sw.operatingSystem} ${sw.osVersion || ''} ${sw.osArchitecture ? `(${sw.osArchitecture})` : ''}</span>
                </div>
            `;
        }

        if (sw.registeredUser) {
            content += `
                <div class="detail-item">
                    <strong>Kayıtlı Kullanıcı:</strong>
                    <span>${sw.registeredUser}</span>
                </div>
            `;
        }

        if (sw.activeUser) {
            content += `
                <div class="detail-item">
                    <strong>Aktif Kullanıcı:</strong>
                    <span>${sw.activeUser}</span>
                </div>
            `;
        }

        if (sw.serialNumber) {
            content += `
                <div class="detail-item">
                    <strong>Seri Numarası:</strong>
                    <span>${sw.serialNumber}</span>
                </div>
            `;
        }

        content += `</div>`;

        // Users list
        if (sw.users && sw.users.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Kullanıcılar (${sw.users.length})</h6>
                    <div class="users-list">
                        ${sw.users.slice(0, 10).map(user => `<span class="user-badge">${user}</span>`).join('')}
                        ${sw.users.length > 10 ? `<span class="text-muted">ve ${sw.users.length - 10} diğer...</span>` : ''}
                    </div>
                </div>
            `;
        }

        // Installed applications with better styling
        if (sw.installedApps && sw.installedApps.length > 0) {
            content += `
                <div class="detail-subsection">
                    <h6>Yüklü Yazılımlar (${sw.installedApps.length})</h6>
                    <div class="software-list-container">
                        <div class="software-list" id="software-list-${device.id}">
                            ${sw.installedApps.slice(0, 20).map(app => `
                                <div class="software-item">
                                    <i class="bi bi-app"></i>
                                    <span class="software-name">${app}</span>
                                </div>
                            `).join('')}
                        </div>
                        ${sw.installedApps.length > 20 ? `
                            <div class="software-load-more">
                                <button class="btn-load-more" onclick="window.deviceDetails.showAllSoftware('${device.id}')">
                                    <i class="bi bi-list"></i>
                                    Tümünü göster (${sw.installedApps.length} yazılım)
                                </button>
                            </div>
                        ` : ''}
                    </div>
                </div>
            `;
        }

        content += `</div>`;

        return content;
    }

    renderNetworkInfo(device) {
        if (!device.networkInfo) return '';

        const net = device.networkInfo;
        return `
            <div class="detail-section">
                <h5><i class="bi bi-wifi"></i> Ağ Bilgileri</h5>
                <div class="detail-grid">
                    <div class="detail-item">
                        <strong>Hostname:</strong>
                        <span>${net.hostname || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Domain:</strong>
                        <span>${net.domain || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Gateway:</strong>
                        <span>${net.gateway || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>DNS Sunucular:</strong>
                        <span>${net.dnsServers ? net.dnsServers.join(', ') : '--'}</span>
                    </div>
                </div>
            </div>
        `;
    }

    renderDeviceUpdates(device) {
        return `
            <div class="detail-section">
                <h5><i class="bi bi-arrow-clockwise"></i> Cihaz Güncellemeleri</h5>
                <div id="device-updates-${device.id}" class="device-updates-container">
                    <div class="loading-updates">
                        <i class="bi bi-hourglass-split"></i>
                        Güncelleme bilgileri yükleniyor...
                    </div>
                </div>
            </div>
        `;
    }

    async loadDeviceUpdates(deviceId) {
        try {
            const updatesContainer = document.getElementById(`device-updates-${deviceId}`);
            if (!updatesContainer) return;

            // Try to load updates from real API
            const updates = await window.api.apiCall(`update/${deviceId}`);
            
            if (updates && updates.length > 0) {
                const availableUpdates = updates.filter(update => update.status === 0); // Available updates
                const downloadedUpdates = updates.filter(update => update.status === 1); // Downloaded updates
                const installedUpdates = updates.filter(update => update.status === 2); // Installed updates
                
                let updatesHtml = '';
                
                if (availableUpdates.length > 0) {
                    updatesHtml += `
                        <div class="updates-section">
                            <h6><i class="bi bi-download"></i> Mevcut Güncellemeler (${availableUpdates.length})</h6>
                            <div class="updates-list">
                                ${availableUpdates.slice(0, 5).map(update => `
                                    <div class="update-item ${this.getUpdatePriorityClass(update.priority)}">
                                        <div class="update-info">
                                            <strong>${update.title}</strong>
                                            <small>${update.updateType} - ${this.getUpdatePriorityText(update.priority)}</small>
                                        </div>
                                        <div class="update-status">
                                            <span class="badge badge-warning">Mevcut</span>
                                        </div>
                                    </div>
                                `).join('')}
                                ${availableUpdates.length > 5 ? `
                                    <div class="updates-show-all">
                                        <button class="btn-secondary" onclick="window.deviceDetails.showAllUpdates('${deviceId}', 'available')">
                                            <i class="bi bi-list"></i>
                                            Tüm mevcut güncellemeleri göster (${availableUpdates.length})
                                        </button>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    `;
                }
                
                if (downloadedUpdates.length > 0) {
                    updatesHtml += `
                        <div class="updates-section">
                            <h6><i class="bi bi-cloud-download"></i> İndirilmiş Güncellemeler (${downloadedUpdates.length})</h6>
                            <div class="updates-list">
                                ${downloadedUpdates.slice(0, 5).map(update => `
                                    <div class="update-item ${this.getUpdatePriorityClass(update.priority)}">
                                        <div class="update-info">
                                            <strong>${update.title}</strong>
                                            <small>${update.updateType} - ${this.getUpdatePriorityText(update.priority)}</small>
                                        </div>
                                        <div class="update-status">
                                            <span class="badge badge-info">İndirildi</span>
                                        </div>
                                    </div>
                                `).join('')}
                                ${downloadedUpdates.length > 5 ? `
                                    <div class="updates-show-all">
                                        <button class="btn-secondary" onclick="window.deviceDetails.showAllUpdates('${deviceId}', 'downloaded')">
                                            <i class="bi bi-list"></i>
                                            Tüm indirilmiş güncellemeleri göster (${downloadedUpdates.length})
                                        </button>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    `;
                }
                
                if (installedUpdates.length > 0) {
                    updatesHtml += `
                        <div class="updates-section">
                            <h6><i class="bi bi-check-circle"></i> Yüklü Güncellemeler (${installedUpdates.length})</h6>
                            <div class="updates-list">
                                ${installedUpdates.slice(0, 3).map(update => `
                                    <div class="update-item">
                                        <div class="update-info">
                                            <strong>${update.title}</strong>
                                            <small>${update.updateType} - ${this.formatDateTime(update.lastChecked)}</small>
                                        </div>
                                        <div class="update-status">
                                            <span class="badge badge-success">Yüklü</span>
                                        </div>
                                    </div>
                                `).join('')}
                                ${installedUpdates.length > 3 ? `
                                    <div class="updates-show-all">
                                        <button class="btn-secondary" onclick="window.deviceDetails.showAllUpdates('${deviceId}', 'installed')">
                                            <i class="bi bi-list"></i>
                                            Tüm yüklü güncellemeleri göster (${installedUpdates.length})
                                        </button>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    `;
                }
                
                if (updatesHtml === '') {
                    updatesHtml = `
                        <div class="no-updates">
                            <i class="bi bi-check-circle"></i>
                            <span>Tüm güncellemeler yüklü</span>
                        </div>
                    `;
                }
                
                updatesContainer.innerHTML = updatesHtml;
            } else {
                updatesContainer.innerHTML = `
                    <div class="no-updates">
                        <i class="bi bi-info-circle"></i>
                        <span>Güncelleme bilgisi bulunamadı</span>
                        <button class="btn-primary" onclick="window.deviceDetails.refreshUpdates('${deviceId}')">
                            <i class="bi bi-arrow-clockwise"></i>
                            Güncellemeleri kontrol et
                        </button>
                    </div>
                `;
            }
        } catch (error) {
            console.warn('Device updates could not be loaded:', error);
            const updatesContainer = document.getElementById(`device-updates-${deviceId}`);
            if (updatesContainer) {
                updatesContainer.innerHTML = `
                    <div class="no-updates error">
                        <i class="bi bi-exclamation-triangle"></i>
                        <span>Güncelleme bilgileri yüklenemedi</span>
                        <small>Hata: ${error.message || 'API bağlantı sorunu'}</small>
                        <div class="error-actions">
                            <button class="btn-secondary" onclick="window.deviceDetails.refreshUpdates('${deviceId}')">
                                <i class="bi bi-arrow-clockwise"></i>
                                Tekrar dene
                            </button>
                            <button class="btn-secondary" onclick="window.deviceDetails.loadDeviceUpdates('${deviceId}')">
                                <i class="bi bi-cloud-download"></i>
                                Yeniden yükle
                            </button>
                        </div>
                    </div>
                `;
            }
        }
    }

    getUpdatePriorityClass(priority) {
        const priorityClasses = {
            4: 'update-security',    // Security
            3: 'update-critical',    // Critical
            2: 'update-high',        // High
            1: 'update-normal',      // Normal
            0: 'update-low'          // Low
        };
        return priorityClasses[priority] || 'update-normal';
    }

    getUpdatePriorityText(priority) {
        const priorityTexts = {
            4: 'Güvenlik',
            3: 'Kritik',
            2: 'Yüksek',
            1: 'Normal',
            0: 'Düşük'
        };
        return priorityTexts[priority] || 'Normal';
    }

    async refreshUpdates(deviceId) {
        const updatesContainer = document.getElementById(`device-updates-${deviceId}`);
        if (updatesContainer) {
            updatesContainer.innerHTML = `
                <div class="loading-updates">
                    <i class="bi bi-hourglass-split"></i>
                    Güncellemeler kontrol ediliyor...
                </div>
            `;
            
            try {
                // Trigger real update scan using API
                await window.api.apiCall(`update/scan/${deviceId}`, {
                    method: 'POST'
                });
                // Wait a bit and reload
                setTimeout(() => this.loadDeviceUpdates(deviceId), 3000);
            } catch (error) {
                console.error('Update scan failed:', error);
                // Still try to reload updates even if scan failed
                setTimeout(() => this.loadDeviceUpdates(deviceId), 1000);
            }
        }
    }

    showAllUpdates(deviceId, type) {
        // This would open a modal or navigate to a detailed updates page
        window.open(`/DeviceUpdates?deviceId=${deviceId}&type=${type}`, '_blank');
    }

    // Device action functions
    async editDevice(deviceId) {
        try {
            const device = await window.api.getDevice(deviceId);
            if (device) {
                // Use the app's edit modal functionality
                if (window.app && window.app.editDevice) {
                    window.app.editDevice(deviceId);
                } else {
                    // Fallback: show device edit modal
                    window.ui.showModal('deviceEditModal');
                    // Populate edit form
                    this.populateEditForm(device);
                }
            }
        } catch (error) {
            window.ui.showError('Cihaz düzenleme açılırken hata oluştu: ' + error.message);
        }
    }

    populateEditForm(device) {
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
                            <label for="edit-mac-address">MAC Adresi:</label>
                            <input type="text" id="edit-mac-address" class="form-control" value="${device.macAddress || ''}" placeholder="Örn: AA:BB:CC:DD:EE:FF" readonly>
                            <small class="text-muted">MAC adresi sistem tarafından otomatik belirlenir</small>
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label for="edit-location">Konum:</label>
                            <input type="text" id="edit-location" class="form-control" value="${device.location || ''}" placeholder="Örn: İkinci Kat - Muhasebe">
                        </div>
                        <div class="form-group">
                            <label for="edit-status">Durum:</label>
                            <select id="edit-status" class="form-control">
                                <option value="0" ${device.status === 0 ? 'selected' : ''}>Aktif</option>
                                <option value="1" ${device.status === 1 ? 'selected' : ''}>Pasif</option>
                                <option value="2" ${device.status === 2 ? 'selected' : ''}>Bakım</option>
                                <option value="3" ${device.status === 3 ? 'selected' : ''}>Arızalı</option>
                            </select>
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

    async confirmDeleteDevice(deviceId, deviceName) {
        if (confirm(`"${deviceName}" cihazını silmek istediğinizden emin misiniz?\n\nBu işlem geri alınamaz ve cihazın tüm verileri silinecektir.`)) {
            await this.deleteDevice(deviceId);
        }
    }

    async deleteDevice(deviceId) {
        try {
            window.ui.showLoading();
            await window.api.deleteDevice(deviceId);
            window.ui.hideLoading();
            
            window.ui.showSuccess('Cihaz başarıyla silindi');
            
            // Redirect to devices list after a short delay
            setTimeout(() => {
                window.location.href = '/Devices';
            }, 1500);
            
        } catch (error) {
            window.ui.hideLoading();
            window.ui.showError('Cihaz silinirken hata oluştu: ' + error.message);
        }
    }

    // Utility functions for rendering
    getStatusBadge(status) {
        const statusMap = {
            0: '<span class="badge badge-success">Çevrimiçi</span>',
            1: '<span class="badge badge-danger">Çevrimdışı</span>',
            2: '<span class="badge badge-warning">Bakım</span>',
            3: '<span class="badge badge-danger">Arızalı</span>'
        };
        return statusMap[status] || '<span class="badge badge-secondary">Bilinmiyor</span>';
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

    getDiscoveryMethodText(discoveryMethod) {
        const methods = {
            0: 'Bilinmiyor',
            1: 'Ağ Keşfi',
            2: 'Ajan',
            3: 'Manuel',
            4: 'İçe Aktarma'
        };
        return methods[discoveryMethod] || 'Bilinmiyor';
    }

    formatDateTime(dateString) {
        if (!dateString) return '--';
        
        try {
            const date = new Date(dateString);
            return date.toLocaleString('tr-TR', {
                timeZone: 'Europe/Istanbul',
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        } catch (error) {
            return '--';
        }
    }

    // Show all software functionality
    showAllSoftware(deviceId) {
        const device = this.currentDevice;
        if (!device || !device.softwareInfo || !device.softwareInfo.installedApps) return;

        const container = document.getElementById(`software-list-${deviceId}`);
        const loadMoreContainer = container?.parentElement.querySelector('.software-load-more');

        if (!container) return;

        // Clear current list and show all software
        container.innerHTML = '';
        
        device.softwareInfo.installedApps.forEach(app => {
            const softwareItem = document.createElement('div');
            softwareItem.className = 'software-item';
            softwareItem.innerHTML = `
                <i class="bi bi-app"></i>
                <span class="software-name">${app}</span>
            `;
            container.appendChild(softwareItem);
        });

        // Hide the load more button
        if (loadMoreContainer) {
            loadMoreContainer.style.display = 'none';
        }
    }

    // Load more software functionality (kept for backward compatibility)
    loadMoreSoftware(deviceId, totalCount) {
        // This function is now replaced by showAllSoftware
        this.showAllSoftware(deviceId);
    }
}

// Initialize device details manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.deviceDetails = new DeviceDetailsManager();
});