# Inventory Management System - Windows Service Installer (PowerShell)
# Bu script API ve Agent'ı Windows servisi olarak kurar

param(
    [switch]$Force,
    [string]$InstallPath = "C:\Program Files\InventoryManagementSystem"
)

# Yönetici kontrolü
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Bu script yönetici olarak çalıştırılmalıdır!"
    Write-Host "PowerShell'i 'Yönetici olarak çalıştır' seçeneği ile açın."
    Read-Host "Devam etmek için Enter tuşuna basın"
    exit 1
}

Write-Host "===========================================" -ForegroundColor Green
Write-Host "Inventory Management System Service Setup" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green

# .NET 8 kontrolü
Write-Host "`n.NET 8 Runtime kontrolü..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET Runtime bulundu: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error ".NET 8 Runtime bulunamadı!"
    Write-Host "Lütfen .NET 8 Runtime'ı indirin: https://dotnet.microsoft.com/download/dotnet/8.0"
    Read-Host "Devam etmek için Enter tuşuna basın"
    exit 1
}

# Mevcut servisleri durdur
Write-Host "`nMevcut servisler kontrol ediliyor..." -ForegroundColor Yellow

$apiService = Get-Service -Name "InventoryManagementApi" -ErrorAction SilentlyContinue
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

# Kurulum dizinlerini oluştur
Write-Host "`nKurulum dizinleri oluşturuluyor..." -ForegroundColor Yellow
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
        Write-Host "Dizin oluşturuldu: $dir" -ForegroundColor Gray
    }
}

# Build ve deploy
Write-Host "`nUygulama build ediliyor ve deploy ediliyor..." -ForegroundColor Yellow

try {
    # API Build
    Write-Host "API build ediliyor..." -ForegroundColor Gray
    $null = dotnet publish "Inventory.Api" -c Release -o "$InstallPath\Api" --self-contained false
    
    # Agent Build
    Write-Host "Agent build ediliyor..." -ForegroundColor Gray
    $null = dotnet publish "Inventory.Agent.Windows" -c Release -o "$InstallPath\Agent" --self-contained false
    
    Write-Host "Build işlemi tamamlandı." -ForegroundColor Green
} catch {
    Write-Error "Build işlemi başarısız: $($_.Exception.Message)"
    Read-Host "Devam etmek için Enter tuşuna basın"
    exit 1
}

# API Servisi oluştur/güncelle
Write-Host "`nAPI servisi yapılandırılıyor..." -ForegroundColor Yellow

$apiExePath = "$InstallPath\Api\Inventory.Api.exe"
$apiServiceExists = Get-WmiObject -Class Win32_Service -Filter "Name='InventoryManagementApi'" -ErrorAction SilentlyContinue

if ($apiServiceExists) {
    Write-Host "Mevcut API servisi güncelleniyor..." -ForegroundColor Gray
    & sc.exe config "InventoryManagementApi" binPath= "`"$apiExePath`"" start= auto | Out-Null
} else {
    Write-Host "Yeni API servisi oluşturuluyor..." -ForegroundColor Gray
    & sc.exe create "InventoryManagementApi" binPath= "`"$apiExePath`"" DisplayName= "Inventory Management API" Description= "Inventory Management System API Service" start= auto obj= "LocalSystem" | Out-Null
}

# Agent Servisi oluştur/güncelle
Write-Host "Agent servisi yapılandırılıyor..." -ForegroundColor Yellow

$agentExePath = "$InstallPath\Agent\Inventory.Agent.Windows.exe"
$agentServiceExists = Get-WmiObject -Class Win32_Service -Filter "Name='InventoryManagementAgent'" -ErrorAction SilentlyContinue

