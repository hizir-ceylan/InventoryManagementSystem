#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

// Page configurations
const pages = {
    'index': {
        title: 'Ana Sayfa',
        activeNav: 'INDEX_ACTIVE',
        scripts: '<script src="js/modules/dashboard.js"></script>'
    },
    'devices': {
        title: 'Cihazlar',
        activeNav: 'DEVICES_ACTIVE',
        scripts: '<script src="js/modules/devices.js"></script>'
    },
    'network-scan': {
        title: 'Ağ Taraması',
        activeNav: 'NETWORK_SCAN_ACTIVE',
        scripts: '<script src="js/modules/network-scan.js"></script>'
    },
    'change-logs': {
        title: 'Değişiklik Logları',
        activeNav: 'CHANGE_LOGS_ACTIVE',
        scripts: '<script src="js/modules/change-logs.js"></script>'
    },
    'device-details': {
        title: 'Cihaz Detayları',
        activeNav: 'DEVICE_DETAILS_ACTIVE',
        scripts: '<script src="js/modules/device-details.js"></script>'
    },
    'vmware-status': {
        title: 'VMware Durumu',
        activeNav: 'VMWARE_ACTIVE',
        scripts: '<script src="js/modules/vmware.js"></script>'
    }
};

function generatePage(pageName) {
    try {
        // Read template
        const templatePath = path.join(__dirname, 'base-template.html');
        let template = fs.readFileSync(templatePath, 'utf8');

        // Read page content
        const contentPath = path.join(__dirname, 'pages', `${pageName}-content.html`);
        let content = '';
        if (fs.existsSync(contentPath)) {
            content = fs.readFileSync(contentPath, 'utf8');
        } else {
            content = `<div class="page-card"><h2>Page content for ${pageName} not found</h2></div>`;
        }

        const config = pages[pageName] || { title: pageName, activeNav: '', scripts: '' };

        // Replace placeholders
        template = template.replace('{{PAGE_TITLE}}', config.title);
        template = template.replace('{{PAGE_CONTENT}}', content);
        template = template.replace('{{PAGE_SCRIPTS}}', config.scripts);

        // Set active navigation
        Object.keys(pages).forEach(page => {
            const activeClass = pages[page].activeNav;
            template = template.replace(`{{${activeClass}}}`, page === pageName ? 'active' : '');
        });

        // Clean up any remaining placeholders
        template = template.replace(/\{\{[^}]+\}\}/g, '');

        // Write page
        const outputPath = path.join(__dirname, `${pageName}.html`);
        fs.writeFileSync(outputPath, template);
        
        console.log(`Generated: ${pageName}.html`);
        return true;
    } catch (error) {
        console.error(`Error generating ${pageName}:`, error.message);
        return false;
    }
}

// Generate all pages or specific page
const targetPage = process.argv[2];
if (targetPage) {
    generatePage(targetPage);
} else {
    Object.keys(pages).forEach(generatePage);
}