# Envanter Yönetim Sistemi - Modern React Frontend

Bu proje, Envanter Yönetim Sistemi için geliştirilmiş modern React tabanlı web uygulamasıdır.

## Özellikler

- ⚛️ **Modern React** - React 19 + TypeScript
- 🎨 **Tailwind CSS** - Modern ve responsive tasarım
- 🔄 **React Query** - Efficient data fetching ve caching
- 🛣️ **React Router** - Çoklu sayfa navigasyonu
- 📱 **Responsive Design** - Mobil ve desktop uyumlu
- 🌐 **API Integration** - RESTful API entegrasyonu
- 🔧 **TypeScript** - Type safety ve geliştirici deneyimi

## Kurulum

### Gereksinimler
- Node.js 18 veya üzeri
- npm veya yarn

### Başlangıç

1. **Bağımlılıkları yükleyin:**
   ```bash
   npm install
   ```

2. **Ortam değişkenlerini ayarlayın:**
   `.env` dosyasında API base URL'ini güncelleyin:
   ```
   VITE_API_BASE_URL=http://localhost:5093
   ```

3. **Geliştirme sunucusunu başlatın:**
   ```bash
   npm run dev
   ```

4. **Tarayıcıda açın:**
   http://localhost:5173

## Komutlar

- `npm run dev` - Geliştirme sunucusunu başlatır
- `npm run build` - Production build oluşturur
- `npm run preview` - Build edilen uygulamayı önizler
- `npm run lint` - ESLint ile kod kontrolü yapar
- `npm run type-check` - TypeScript tip kontrolü yapar

## Proje Yapısı

```
src/
├── api/           # API istemci fonksiyonları
├── components/    # Yeniden kullanılabilir React bileşenleri
├── hooks/         # Custom React hooks
├── pages/         # Sayfa bileşenleri
├── types/         # TypeScript type tanımları
├── utils/         # Yardımcı fonksiyonlar
├── App.tsx        # Ana uygulama bileşeni
├── main.tsx       # Uygulama entry point
└── index.css      # Global CSS ve Tailwind imports
```

## Sayfalar

- **Dashboard** - Sistem özeti ve istatistikler
- **Cihazlar** - Cihaz listesi ve yönetimi
- **Ağ Taraması** - Otomatik cihaz keşfi
- **Değişiklik Logları** - Sistem değişiklik takibi

## API Entegrasyonu

Uygulama, aşağıdaki API endpoint'lerini kullanır:

- `GET /api/device` - Tüm cihazları listeler
- `GET /api/device/{id}` - Belirli bir cihazı getirir
- `POST /api/device` - Yeni cihaz ekler
- `PUT /api/device/{id}` - Cihazı günceller
- `DELETE /api/device/{id}` - Cihazı siler
- `POST /api/networkscan/start` - Ağ taraması başlatır
- `GET /api/changelog` - Değişiklik loglarını getirir

## Deployment

### Production Build

```bash
npm run build
```

Build edilen dosyalar `dist/` klasöründe oluşturulur.

### Web Sunucusu Konfigürasyonu

SPA (Single Page Application) olduğu için, tüm route'ların `index.html`'e yönlendirilmesi gerekir.

**Nginx örneği:**
```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

**Apache örneği:**
```apache
RewriteEngine On
RewriteCond %{REQUEST_FILENAME} !-f
RewriteCond %{REQUEST_FILENAME} !-d
RewriteRule . /index.html [L]
```

### Ortam Değişkenleri

Production için `.env.production` dosyasını oluşturun:
```
VITE_API_BASE_URL=https://its.company.gov.tr
```

## Geliştirme

### Yeni Bileşen Ekleme

1. `src/components/` altında yeni dosya oluşturun
2. TypeScript interface'leri tanımlayın
3. Tailwind CSS ile stilleyın
4. Export edin

### Yeni Sayfa Ekleme

1. `src/pages/` altında yeni dosya oluşturun
2. `src/App.tsx`'de route ekleyin
3. `src/components/Layout.tsx`'de navigation ekleyin

### API Hook Ekleme

1. `src/api/` altında API fonksiyonu ekleyin
2. `src/hooks/` altında React Query hook oluşturun
3. Bileşenlerde kullanın

## Teknik Detaylar

- **React 19** - En son React özellikleri
- **TypeScript** - Tam tip güvenliği
- **Vite** - Hızlı build ve HMR
- **Tailwind CSS** - Utility-first CSS framework
- **React Query** - Server state yönetimi
- **React Router v6** - Client-side routing
- **Lucide React** - Modern ikonlar
- **Axios** - HTTP client

## Tarayıcı Desteği

- Chrome/Edge 88+
- Firefox 78+
- Safari 14+

## Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit edin (`git commit -m 'Add amazing feature'`)
4. Push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.