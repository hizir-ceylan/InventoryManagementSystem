@echo off
REM Inventory Management System - Windows Service Uninstaller
REM Bu script API ve Agent servislerini kaldırır

echo ==========================================
echo Inventory Management System Service Removal
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

REM Servisleri durdur
echo Servisler durduruluyor...

sc query "InventoryManagementAgent" >nul 2>&1
if %errorLevel% equ 0 (
    echo Agent servisi durduruluyor...
    sc stop "InventoryManagementAgent"
    timeout /t 5 /nobreak >nul
    echo Agent servisi kaldırılıyor...
    sc delete "InventoryManagementAgent"
) else (
    echo Agent servisi bulunamadı.
)

sc query "InventoryManagementApi" >nul 2>&1
if %errorLevel% equ 0 (
    echo API servisi durduruluyor...
    sc stop "InventoryManagementApi"
    timeout /t 5 /nobreak >nul
    echo API servisi kaldırılıyor...
    sc delete "InventoryManagementApi"
) else (
    echo API servisi bulunamadı.
)

REM Firewall kurallarını kaldır
echo Firewall kuralları kaldırılıyor...
netsh advfirewall firewall delete rule name="Inventory Management API" >nul 2>&1

set /p REMOVE_FILES="Kurulum dosyalarını da kaldırmak istiyor musunuz? (Y/N): "
if /i "%REMOVE_FILES%"=="Y" (
    set "INSTALL_DIR=C:\Program Files\InventoryManagementSystem"
    echo Kurulum dosyaları kaldırılıyor...
    if exist "%INSTALL_DIR%" (
        rmdir /s /q "%INSTALL_DIR%"
        echo Kurulum dizini kaldırıldı.
    )
)

echo.
echo ==========================================
echo Kaldırma işlemi tamamlandı!
echo ==========================================
echo.

pause