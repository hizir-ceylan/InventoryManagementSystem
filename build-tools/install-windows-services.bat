@echo off
REM Inventory Management System - Windows Service Installer
REM Bu script API ve Agent'ı Windows servisi olarak kurar

echo ==========================================
echo Inventory Management System Service Setup
echo ==========================================

REM Yönetici yetkileri kontrolü
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo HATA: Bu script yönetici olarak çalıştırılmalıdır!
    echo Sağ tıklayıp "Yönetici olarak çalıştır" seçeneğini kullanın.
    pause
    exit /b 1
)

echo Administrator yetkisi doğrulandı.

REM .NET 8 kontrolü
echo .NET 8 Runtime kontrolü yapılıyor...
dotnet --version >nul 2>&1
if %errorLevel% neq 0 (
    echo HATA: .NET 8 Runtime bulunamadı!
    echo Lütfen .NET 8 Runtime'ı indirin: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET 8 Runtime bulundu.

REM Kurulum dizini belirleme
set "INSTALL_DIR=C:\Program Files\InventoryManagementSystem"
echo Kurulum dizini: %INSTALL_DIR%

REM Mevcut servisleri durdur
echo Mevcut servisleri kontrol ediliyor...
sc query "InventoryManagementApi" >nul 2>&1
if %errorLevel% equ 0 (
    echo API servisi durduruluyor...
    sc stop "InventoryManagementApi"
    timeout /t 3 /nobreak >nul
)

sc query "InventoryManagementAgent" >nul 2>&1
if %errorLevel% equ 0 (
    echo Agent servisi durduruluyor...
    sc stop "InventoryManagementAgent" 
    timeout /t 3 /nobreak >nul
)

REM Kurulum dizinini oluştur
echo Kurulum dizini oluşturuluyor...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Api" mkdir "%INSTALL_DIR%\Api"
if not exist "%INSTALL_DIR%\Agent" mkdir "%INSTALL_DIR%\Agent"
if not exist "%INSTALL_DIR%\Data" mkdir "%INSTALL_DIR%\Data"
if not exist "%INSTALL_DIR%\Logs" mkdir "%INSTALL_DIR%\Logs"

REM Proje dizinini kontrol et
if not exist "Inventory.Api\bin\Release\net8.0\publish" (
    echo Release build bulunamadı. Build işlemi başlatılıyor...
    dotnet publish Inventory.Api -c Release -o "%INSTALL_DIR%\Api"
    if %errorLevel% neq 0 (
        echo HATA: API build işlemi başarısız!
        pause
        exit /b 1
    )
) else (
    echo API dosyaları kopyalanıyor...
    xcopy /s /y "Inventory.Api\bin\Release\net8.0\publish\*" "%INSTALL_DIR%\Api\"
)

if not exist "Inventory.Agent.Windows\bin\Release\net8.0\publish" (
    echo Release build bulunamadı. Build işlemi başlatılıyor...
    dotnet publish Inventory.Agent.Windows -c Release -o "%INSTALL_DIR%\Agent"
    if %errorLevel% neq 0 (
        echo HATA: Agent build işlemi başarısız!
        pause
        exit /b 1
    )
) else (
    echo Agent dosyaları kopyalanıyor...
    xcopy /s /y "Inventory.Agent.Windows\bin\Release\net8.0\publish\*" "%INSTALL_DIR%\Agent\"
)

REM API Servisi oluştur
echo API servisi oluşturuluyor...
sc create "InventoryManagementApi" ^
    binPath= "\"%INSTALL_DIR%\Api\Inventory.Api.exe\"" ^
    DisplayName= "Inventory Management API" ^
    Description= "Inventory Management System API Service" ^
    start= auto ^
    obj= "LocalSystem"

if %errorLevel% neq 0 (
    echo Uyarı: API servisi zaten mevcut olabilir.
    echo API servisi güncelleniyor...
    sc config "InventoryManagementApi" ^
        binPath= "\"%INSTALL_DIR%\Api\Inventory.Api.exe\"" ^
        start= auto
)

REM Agent Servisi oluştur
echo Agent servisi oluşturuluyor...
sc create "InventoryManagementAgent" ^
    binPath= "\"%INSTALL_DIR%\Agent\Inventory.Agent.Windows.exe\" --service" ^
    DisplayName= "Inventory Management Agent" ^
    Description= "Inventory Management System Agent Service" ^
    start= auto ^
    obj= "LocalSystem" ^
    depend= "InventoryManagementApi"

if %errorLevel% neq 0 (
    echo Uyarı: Agent servisi zaten mevcut olabilir.
    echo Agent servisi güncelleniyor...
    sc config "InventoryManagementAgent" ^
        binPath= "\"%INSTALL_DIR%\Agent\Inventory.Agent.Windows.exe\" --service" ^
        start= auto ^
        depend= "InventoryManagementApi"
)

REM Başlangıç gecikmesi ayarla (Agent'ın API'den sonra başlaması için)
echo Agent başlangıç gecikmesi ayarlanıyor...
sc config "InventoryManagementAgent" start= delayed-auto

REM Çevre değişkenlerini ayarla
echo Çevre değişkenleri ayarlanıyor...
setx ApiSettings__BaseUrl "http://localhost:5093" /M >nul
setx ApiSettings__EnableOfflineStorage "true" /M >nul
setx ApiSettings__OfflineStoragePath "%INSTALL_DIR%\Data\OfflineStorage" /M >nul

REM Firewall kuralları ekle
echo Firewall kuralları ekleniyor...
netsh advfirewall firewall add rule name="Inventory Management API" dir=in action=allow protocol=TCP localport=5093 >nul 2>&1

REM API Servisini başlat
echo API servisi başlatılıyor...
sc start "InventoryManagementApi"
if %errorLevel% neq 0 (
    echo Uyarı: API servisi başlatılamadı. Manuel olarak başlatmayı deneyin.
)

REM Agent başlatma öncesi bekleme
echo Agent servisi başlatılmadan önce API'nin hazır olması bekleniyor...
timeout /t 10 /nobreak

REM Agent Servisini başlat
echo Agent servisi başlatılıyor...
sc start "InventoryManagementAgent"
if %errorLevel% neq 0 (
    echo Uyarı: Agent servisi başlatılamadı. Manuel olarak başlatmayı deneyin.
)

echo.
echo ==========================================
echo Kurulum tamamlandı!
echo ==========================================
echo.
echo Servis Durumu Kontrolü:
sc query "InventoryManagementApi" | findstr "STATE"
sc query "InventoryManagementAgent" | findstr "STATE"
echo.
echo Yararlı Komutlar:
echo   Servisleri görüntüle: services.msc
echo   API servis durumu: sc query "InventoryManagementApi"
echo   Agent servis durumu: sc query "InventoryManagementAgent"
echo   API'yi test et: http://localhost:5093/swagger
echo.
echo Servis logları Windows Event Viewer'da "Application" bölümünde görülebilir.
echo Kurulum dizini: %INSTALL_DIR%

pause