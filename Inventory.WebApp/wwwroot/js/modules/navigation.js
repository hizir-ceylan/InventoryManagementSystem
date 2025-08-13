// Navigation Module for Multi-Page Application
class NavigationManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupMobileMenu();
        this.highlightCurrentPage();
    }

    // Setup mobile menu
    setupMobileMenu() {
        const toggle = document.getElementById('navbar-toggle');
        const menu = document.getElementById('navbar-menu');

        if (toggle && menu) {
            toggle.addEventListener('click', () => {
                menu.classList.toggle('show');
            });

            // Close menu when clicking on links
            menu.addEventListener('click', (e) => {
                if (e.target.classList.contains('nav-link')) {
                    menu.classList.remove('show');
                }
            });

            // Close menu when clicking outside
            document.addEventListener('click', (e) => {
                if (!toggle.contains(e.target) && !menu.contains(e.target)) {
                    menu.classList.remove('show');
                }
            });
        }
    }

    // Highlight current page in navigation
    highlightCurrentPage() {
        const currentPage = this.getCurrentPageName();
        const navLinks = document.querySelectorAll('.nav-link');
        
        navLinks.forEach(link => {
            link.classList.remove('active');
            const href = link.getAttribute('href');
            if (href && href.includes(currentPage)) {
                link.classList.add('active');
            }
        });
    }

    // Get current page name from URL
    getCurrentPageName() {
        const path = window.location.pathname;
        const filename = path.split('/').pop();
        return filename.replace('.html', '') || 'devices'; // default to devices
    }

    // Navigate to devices page with optional filter
    navigateToDevices(filterType = null) {
        const url = new URL('devices.html', window.location.origin);
        if (filterType) {
            url.searchParams.set('filter', filterType);
        }
        window.location.href = url.toString();
    }

    // Apply URL filters if present
    applyUrlFilters() {
        const urlParams = new URLSearchParams(window.location.search);
        const filter = urlParams.get('filter');
        
        if (filter && window.deviceManager) {
            window.deviceManager.applyFilter(filter);
        }
    }
}

// Global navigation functions for backward compatibility
function navigateToDevices(filterType) {
    if (window.navigation) {
        window.navigation.navigateToDevices(filterType);
    }
}

function openApiDocumentation() {
    const apiUrl = window.INVENTORY_CONFIG?.getApiUrl() || 'http://localhost:5093';
    window.open(`${apiUrl}/index.html`, '_blank');
}

// Initialize navigation when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.navigation = new NavigationManager();
});