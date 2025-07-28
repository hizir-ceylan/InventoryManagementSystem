# Windows Setup.exe Kurulum Rehberi

Bu doküman, Inventory Management System için Windows üzerinde çalışan bir **setup.exe** kurulum dosyası oluşturma rehberidir.

## Özellikler

✅ **Tek dosya kurulum**: Tüm sistem tek bir setup.exe dosyasında  
✅ **.NET 8 uyumluluğu**: .NET 9 uyumluluk sorunları çözüldü  
✅ **Otomatik .NET kurulumu**: Eksik ise .NET 8 Runtime otomatik yüklenir  
✅ **Windows servisleri**: API ve Agent otomatik servis olarak kurulur  
✅ **Firewall yapılandırması**: Port 5093 otomatik açılır  
✅ **Masaüstü kısayolları**: Swagger UI ve sistem klasörü  
✅ **Otomatik kaldırma**: Temiz kaldırma işlemi  

## Gereksinimler (Geliştirici Bilgisayarı)

### Zorunlu
- Windows 10/11 veya Windows Server 2016+
- **.NET 8 SDK** - [İndir](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Yönetici yetkileri** (servisleri test etmek için)

### Setup.exe oluşturmak için
- **Inno Setup 6** - [İndir](https://jrsoftware.org/isinfo.php)

## Kurulum Rehberi

### Adım 1: Projeyi Hazırla

```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### Adım 2: .NET 9 Uyumluluk Sorunu Çözümü

⚠️ **Önemli**: Eğer sisteminizde .NET 9 varsa ve .NET 8 ile uyumluluk sorunu yaşıyorsanız, bu otomatik olarak çözülmüştür. `System.Management` paketi .NET 8 uyumlu versiyona güncellenmiştir.

### Adım 3: Inno Setup Kurulumu

1. [Inno Setup 6](https://jrsoftware.org/isinfo.php) sayfasından indirin
2. Normal kurulum yapın (varsayılan ayarlarla)
3. Kurulum sonrası Inno Setup PATH'e eklenir

### Adım 4: Setup.exe Oluşturma

#### Seçenek A: PowerShell Script (Önerilen)
```powershell
# Yönetici olarak PowerShell açın
.\Build-Setup.ps1
```

#### Seçenek B: Batch Script
```cmd
REM Yönetici olarak Command Prompt açın
Build-Setup.bat
```

#### Seçenek C: Manuel İşlem
```bash
# 1. Projeleri derle
dotnet restore
dotnet build --configuration Release

# 2. Publish et
dotnet publish Inventory.Api --configuration Release --output "Published\Api"
dotnet publish Inventory.Agent.Windows --configuration Release --output "Published\Agent"

# 3. Setup.exe oluştur
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" InventoryManagementSystem.iss
```

### Adım 5: Çıktılar

Başarılı build sonrası şu dosyalar oluşur:

```
📁 Setup/
  📄 InventoryManagementSystem-Setup.exe    ← Bu dosyayı dağıtın

📁 Published/
  📁 Api/          ← API dosyaları
  📁 Agent/        ← Agent dosyaları
```

## Setup.exe Kullanımı

### Hedef Bilgisayarda Kurulum

1. **InventoryManagementSystem-Setup.exe** dosyasını hedef bilgisayara kopyalayın
2. **Sağ tık → "Yönetici olarak çalıştır"**
3. Kurulum wizard'ını takip edin
4. .NET 8 eksikse otomatik yüklenecek
5. Servisler otomatik başlatılacak

### Kurulum Sonrası

- **API**: http://localhost:5093/swagger
- **Servis Yönetimi**: `services.msc`
- **Event Loglar**: `eventvwr.msc`
- **Kurulum Klasörü**: `C:\Program Files\Inventory Management System\`

## Sorun Giderme

### "Build failed" Hatası
```bash
# NuGet cache temizle
dotnet nuget locals all --clear
dotnet restore --force
dotnet build
```

### "Inno Setup not found" Hatası
```bash
# PATH kontrolü
where iscc

# Manuel çalıştırma
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" InventoryManagementSystem.iss
```

### ".NET 9 Uyumluluk Sorunu"
✅ Bu proje artık .NET 8 ile tamamen uyumludur. `System.Management` paketi 8.0.0 versiyonuna güncellenmiştir.

### Servisler Başlamıyor
```cmd
# Manuel başlatma
sc start InventoryManagementApi
timeout /t 10
sc start InventoryManagementAgent

# Log kontrol
eventvwr.msc → Windows Logs → Application
```

## Gelişmiş Seçenekler

### Self-Contained Build
.NET Runtime'ı da dahil etmek için:

```powershell
.\Build-Setup.ps1 -SelfContained
```

**Avantajlar**: Hedef bilgisayarda .NET kurulu olması gerekmez  
**Dezavantajlar**: Setup dosyası ~100MB büyük olur

### Debug Build
Geliştirme için debug sürümü:

```powershell
.\Build-Setup.ps1 -Configuration Debug
```

## Script Parametreleri

### Build-Setup.ps1 Parametreleri
```powershell
-Configuration Release|Debug    # Build konfigürasyonu (varsayılan: Release)
-SelfContained                  # .NET Runtime dahil et (varsayılan: false)  
-SkipInnoSetup                  # Setup.exe oluşturmayı atla
```

### Örnek Kullanım
```powershell
# Sadece dosyaları hazırla, setup.exe oluşturma
.\Build-Setup.ps1 -SkipInnoSetup

# Self-contained debug build
.\Build-Setup.ps1 -Configuration Debug -SelfContained
```

## Dosya Yapısı

Kurulum sonrası hedef bilgisayarda şu yapı oluşur:

```
C:\Program Files\Inventory Management System\
├── 📁 Api/                    ← API servisi dosyaları
├── 📁 Agent/                  ← Agent servisi dosyaları
├── 📁 Data/                   ← Veritabanı ve offline veriler
├── 📁 Logs/                   ← Log dosyaları
├── 📄 README.md
├── 📄 WINDOWS-SERVICE-README.md
└── 📄 Uninstall.ps1          ← Kaldırma scripti
```

## Git Clone Sorununun Çözümü

❌ **Eski yöntem**: `git clone` + script çalıştırma  
✅ **Yeni yöntem**: Tek setup.exe dosyası

**Avantajlar**:
- Git kurulumu gerekmez
- İnternet bağlantısı gerekmez (kurulum sırasında)
- Tek tıklama kurulum
- Profesyonel görünüm
- Otomatik kaldırma desteği

## Dağıtım

1. **InventoryManagementSystem-Setup.exe** dosyasını oluşturun
2. Hedef bilgisayarlara kopyalayın (USB, ağ paylaşımı, vs.)
3. Her bilgisayarda yönetici olarak çalıştırın
4. Kurulum tamamlandığında sistem hazır

**Not**: Her hedef bilgisayar için aynı setup.exe dosyası kullanılabilir.

## Güncelleme

Yeni sürüm dağıtmak için:

1. Yeni setup.exe oluşturun
2. Eski sürümü kaldırın (Control Panel veya Uninstall.ps1)
3. Yeni setup.exe'yi yükleyin

Alternatif olarak, setup.exe aynı sürümü algılar ve güncelleme yapar.

---

**Sonuç**: Artık git clone gerektirmeyen, tek dosya kurulum sisteminiz hazır! 🎉