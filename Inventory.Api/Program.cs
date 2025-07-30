
using Inventory.Api.Services;
using Inventory.Api.BackgroundServices;
using Inventory.Api.Middleware;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Windows Service support ekle
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "InventoryManagementApi";
            });

            // Veritabanı bağlamını ekle
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var serverSettings = builder.Configuration.GetSection("ServerSettings");
            var serverMode = serverSettings.GetValue<string>("Mode", "Local");
            var remoteConnectionString = serverSettings.GetValue<string>("RemoteDatabaseConnectionString", "");

            // Uzak bağlantı dizesi yapılandırılmış ve mod uzak olarak ayarlanmışsa onu kullan
            if (!string.IsNullOrEmpty(remoteConnectionString) && serverMode.Equals("Remote", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = remoteConnectionString;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                // Bağlantı dizesi sağlanmamışsa varsayılan olarak SQLite kullan
                // Önce sistem geneli veri yolu environment variable'ını kontrol et (Windows Service için)
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

            if (connectionString.Contains("Data Source"))
            {
                // SQLite
                builder.Services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlite(connectionString));
            }
            else
            {
                // SQL Server
                builder.Services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            // Servisleri container'a ekle
            builder.Services.AddControllers();
            
            // Özel servisleri kaydet
            builder.Services.AddScoped<INetworkScanService, NetworkScanService>();
            builder.Services.AddScoped<INetworkScannerService, NetworkScannerService>();
            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddSingleton<ICentralizedLoggingService, CentralizedLoggingService>();
            
            // Arka plan servislerini kaydet
            builder.Services.AddHostedService<NetworkScanBackgroundService>();
            
            // Swagger/OpenAPI yapılandırması hakkında daha fazla bilgi için: https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Inventory Management System API",
                    Version = "v1",
                    Description = "Agent kurulu ve ağ keşfi ile bulunan cihazları destekleyen envanter cihaz yönetimi API'si"
                });
                c.EnableAnnotations();
            });

            // Docker ortamı için CORS desteği ekle
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            var app = builder.Build();

            // Veritabanının oluşturulduğundan emin ol
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    logger.LogInformation("Veritabanı bağlantısı kontrol ediliyor...");
                    context.Database.EnsureCreated();
                    logger.LogInformation("Veritabanı başarıyla oluşturuldu veya mevcut.");
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Veritabanı oluşturulurken hata oluştu: {ErrorMessage}", ex.Message);
                    throw;
                }
            }

            // HTTP istek pipeline'ını yapılandır
            // Docker testleri için tüm ortamlarda Swagger'ı etkinleştir
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Management System API v1");
                c.RoutePrefix = string.Empty; // Swagger UI'ı kök dizinde kullanılabilir yapar
            });

            // CORS'u etkinleştir
            app.UseCors("AllowAll");

            // İstek günlükleme middleware'ini ekle
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Docker ortamı için HTTPS yönlendirmesini kaldır
            // app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
