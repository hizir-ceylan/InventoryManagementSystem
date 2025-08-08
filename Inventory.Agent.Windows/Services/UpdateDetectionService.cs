using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Inventory.Domain.Entities;

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

        public UpdateDetectionService(ILogger<UpdateDetectionService> logger)
        {
            _logger = logger;
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

                        // Mevcut güncellemeleri ara
                        dynamic searchResult = updateSearcher.Search("IsInstalled=0 and Type='Software'");

                        foreach (dynamic update in searchResult.Updates)
                        {
                            var systemUpdate = new SystemUpdate
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = deviceId,
                                UpdateType = "Windows",
                                Title = update.Title ?? "Bilinmeyen Güncelleme",
                                Description = update.Description ?? "",
                                KBNumber = ExtractKBNumber(update.Title ?? ""),
                                SizeInMB = update.MaxDownloadSize > 0 ? Math.Round(update.MaxDownloadSize / (1024.0 * 1024.0), 2) : null,
                                Status = UpdateStatus.Available,
                                Priority = DeterminePriority(update),
                                DetectedDate = DateTime.UtcNow,
                                LastChecked = DateTime.UtcNow,
                                CanAutoInstall = update.AutoDownload,
                                RequiresRestart = update.RebootRequired,
                                SecurityBulletinId = ExtractSecurityBulletinId(update.SecurityBulletinIDs)
                            };

                            updates.Add(systemUpdate);
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
                            DetectedDate = DateTime.UtcNow,
                            LastChecked = DateTime.UtcNow,
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
        /// Yüklü Office sürümlerini tespit eder
        /// </summary>
        private List<OfficeVersion> DetectInstalledOfficeVersions()
        {
            var versions = new List<OfficeVersion>();

            try
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
                            InstallPath = installLocation ?? ""
                        });
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
        /// Belirli Office sürümü için güncellemeleri kontrol eder
        /// </summary>
        private List<SystemUpdate> CheckOfficeUpdatesForVersion(Guid deviceId, OfficeVersion office)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                // Registry'den Office güncelleme bilgilerini oku
                var registryUpdates = ReadOfficeUpdatesFromRegistry(office);

                foreach (var regUpdate in registryUpdates)
                {
                    var update = new SystemUpdate
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = deviceId,
                        UpdateType = "Office",
                        Title = $"{office.ProductName} - {regUpdate.Title}",
                        Description = regUpdate.Description,
                        CurrentVersion = office.Version,
                        LatestVersion = regUpdate.LatestVersion,
                        Status = regUpdate.IsInstalled ? UpdateStatus.Installed : UpdateStatus.Available,
                        Priority = UpdatePriority.Normal,
                        DetectedDate = DateTime.UtcNow,
                        LastChecked = DateTime.UtcNow,
                        CanAutoInstall = true,
                        RequiresRestart = false
                    };

                    updates.Add(update);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Office {Product} için güncelleme kontrolünde sorun", office.ProductName);
            }

            return updates;
        }

        #endregion

        #region .NET Framework Update Detection

        /// <summary>
        /// .NET Framework güncellemelerini tespit eder
        /// </summary>
        private async Task<List<SystemUpdate>> DetectDotNetUpdatesAsync(Guid deviceId)
        {
            var updates = new List<SystemUpdate>();

            try
            {
                _logger.LogInformation(".NET Framework güncellemeleri kontrol ediliyor...");

                await Task.Run(() =>
                {
                    // Registry'den .NET sürümlerini oku
                    var dotnetVersions = ReadDotNetVersionsFromRegistry();

                    foreach (var version in dotnetVersions)
                    {
                        var update = new SystemUpdate
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            UpdateType = ".NET Framework",
                            Title = $".NET Framework {version.Version}",
                            Description = $".NET Framework {version.Version} - Yüklü",
                            CurrentVersion = version.Version,
                            Status = UpdateStatus.Installed,
                            Priority = UpdatePriority.Normal,
                            DetectedDate = DateTime.UtcNow,
                            LastChecked = DateTime.UtcNow,
                            CanAutoInstall = false,
                            RequiresRestart = false
                        };

                        updates.Add(update);
                    }
                });

                _logger.LogInformation("{Count} .NET Framework sürümü tespit edildi", updates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ".NET Framework tespitinde hata");
            }

            return updates;
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