if ($agentServiceExists) {
    Write-Host "Mevcut Agent servisi güncelleniyor..." -ForegroundColor Gray
    & sc.exe config "InventoryManagementAgent" binPath= "`"$agentExePath`" --service" start= auto depend= "InventoryManagementApi" | Out-Null
} else {
    Write-Host "Yeni Agent servisi oluşturuluyor..." -ForegroundColor Gray
    & sc.exe create "InventoryManagementAgent" binPath= "`"$agentExePath`" --service" DisplayName= "Inventory Management Agent" Description= "Inventory Management System Agent Service" start= auto obj= "LocalSystem" depend= "InventoryManagementApi" | Out-Null
}

# Agent başlangıç gecikmesi
Write-Host "Agent başlangıç gecikmesi ayarlanıyor..." -ForegroundColor Gray
& sc.exe config "InventoryManagementAgent" start= delayed-auto | Out-Null

# Çevre değişkenleri
Write-Host "`nÇevre değişkenleri ayarlanıyor..." -ForegroundColor Yellow

[Environment]::SetEnvironmentVariable("ApiSettings__BaseUrl", "http://localhost:5093", [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__EnableOfflineStorage", "true", [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__OfflineStoragePath", "$InstallPath\Data\OfflineStorage", [EnvironmentVariableTarget]::Machine)

# Firewall kuralı
Write-Host "Firewall kuralı ekleniyor..." -ForegroundColor Yellow
try {
    $null = New-NetFirewallRule -DisplayName "Inventory Management API" -Direction Inbound -Protocol TCP -LocalPort 5093 -Action Allow -ErrorAction SilentlyContinue
} catch {
    Write-Warning "Firewall kuralı eklenirken uyarı: $($_.Exception.Message)"
}

# Servisleri başlat
Write-Host "`nServisler başlatılıyor..." -ForegroundColor Yellow

try {
    Write-Host "API servisi başlatılıyor..." -ForegroundColor Gray
    Start-Service -Name "InventoryManagementApi"
    
    Write-Host "API'nin hazır olması bekleniyor..." -ForegroundColor Gray
    Start-Sleep -Seconds 10
    
    Write-Host "Agent servisi başlatılıyor..." -ForegroundColor Gray
    Start-Service -Name "InventoryManagementAgent"
    
    Write-Host "Servisler başarıyla başlatıldı." -ForegroundColor Green
} catch {
    Write-Warning "Servis başlatma uyarısı: $($_.Exception.Message)"
    Write-Host "Servisleri manuel olarak başlatabilirsiniz: services.msc"
}

# Durum kontrolü
Write-Host "`n===========================================" -ForegroundColor Green
Write-Host "Kurulum Tamamlandı!" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green

Write-Host "`nServis Durumları:" -ForegroundColor Yellow
$apiStatus = (Get-Service -Name "InventoryManagementApi").Status
$agentStatus = (Get-Service -Name "InventoryManagementAgent").Status
Write-Host "  API: $apiStatus" -ForegroundColor $(if($apiStatus -eq "Running") {"Green"} else {"Red"})
Write-Host "  Agent: $agentStatus" -ForegroundColor $(if($agentStatus -eq "Running") {"Green"} else {"Red"})

Write-Host "`nErişim Bilgileri:" -ForegroundColor Yellow
Write-Host "  • API: http://localhost:5093"
Write-Host "  • Swagger UI: http://localhost:5093/swagger"
Write-Host "  • Kurulum Dizini: $InstallPath"

Write-Host "`nYönetim Komutları:" -ForegroundColor Yellow
Write-Host "  • Servis yöneticisi: services.msc"
Write-Host "  • Event logları: eventvwr.msc"
Write-Host "  • Servisleri yönet: .\scripts\manage-services.bat"

Write-Host "`nTest:" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5093/api/device" -UseBasicParsing -TimeoutSec 5
    Write-Host "  ✓ API test başarılı (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "  ✗ API test başarısız - Servislerin başlaması birkaç dakika sürebilir" -ForegroundColor Yellow
}

Write-Host "`nKurulum tamamlandi!" -ForegroundColor Green
Read-Host "`nDevam etmek için Enter tuşuna basın"