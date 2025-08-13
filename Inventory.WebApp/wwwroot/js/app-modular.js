// Main Application - Simplified for modular architecture
class InventoryApp {
    constructor() {
        this.initialized = false;
        this.init();
    }

    async init() {
        if (this.initialized) return;
        
        try {
            // Wait for all modules to be available
            await this.waitForModules();
            
            // Load initial statistics for all pages
            await this.loadGlobalStatistics();
            
            this.initialized = true;
            console.log('Inventory Management System initialized successfully');
            
        } catch (error) {
            console.error('Failed to initialize application:', error);
            if (window.ui) {
                window.ui.showError('Uygulama başlatılırken hata oluştu: ' + error.message);
            }
        }
    }

    async waitForModules() {
        const maxWaitTime = 5000; // 5 seconds
        const checkInterval = 100; // 100ms
        let waitTime = 0;

        return new Promise((resolve, reject) => {
            const checkModules = () => {
                if (window.api && window.ui && window.navigation) {
                    resolve();
                    return;
                }

                waitTime += checkInterval;
                if (waitTime >= maxWaitTime) {
                    reject(new Error('Modules failed to load within timeout'));
                    return;
                }

                setTimeout(checkModules, checkInterval);
            };

            checkModules();
        });
    }

    async loadGlobalStatistics() {
        try {
            if (window.api) {
                const stats = await window.api.getStatistics();
                if (window.ui) {
                    window.ui.updateStatistics(stats);
                }
            }
        } catch (error) {
            console.warn('Could not load global statistics:', error.message);
        }
    }

    // Legacy compatibility methods - these will delegate to appropriate modules
    showError(message) {
        if (window.ui) {
            window.ui.showError(message);
        }
    }

    hideError() {
        if (window.ui) {
            window.ui.hideError();
        }
    }

    showLoading() {
        if (window.ui) {
            window.ui.showLoading();
        }
    }

    hideLoading() {
        if (window.ui) {
            window.ui.hideLoading();
        }
    }

    // Handle stat card clicks to navigate to devices with filter
    handleStatCardClick(filterType) {
        if (window.navigation) {
            window.navigation.navigateToDevices(filterType);
        }
    }

    // Network scan methods - delegate to network scan module
    updateNetworkRange() {
        if (window.networkScan) {
            window.networkScan.updateNetworkRange();
        }
    }

    startNetworkScan() {
        if (window.networkScan) {
            window.networkScan.startNetworkScan();
        }
    }

    // Device management methods - delegate to device manager
    showAddDeviceModal() {
        if (window.deviceManager) {
            window.deviceManager.showAddDeviceModal();
        }
    }
}

// Global app instance
let app;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    app = new InventoryApp();
});

// Global compatibility functions
function hideError() {
    if (app) app.hideError();
}

function showLoading() {
    if (app) app.showLoading();
}

function hideLoading() {
    if (app) app.hideLoading();
}