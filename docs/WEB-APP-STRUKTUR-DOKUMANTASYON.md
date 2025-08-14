# Çaykur Envanter Yönetim Sistemi - Web App Yapı Dokümantasyonu

Bu dokümantasyon, Inventory Management System'in web uygulaması yapısını detaylı olarak açıklar.

## 📁 **Genel Dizin Yapısı**

```
Inventory.WebApp/
├── Pages/                          # Razor Pages
│   ├── Shared/                     # Paylaşılan layout ve componentler
│   │   ├── _Layout.cshtml          # Ana layout (navbar, footer)
│   │   └── _DeviceModals.cshtml    # Cihaz modalleri (düzenle, ekle, detay)
│   ├── Index.cshtml                # Ana sayfa (dashboard)
│   ├── Devices.cshtml              # Cihazlar listesi sayfası
│   ├── DeviceDetails.cshtml        # Cihaz detayları sayfası
│   ├── NetworkScan.cshtml          # Ağ taraması sayfası
│   ├── ChangeLogs.cshtml           # Değişiklik logları sayfası
│   └── VMwareStatus.cshtml         # VMware durumu sayfası
├── wwwroot/                        # Static dosyalar
│   ├── css/
│   │   └── style.css               # Ana CSS dosyası (Çay teması)
│   ├── js/
│   │   ├── app.js                  # Ana uygulama logic'i (eski tek dosya sistem)
│   │   ├── devices.js              # Cihaz listesi JavaScript'i
│   │   ├── config.js               # Uygulama konfigürasyonu
│   │   └── modules/                # Modüler JavaScript dosyaları
│   │       ├── navigation.js       # Navigasyon yönetimi
│   │       ├── api.js              # API çağrıları
│   │       ├── ui.js               # UI yardımcı fonksiyonları
│   │       ├── statistics.js       # İstatistik hesaplamaları
│   │       ├── devices.js          # Cihaz işlemleri modülü
│   │       ├── device-details.js   # Cihaz detayları modülü
│   │       └── network-scan.js     # Ağ taraması modülü
│   └── lib/                        # Third-party kütüphaneler
└── Program.cs                      # Uygulama başlangıç noktası
```

## 🎨 **UI Temaları ve Stil Sistemi**

### Ana Tema: Çay Teması (Tea Theme)
- **Ana Renk:** `--tea-green: #2d5016` (Koyu yeşil)
- **Vurgu Rengi:** `--tea-gold: #d4af37` (Altın sarısı)
- **Arka Plan:** `--tea-cream: #f5f5dc` (Krem)

### CSS Organizasyonu (`style.css`)
1. **CSS Değişkenleri ve Renk Paleti** (Satır 1-80)
2. **Navbar ve Navigasyon** (Satır 81-360)
3. **Butonlar ve Form Elementleri** (Satır 361-600)
4. **Kartlar ve Componentler** (Satır 601-1000)
5. **Tablolar ve Listeler** (Satır 1001-1600)
6. **Responsive Tasarım** (Satır 1601-3500)

## 📄 **Sayfa Yapıları ve İçerikleri**

### 1. Ana Sayfa (`Index.cshtml`)
**Sorumlu Dosyalar:**
- **Layout:** `_Layout.cshtml`
- **JavaScript:** `app-modular.js` + modüller
- **CSS:** `style.css`

**İçerik:**
- Dashboard istatistik kartları
- Hızlı erişim butonları
- Son aktivite özeti

### 2. Cihazlar Sayfası (`Devices.cshtml`)
**Sorumlu Dosyalar:**
- **Layout:** `_Layout.cshtml`
- **Modals:** `_DeviceModals.cshtml`
- **JavaScript:** `devices.js`, `modules/devices.js`
- **CSS:** `.devices-table`, `.page-header-centered`

**İçerik:**
- Ortalanmış sayfa başlığı ve butonları
- Arama ve filtreleme araçları
- Cihaz listesi tablosu
- Cihaz ekleme/düzenleme modalleri

