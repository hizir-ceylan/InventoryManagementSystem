using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Inventory.Domain.Entities;
using Inventory.Shared.Utils;

namespace Inventory.Agent.Windows.Services
{
    /// <summary>
    /// Windows ve Office güncellemelerini tespit eden servis
    /// Sadece güncelleme durumunu rapor eder, otomatik yükleme yapmaz
    /// </summary>
    public class UpdateDetectionService
    {
        #region Fields

        private readonly ILogger<UpdateDetectionService> _logger;
        private readonly string _logPath;

        #endregion

        #region Constructor

        public UpdateDetectionService(ILogger<UpdateDetectionService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateDetectionService>.Instance;
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "InventoryManagementSystem", "UpdateLogs");
            
            // Log dizinini oluştur
            Directory.CreateDirectory(_logPath);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tüm sistem güncellemelerini tespit eder
        /// </summary>
        /// <param name="deviceId">Cihaz ID'si</param>
        /// <returns>Tespit edilen güncellemeler listesi</returns>
        public async Task<List<SystemUpdate>> DetectAllUpdatesAsync(Guid deviceId)
        {
            var allUpdates = new List<SystemUpdate>();

            try
            {
                _logger.LogInformation("Sistem güncellemeleri taranıyor...");

                // Windows güncellemelerini tespit et
                var windowsUpdates = await DetectWindowsUpdatesAsync(deviceId);
                allUpdates.AddRange(windowsUpdates);

                // Office güncellemelerini tespit et
                var officeUpdates = await DetectOfficeUpdatesAsync(deviceId);
                allUpdates.AddRange(officeUpdates);

                // .NET Framework güncellemelerini tespit et
                var dotnetUpdates = await DetectDotNetUpdatesAsync(deviceId);
                allUpdates.AddRange(dotnetUpdates);

                _logger.LogInformation("Toplam {UpdateCount} güncelleme tespit edildi", allUpdates.Count);

                // Sonuçları dosyaya kaydet
                await SaveUpdateResultsToFileAsync(allUpdates);

                return allUpdates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncelleme tespiti sırasında hata oluştu");
                return allUpdates;
            }
        }

        #endregion

        #region Windows Update Detection

        /// <summary>
        /// Windows güncellemelerini tespit eder
        /// </summary>
        private async Task<List<SystemUpdate>> DetectWindowsUpdatesAsync(Guid deviceId)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                _logger.LogInformation("Windows güncellemeleri kontrol ediliyor...");

