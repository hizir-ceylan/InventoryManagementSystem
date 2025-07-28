@echo off
REM Inventory Management System - Service Manager
REM Bu script servisleri yönetmek için kullanılır

echo ==========================================
echo Inventory Management System Service Manager
echo ==========================================

:MENU
echo.
echo 1. Servis durumunu görüntüle
echo 2. API servisini başlat
echo 3. API servisini durdur
echo 4. Agent servisini başlat
echo 5. Agent servisini durdur
echo 6. Her iki servisi başlat
echo 7. Her iki servisi durdur
echo 8. API'yi test et (browser)
echo 9. Event loglarını görüntüle
echo 0. Çıkış
echo.

set /p CHOICE="Seçiminiz (0-9): "

if "%CHOICE%"=="1" goto STATUS
if "%CHOICE%"=="2" goto START_API
if "%CHOICE%"=="3" goto STOP_API
if "%CHOICE%"=="4" goto START_AGENT
if "%CHOICE%"=="5" goto STOP_AGENT
if "%CHOICE%"=="6" goto START_ALL
if "%CHOICE%"=="7" goto STOP_ALL
if "%CHOICE%"=="8" goto TEST_API
if "%CHOICE%"=="9" goto VIEW_LOGS
if "%CHOICE%"=="0" goto EXIT

echo Geçersiz seçim!
goto MENU

:STATUS
echo.
echo === Servis Durumları ===
sc query "InventoryManagementApi" 2>nul | findstr /C:"STATE" || echo API servisi yüklü değil
sc query "InventoryManagementAgent" 2>nul | findstr /C:"STATE" || echo Agent servisi yüklü değil
echo.
pause
goto MENU

:START_API
echo.
echo API servisi başlatılıyor...
sc start "InventoryManagementApi"
echo.
pause
goto MENU

:STOP_API
echo.
echo API servisi durduruluyor...
sc stop "InventoryManagementApi"
echo.
pause
goto MENU

:START_AGENT
echo.
echo Agent servisi başlatılıyor...
sc start "InventoryManagementAgent"
echo.
pause
goto MENU

:STOP_AGENT
echo.
echo Agent servisi durduruluyor...
sc stop "InventoryManagementAgent"
echo.
pause
goto MENU

:START_ALL
echo.
echo Tüm servisler başlatılıyor...
echo API servisi başlatılıyor...
sc start "InventoryManagementApi"
echo API'nin hazır olması bekleniyor...
timeout /t 5 /nobreak >nul
echo Agent servisi başlatılıyor...
sc start "InventoryManagementAgent"
echo.
pause
goto MENU

:STOP_ALL
echo.
echo Tüm servisler durduruluyor...
echo Agent servisi durduruluyor...
sc stop "InventoryManagementAgent"
timeout /t 3 /nobreak >nul
echo API servisi durduruluyor...
sc stop "InventoryManagementApi"
echo.
pause
goto MENU

:TEST_API
echo.
echo API test ediliyor (browser açılacak)...
start http://localhost:5093/swagger
echo.
pause
goto MENU

:VIEW_LOGS
echo.
echo Event Viewer açılıyor...
echo Application Logs bölümünde "InventoryManagement" kaynaklı logları arayın.
eventvwr.msc
echo.
pause
goto MENU

:EXIT
echo Çıkılıyor...
exit /b 0