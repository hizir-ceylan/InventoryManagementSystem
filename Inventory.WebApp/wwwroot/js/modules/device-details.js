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

                <div class="detail-section">
                    <h5><i class="bi bi-clock-history"></i> Değişiklik Geçmişi</h5>
                    <div class="change-history">
                        <button class="btn-info" onclick="window.location.href='/ChangeLogs?deviceId=${device.id}'">
                            <i class="bi bi-journal-text"></i>
                            Değişiklik Loglarını Görüntüle
                        </button>
                    </div>
                </div>
            </div>
        `;
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
                                <button class="btn-load-more" onclick="window.deviceDetails.loadMoreSoftware('${device.id}', ${sw.installedApps.length})">
                                    <i class="bi bi-chevron-down"></i>
                                    Daha fazla yazılım göster (${sw.installedApps.length - 20} kalan)
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

    renderVMwareInfo(device) {
        if (!device.isVirtual || !device.vmwareInfo) return '';

        const vm = device.vmwareInfo;
        return `
            <div class="detail-section">
                <h5><i class="bi bi-cloud"></i> VMware Bilgileri</h5>
                <div class="detail-grid">
                    <div class="detail-item">
                        <strong>VM Adı:</strong>
                        <span>${vm.name || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Güç Durumu:</strong>
                        <span>${vm.powerState || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>vCPU:</strong>
                        <span>${vm.cpuCount || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>vRAM:</strong>
                        <span>${vm.memoryGB ? vm.memoryGB + ' GB' : '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Disk Boyutu:</strong>
                        <span>${vm.diskGB ? vm.diskGB + ' GB' : '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>ESXi Host:</strong>
                        <span>${vm.hostName || '--'}</span>
                    </div>
                </div>
            </div>
        `;
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

    // Load more software functionality
    loadMoreSoftware(deviceId, totalCount) {
        const device = this.currentDevice;
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
            loadMoreBtn.setAttribute('onclick', `window.deviceDetails.loadMoreSoftware('${deviceId}', ${device.softwareInfo.installedApps.length})`);
        } else {
            loadMoreContainer.style.display = 'none';
        }
    }
}

// Initialize device details manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.deviceDetails = new DeviceDetailsManager();
});