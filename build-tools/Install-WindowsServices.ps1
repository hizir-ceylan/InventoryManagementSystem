# Inventory Management System - Windows Service Installer (PowerShell)
# Bu script API ve Agent'ı Windows servisi olarak kurar

param(
    [switch]$Force,
    [string]$InstallPath = "C:\Program Files\InventoryManagementSystem"
)

# Yönetici kontrolü
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Bu script yonetici olarak calistirilmalidir!"
    Write-Host "PowerShell'i 'Yonetici olarak calistir' secenegi ile acin."
    Read-Host -Prompt "Devam etmek icin Enter tusuna basin"
    exit 1
}

Write-Host "===========================================" -ForegroundColor Green
Write-Host "Inventory Management System Service Setup" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green

# .NET 8 kontrolu
Write-Host "`n.NET 8 Runtime kontrolu..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET Runtime bulundu: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error ".NET 8 Runtime bulunamadi!"
    Write-Host "Lutfen .NET 8 Runtime'i indirin: https://dotnet.microsoft.com/download/dotnet/8.0"
    Read-Host -Prompt "Devam etmek icin Enter tusuna basin"
    exit 1
}

# Mevcut servisleri durdur
Write-Host "`nMevcut servisler kontrol ediliyor..." -ForegroundColor Yellow
$apiService  = Get-Service -Name "InventoryManagementApi"    -ErrorAction SilentlyContinue
if ($apiService) {
    Write-Host "API servisi durduruluyor..." -ForegroundColor Yellow
    Stop-Service -Name "InventoryManagementApi" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}
$agentService = Get-Service -Name "InventoryManagementAgent" -ErrorAction SilentlyContinue
if ($agentService) {
    Write-Host "Agent servisi durduruluyor..." -ForegroundColor Yellow
    Stop-Service -Name "InventoryManagementAgent" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

# Kurulum dizinlerini olustur
Write-Host "`nKurulum dizinleri olusturuluyor..." -ForegroundColor Yellow
Write-Host "Kurulum dizini: $InstallPath"
$directories = @(
    $InstallPath,
    "$InstallPath\Api",
    "$InstallPath\Agent",
    "$InstallPath\Data",
    "$InstallPath\Logs"
)
foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Dizin olusturuldu: $dir" -ForegroundColor Gray
    }
}

# Build ve deploy
Write-Host "`nUygulama build ediliyor ve deploy ediliyor..." -ForegroundColor Yellow
try {
    Write-Host "API build ediliyor..."   -ForegroundColor Gray
    $null = dotnet publish "..\Inventory.Api" -c Release -o "$InstallPath\Api" --self-contained false
    Write-Host "Agent build ediliyor..." -ForegroundColor Gray
    $null = dotnet publish "..\Inventory.Agent.Windows" -c Release -o "$InstallPath\Agent" --self-contained false
    Write-Host "Build islemi tamamlandi." -ForegroundColor Green
} catch {
    Write-Error "Build islemi basarisiz: $($_.Exception.Message)"
    Read-Host -Prompt "Devam etmek icin Enter tusuna basin"
    exit 1
}

# API servisini oluştur veya güncelle
Write-Host "`nAPI servisi yapılandırılıyor..." -ForegroundColor Yellow
$apiExePath       = "$InstallPath\Api\Inventory.Api.exe"
$apiServiceExists = Get-Service -Name "InventoryManagementApi" -ErrorAction SilentlyContinue
if (-not $apiServiceExists) {
    New-Service -Name "InventoryManagementApi" `
        -BinaryPathName $apiExePath `
        -DisplayName "Inventory Management API" `
        -Description "Inventory Management System API Service" `
        -StartupType Automatic
    Write-Host "Yeni API servisi olusturuldu." -ForegroundColor Gray
} else {
    Set-Service -Name "InventoryManagementApi" -BinaryPathName $apiExePath
    Write-Host "Mevcut API servisi guncellendi." -ForegroundColor Gray
}

