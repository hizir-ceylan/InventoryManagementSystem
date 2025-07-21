
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

            // Add database context
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Default to SQLite if no connection string is provided
                connectionString = "Data Source=inventory.db";
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

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Register custom services
            builder.Services.AddScoped<INetworkScanService, NetworkScanService>();
            builder.Services.AddScoped<INetworkScannerService, NetworkScannerService>();
            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddSingleton<ICentralizedLoggingService, CentralizedLoggingService>();
            
            // Register background services
            builder.Services.AddHostedService<NetworkScanBackgroundService>();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Inventory Management System API",
                    Version = "v1",
                    Description = "API for managing inventory devices with support for both agent-installed and network-discovered devices"
                });
                c.EnableAnnotations();
            });

            // Add CORS support for Docker environment
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

            // Ensure database is created
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                context.Database.EnsureCreated();
            }

            // Configure the HTTP request pipeline.
            // Enable Swagger in all environments for Docker testing
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Management System API v1");
                c.RoutePrefix = string.Empty; // Makes Swagger UI available at the root
            });

            // Enable CORS
            app.UseCors("AllowAll");

            // Add request logging middleware
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Remove HTTPS redirection for Docker environment
            // app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
