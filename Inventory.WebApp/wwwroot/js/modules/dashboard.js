// Dashboard Module
class DashboardManager {
    constructor() {
        this.init();
    }

    init() {
        this.loadRecentActivity();
        this.checkSystemStatus();
        this.startAutoRefresh();
    }

    async loadRecentActivity() {
        try {
            // In a real implementation, this would load from an API
            // For now, we'll just update the timestamps
            this.updateActivityTimestamps();
        } catch (error) {
            console.warn('Could not load recent activity:', error.message);
        }
    }

    updateActivityTimestamps() {
        const activityItems = document.querySelectorAll('.activity-item small');
        // This is just a demo - in reality you'd load from API
        const times = ['2 dakika önce', '15 dakika önce', '1 saat önce'];
        activityItems.forEach((item, index) => {
            if (times[index]) {
                item.textContent = times[index];
            }
        });
    }

    async checkSystemStatus() {
        try {
            // Check API health
            await this.checkApiHealth();
            
            // Check VMware connection
            await this.checkVMwareStatus();
            
        } catch (error) {
            console.warn('System status check failed:', error.message);
        }
    }

    async checkApiHealth() {
        try {
            await window.api.healthCheck();
            // API is working if we get here
        } catch (error) {
            this.updateSystemStatus('api', false);
        }
    }

    async checkVMwareStatus() {
        try {
            const status = await window.api.getVMwareStatus();
            this.updateVMwareStatus(status.connected, status.error);
        } catch (error) {
            this.updateVMwareStatus(false, error.message);
        }
    }

    updateVMwareStatus(connected, errorMessage = null) {
        const statusIndicator = document.getElementById('vmware-connection-status');
        const statusText = document.getElementById('vmware-connection-text');

        if (statusIndicator) {
            if (connected) {
                statusIndicator.className = 'status-indicator online';
            } else {
                statusIndicator.className = 'status-indicator offline';
            }
        }

        if (statusText) {
            if (connected) {
                statusText.textContent = 'Bağlı';
            } else {
                statusText.textContent = errorMessage || 'Bağlantı yok';
            }
        }
    }

    updateSystemStatus(service, isOnline) {
        // This could be expanded to update various system status indicators
        console.log(`${service} status: ${isOnline ? 'online' : 'offline'}`);
    }

    startAutoRefresh() {
        // Refresh system status every 5 minutes
        setInterval(() => {
            this.checkSystemStatus();
        }, 5 * 60 * 1000);

        // Update activity timestamps every minute
        setInterval(() => {
            this.updateActivityTimestamps();
        }, 60 * 1000);
    }
}

// Initialize dashboard manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new DashboardManager();
});