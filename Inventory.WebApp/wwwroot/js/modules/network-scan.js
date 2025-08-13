// Network Scan Module
class NetworkScanManager {
    constructor() {
        this.scanResults = [];
        this.currentScanId = null;
        this.scanInProgress = false;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.updateNetworkRange();
    }

    setupEventListeners() {
        const rangePreset = document.getElementById('network-range-preset');
        if (rangePreset) {
            rangePreset.addEventListener('change', () => this.updateNetworkRange());
        }
    }

    updateNetworkRange() {
        const preset = document.getElementById('network-range-preset')?.value;
        const rangeInput = document.getElementById('network-range');
        
        if (rangeInput && preset) {
            rangeInput.value = preset;
        }
    }

    async startNetworkScan() {
        if (this.scanInProgress) return;

        try {
            const networkRange = document.getElementById('network-range')?.value || 'auto';
            const timeout = parseInt(document.getElementById('scan-timeout')?.value) || 5;
            const portScan = document.getElementById('port-scan')?.value || 'common';

            this.scanInProgress = true;
            this.updateScanButton(true);
            this.showScanResults();

            const scanResponse = await window.api.startNetworkScan(networkRange, timeout, portScan);
            this.currentScanId = scanResponse.scanId;

            // Poll for scan results
            this.pollScanProgress();

        } catch (error) {
            window.ui.showError('Ağ taraması başlatılırken hata oluştu: ' + error.message);
            this.scanInProgress = false;
            this.updateScanButton(false);
        }
    }

    async pollScanProgress() {
        if (!this.currentScanId) return;

        try {
            const status = await window.api.getNetworkScanStatus(this.currentScanId);
            this.updateProgressUI(status);

            if (status.completed) {
                const results = await window.api.getNetworkScanResults(this.currentScanId);
                this.scanResults = results || [];
                this.renderScanResults();
                this.scanInProgress = false;
                this.updateScanButton(false);
            } else {
                // Continue polling
                setTimeout(() => this.pollScanProgress(), 2000);
            }

        } catch (error) {
            window.ui.showError('Tarama durumu kontrol edilirken hata oluştu: ' + error.message);
            this.scanInProgress = false;
            this.updateScanButton(false);
        }
    }

    updateScanButton(scanning) {
        const button = document.getElementById('start-scan-btn');
        if (button) {
            if (scanning) {
                button.innerHTML = '<i class="bi bi-hourglass-split"></i> Tarama Devam Ediyor...';
                button.disabled = true;
            } else {
                button.innerHTML = '<i class="bi bi-play-fill"></i> Taramayı Başlat';
                button.disabled = false;
            }
        }
    }

    showScanResults() {
        const resultsDiv = document.getElementById('scan-results');
        if (resultsDiv) {
            resultsDiv.style.display = 'block';
        }
    }

    updateProgressUI(status) {
        const progressFill = document.getElementById('progress-fill');
        const scanStatus = document.getElementById('scan-status');

        if (progressFill) {
            const progress = status.progress || 0;
            progressFill.style.width = `${progress}%`;
        }

        if (scanStatus) {
            scanStatus.textContent = status.message || 'Tarama devam ediyor...';
        }
    }

    renderScanResults() {
        const tableBody = document.getElementById('scan-results-body');
        const resultsTable = document.getElementById('results-table');

        if (!tableBody) return;

        if (this.scanResults.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6" class="text-center">Hiç cihaz bulunamadı</td></tr>';
            if (resultsTable) resultsTable.style.display = 'block';
            return;
        }

        tableBody.innerHTML = this.scanResults.map(result => `
            <tr>
                <td>${result.ipAddress}</td>
                <td>${result.macAddress || '--'}</td>
                <td>${result.deviceName || 'Bilinmiyor'}</td>
                <td>${this.getDeviceStatusBadge(result.isOnline)}</td>
                <td>${result.openPorts ? result.openPorts.join(', ') : '--'}</td>
                <td>
                    <button class="btn-sm btn-success" onclick="networkScan.addToInventory('${result.ipAddress}', '${result.macAddress || ''}', '${result.deviceName || ''}')" title="Envantere Ekle">
                        <i class="bi bi-plus-circle"></i>
                    </button>
                </td>
            </tr>
        `).join('');

        if (resultsTable) resultsTable.style.display = 'block';
    }

    getDeviceStatusBadge(isOnline) {
        if (isOnline) {
            return '<span class="device-status status-active">Çevrimiçi</span>';
        } else {
            return '<span class="device-status status-inactive">Çevrimdışı</span>';
        }
    }

    async addToInventory(ipAddress, macAddress, deviceName) {
        try {
            const deviceData = {
                name: deviceName || `Cihaz-${ipAddress}`,
                ipAddress: ipAddress,
                macAddress: macAddress || '',
                type: 0, // Unknown
                status: 0, // Active
                discoveryMethod: 'network'
            };

            await window.api.createDevice(deviceData);
            window.ui.showSuccess(`${deviceData.name} envantere eklendi.`);
            
        } catch (error) {
            window.ui.showError('Cihaz envantere eklenirken hata oluştu: ' + error.message);
        }
    }
}

// Initialize network scan manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.networkScan = new NetworkScanManager();
});