# Agent servisini oluştur veya güncelle
Write-Host "Agent servisi yapılandırılıyor..." -ForegroundColor Yellow
$agentExePath       = "$InstallPath\Agent\Inventory.Agent.Windows.exe"
$agentServiceExists = Get-Service -Name "InventoryManagementAgent" -ErrorAction SilentlyContinue
if (-not $agentServiceExists) {
    New-Service -Name "InventoryManagementAgent" `
        -BinaryPathName $agentExePath `
        -DisplayName "Inventory Management Agent" `
        -Description "Inventory Management System Agent Service" `
        -StartupType Automatic `
        -DependsOn "InventoryManagementApi"
    Write-Host "Yeni Agent servisi olusturuldu." -ForegroundColor Gray
} else {
    Set-Service -Name "InventoryManagementAgent" -BinaryPathName $agentExePath
    Write-Host "Mevcut Agent servisi guncellendi." -ForegroundColor Gray
}

# Agent başlangıç gecikmesi
Write-Host "Agent baslangic gecikmesi ayarlaniyor..." -ForegroundColor Gray
sc.exe config "InventoryManagementAgent" start= delayed-auto | Out-Null

# Çevre değişkenleri
Write-Host "`nCevre degiskenleri ayarlaniyor..." -ForegroundColor Yellow
[Environment]::SetEnvironmentVariable("ApiSettings__BaseUrl",          "http://localhost:5093",               [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__EnableOfflineStorage", "true",                             [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__OfflineStoragePath",  "$InstallPath\Data\OfflineStorage",  [EnvironmentVariableTarget]::Machine)

# Firewall kuralı
Write-Host "Firewall kurali ekleniyor..." -ForegroundColor Yellow
try {
    $null = New-NetFirewallRule -DisplayName "Inventory Management API" -Direction Inbound -Protocol TCP -LocalPort 5093 -Action Allow -ErrorAction SilentlyContinue
} catch {
    Write-Warning "Firewall kurali eklenirken uyari: $($_.Exception.Message)"
}

# Servisleri başlat
Write-Host "`nServisler baslatiliyor..." -ForegroundColor Yellow
try {
    Write-Host "API servisi baslatiliyor..." -ForegroundColor Gray
    Start-Service -Name "InventoryManagementApi"
    Write-Host "API'nin hazir olmasi bekleniyor..." -ForegroundColor Gray
    Start-Sleep -Seconds 10
    Write-Host "Agent servisi baslatiliyor..." -ForegroundColor Gray
    Start-Service -Name "InventoryManagementAgent"
    Write-Host "Servisler basariyla baslatildi." -ForegroundColor Green
} catch {
    Write-Warning "Servis baslatma uyari: $($_.Exception.Message)"
    Write-Host "Servisleri manuel olarak baslatabilirsiniz: services.msc"
}

# Durum kontrolü
Write-Host "`n===========================================" -ForegroundColor Green
Write-Host "Kurulum Tamamlandi!"                          -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green
Write-Host "`nServis Durumlari:" -ForegroundColor Yellow
$apiStatus   = (Get-Service -Name "InventoryManagementApi").Status
$agentStatus = (Get-Service -Name "InventoryManagementAgent").Status
Write-Host "  API:   $apiStatus"   -ForegroundColor $(if($apiStatus   -eq "Running") {"Green"} else {"Red"})
Write-Host "  Agent: $agentStatus" -ForegroundColor $(if($agentStatus -eq "Running") {"Green"} else {"Red"})

Write-Host "`nErisim Bilgileri:" -ForegroundColor Yellow
Write-Host "  - API:          http://localhost:5093"
Write-Host "  - Swagger UI:    http://localhost:5093/swagger"
Write-Host "  - Kurulum Dizin: $InstallPath"

Write-Host "`nYonetim Komutlari:" -ForegroundColor Yellow
Write-Host "  - Servis yoneticisi: services.msc"
Write-Host "  - Event loglari:    eventvwr.msc"
Write-Host "  - Servisleri yonet: .\\scripts\\manage-services.bat"

Write-Host "`nTest:" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5093/api/device" -UseBasicParsing -TimeoutSec 5
    Write-Host "  [OK] API test basarili (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] API test basarisiz - Servislerin baslamasi birkac dakika surebilir" -ForegroundColor Yellow
}

Write-Host "`nKurulum tamamlandi!" -ForegroundColor Green
Read-Host -Prompt "Devam etmek icin Enter tusuna basin"
