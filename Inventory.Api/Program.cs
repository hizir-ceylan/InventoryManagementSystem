
using Inventory.Api.Services;
using Inventory.Api.BackgroundServices;
using Inventory.Api.Middleware;

namespace Inventory.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Management System API v1");
                    c.RoutePrefix = string.Empty; // Makes Swagger UI available at the root
                });
            }

            // Add request logging middleware
            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