**Önemli CSS Sınıfları:**
- `.page-header-centered` - Ortalanmış başlık
- `.page-title-center` - Sayfa başlığı
- `.page-actions-center` - Aksiyon butonları
- `.devices-table` - Cihaz tablosu

### 3. Cihaz Detayları (`DeviceDetails.cshtml`)
**Sorumlu Dosyalar:**
- **Layout:** `_Layout.cshtml`
- **JavaScript:** `modules/device-details.js`
- **CSS:** `.device-details-header`, `.device-info-group`

**İçerik:**
- Cihaz temel bilgileri
- Donanım bilgileri
- Yazılım bilgileri  
- Güncelleme durumu
- Değişiklik geçmişi

**Güncelleme Sistemi:**
- API Endpoint: `/api/update/{deviceId}`
- Scan Endpoint: `/api/update/scan/{deviceId}`
- Gerçek zamanlı güncelleme kontrolü

### 4. Ağ Taraması (`NetworkScan.cshtml`)
**Sorumlu Dosyalar:**
- **Layout:** `_Layout.cshtml`
- **JavaScript:** `modules/network-scan.js`
- **CSS:** `.network-scan-content`, `.scan-config`

**İçerik:**
- Ağ aralığı konfigürasyonu
- Tarama ayarları (timeout, port)
- Tarama sonuçları tablosu
- Bulunan cihazları kaydetme

**Özellikler:**
- Otomatik ağ algılama
- Önceden tanımlı ağ aralıkları
- Real-time tarama progress'i

### 5. Değişiklik Logları (`ChangeLogs.cshtml`)
**Sorumlu Dosyalar:**
- **Layout:** `_Layout.cshtml`
- **CSS:** `.change-logs-table`

**İçerik:**
- Sistem değişiklik geçmişi
- Filtreleme ve arama
- Detaylı değişiklik bilgileri

## 🧭 **Navbar Yapısı (`_Layout.cshtml`)**

### Desktop Navbar
```html
<nav class="navbar">
    <div class="container">
        <!-- Sol: Brand -->
        <div class="navbar-left">
            <div class="navbar-brand">Çaykur Envanter Yönetim Sistemi</div>
        </div>
        
        <!-- Orta: Navigasyon Menüsü -->
        <div class="navbar-center">
            <ul class="navbar-nav">
                <li><a href="/">Ana Sayfa</a></li>
                <li><a href="/Devices">Cihazlar</a></li>
                <li><a href="/DeviceDetails">Cihaz Detayları</a></li>
                <li><a href="/NetworkScan">Ağ Taraması</a></li>
                <li><a href="/ChangeLogs">Değişiklik Logları</a></li>
                <li><a href="/VMwareStatus">VMware Durumu</a></li>
                <li><a onclick="openApiDocumentation()">API Dökümanları</a></li>
            </ul>
        </div>
        
        <!-- Sağ: Boş (Son güncelleme göstergesi kaldırıldı) -->
        <div class="navbar-right"></div>
    </div>
</nav>
```

### Mobile Navbar
- **Hamburger Menü:** Sağ üst köşede
- **Responsive:** 768px altında aktif
- **Animasyon:** Slide-down efekti

## 🧩 **Modal Yapıları (`_DeviceModals.cshtml`)**

### 1. Cihaz Detay Modalı (`deviceDetailModal`)
- Cihaz bilgilerini görüntüleme
- Salt okunur format

### 2. Cihaz Düzenleme Modalı (`deviceEditModal`)
- Cihaz bilgilerini düzenleme
- Form validasyonu
- API: `PUT /api/device/{id}`

### 3. Cihaz Ekleme Modalı (`deviceAddModal`)
- Boş cihaz oluşturma
- Placeholder cihazlar için
- API: `POST /api/device`

## ⚙️ **JavaScript Modül Sistemi**

### Ana Modüller

#### 1. `navigation.js`
**Fonksiyonlar:**
- `setupMobileMenu()` - Mobil menü kurulumu
- `highlightCurrentPage()` - Aktif sayfa vurgulama
- `navigateToDevices(filterType)` - Cihazlar sayfasına yönlendirme

