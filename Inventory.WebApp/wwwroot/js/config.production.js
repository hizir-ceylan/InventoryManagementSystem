// Production Configuration for Inventory Management System Web App
window.INVENTORY_CONFIG = {
    // API Configuration
    API_BASE_URL: window.location.protocol === 'https:' ? 'https://localhost:7093' : 'http://localhost:5093',
    
    // Production API URL - Bu değeri gerçek sunucu kurulumunda güncelleyin
    PRODUCTION_API_URL: 'https://api.company.gov.tr', // Gerçek API domain'inizi buraya yazın
    
    // Auto-detect API URL based on environment
    getApiUrl: function() {
        // In production, use configured production URL
        if (this.PRODUCTION_API_URL && window.location.hostname !== 'localhost') {
            return this.PRODUCTION_API_URL;
        }
        
        // For development/testing, use localhost
        return this.API_BASE_URL;
    },
    
    // Application settings
    AUTO_REFRESH_INTERVAL: 30000, // 30 seconds
    
    // Company branding
    COMPANY_NAME: 'Çaykur',
    APP_TITLE: 'Envanter Yönetim Sistemi',
    
    // Production specific settings
    ENABLE_DEBUG_MODE: false,
    ENABLE_MOCK_DATA: false, // Production'da mock data devre dışı
    
    // Security settings
    ENABLE_HTTPS_ONLY: true,
    
    // Performance settings
    REQUEST_TIMEOUT: 15000, // 15 seconds
    MAX_RETRY_ATTEMPTS: 3
};