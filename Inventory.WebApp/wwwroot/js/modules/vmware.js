// VMware Module
class VMwareManager {
    constructor() {
        this.serverStatus = null;
        this.virtualMachines = [];
        this.metrics = null;
        this.init();
    }

    init() {
        this.loadVMwareData();
        this.setupAutoRefresh();
    }

    async loadVMwareData() {
        await Promise.all([
            this.loadServerStatus(),
            this.loadVirtualMachines(),
            this.loadMetrics()
        ]);
    }

    async loadServerStatus() {
        try {
            const status = await window.api.getVMwareStatus();
            this.serverStatus = status;
            this.updateServerStatusUI();
        } catch (error) {
            this.updateServerStatusUI(false, error.message);
        }
    }

    async loadVirtualMachines() {
        try {
            window.ui.showLoading();
            const vms = await window.api.getVirtualMachines();
            this.virtualMachines = vms || [];
            this.renderVirtualMachines();
            window.ui.hideLoading();
        } catch (error) {
            window.ui.showError('Sanal makineler yüklenirken hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
    }

    async loadMetrics() {
        try {
            if (this.serverStatus && this.serverStatus.connected) {
                this.updateMetricsUI(this.serverStatus.metrics);
            }
        } catch (error) {
            console.warn('VMware metrics could not be loaded:', error.message);
        }
    }

    updateServerStatusUI(connected = false, errorMessage = null) {
        const elements = {
            'server-address': document.getElementById('server-address'),
            'connection-status': document.getElementById('connection-status'),
            'last-sync': document.getElementById('last-sync')
        };

        if (elements['server-address']) {
            elements['server-address'].textContent = this.serverStatus?.serverAddress || '10.0.0.10';
        }

        if (elements['connection-status']) {
            if (connected || (this.serverStatus && this.serverStatus.connected)) {
                elements['connection-status'].innerHTML = `
                    <span class="status-indicator online"></span>
                    Bağlı
                `;
            } else {
                elements['connection-status'].innerHTML = `
                    <span class="status-indicator offline"></span>
                    ${errorMessage || 'Bağlantı yok'}
                `;
            }
        }

        if (elements['last-sync']) {
            elements['last-sync'].textContent = this.serverStatus?.lastSync ? 
                window.ui.formatDateTime(this.serverStatus.lastSync) : '--';
        }
    }

    updateMetricsUI(metrics) {
        if (!metrics) return;

        const metricElements = {
            'total-storage': `${metrics.totalStorageGB || '--'} GB`,
            'used-storage': `${metrics.usedStorageGB || '--'} GB`,
            'free-storage': `${metrics.freeStorageGB || '--'} GB`,
            'cpu-usage': `${metrics.cpuUsagePercent || '--'}%`,
            'memory-usage': `${metrics.memoryUsagePercent || '--'}%`,
            'active-vms': metrics.activeVMs || '--'
        };

        Object.entries(metricElements).forEach(([id, value]) => {
            const element = document.getElementById(id);
            if (element) {
                element.textContent = value;
            }
        });
    }

    renderVirtualMachines() {
        const tableBody = document.getElementById('vm-table-body');
        const noVmsDiv = document.getElementById('no-vms');

        if (!tableBody) return;

        if (this.virtualMachines.length === 0) {
            tableBody.innerHTML = '';
            window.ui.showNoDataMessage('no-vms');
            return;
        }

        window.ui.hideNoDataMessage('no-vms');

        tableBody.innerHTML = this.virtualMachines.map(vm => `
            <tr>
                <td>
                    <div class="vm-name">
                        <strong>${vm.name || 'Bilinmeyen VM'}</strong>
                        <span class="vm-badge">VM</span>
                    </div>
                </td>
                <td>${vm.ipAddress || '--'}</td>
                <td>${this.getVMStatusBadge(vm.powerState)}</td>
                <td class="hide-mobile">${vm.guestOS || '--'}</td>
                <td class="hide-mobile">${vm.cpuCount || '--'}</td>
                <td class="hide-mobile">${vm.memoryGB || '--'}</td>
                <td class="hide-mobile">${vm.diskGB || '--'}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn-sm btn-info" onclick="vmware.viewVMDetails('${vm.id}')" title="VM Detayları">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button class="btn-sm btn-success" onclick="vmware.addToInventory('${vm.id}')" title="Envantere Ekle">
                            <i class="bi bi-plus-circle"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    getVMStatusBadge(powerState) {
        const statusMap = {
            'poweredOn': { text: 'Açık', class: 'status-active' },
            'poweredOff': { text: 'Kapalı', class: 'status-inactive' },
            'suspended': { text: 'Askıda', class: 'status-maintenance' }
        };
        
        const statusInfo = statusMap[powerState] || { text: 'Bilinmiyor', class: 'status-unknown' };
        return `<span class="device-status ${statusInfo.class}">${statusInfo.text}</span>`;
    }

    async syncVirtualMachines() {
        try {
            window.ui.showLoading();
            const result = await window.api.syncVirtualMachines();
            await this.loadVirtualMachines();
            window.ui.showSuccess(`${result.syncedCount || 0} sanal makine senkronize edildi.`);
            window.ui.hideLoading();
        } catch (error) {
            window.ui.showError('Sanal makineler senkronize edilirken hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
    }

    async testConnection() {
        try {
            window.ui.showLoading();
            const result = await window.api.testVMwareConnection();
            
            if (result.success) {
                window.ui.showSuccess('VMware sunucusuna başarıyla bağlanıldı!');
                await this.loadServerStatus();
            } else {
                window.ui.showError('VMware sunucusuna bağlanılamadı: ' + result.error);
            }
            
            window.ui.hideLoading();
        } catch (error) {
            window.ui.showError('Bağlantı testi sırasında hata oluştu: ' + error.message);
            window.ui.hideLoading();
        }
    }

    async saveConfiguration() {
        try {
            const config = {
                serverAddress: document.getElementById('vmware-server')?.value || '10.0.0.10',
                username: document.getElementById('vmware-username')?.value || '',
                password: document.getElementById('vmware-password')?.value || '',
                syncInterval: parseInt(document.getElementById('vmware-sync-interval')?.value) || 30
            };

            if (!config.username || !config.password) {
                window.ui.showError('Kullanıcı adı ve şifre gereklidir.');
                return;
            }

            await window.api.updateVMwareConfiguration(config);
            window.ui.showSuccess('VMware ayarları başarıyla kaydedildi.');
            
            // Clear password field for security
            const passwordField = document.getElementById('vmware-password');
            if (passwordField) passwordField.value = '';
            
        } catch (error) {
            window.ui.showError('Ayarlar kaydedilirken hata oluştu: ' + error.message);
        }
    }

    async viewVMDetails(vmId) {
        const vm = this.virtualMachines.find(v => v.id === vmId);
        if (vm) {
            // Store VM data and show details modal or navigate to details page
            sessionStorage.setItem('selectedVM', JSON.stringify(vm));
            // For now, just show an alert with VM details
            alert(`VM Detayları:\nAd: ${vm.name}\nDurum: ${vm.powerState}\nİşletim Sistemi: ${vm.guestOS}\nCPU: ${vm.cpuCount}\nRAM: ${vm.memoryGB} GB\nDisk: ${vm.diskGB} GB`);
        }
    }

    async addToInventory(vmId) {
        try {
            const vm = this.virtualMachines.find(v => v.id === vmId);
            if (!vm) return;

            const deviceData = {
                name: vm.name,
                ipAddress: vm.ipAddress,
                type: 'virtual',
                status: vm.powerState === 'poweredOn' ? 0 : 1,
                discoveryMethod: 'vmware',
                isVirtual: true,
                vmwareId: vm.id,
                model: vm.guestOS,
                location: 'VMware vSphere'
            };

            await window.api.createDevice(deviceData);
            window.ui.showSuccess(`${vm.name} sanal makinesi envantere eklendi.`);
            
        } catch (error) {
            window.ui.showError('Sanal makine envantere eklenirken hata oluştu: ' + error.message);
        }
    }

    async refreshData() {
        await this.loadVMwareData();
        window.ui.updateLastUpdateTime();
    }

    setupAutoRefresh() {
        // Refresh VMware data every 5 minutes
        setInterval(() => {
            this.loadVMwareData();
        }, 5 * 60 * 1000);
    }
}

// Initialize VMware manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.vmware = new VMwareManager();
});