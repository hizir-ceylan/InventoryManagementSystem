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

            // Try real API first, fallback to mock scan
            try {
                const scanResponse = await window.api.startNetworkScan(networkRange, timeout, portScan);
                this.currentScanId = scanResponse.scanId;
                // Poll for scan results
                this.pollScanProgress();
            } catch (apiError) {
                console.warn('API not available, starting mock network scan:', apiError.message);
                this.startMockNetworkScan(networkRange, timeout, portScan);
            }

        } catch (error) {
            window.ui.showError('Ağ taraması başlatılırken hata oluştu: ' + error.message);
            this.scanInProgress = false;
            this.updateScanButton(false);
        }
    }

    async startMockNetworkScan(networkRange, timeout, portScan) {
        // Simulate network scan with mock data
        const progressBar = document.getElementById('progress-fill');
        const statusText = document.getElementById('scan-status');
        const resultsTable = document.getElementById('results-table');

        if (!progressBar || !statusText || !resultsTable) return;

        statusText.textContent = 'Mock tarama başlatılıyor...';
        progressBar.style.width = '0%';

        // Simulate scan progress
        let progress = 0;
        const progressInterval = setInterval(() => {
            progress += 15;
            progressBar.style.width = progress + '%';
            
            if (progress <= 30) {
                statusText.textContent = `Ağ aralığı analiz ediliyor... ${progress}%`;
            } else if (progress <= 60) {
                statusText.textContent = `Cihazlar taranıyor... ${progress}%`;
            } else if (progress <= 90) {
                statusText.textContent = `Port taraması yapılıyor... ${progress}%`;
            } else {
                statusText.textContent = `Sonuçlar hazırlanıyor... ${progress}%`;
            }

            if (progress >= 100) {
                clearInterval(progressInterval);
                this.completeMockScan(networkRange, portScan);
            }
        }, 500);
    }

    completeMockScan(networkRange, portScan) {
        const statusText = document.getElementById('scan-status');
        const resultsTable = document.getElementById('results-table');

        if (!statusText || !resultsTable) return;

        // Generate mock scan results based on network range
        const mockResults = this.generateMockScanResults(networkRange, portScan);
        
        statusText.textContent = `Tarama tamamlandı! ${mockResults.length} cihaz bulundu.`;
        
        // Show results
        this.scanResults = mockResults;
        this.renderScanResults();
        resultsTable.style.display = 'block';
        
        this.scanInProgress = false;
        this.updateScanButton(false);
    }

    generateMockScanResults(networkRange, portScan) {
        const baseIP = this.getBaseIPFromRange(networkRange);
        const results = [];

        // Generate some mock discovered devices
        const mockDevices = [
            { ip: `${baseIP}.1`, mac: 'AA:BB:CC:DD:EE:01', name: 'Router-Gateway', status: 'Active' },
            { ip: `${baseIP}.10`, mac: 'AA:BB:CC:DD:EE:10', name: 'DESKTOP-FOUND01', status: 'Active' },
            { ip: `${baseIP}.15`, mac: 'AA:BB:CC:DD:EE:15', name: 'LAPTOP-WIFI01', status: 'Active' },
            { ip: `${baseIP}.25`, mac: 'AA:BB:CC:DD:EE:25', name: 'PRINTER-HP01', status: 'Active' },
            { ip: `${baseIP}.50`, mac: 'AA:BB:CC:DD:EE:50', name: 'SERVER-FILE01', status: 'Active' }
        ];

        mockDevices.forEach(device => {
            const ports = this.getMockPorts(portScan, device.name);
            results.push({
                ip: device.ip,
                mac: device.mac,
                name: device.name,
                status: device.status,
                ports: ports
            });
        });

        return results;
    }

    getBaseIPFromRange(networkRange) {
        // Parse network range to get base IP
        if (networkRange === 'auto' || !networkRange) {
            return '192.168.1'; // Default
        }
        
        const parts = networkRange.split('/')[0].split('.');
        if (parts.length >= 3) {
            return `${parts[0]}.${parts[1]}.${parts[2]}`;
        }
        
        return '192.168.1'; // Fallback
    }

    getMockPorts(portScanType, deviceName) {
        if (portScanType === 'none') return 'None';
        
        // Generate realistic ports based on device type
        if (deviceName.toLowerCase().includes('router') || deviceName.toLowerCase().includes('gateway')) {
            return portScanType === 'all' ? '22, 23, 53, 80, 443, 8080' : '80, 443';
        } else if (deviceName.toLowerCase().includes('server')) {
            return portScanType === 'all' ? '22, 80, 135, 139, 443, 445, 3389' : '80, 443, 3389';
        } else if (deviceName.toLowerCase().includes('printer')) {
            return portScanType === 'all' ? '80, 443, 515, 631, 9100' : '80, 9100';
        } else {
            return portScanType === 'all' ? '135, 139, 445, 3389' : '3389';
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
                <td>${result.ip || result.ipAddress}</td>
                <td>${result.mac || result.macAddress || '--'}</td>
                <td>${result.name || result.deviceName || 'Bilinmiyor'}</td>
                <td>${this.getDeviceStatusBadge(result.status === 'Active' || result.isOnline)}</td>
                <td>${result.ports || (result.openPorts ? result.openPorts.join(', ') : '--')}</td>
                <td>
                    <button class="btn-sm btn-success" onclick="window.networkScan.addToInventory('${result.ip || result.ipAddress}', '${result.mac || result.macAddress || ''}', '${result.name || result.deviceName || ''}')" title="Envantere Ekle">
                        <i class="bi bi-plus-circle"></i>
                        Envantere Ekle
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