                // WUA (Windows Update Agent) kullanarak güncellemeleri tespit et
                await Task.Run(() =>
                {
                    try
                    {
                        // Windows Update Session oluştur
                        dynamic updateSession = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
                        dynamic updateSearcher = updateSession.CreateUpdateSearcher();

                        // Mevcut güncellemeleri ara - kapsamlı sorgular ile tüm pending güncellemeleri yakala
                        // IsInstalled=0: Yüklü olmayan güncellemeler
                        // IsDownloaded=1: İndirilmiş güncellemeler
                        // RebootRequired=1: Yeniden başlatma gerektiren güncellemeler (yüklü ama restart bekleyen)
                        var searchQueries = new[]
                        {
                            "IsInstalled=0 and Type='Software' and IsHidden=0", // Mevcut güncellemeler
                            "IsInstalled=0 and IsDownloaded=1 and Type='Software' and IsHidden=0", // İndirilmiş ama yüklenmemiş
                            "RebootRequired=1 and Type='Software' and IsHidden=0", // Restart bekleyen güncellemeler (yüklü ama restart gerekli)
                            "IsInstalled=0 and Type='Driver' and IsHidden=0", // Driver güncellemeleri
                            "RebootRequired=1 and Type='Driver' and IsHidden=0", // Restart bekleyen driver güncellemeleri
                            // Office ve diğer Microsoft ürünleri için özel aramalar
                            "IsInstalled=0 and Categories.CategoryID='28BC880E-0592-4CBF-8F95-C79B17911D5F'", // Microsoft Office güncellemeleri
                            "IsInstalled=0 and Categories.CategoryID='0FA1201D-4330-4FA8-8AE9-B877473B6441'", // Güvenlik güncellemeleri
                            "IsInstalled=0 and Categories.CategoryID='E6CF1350-C01B-414D-A61F-263D14D133B4'", // Kritik güncellemeler
                            "IsInstalled=0 and Categories.CategoryID='CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83'" // Windows güncellemeleri
                        };

                        foreach (var searchQuery in searchQueries)
                        {
                            try
                            {
                                _logger.LogDebug("Windows Update arama sorgusu: {Query}", searchQuery);
                                dynamic searchResult = updateSearcher.Search(searchQuery);

                                foreach (dynamic update in searchResult.Updates)
                                {
                                    // Aynı güncellemenin birden fazla kez eklenmesini önle
                                    var updateId = update.Identity?.UpdateID?.ToString() ?? update.Title;
                                    if (updates.Any(u => u.Title == update.Title || u.KBNumber == ExtractKBNumber(update.Title ?? "")))
                                        continue;

                                    // Güncelleme durumunu daha doğru belirle
                                    UpdateStatus status;
                                    if (update.RebootRequired && update.IsInstalled)
                                    {
                                        status = UpdateStatus.PendingRestart; // Yüklü ama restart bekliyor
                                    }
                                    else if (update.IsDownloaded && !update.IsInstalled)
                                    {
                                        status = UpdateStatus.Downloaded; // İndirilmiş ama yüklenmemiş
                                    }
                                    else if (!update.IsInstalled)
                                    {
                                        status = UpdateStatus.Available; // Mevcut
                                    }
                                    else
                                    {
                                        status = UpdateStatus.Installed; // Yüklü
                                    }

                                    var systemUpdate = new SystemUpdate
                                    {
                                        Id = Guid.NewGuid(),
                                        DeviceId = deviceId,
                                        UpdateType = DetermineUpdateType(update),
                                        Title = update.Title ?? "Bilinmeyen Güncelleme",
                                        Description = update.Description ?? "",
                                        KBNumber = ExtractKBNumber(update.Title ?? ""),
                                        SizeInMB = GetUpdateSizeInMB(update),
                                        Status = status,
                                        Priority = DeterminePriority(update),
                                        DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                                        LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                                        ReleaseDate = GetReleaseDate(update),
                                        CanAutoInstall = update.AutoDownload ?? false,
                                        RequiresRestart = update.RebootRequired ?? false,
                                        SecurityBulletinId = ExtractSecurityBulletinId(update.SecurityBulletinIDs)
                                    };

                                    updates.Add(systemUpdate);
                                }
                            }
                            catch (Exception searchEx)
                            {
                                _logger.LogWarning(searchEx, "Güncelleme arama sorgusu başarısız: {Query}", searchQuery);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Windows Update API erişiminde sorun: {Error}", ex.Message);
                        
                        // Fallback: WMI kullanarak yüklü güncellemeleri kontrol et
                        CheckInstalledUpdatesViaWMI(updates, deviceId);
                    }
                });

                _logger.LogInformation("{Count} Windows güncellemesi tespit edildi", updates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Windows güncellemesi tespitinde hata");
            }

            return updates;
        }

        /// <summary>
        /// WMI kullanarak yüklü güncellemeleri kontrol eder
        /// </summary>
        private void CheckInstalledUpdatesViaWMI(List<SystemUpdate> updates, Guid deviceId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
                using var results = searcher.Get();

                foreach (ManagementObject result in results.Cast<ManagementObject>())
                {
                    var hotfixId = result["HotFixID"]?.ToString();
                    var description = result["Description"]?.ToString();
                    var installedOn = result["InstalledOn"]?.ToString();

                    if (!string.IsNullOrEmpty(hotfixId))
                    {
                        var update = new SystemUpdate
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            UpdateType = "Windows",
                            Title = $"Windows Update - {hotfixId}",
                            Description = description ?? "",
                            KBNumber = hotfixId,
                            Status = UpdateStatus.Installed,
                            Priority = UpdatePriority.Normal,
                            DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                            LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                            CanAutoInstall = false,
                            RequiresRestart = false
                        };

                        if (DateTime.TryParse(installedOn, out var installDate))
                        {
                            update.ReleaseDate = installDate;
                        }

                        updates.Add(update);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WMI ile güncelleme kontrolünde sorun");
            }
        }

        #endregion

        #region Office Update Detection

        /// <summary>
        /// Microsoft Office güncellemelerini tespit eder
        /// </summary>
        private async Task<List<SystemUpdate>> DetectOfficeUpdatesAsync(Guid deviceId)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                _logger.LogInformation("Office güncellemeleri kontrol ediliyor...");

                await Task.Run(() =>
                {
                    // Office sürümlerini tespit et
                    var officeVersions = DetectInstalledOfficeVersions();

                    foreach (var office in officeVersions)
                    {
                        // Her Office sürümü için mevcut güncellemeleri kontrol et
                        var officeUpdates = CheckOfficeUpdatesForVersion(deviceId, office);
                        updates.AddRange(officeUpdates);
                    }
                });

                _logger.LogInformation("{Count} Office güncellemesi tespit edildi", updates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Office güncellemesi tespitinde hata");
            }

            return updates;
        }

