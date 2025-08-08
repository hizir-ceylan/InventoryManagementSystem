
using Inventory.Api.Services;
using Inventory.Api.BackgroundServices;
using Inventory.Api.Middleware;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api
{
    /// <summary>
    /// Inventory Management System - Ana API Sunucusu
    /// Bu sınıf sistem başlatma, servislerin kaydı ve middleware yapılandırmasını yönetir
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================================================
            // BÖLÜM 1: WINDOWS SERVICE KONFIGÜRASYONU
            // =====================================================================
            
            // Windows Service desteği - Sunucuda service olarak çalışabilmesi için
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "InventoryManagementApi";
            });

            // =====================================================================
            // BÖLÜM 2: VERİTABANI BAĞLANTI KONFIGÜRASYONU
            // =====================================================================
            
            // Veritabanı bağlantı dizesini yapılandır
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var serverSettings = builder.Configuration.GetSection("ServerSettings");
            var serverMode = serverSettings.GetValue<string>("Mode", "Local");
            var remoteConnectionString = serverSettings.GetValue<string>("RemoteDatabaseConnectionString", "");

            // Uzak sunucu konfigürasyonu - Production ortamları için
            if (!string.IsNullOrEmpty(remoteConnectionString) && serverMode.Equals("Remote", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = remoteConnectionString;
            }

            // SQLite varsayılan konfigürasyonu - Geliştirme ve test ortamları için
            if (string.IsNullOrEmpty(connectionString))
            {
                // Önce sistem geneli veri dizini environment variable'ını kontrol et (Windows Service için)
                var dataPath = Environment.GetEnvironmentVariable("INVENTORY_DATA_PATH", EnvironmentVariableTarget.Machine);
                
                if (!string.IsNullOrEmpty(dataPath) && Directory.Exists(dataPath))
                {
                    // Sistem geneli persistent path kullan (Windows Service için ideal)
                    var dbPath = Path.Combine(dataPath, "inventory.db");
                    connectionString = $"Data Source={dbPath}";
                }
                else
                {
                    // Fallback: Uygulama dizininde 'Data' klasörü oluştur ve veritabanını orada sakla
                    var appDirectory = AppContext.BaseDirectory;
                    var dataDirectory = Path.Combine(appDirectory, "Data");
                    
                    // Data klasörünün var olduğundan emin ol
                    if (!Directory.Exists(dataDirectory))
                    {
                        Directory.CreateDirectory(dataDirectory);
                    }
                    
                    var dbPath = Path.Combine(dataDirectory, "inventory.db");
                    connectionString = $"Data Source={dbPath}";
                }
            }

            // Veritabanı provider seçimi - SQLite veya SQL Server
            if (connectionString.Contains("Data Source"))
            {
                // SQLite konfigürasyonu - Development ve küçük kurumlar için
                builder.Services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlite(connectionString));
            }
            else
            {
                // SQL Server konfigürasyonu - Production ve büyük kurumlar için  
                builder.Services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            // =====================================================================
            // BÖLÜM 3: DEPENDENCY INJECTION VE SERVİS KAYITLARI
            // =====================================================================
            
            // ASP.NET Core temel servisleri
            builder.Services.AddControllers();
            
            // Business logic servisleri
            builder.Services.AddScoped<INetworkScanService, NetworkScanService>();
            builder.Services.AddScoped<INetworkScannerService, NetworkScannerService>();
            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddSingleton<ICentralizedLoggingService, CentralizedLoggingService>();
            
            // Background (arka plan) servisleri - Otomatik işlemler için
            builder.Services.AddHostedService<NetworkScanBackgroundService>();
            
            // =====================================================================
            // BÖLÜM 4: SWAGGER/OPENAPI DOKÜMANTASYON KONFIGÜRASYONU
            // =====================================================================
            
            // API keşfedilebilirlik için Swagger/OpenAPI desteği
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Inventory Management System API",
                    Version = "v1",
                    Description = "Agent kurulu ve ağ keşfi ile bulunan cihazları destekleyen envanter cihaz yönetimi API'si"
                });
                c.EnableAnnotations(); // Controller'lardaki SwaggerOperation attribute'larını etkinleştir
            });

            // =====================================================================
            // BÖLÜM 5: CORS (Cross-Origin Resource Sharing) KONFIGÜRASYONU
            // =====================================================================
            
            // Web uygulaması ve Docker ortamları için CORS desteği
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()   // Tüm domain'lerden erişim izni
                            .AllowAnyMethod()   // Tüm HTTP methodları (GET, POST, PUT, DELETE)
                            .AllowAnyHeader();  // Tüm HTTP header'ları
                    });
            });

            var app = builder.Build();

            // =====================================================================
            // BÖLÜM 6: VERİTABANI BAŞLATMA VE MİGRATION
            // =====================================================================
            
            // Uygulama başlatılırken veritabanının hazır olduğundan emin ol
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    logger.LogInformation("Veritabanı bağlantısı kontrol ediliyor...");
                    context.Database.EnsureCreated(); // Veritabanı yoksa oluştur
                    logger.LogInformation("Veritabanı başarıyla oluşturuldu veya mevcut.");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Veritabanı oluşturulurken hata oluştu: {ErrorMessage}", ex.Message);
                    throw; // Veritabanı hatası kritik - uygulama başlatılamaz
                }
            }

            // =====================================================================
            // BÖLÜM 7: HTTP PIPELINE VE MIDDLEWARE KONFIGÜRASYONU
            // =====================================================================

            // Swagger UI - API dokümantasyonu (tüm ortamlarda aktif)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Management System API v1");
                c.RoutePrefix = ""; // Swagger UI'ı root'ta (/) kullanılabilir yapar
            });

            // Static dosya desteği (CSS, JS, resimler için)
            app.UseStaticFiles();

            // CORS middleware'ini etkinleştir - Web uygulaması bağlantıları için
            app.UseCors("AllowAll");

            // İstek loglaması - Tüm API çağrılarını logla
            app.UseMiddleware<RequestLoggingMiddleware>();

            // HTTPS yönlendirmesi Docker ortamında devre dışı
            // app.UseHttpsRedirection();

            // Authorization middleware (şu anda pasif)
            app.UseAuthorization();

            // Controller routing - API endpoint'lerini etkinleştir
            app.MapControllers();

            // =====================================================================
            // BÖLÜM 8: API BİLGİ ENDPOINT'İ
            // =====================================================================
            
            // Sistem durumu ve bilgi endpoint'i - Sağlık kontrolü için
            app.MapGet("/api/info", () => new
            {
                Message = "Inventory Management System API",
                Version = "v1.0",
                WebInterface = "Web arayüzü artık ayrı bir uygulamada çalışıyor. Inventory.WebApp projesini başlatın.",
                Swagger = "/",
                Status = "Running"
            });

            // Uygulamayı başlat
            app.Run();
        }
    }
}
