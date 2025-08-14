// Shared Statistics Module
class StatisticsManager {
    constructor() {
        this.apiBaseUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
        this.refreshInterval = null;
        this.lastStats = null;
        this.init();
    }

    init() {
        // Auto-refresh statistics every 30 seconds
        this.startAutoRefresh();
        
        // Load initial statistics
        this.loadStatistics();
    }

    startAutoRefresh() {
        // Clear existing interval if any
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }

        // Set up new interval
        this.refreshInterval = setInterval(() => {
            this.loadStatistics(true); // Silent refresh
        }, window.INVENTORY_CONFIG?.AUTO_REFRESH_INTERVAL || 30000);
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    async loadStatistics(silent = false) {
        try {
            if (!silent && window.ui) {
                window.ui.showLoading();
            }

            // Load all devices to calculate statistics
            const allDevicesResponse = await fetch(`${this.apiBaseUrl}/api/Device`);
            let allDevices = [];
            
            if (allDevicesResponse.ok) {
                allDevices = await allDevicesResponse.json();
            }

            // Calculate statistics
            const stats = this.calculateStatistics(allDevices);
            
            // Load update statistics
            await this.loadUpdateStatistics(stats);

            // Update the display
            this.updateStatisticsDisplay(stats);

            // Store for future reference
            this.lastStats = stats;

            if (!silent && window.ui) {
                window.ui.hideLoading();
                window.ui.updateLastUpdateTime();
            }

        } catch (error) {
            console.warn('Could not load statistics:', error);
            if (!silent && window.ui) {
                window.ui.showError('İstatistikler yüklenirken hata oluştu: ' + error.message);
                window.ui.hideLoading();
            }
        }
    }

    calculateStatistics(devices) {
        const totalDevices = devices?.length || 0;
        
        // Active devices are those with status 0 (Online)
        const activeDevices = devices?.filter(d => d.status === 0).length || 0;
        
        // Calculate other stats if needed
        const offlineDevices = devices?.filter(d => d.status === 1).length || 0;
        const unknownDevices = devices?.filter(d => d.status === 2).length || 0;

        return {
            totalDevices,
            activeDevices,
            offlineDevices,
            unknownDevices,
            updateDevices: 0 // Will be filled by loadUpdateStatistics
        };
    }

    async loadUpdateStatistics(stats) {
        try {
            // Try the statistics endpoint first
            const response = await fetch(`${this.apiBaseUrl}/api/Update/statistics`);
            if (response.ok) {
                const updateStats = await response.json();
                stats.updateDevices = updateStats.availableCount || 0;
            } else {
                // Fallback: count devices with available updates
                const updatesResponse = await fetch(`${this.apiBaseUrl}/api/Update/available`);
                if (updatesResponse.ok) {
                    const updates = await updatesResponse.json();
                    const deviceIds = new Set(updates.map(u => u.deviceId));
                    stats.updateDevices = deviceIds.size;
                } else {
                    stats.updateDevices = 0;
                }
            }
        } catch (error) {
            console.warn('Could not load update statistics:', error);
            stats.updateDevices = 0;
        }
    }

    updateStatisticsDisplay(stats) {
        // Update total devices
        const totalElement = document.getElementById('total-devices');
        if (totalElement) {
            totalElement.textContent = stats.totalDevices;
        }

        // Update active devices
        const activeElement = document.getElementById('active-devices');
        if (activeElement) {
            activeElement.textContent = stats.activeDevices;
        }

        // Update devices with available updates
        const updateElement = document.getElementById('update-devices');
        if (updateElement) {
            updateElement.textContent = stats.updateDevices;
        }

        // Dispatch event for other components to listen to
        window.dispatchEvent(new CustomEvent('statisticsUpdated', {
            detail: stats
        }));
    }

    // Get current statistics
    getCurrentStats() {
        return this.lastStats;
    }

    // Force refresh statistics
    async refresh() {
        await this.loadStatistics(false);
    }

    // Handle stat card clicks
    handleStatCardClick(type) {
        switch (type) {
            case 'total':
                this.navigateToDevices();
                break;
            case 'active':
                this.navigateToDevices('status=online');
                break;
            case 'updates':
                this.navigateToDevices('updates=available');
                break;
            default:
                this.navigateToDevices();
        }
    }

    navigateToDevices(filter = null) {
        let url = '/Devices';
        if (filter) {
            url += '?' + filter;
        }
        window.location.href = url;
    }

    // Create statistics HTML for inclusion in pages
    createStatisticsHTML() {
        return `
            <div id="stats-cards" class="stats-section">
                <div class="container">
                    <div class="stats-grid">
                        <div class="stat-card primary-card" onclick="window.statistics.handleStatCardClick('total')">
                            <div class="stat-content">
                                <div class="stat-info">
                                    <h3 class="stat-title">Toplam Cihaz</h3>
                                    <h2 class="stat-number" id="total-devices">-</h2>
                                    <p class="stat-description">Kayıtlı cihazlar</p>
                                </div>
                                <div class="stat-icon">
                                    <i class="bi bi-laptop"></i>
                                    <div class="icon-bg"></div>
                                </div>
                            </div>
                            <div class="stat-waves">
                                <div class="wave wave1"></div>
                                <div class="wave wave2"></div>
                            </div>
                        </div>

                        <div class="stat-card success-card" onclick="window.statistics.handleStatCardClick('active')">
                            <div class="stat-content">
                                <div class="stat-info">
                                    <h3 class="stat-title">Aktif Cihazlar</h3>
                                    <h2 class="stat-number" id="active-devices">-</h2>
                                    <p class="stat-description">Çalışır durumda</p>
                                </div>
                                <div class="stat-icon">
                                    <i class="bi bi-check-circle"></i>
                                    <div class="icon-bg"></div>
                                </div>
                            </div>
                            <div class="stat-waves">
                                <div class="wave wave1"></div>
                                <div class="wave wave2"></div>
                            </div>
                        </div>

                        <div class="stat-card update-card" onclick="window.statistics.handleStatCardClick('updates')">
                            <div class="stat-content">
                                <div class="stat-info">
                                    <h3 class="stat-title">Güncelleme Mevcut</h3>
                                    <h2 class="stat-number" id="update-devices">-</h2>
                                    <p class="stat-description">Güncellenmesi gereken</p>
                                </div>
                                <div class="stat-icon">
                                    <i class="bi bi-arrow-up-circle"></i>
                                    <div class="icon-bg"></div>
                                </div>
                            </div>
                            <div class="stat-waves">
                                <div class="wave wave1"></div>
                                <div class="wave wave2"></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
}

// Initialize statistics manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    if (!window.statistics) {
        window.statistics = new StatisticsManager();
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.statistics) {
        window.statistics.stopAutoRefresh();
    }
});