// UI Utilities Module
class UIManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupModals();
        this.updateLastUpdateTime();
        this.startAutoRefresh();
    }

    // Loading indicator methods
    showLoading() {
        const loading = document.getElementById('loading');
        if (loading) {
            loading.classList.remove('d-none');
        }
    }

    hideLoading() {
        const loading = document.getElementById('loading');
        if (loading) {
            loading.classList.add('d-none');
        }
    }

    // Error handling methods
    showError(message, duration = 5000) {
        const errorAlert = document.getElementById('error-alert');
        const errorMessage = document.getElementById('error-message');
        
        if (errorAlert && errorMessage) {
            errorMessage.textContent = message;
            errorAlert.classList.remove('d-none');
            
            // Auto-hide after duration
            if (duration > 0) {
                setTimeout(() => this.hideError(), duration);
            }
        }
        
        console.error('UI Error:', message);
    }

    showSuccess(message, duration = 3000) {
        // Create success alert if it doesn't exist
        let successAlert = document.getElementById('success-alert');
        if (!successAlert) {
            successAlert = document.createElement('div');
            successAlert.id = 'success-alert';
            successAlert.className = 'success-alert';
            successAlert.innerHTML = `
                <div class="success-content">
                    <i class="bi bi-check-circle"></i>
                    <span id="success-message"></span>
                    <button type="button" class="success-close" onclick="window.ui.hideSuccess()">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `;
            document.body.appendChild(successAlert);
        }
        
        const successMessage = document.getElementById('success-message');
        if (successMessage) {
            successMessage.textContent = message;
            successAlert.classList.remove('d-none');
            
            // Auto-hide after duration
            if (duration > 0) {
                setTimeout(() => this.hideSuccess(), duration);
            }
        }
        
        console.log('UI Success:', message);
    }

    hideError() {
        const errorAlert = document.getElementById('error-alert');
        if (errorAlert) {
            errorAlert.classList.add('d-none');
        }
    }

    hideSuccess() {
        const successAlert = document.getElementById('success-alert');
        if (successAlert) {
            successAlert.classList.add('d-none');
        }
    }

    // Modal methods
    setupModals() {
        // Close modal when clicking on overlay
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal-overlay')) {
                this.closeModal();
            }
        });

        // Close modal with ESC key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeModal();
            }
        });
    }

    showModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('show');
            document.body.style.overflow = 'hidden';
        }
    }

    closeModal() {
        const modals = document.querySelectorAll('.modal-overlay');
        modals.forEach(modal => {
            modal.classList.remove('show');
        });
        document.body.style.overflow = '';
    }

    // Statistics update methods
    updateStatistics(stats) {
        const elements = {
            'total-devices': stats.totalDevices || 0,
            'active-devices': stats.activeDevices || 0,
            'update-devices': stats.devicesNeedingUpdates || 0,
            'virtual-devices': stats.virtualDevices || 0
        };

        Object.entries(elements).forEach(([id, value]) => {
            const element = document.getElementById(id);
            if (element) {
                element.textContent = value;
            }
        });
    }

    // Format date/time
    formatDateTime(dateString) {
        if (!dateString) return '--';
        
        const date = new Date(dateString);
        return date.toLocaleString('tr-TR', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    formatDate(dateString) {
        if (!dateString) return '--';
        
        const date = new Date(dateString);
        return date.toLocaleDateString('tr-TR');
    }

    // Device status formatting
    getStatusBadge(status) {
        const statusMap = {
            0: { text: 'Aktif', class: 'status-active' },
            1: { text: 'Pasif', class: 'status-inactive' },
            2: { text: 'Bakım', class: 'status-maintenance' },
            3: { text: 'Arızalı', class: 'status-error' }
        };
        
        const statusInfo = statusMap[status] || { text: 'Bilinmiyor', class: 'status-unknown' };
        return `<span class="device-status ${statusInfo.class}">${statusInfo.text}</span>`;
    }

    // Device type formatting
    getDeviceTypeText(type) {
        const typeMap = {
            0: 'Bilinmiyor', 1: 'Laptop', 2: 'Masaüstü', 3: 'Sunucu',
            4: 'Yazıcı', 5: 'Tarayıcı', 6: 'Kamera', 7: 'IP Telefon',
            8: 'Ağ Cihazı', 9: 'Router', 10: 'Switch', 11: 'Access Point',
            12: 'Depolama', 13: 'Tablet', 14: 'Akıllı Telefon', 15: 'Akıllı TV',
            16: 'Projektör/Ekran', 17: 'Sanal Makine', 18: 'Diğer', 'virtual': 'Sanal Cihaz'
        };
        
        return typeMap[type] || 'Bilinmiyor';
    }

    // Discovery method formatting
    getDiscoveryMethodText(method) {
        const methodMap = {
            'agent': 'Ajan',
            'network': 'Ağ',
            'vmware': 'VMware',
            'manual': 'Manuel',
            'import': 'İçe Aktarım'
        };
        
        return methodMap[method] || method || '--';
    }

    // Update last update time
    updateLastUpdateTime() {
        const now = new Date();
        const timeString = now.toLocaleTimeString('tr-TR');
        
        const elements = ['last-update', 'last-update-mobile'];
        elements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.textContent = `Son güncelleme: ${timeString}`;
            }
        });
    }

    // Auto-refresh setup
    startAutoRefresh() {
        // Update time every minute
        setInterval(() => {
            this.updateLastUpdateTime();
        }, 60000);
    }

    // Table utilities
    createActionButtons(deviceId, deviceName) {
        return `
            <div class="action-buttons">
                <button class="btn-sm btn-info" onclick="viewDeviceDetails('${deviceId}')" title="Detayları Görüntüle">
                    <i class="bi bi-eye"></i>
                </button>
                <button class="btn-sm btn-warning" onclick="editDevice('${deviceId}')" title="Düzenle">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn-sm btn-secondary" onclick="window.location.href='/ChangeLogs?deviceId=${deviceId}'" title="Değişiklik Logları">
                    <i class="bi bi-journal-text"></i>
                </button>
                <button class="btn-sm btn-danger" onclick="confirmDeleteDevice('${deviceId}', '${deviceName}')" title="Sil">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        `;
    }

    // Show/hide no data messages
    showNoDataMessage(containerId, message = 'Veri bulunamadı') {
        const container = document.getElementById(containerId);
        if (container) {
            container.classList.remove('d-none');
        }
    }

    hideNoDataMessage(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.classList.add('d-none');
        }
    }
}

// Global UI functions for backward compatibility
function hideError() {
    if (window.ui) {
        window.ui.hideError();
    }
}

function closeModal() {
    if (window.ui) {
        window.ui.closeModal();
    }
}

// Initialize UI manager
window.ui = new UIManager();