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
        
        if (deviceId && !this.currentDevice) {
            this.loadDeviceById(deviceId);
        }
    }

    async loadDeviceById(deviceId) {
        try {
            window.ui.showLoading();
            const device = await window.api.getDevice(deviceId);
            this.currentDevice = device;
            this.renderDeviceDetails();
            window.ui.hideLoading();
        } catch (error) {
            window.ui.showError('Cihaz detayları yüklenirken hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
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
                    ${window.ui.getStatusBadge(device.status)}
                </div>
                <div class="device-actions">
                    <button class="btn-warning" onclick="editDevice('${device.id}')">
                        <i class="bi bi-pencil"></i>
                        Düzenle
                    </button>
                    <button class="btn-danger" onclick="confirmDeleteDevice('${device.id}', '${device.name}')">
                        <i class="bi bi-trash"></i>
                        Cihazı Sil
                    </button>
                    <button class="btn-secondary" onclick="window.location.href='devices.html'">
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
                            <span>${window.ui.getDeviceTypeText(device.type)}</span>
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
                            <span>${window.ui.getDiscoveryMethodText(device.discoveryMethod)}</span>
                        </div>
                        <div class="detail-item">
                            <strong>Son Görülme:</strong>
                            <span>${window.ui.formatDateTime(device.lastSeen)}</span>
                        </div>
                    </div>
                </div>

                ${this.renderHardwareInfo(device)}
                ${this.renderSoftwareInfo(device)}
                ${this.renderNetworkInfo(device)}
                ${this.renderVMwareInfo(device)}

                <div class="detail-section">
                    <h5><i class="bi bi-clock-history"></i> Değişiklik Geçmişi</h5>
                    <div class="change-history">
                        <button class="btn-info" onclick="window.location.href='change-logs.html?deviceId=${device.id}'">
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
        return `
            <div class="detail-section">
                <h5><i class="bi bi-cpu"></i> Donanım Bilgileri</h5>
                <div class="detail-grid">
                    <div class="detail-item">
                        <strong>İşlemci:</strong>
                        <span>${hw.cpu || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>RAM:</strong>
                        <span>${hw.totalRamGB ? hw.totalRamGB + ' GB' : '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Anakart:</strong>
                        <span>${hw.motherboard || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>BIOS:</strong>
                        <span>${hw.biosVersion || '--'}</span>
                    </div>
                </div>
            </div>
        `;
    }

    renderSoftwareInfo(device) {
        if (!device.softwareInfo) return '';

        const sw = device.softwareInfo;
        return `
            <div class="detail-section">
                <h5><i class="bi bi-laptop"></i> Yazılım Bilgileri</h5>
                <div class="detail-grid">
                    <div class="detail-item">
                        <strong>İşletim Sistemi:</strong>
                        <span>${sw.operatingSystem || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>OS Sürümü:</strong>
                        <span>${sw.osVersion || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Bilgisayar Adı:</strong>
                        <span>${sw.computerName || '--'}</span>
                    </div>
                    <div class="detail-item">
                        <strong>Kullanıcı:</strong>
                        <span>${sw.currentUser || '--'}</span>
                    </div>
                </div>
            </div>
        `;
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
}

// Initialize device details manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.deviceDetails = new DeviceDetailsManager();
});