        /// <summary>
        /// Yüklü Office sürümlerini tespit eder (hem MSI hem de C2R sürümler)
        /// </summary>
        private List<OfficeVersion> DetectInstalledOfficeVersions()
        {
            var versions = new List<OfficeVersion>();

            try
            {
                // İlk olarak C2R (Click-to-Run) sürümleri kontrol et
                var c2rVersions = DetectOfficeC2RVersions();
                versions.AddRange(c2rVersions);

                // Sonra MSI tabanlı sürümleri kontrol et (sadece C2R bulunamadıysa)
                if (!versions.Any())
                {
                    using var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_Product WHERE Name LIKE '%Microsoft Office%' OR Name LIKE '%Microsoft 365%'");
                    using var results = searcher.Get();

                foreach (ManagementObject result in results.Cast<ManagementObject>())
                {
                    var name = result["Name"]?.ToString();
                    var version = result["Version"]?.ToString();
                    var installLocation = result["InstallLocation"]?.ToString();

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
                    {
                        versions.Add(new OfficeVersion
                        {
                            ProductName = name,
                            Version = version,
                            InstallPath = installLocation ?? "",
                            InstallationType = "MSI"
                        });
                    }
                }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Office sürüm tespitinde sorun");
            }

            return versions;
        }

        /// <summary>
        /// Office Click-to-Run (C2R) sürümlerini tespit eder
        /// </summary>
        private List<OfficeVersion> DetectOfficeC2RVersions()
        {
            var versions = new List<OfficeVersion>();

            try
            {
                // C2R Office için kayıt defteri kontrol
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (key != null)
                {
                    var productIds = key.GetValue("ProductReleaseIds")?.ToString();
                    var versionToReport = key.GetValue("VersionToReport")?.ToString();
                    var platform = key.GetValue("Platform")?.ToString();
                    var channel = key.GetValue("CDNBaseUrl")?.ToString();

                    if (!string.IsNullOrEmpty(productIds) && !string.IsNullOrEmpty(versionToReport))
                    {
                        var officeVersion = new OfficeVersion
                        {
                            ProductName = $"Microsoft 365 Apps ({productIds})",
                            Version = versionToReport,
                            InstallPath = key.GetValue("InstallationPath")?.ToString() ?? "",
                            InstallationType = "C2R",
                            Platform = platform,
                            UpdateChannel = ExtractChannelFromUrl(channel)
                        };

                        versions.Add(officeVersion);
                        _logger.LogInformation("Office C2R tespit edildi: {Product} v{Version} - {Channel}", 
                            officeVersion.ProductName, officeVersion.Version, officeVersion.UpdateChannel);
                    }
                }

                // Office 2019/2021 C2R kontrol
                using var officeKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office");
                if (officeKey != null)
                {
                    foreach (var subKeyName in officeKey.GetSubKeyNames().Where(name => name.Contains("16.0") || name.Contains("15.0")))
                    {
                        using var versionKey = officeKey.OpenSubKey($@"{subKeyName}\Common\InstallRoot");
                        if (versionKey != null)
                        {
                            var path = versionKey.GetValue("Path")?.ToString();
                            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
                            {
                                var c2rConfigPath = System.IO.Path.Combine(path, "AppvIsvStream32.xml");
                                if (System.IO.File.Exists(c2rConfigPath) && !versions.Any(v => v.InstallPath == path))
                                {
                                    var officeName = subKeyName.Contains("16.0") ? "Microsoft Office 2019/2021" : "Microsoft Office 2013";
                                    versions.Add(new OfficeVersion
                                    {
                                        ProductName = officeName,
                                        Version = subKeyName,
                                        InstallPath = path,
                                        InstallationType = "C2R"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Office C2R tespit işleminde sorun");
            }

            return versions;
        }

        /// <summary>
        /// Güncellenme kanalını URL'den çıkarır
        /// </summary>
        private string ExtractChannelFromUrl(string? channelUrl)
        {
            if (string.IsNullOrEmpty(channelUrl))
                return "Unknown";

            if (channelUrl.Contains("492350f6-3a01-4f97-b9c0-c7c6ddf67d60"))
                return "Current Channel";
            else if (channelUrl.Contains("7ffbc6bf-bc32-4f92-8982-f9dd17fd3114"))
                return "Semi-Annual Enterprise Channel";
            else if (channelUrl.Contains("64256afe-f5d9-4f86-8936-8840a6a4f5be"))
                return "Monthly Enterprise Channel";
            else if (channelUrl.Contains("55336b82-a18d-4dd6-b5f6-9e5095c314a6"))
                return "Semi-Annual Enterprise Channel (Preview)";
            else if (channelUrl.Contains("5440fd1f-7ecb-4221-8110-145efaa6372f"))
                return "Beta Channel";
            else
                return "Custom";
        }

        /// <summary>
        /// Belirli Office sürümü için güncellemeleri kontrol eder
        /// </summary>
        private List<SystemUpdate> CheckOfficeUpdatesForVersion(Guid deviceId, OfficeVersion office)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                if (office.InstallationType == "C2R")
                {
                    // C2R sürümler için özel kontrol
                    var c2rUpdates = CheckOfficeC2RUpdates(office);
                    foreach (var c2rUpdate in c2rUpdates)
                    {
                        var update = new SystemUpdate
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            UpdateType = "Office (C2R)",
                            Title = $"{office.ProductName} - {c2rUpdate.Title}",
                            Description = $"C2R Update via {office.UpdateChannel}: {c2rUpdate.Description}",
                            CurrentVersion = office.Version,
                            LatestVersion = c2rUpdate.LatestVersion,
                            Status = c2rUpdate.IsInstalled ? UpdateStatus.Installed : UpdateStatus.Available,
                            Priority = UpdatePriority.Normal,
                            DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                            LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                            CanAutoInstall = true,
                            RequiresRestart = false
                        };

                        updates.Add(update);
                    }
                }
                else
                {
                    // MSI sürümler için mevcut yöntem
                    var registryUpdates = ReadOfficeUpdatesFromRegistry(office);

                    foreach (var regUpdate in registryUpdates)
                    {
                        var update = new SystemUpdate
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            UpdateType = "Office (MSI)",
                            Title = $"{office.ProductName} - {regUpdate.Title}",
                            Description = regUpdate.Description,
                            CurrentVersion = office.Version,
                            LatestVersion = regUpdate.LatestVersion,
                            Status = regUpdate.IsInstalled ? UpdateStatus.Installed : UpdateStatus.Available,
                            Priority = UpdatePriority.Normal,
                            DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                            LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                            CanAutoInstall = true,
                            RequiresRestart = false
                        };

                        updates.Add(update);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Office {Product} için güncelleme kontrolünde sorun", office.ProductName);
            }

            return updates;
        }

        /// <summary>
        /// Office C2R güncellemelerini kontrol eder
        /// </summary>
        private List<OfficeRegistryUpdate> CheckOfficeC2RUpdates(OfficeVersion office)
        {
            var updates = new List<OfficeRegistryUpdate>();

            try
            {
                // C2R için ODT (Office Deployment Tool) kullanarak güncelleme kontrolü
                using var configKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (configKey != null)
                {
                    var updateUrl = configKey.GetValue("UpdateUrl")?.ToString();
                    var currentVersion = configKey.GetValue("VersionToReport")?.ToString();
                    var updateChannel = configKey.GetValue("UpdateChannel")?.ToString();

                    // Bu kısım kaldırıldı - sadece otomatik güncelleme etkin olması güncelleme mevcut demek değildir
                    // Sadece gerçekten bekleyen güncellemeler raporlanacak

                    // Bekleyen güncellemeleri daha spesifik kontrol et
                    using var updateKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Updates");
                    if (updateKey != null)
                    {
                        // Gerçek bir güncelleme var mı kontrol et
                        var availableVersion = updateKey.GetValue("UpdatesAvailable")?.ToString();
                        var updateToApply = updateKey.GetValue("UpdateToApply")?.ToString();
                        var executionState = updateKey.GetValue("ExecutionState")?.ToString();
                        
                        // Sadece gerçekten bekleyen güncelleme varsa rapor et
                        if (!string.IsNullOrEmpty(availableVersion) || 
                            !string.IsNullOrEmpty(updateToApply) || 
                            (executionState != null && executionState != "0" && executionState != "Idle"))
                        {
                            updates.Add(new OfficeRegistryUpdate
                            {
                                Title = "Bekleyen Office Güncellemesi",
                                Description = "Office C2R güncellemesi bekleniyor. Güncelleme otomatik olarak uygulanacak.",
                                LatestVersion = availableVersion ?? "Bekliyor",
                                IsInstalled = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Office C2R güncelleme kontrolünde sorun");
            }

            return updates;
        }

        #endregion

        #region .NET Framework Update Detection

        /// <summary>
        /// .NET Framework güncellemelerini tespit eder
        /// Sadece yeni sürüm mevcut olan .NET Framework güncellemelerini rapor eder
        /// </summary>
        private async Task<List<SystemUpdate>> DetectDotNetUpdatesAsync(Guid deviceId)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                _logger.LogInformation(".NET Framework güncellemeleri kontrol ediliyor...");

                await Task.Run(() =>
                {
                    // Windows Update Agent'dan .NET Framework güncellemelerini ara
                    try
                    {
                        // Windows Update Session oluştur
                        dynamic updateSession = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
                        dynamic updateSearcher = updateSession.CreateUpdateSearcher();

                        // .NET Framework ile ilgili güncellemeleri ara
                        var searchQuery = "IsInstalled=0 and Type='Software' and IsHidden=0";
                        _logger.LogDebug(".NET Framework güncellemeleri aranıyor: {Query}", searchQuery);
                        
                        dynamic searchResult = updateSearcher.Search(searchQuery);

                        foreach (dynamic update in searchResult.Updates)
                        {
                            string title = update.Title?.ToString() ?? "";
                            string description = update.Description?.ToString() ?? "";
                            
                            // Sadece .NET Framework ile ilgili güncellemeleri filtrele
                            if (title.Contains(".NET Framework") || title.Contains("Microsoft .NET Framework") || 
                                description.Contains(".NET Framework"))
                            {
                                var systemUpdate = new SystemUpdate
                                {
                                    Id = Guid.NewGuid(),
                                    DeviceId = deviceId,
                                    UpdateType = ".NET Framework",
                                    Title = title,
                                    Description = description,
                                    KBNumber = ExtractKBNumber(title),
                                    SizeInMB = GetUpdateSizeInMB(update),
                                    Status = UpdateStatus.Available,
                                    Priority = DeterminePriority(update),
                                    DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                                    LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                                    ReleaseDate = GetReleaseDate(update),
                                    CanAutoInstall = update.EulaAccepted ?? false,
                                    RequiresRestart = update.RebootRequired ?? false,
                                    SecurityBulletinId = ExtractSecurityBulletinId(update.SecurityBulletinIDs)
                                };

                                updates.Add(systemUpdate);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Windows Update Agent ile .NET Framework güncellemeleri aranamadı, registry kontrolüne geçiliyor");
                        
                        // Fallback: Sadece gerçekten eksik olan .NET Framework sürümlerini kontrol et
                        CheckForMissingCriticalDotNetVersions(deviceId, updates);
                    }
                });

                _logger.LogInformation("{Count} .NET Framework güncellemesi tespit edildi", updates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ".NET Framework güncellemesi tespitinde hata");
            }

            return updates;
        }

        /// <summary>
        /// Kritik .NET Framework sürümlerinin eksik olup olmadığını kontrol eder
        /// </summary>
        private void CheckForMissingCriticalDotNetVersions(Guid deviceId, List<SystemUpdate> updates)
        {
            try
            {
                // Registry'den yüklü .NET sürümlerini oku
                var installedVersions = ReadDotNetVersionsFromRegistry();
                var installedVersionStrings = installedVersions.Select(v => v.Version).ToHashSet();

                // Güncel critical .NET Framework sürümlerini kontrol et
                var criticalVersions = new Dictionary<string, string>
                {
                    { "4.8", "4.8.04161" },      // .NET Framework 4.8 (en güncel)
                    { "4.7.2", "4.7.03062" },    // .NET Framework 4.7.2
                    { "4.6.2", "4.6.01586" }     // .NET Framework 4.6.2
                };

                foreach (var criticalVersion in criticalVersions)
                {
                    bool hasThisVersionOrNewer = installedVersionStrings.Any(installed => 
                        installed.StartsWith(criticalVersion.Key) && 
                        string.Compare(installed, criticalVersion.Value, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!hasThisVersionOrNewer)
                    {
                        var update = new SystemUpdate
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            UpdateType = ".NET Framework",
                            Title = $"Microsoft .NET Framework {criticalVersion.Key}",
                            Description = $"Microsoft .NET Framework {criticalVersion.Key} is recommended for this system",
                            CurrentVersion = GetHighestInstalledDotNetVersion(installedVersions, criticalVersion.Key),
                            LatestVersion = criticalVersion.Value,
                            Status = UpdateStatus.Available,
                            Priority = UpdatePriority.High,
                            DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                            LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                            CanAutoInstall = true,
                            RequiresRestart = false
                        };

                        updates.Add(update);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kritik .NET Framework sürümleri kontrol edilemedi");
            }
        }

        /// <summary>
        /// Belirli bir major version için en yüksek yüklü .NET Framework sürümünü bulur
        /// </summary>
        private string? GetHighestInstalledDotNetVersion(List<DotNetVersion> installedVersions, string majorVersion)
        {
            return installedVersions
                .Where(v => v.Version.StartsWith(majorVersion))
                .OrderByDescending(v => v.Version)
                .FirstOrDefault()?.Version;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// KB numarasını metinden çıkarır
        /// </summary>
        private string ExtractKBNumber(string title)
        {
            var match = System.Text.RegularExpressions.Regex.Match(title, @"KB(\d+)");
            return match.Success ? match.Value : "";
        }

        /// <summary>
        /// Güvenlik bülteni ID'sini çıkarır
        /// </summary>
        private string? ExtractSecurityBulletinId(dynamic bulletinIds)
        {
            try
            {
                if (bulletinIds != null && bulletinIds.Count > 0)
                {
                    return bulletinIds[0]?.ToString();
                }
            }
            catch
            {
                // Ignore error
            }
            return null;
        }

        /// <summary>
        /// Güncelleme türünü belirler (Windows, Office, .NET Framework, vb.)
        /// </summary>
        private string DetermineUpdateType(dynamic update)
        {
            try
            {
                string title = update.Title?.ToString() ?? "";
                string description = update.Description?.ToString() ?? "";
                
                // Office güncellemeleri
                if (title.Contains("Office") || title.Contains("Microsoft Office") || 
                    title.Contains("Word") || title.Contains("Excel") || title.Contains("PowerPoint") || 
                    title.Contains("Outlook") || title.Contains("Access") || title.Contains("Publisher") ||
                    title.Contains("OneNote") || title.Contains("Project") || title.Contains("Visio") ||
                    description.Contains("Office") || description.Contains("Microsoft Office"))
                {
                    return "Office";
                }
                
                // .NET Framework güncellemeleri
                if (title.Contains(".NET Framework") || title.Contains("Microsoft .NET Framework") ||
                    description.Contains(".NET Framework"))
                {
                    return ".NET Framework";
                }
                
                // Visual C++ Redistributable güncellemeleri
                if (title.Contains("Visual C++") || title.Contains("Microsoft Visual C++") ||
                    title.Contains("VC++ Redistributable"))
                {
                    return "Visual C++";
                }
                
                // SQL Server güncellemeleri
                if (title.Contains("SQL Server") || title.Contains("Microsoft SQL Server"))
                {
                    return "SQL Server";
                }
                
                // Driver güncellemeleri
                if (update.Type?.ToString() == "Driver" || title.Contains("Driver") || 
                    title.Contains("Sürücü"))
                {
                    return "Driver";
                }
                
                // Windows Defender/Security güncellemeleri
                if (title.Contains("Windows Defender") || title.Contains("Malicious Software Removal Tool") ||
                    title.Contains("Security Essentials") || title.Contains("Antimalware"))
                {
                    return "Security";
                }
                
                // Edge güncellemeleri
                if (title.Contains("Microsoft Edge") || title.Contains("Edge"))
                {
                    return "Edge";
                }
                
                // Varsayılan olarak Windows güncellemesi
                return "Windows";
            }
            catch
            {
                return "Windows";
            }
        }

        /// <summary>
        /// Güncelleme boyutunu MB cinsinden alır
        /// </summary>
        private double? GetUpdateSizeInMB(dynamic update)
        {
            try
            {
                var maxDownloadSize = update.MaxDownloadSize;
                if (maxDownloadSize != null && maxDownloadSize > 0)
                {
                    return Math.Round((double)maxDownloadSize / (1024.0 * 1024.0), 2);
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        /// <summary>
        /// Güncelleme yayın tarihini alır
        /// </summary>
        private DateTime? GetReleaseDate(dynamic update)
        {
            try
            {
                var lastDeploymentChangeTime = update.LastDeploymentChangeTime;
                if (lastDeploymentChangeTime != null)
                {
                    return (DateTime)lastDeploymentChangeTime;
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        /// <summary>
        /// Güncelleme önceliğini belirler
        /// </summary>
        private UpdatePriority DeterminePriority(dynamic update)
        {
            try
            {
                var title = update.Title?.ToString()?.ToLower() ?? "";
                
                if (title.Contains("security") || title.Contains("güvenlik"))
                    return UpdatePriority.Security;
                
                if (title.Contains("critical") || title.Contains("kritik"))
                    return UpdatePriority.Critical;
                    
                if (title.Contains("important") || title.Contains("önemli"))
                    return UpdatePriority.High;
                    
                return UpdatePriority.Normal;
            }
            catch
            {
                return UpdatePriority.Normal;
            }
        }

        /// <summary>
        /// Güncelleme sonuçlarını dosyaya kaydeder
        /// </summary>
        private async Task SaveUpdateResultsToFileAsync(List<SystemUpdate> updates)
        {
            try
            {
                var fileName = $"updates-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                var filePath = Path.Combine(_logPath, fileName);

                var json = JsonSerializer.Serialize(updates, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Güncelleme sonuçları kaydedildi: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Güncelleme sonuçları kaydedilemedi");
            }
        }

        /// <summary>
        /// Registry'den Office güncellemelerini okur
        /// </summary>
        private List<OfficeRegistryUpdate> ReadOfficeUpdatesFromRegistry(OfficeVersion office)
        {
            // Registry okuma implementasyonu
            // Bu method Office'in registry anahtarlarından güncelleme bilgilerini okur
            return new List<OfficeRegistryUpdate>();
        }

        /// <summary>
        /// Registry'den .NET sürümlerini okur
        /// </summary>
        private List<DotNetVersion> ReadDotNetVersionsFromRegistry()
        {
            var versions = new List<DotNetVersion>();
            
            try
            {
                // .NET Framework registry anahtarlarını oku
                using var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");
                if (ndpKey != null)
                {
                    foreach (var versionKeyName in ndpKey.GetSubKeyNames())
                    {
                        if (versionKeyName.StartsWith("v"))
                        {
                            using var versionKey = ndpKey.OpenSubKey(versionKeyName);
                            var version = versionKey?.GetValue("Version")?.ToString();
                            
                            if (!string.IsNullOrEmpty(version))
                            {
                                versions.Add(new DotNetVersion { Version = version });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ".NET sürüm bilgileri okunamadı");
            }

            return versions;
        }

        #endregion

        #region Helper Classes

        private class OfficeVersion
        {
            public string ProductName { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string InstallPath { get; set; } = string.Empty;
            public string InstallationType { get; set; } = string.Empty; // MSI or C2R
            public string? Platform { get; set; } // x86 or x64
            public string? UpdateChannel { get; set; } // For C2R installations
        }

        private class OfficeRegistryUpdate
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string LatestVersion { get; set; } = string.Empty;
            public bool IsInstalled { get; set; }
        }

        private class DotNetVersion
        {
            public string Version { get; set; } = string.Empty;
        }

        #endregion
    }
}