#### 2. `api.js`
**Fonksiyonlar:**
- `apiCall(endpoint, options)` - Genel API çağrısı
- `getDevice(id)` - Tekil cihaz getirme
- `getDevices()` - Tüm cihazları getirme
- Base URL: `/api/`

#### 3. `ui.js`
**Fonksiyonlar:**
- `showModal(modalId)` - Modal açma
- `hideModal(modalId)` - Modal kapatma
- `showError(message)` - Hata mesajı
- `showSuccess(message)` - Başarı mesajı
- `showLoading()` / `hideLoading()` - Yükleme göstergesi

#### 4. `devices.js`
**Fonksiyonlar:**
- `loadDevices()` - Cihaz listesi yükleme
- `filterDevices()` - Cihaz filtreleme
- `editDevice(id)` - Cihaz düzenleme modalı
- `deleteDevice(id)` - Cihaz silme
- `refreshDevices()` - Liste yenileme

#### 5. `device-details.js`
**Fonksiyonlar:**
- `loadDeviceUpdates(deviceId)` - Güncelleme bilgileri
- `refreshUpdates(deviceId)` - Güncelleme taraması
- `editDevice(deviceId)` - Düzenleme modalı
- `populateEditForm(device)` - Form doldurma

#### 6. `network-scan.js`
**Fonksiyonlar:**
- `startNetworkScan()` - Ağ taraması başlatma
- `updateProgress(percent)` - Progress güncelleme
- `displayScanResults(results)` - Sonuçları gösterme

## 🎯 **API Entegrasyon Noktaları**

### Cihaz İşlemleri
- **GET** `/api/device` - Tüm cihazlar
- **GET** `/api/device/{id}` - Tekil cihaz
- **POST** `/api/device` - Yeni cihaz
- **PUT** `/api/device/{id}` - Cihaz güncelleme
- **DELETE** `/api/device/{id}` - Cihaz silme

### Güncelleme İşlemleri
- **GET** `/api/update/{deviceId}` - Cihaz güncellemeleri
- **POST** `/api/update/scan/{deviceId}` - Güncelleme taraması
- **GET** `/api/update/available` - Mevcut güncellemeler
- **GET** `/api/update/critical` - Kritik güncellemeler

### Ağ İşlemleri
- **POST** `/api/networkscan/start` - Ağ taraması başlat
- **GET** `/api/networkscan/status/{scanId}` - Tarama durumu
- **GET** `/api/networkscan/results/{scanId}` - Tarama sonuçları

## 📱 **Responsive Tasarım Breakpoint'leri**

### Desktop (>768px)
- Tam navbar görünümü
- Tüm tablo sütunları görünür
- Yan yana form elementleri

### Mobile (<768px)
- Hamburger menu
- Gizli tablo sütunları (`.hide-mobile`)
- Stack'lenmiş form elementleri
- Touch-friendly butonlar

## 🚀 **Performans Optimizasyonları**

### JavaScript
- Modüler yapı ile kod bölünmesi
- Lazy loading için hazır altyapı
- Event delegation kullanımı

### CSS
- CSS custom properties (variables)
- Efficient selector'lar
- Minimal media query'ler

### API
- Response caching
- Error handling
- Loading states

## 🔧 **Geliştirme ve Bakım Notları**

### Yeni Sayfa Ekleme
1. `Pages/` altında yeni `.cshtml` dosyası
2. Navbar'a link ekleme (`_Layout.cshtml`)
3. Gerekirse yeni JavaScript modülü
4. CSS stil ekleme

### Modal Ekleme
1. `_DeviceModals.cshtml`'e modal markup
2. JavaScript'te modal fonksiyonları
3. CSS stil ekleme

### API Endpoint Ekleme
1. Controller'da yeni method
2. JavaScript'te API çağrısı fonksiyonu
3. UI'da kullanım

Bu dokümantasyon, sistem mimarisini anlamak ve geliştirilmesine katkıda bulunmak için kapsamlı bir rehber sağlar.