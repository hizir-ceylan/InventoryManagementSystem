using Inventory.Data;
using Inventory.Domain.Entities;
using Inventory.Api.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services
{
    public interface IVMwareService
    {
        // Server Configuration
        Task<VMwareStatusDto> GetServerStatusAsync();
        Task UpdateConfigurationAsync(VMwareConfigurationDto config);
        Task<VMwareConnectionResult> TestConnectionAsync();
        
        // Virtual Machines
        Task<IEnumerable<VirtualMachineDto>> GetVirtualMachinesAsync();
        Task<VirtualMachineDto?> GetVirtualMachineAsync(Guid id);
        Task<VMwareSyncResult> SyncVirtualMachinesAsync();
        Task<VMwareAddResult> AddVirtualMachineToInventoryAsync(string vmwareId);
        
        // Sync History
        Task<IEnumerable<VMwareSyncLogDto>> GetSyncHistoryAsync(DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<VMwareSyncLogDto?> GetSyncLogDetailsAsync(Guid id);
    }

    public class VMwareService : IVMwareService
    {
        private readonly ILogger<VMwareService> _logger;
        private readonly InventoryDbContext _context;
        private readonly IConfiguration _configuration;

        public VMwareService(
            ILogger<VMwareService> logger, 
            InventoryDbContext context, 
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        #region Server Configuration

        public async Task<VMwareStatusDto> GetServerStatusAsync()
        {
            try
            {
                var server = await GetOrCreateVMwareServerAsync();
                
                return new VMwareStatusDto
                {
                    Connected = server.IsConnected,
                    ServerAddress = server.ServerAddress,
                    LastConnection = server.LastConnection,
                    Error = server.ConnectionError,
                    LastSync = server.LastSyncDate,
                    Metrics = server.IsConnected ? new VMwareMetricsDto
                    {
                        TotalStorageGB = server.TotalStorageGB,
                        UsedStorageGB = server.UsedStorageGB,
                        FreeStorageGB = server.FreeStorageGB,
                        CpuUsagePercent = server.CpuUsagePercent,
                        MemoryUsagePercent = (server.UsedMemoryMB ?? 0) * 100.0 / (server.TotalMemoryMB ?? 1),
                        ActiveVMs = server.RunningVMs,
                        TotalVMs = server.TotalVMs
                    } : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VMware server status");
                return new VMwareStatusDto
                {
                    Connected = false,
                    Error = ex.Message
                };
            }
        }

        public async Task UpdateConfigurationAsync(VMwareConfigurationDto config)
        {
            try
            {
                var server = await GetOrCreateVMwareServerAsync();
                
                server.ServerAddress = config.ServerAddress;
                server.Username = config.Username;
                server.SyncIntervalMinutes = config.SyncInterval;
                server.UpdatedAt = DateTime.UtcNow;
                
                // Note: In production, password should be encrypted
                // For now, we'll store a flag that password was updated
                if (!string.IsNullOrEmpty(config.Password))
                {
                    _logger.LogInformation("VMware password updated (not stored in this demo implementation)");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("VMware configuration updated for server: {ServerAddress}", config.ServerAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating VMware configuration");
                throw;
            }
        }

        public async Task<VMwareConnectionResult> TestConnectionAsync()
        {
            try
            {
                var server = await GetOrCreateVMwareServerAsync();
                
                // In a real implementation, this would use VMware PowerCLI or .NET SDK
                // For demo purposes, we'll simulate a connection test
                
                var isLocalhost = server.ServerAddress?.Contains("localhost") == true || 
                                 server.ServerAddress?.Contains("127.0.0.1") == true;
                
                if (isLocalhost)
                {
                    // Simulate failed connection for localhost
                    server.IsConnected = false;
                    server.ConnectionError = "Connection refused: VMware vSphere not running on localhost";
                    server.LastConnection = DateTime.UtcNow;
                }
                else if (server.ServerAddress == "10.0.0.10")
                {
                    // Simulate successful connection for the specified server
                    server.IsConnected = true;
                    server.ConnectionError = null;
                    server.LastConnection = DateTime.UtcNow;
                    server.Version = "7.0.3";
                    server.Build = "20845922";
                    server.ProductType = "vCenter";
                    
                    // Simulate some metrics
                    server.TotalStorageGB = 2048;
                    server.UsedStorageGB = 1024;
                    server.FreeStorageGB = 1024;
                    server.TotalVMs = 15;
                    server.RunningVMs = 12;
                    server.StoppedVMs = 3;
                }
                else
                {
                    // Simulate connection attempt for other servers
                    server.IsConnected = false;
                    server.ConnectionError = $"Cannot connect to {server.ServerAddress}: Host unreachable";
                    server.LastConnection = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new VMwareConnectionResult
                {
                    Success = server.IsConnected,
                    Error = server.ConnectionError,
                    ServerInfo = server.IsConnected ? new
                    {
                        Version = server.Version,
                        Build = server.Build,
                        ProductType = server.ProductType
                    } : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing VMware connection");
                return new VMwareConnectionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        #endregion

        #region Virtual Machines

        public async Task<IEnumerable<VirtualMachineDto>> GetVirtualMachinesAsync()
        {
            try
            {
                // In a real implementation, this would query VMware vSphere
                // For demo purposes, we'll return data from our database and simulate some VMs
                
                var existingVMs = await _context.VMwareInfos
                    .Include(v => v.Device)
                    .Select(v => new VirtualMachineDto
                    {
                        Id = v.VMwareId,
                        Name = v.Device!.Name,
                        IpAddress = v.Device.IpAddress,
                        PowerState = v.PowerState,
                        GuestOS = v.GuestOS,
                        CpuCount = v.CpuCount,
                        MemoryGB = v.MemoryMB / 1024,
                        DiskGB = v.ProvisionedSpaceGB,
                        HostName = v.HostName,
                        InInventory = true,
                        DeviceId = v.DeviceId
                    })
                    .ToListAsync();

                // Simulate some additional VMs that are not yet in inventory
                var simulatedVMs = new List<VirtualMachineDto>();
                
                var server = await GetOrCreateVMwareServerAsync();
                if (server.IsConnected && server.ServerAddress == "10.0.0.10")
                {
                    simulatedVMs.AddRange(new[]
                    {
                        new VirtualMachineDto
                        {
                            Id = "vm-001",
                            Name = "PROD-WEB-01",
                            IpAddress = "10.0.0.101",
                            PowerState = "poweredOn",
                            GuestOS = "Windows Server 2022",
                            CpuCount = 4,
                            MemoryGB = 16,
                            DiskGB = 200,
                            HostName = "esxi-01.company.local",
                            InInventory = false
                        },
                        new VirtualMachineDto
                        {
                            Id = "vm-002", 
                            Name = "PROD-DB-01",
                            IpAddress = "10.0.0.102",
                            PowerState = "poweredOn",
                            GuestOS = "Ubuntu Server 22.04",
                            CpuCount = 8,
                            MemoryGB = 32,
                            DiskGB = 500,
                            HostName = "esxi-02.company.local",
                            InInventory = false
                        },
                        new VirtualMachineDto
                        {
                            Id = "vm-003",
                            Name = "TEST-ENV-01",
                            IpAddress = "10.0.0.103",
                            PowerState = "poweredOff",
                            GuestOS = "CentOS 8",
                            CpuCount = 2,
                            MemoryGB = 8,
                            DiskGB = 100,
                            HostName = "esxi-01.company.local",
                            InInventory = false
                        }
                    });
                }

                return existingVMs.Concat(simulatedVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual machines");
                throw;
            }
        }

        public async Task<VirtualMachineDto?> GetVirtualMachineAsync(Guid id)
        {
            try
            {
                var vmwareInfo = await _context.VMwareInfos
                    .Include(v => v.Device)
                    .FirstOrDefaultAsync(v => v.DeviceId == id);

                if (vmwareInfo?.Device != null)
                {
                    return new VirtualMachineDto
                    {
                        Id = vmwareInfo.VMwareId,
                        Name = vmwareInfo.Device.Name,
                        IpAddress = vmwareInfo.Device.IpAddress,
                        PowerState = vmwareInfo.PowerState,
                        GuestOS = vmwareInfo.GuestOS,
                        CpuCount = vmwareInfo.CpuCount,
                        MemoryGB = vmwareInfo.MemoryMB / 1024,
                        DiskGB = vmwareInfo.ProvisionedSpaceGB,
                        HostName = vmwareInfo.HostName,
                        InInventory = true,
                        DeviceId = id
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual machine {Id}", id);
                throw;
            }
        }

        public async Task<VMwareSyncResult> SyncVirtualMachinesAsync()
        {
            var syncLog = new VMwareSyncLog
            {
                Id = Guid.NewGuid(),
                SyncStartTime = DateTime.UtcNow,
                Status = "InProgress",
                SyncType = "Manual",
                SyncTrigger = "User"
            };

            try
            {
                var server = await GetOrCreateVMwareServerAsync();
                syncLog.VMwareServerId = server.Id;

                _context.VMwareSyncLogs.Add(syncLog);
                await _context.SaveChangesAsync();

                // In a real implementation, this would connect to VMware and sync VMs
                // For demo purposes, we'll simulate the sync process

                await Task.Delay(2000); // Simulate sync time

                if (server.IsConnected)
                {
                    // Simulate finding and processing VMs
                    syncLog.VirtualMachinesFound = 15;
                    syncLog.VirtualMachinesCreated = 3;
                    syncLog.VirtualMachinesUpdated = 12;
                    syncLog.VirtualMachinesDeleted = 0;
                    syncLog.ErrorsEncountered = 0;
                    syncLog.Status = "Completed";
                    
                    // Update server stats
                    server.LastSyncDate = DateTime.UtcNow;
                    server.LastSyncStatus = "Success";
                    server.LastSyncError = null;
                }
                else
                {
                    syncLog.Status = "Failed";
                    syncLog.ErrorMessage = "VMware server not connected";
                    syncLog.ErrorsEncountered = 1;
                }

                syncLog.SyncEndTime = DateTime.UtcNow;
                syncLog.Duration = syncLog.SyncEndTime - syncLog.SyncStartTime;

                await _context.SaveChangesAsync();

                return new VMwareSyncResult
                {
                    Success = syncLog.Status == "Completed",
                    SyncedCount = syncLog.VirtualMachinesFound,
                    CreatedCount = syncLog.VirtualMachinesCreated,
                    UpdatedCount = syncLog.VirtualMachinesUpdated,
                    ErrorCount = syncLog.ErrorsEncountered,
                    Duration = syncLog.Duration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing virtual machines");
                
                syncLog.Status = "Failed";
                syncLog.ErrorMessage = ex.Message;
                syncLog.SyncEndTime = DateTime.UtcNow;
                syncLog.Duration = syncLog.SyncEndTime - syncLog.SyncStartTime;
                syncLog.ErrorsEncountered = 1;

                await _context.SaveChangesAsync();

                throw;
            }
        }

        public async Task<VMwareAddResult> AddVirtualMachineToInventoryAsync(string vmwareId)
        {
            try
            {
                // Check if VM already exists in inventory
                var existingVMware = await _context.VMwareInfos
                    .FirstOrDefaultAsync(v => v.VMwareId == vmwareId);

                if (existingVMware != null)
                {
                    return new VMwareAddResult
                    {
                        Success = false,
                        Error = "Sanal makine zaten envanterde mevcut",
                        DeviceId = existingVMware.DeviceId
                    };
                }

                // In a real implementation, we would fetch VM details from VMware
                // For demo, we'll use simulated data based on the vmwareId
                var vmData = GetSimulatedVMData(vmwareId);
                if (vmData == null)
                {
                    return new VMwareAddResult
                    {
                        Success = false,
                        Error = "Sanal makine VMware'de bulunamadı"
                    };
                }

                // Create device in inventory
                var device = new Device
                {
                    Id = Guid.NewGuid(),
                    Name = vmData.Name,
                    IpAddress = vmData.IpAddress,
                    DeviceType = DeviceType.VirtualMachine,
                    Status = vmData.PowerState == "poweredOn" ? 0 : 1,
                    DiscoveryMethod = DiscoveryMethod.VMware,
                    IsVirtual = true,
                    Location = "VMware vSphere",
                    Model = vmData.GuestOS,
                    CreatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow
                };

                // Create VMware info
                var vmwareInfo = new VMwareInfo
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    VMwareId = vmwareId,
                    PowerState = vmData.PowerState,
                    GuestOS = vmData.GuestOS,
                    HostName = vmData.HostName,
                    CpuCount = vmData.CpuCount,
                    MemoryMB = vmData.MemoryGB * 1024,
                    ProvisionedSpaceGB = vmData.DiskGB,
                    LastSyncDate = DateTime.UtcNow,
                    SyncStatus = "Success"
                };

                device.VMwareInfo = vmwareInfo;

                _context.Devices.Add(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added virtual machine to inventory: {VMName} ({VMwareId})", vmData.Name, vmwareId);

                return new VMwareAddResult
                {
                    Success = true,
                    DeviceId = device.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding virtual machine to inventory: {VMwareId}", vmwareId);
                return new VMwareAddResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        #endregion

        #region Sync History

        public async Task<IEnumerable<VMwareSyncLogDto>> GetSyncHistoryAsync(DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            try
            {
                var query = _context.VMwareSyncLogs.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(l => l.SyncStartTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.SyncStartTime <= endDate.Value);

                var logs = await query
                    .OrderByDescending(l => l.SyncStartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new VMwareSyncLogDto
                    {
                        Id = l.Id,
                        SyncStartTime = l.SyncStartTime,
                        SyncEndTime = l.SyncEndTime,
                        Status = l.Status,
                        ErrorMessage = l.ErrorMessage,
                        VirtualMachinesFound = l.VirtualMachinesFound,
                        VirtualMachinesCreated = l.VirtualMachinesCreated,
                        VirtualMachinesUpdated = l.VirtualMachinesUpdated,
                        Duration = l.Duration
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync history");
                throw;
            }
        }

        public async Task<VMwareSyncLogDto?> GetSyncLogDetailsAsync(Guid id)
        {
            try
            {
                var log = await _context.VMwareSyncLogs
                    .Where(l => l.Id == id)
                    .Select(l => new VMwareSyncLogDto
                    {
                        Id = l.Id,
                        SyncStartTime = l.SyncStartTime,
                        SyncEndTime = l.SyncEndTime,
                        Status = l.Status,
                        ErrorMessage = l.ErrorMessage,
                        VirtualMachinesFound = l.VirtualMachinesFound,
                        VirtualMachinesCreated = l.VirtualMachinesCreated,
                        VirtualMachinesUpdated = l.VirtualMachinesUpdated,
                        Duration = l.Duration
                    })
                    .FirstOrDefaultAsync();

                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync log details {Id}", id);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<VMwareServer> GetOrCreateVMwareServerAsync()
        {
            var server = await _context.VMwareServers
                .FirstOrDefaultAsync(s => s.IsActive);

            if (server == null)
            {
                server = new VMwareServer
                {
                    Id = Guid.NewGuid(),
                    Name = "Default VMware Server",
                    ServerAddress = "10.0.0.10",
                    Port = 443,
                    IsActive = true,
                    AutoSync = true,
                    SyncIntervalMinutes = 30,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VMwareServers.Add(server);
                await _context.SaveChangesAsync();
            }

            return server;
        }

        private VirtualMachineDto? GetSimulatedVMData(string vmwareId)
        {
            var simulatedVMs = new Dictionary<string, VirtualMachineDto>
            {
                ["vm-001"] = new VirtualMachineDto
                {
                    Id = "vm-001",
                    Name = "PROD-WEB-01",
                    IpAddress = "10.0.0.101",
                    PowerState = "poweredOn",
                    GuestOS = "Windows Server 2022",
                    CpuCount = 4,
                    MemoryGB = 16,
                    DiskGB = 200,
                    HostName = "esxi-01.company.local"
                },
                ["vm-002"] = new VirtualMachineDto
                {
                    Id = "vm-002",
                    Name = "PROD-DB-01",
                    IpAddress = "10.0.0.102",
                    PowerState = "poweredOn",
                    GuestOS = "Ubuntu Server 22.04",
                    CpuCount = 8,
                    MemoryGB = 32,
                    DiskGB = 500,
                    HostName = "esxi-02.company.local"
                },
                ["vm-003"] = new VirtualMachineDto
                {
                    Id = "vm-003",
                    Name = "TEST-ENV-01",
                    IpAddress = "10.0.0.103",
                    PowerState = "poweredOff",
                    GuestOS = "CentOS 8",
                    CpuCount = 2,
                    MemoryGB = 8,
                    DiskGB = 100,
                    HostName = "esxi-01.company.local"
                }
            };

            return simulatedVMs.TryGetValue(vmwareId, out var vm) ? vm : null;
        }

        #endregion
    }

    #region Result Classes

    public class VMwareConnectionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? ServerInfo { get; set; }
    }

    public class VMwareSyncResult
    {
        public bool Success { get; set; }
        public int SyncedCount { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public class VMwareAddResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? DeviceId { get; set; }
    }

    #endregion
}