# Build Tools

Bu dizin Inventory Management System için build ve deployment araçlarını içerir.

## Ana Kurulum Scripti

### Build-Setup.ps1
Windows için ana build ve setup oluşturma scripti. Bu script:
- .NET 8 SDK gereksinimini kontrol eder
- Projeyi Release modunda build eder
- API ve Agent için published dosyaları hazırlar
- Inno Setup ile kurulum dosyası (setup.exe) oluşturur
- Windows servislerini kurar ve yapılandırır

**Kullanım:**
```powershell
# Yönetici PowerShell'de:
cd build-tools
.\Build-Setup.ps1
```

**Oluşturulan Dosyalar:**
- `Setup\InventoryManagementSystem-Setup.exe` - Kurulum dosyası
- `Published\Api\` - API published dosyaları
- `Published\Agent\` - Agent published dosyaları

## Docker Araçları

### quick-start.sh
Docker ortamını hızlıca başlatmak için kullanılır.

### test-docker.sh
Docker kurulumunu test etmek için kullanılır.

### test-build.sh
Build işlemlerini test etmek için kullanılır.

### test-logging.sh
Logging sistemini test etmek için kullanılır.

## Diğer Dosyalar

### InventoryManagementSystem.iss
Inno Setup kurulum scripti. Build-Setup.ps1 tarafından kullanılır.

## Gereksinimler

- Windows 10/11 veya Windows Server 2016+
- .NET 8 SDK
- Inno Setup (setup.exe oluşturmak için)
- Yönetici yetkileri

## Kurulum Sonrası

Kurulum tamamlandığında:
- API: http://localhost:5093
- Swagger UI: http://localhost:5093/swagger
- Windows Servisleri otomatik başlar
- Event Log'a kayıtlar yazılır