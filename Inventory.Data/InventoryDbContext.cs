using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<ChangeLog> ChangeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Simple Device configuration for now
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MacAddress).HasMaxLength(17);
                entity.Property(e => e.IpAddress).HasMaxLength(15);
                entity.Property(e => e.Model).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.HasIndex(e => e.MacAddress);
                entity.HasIndex(e => e.IpAddress);

                // Configure DeviceHardwareInfo as owned entity
                entity.OwnsOne(e => e.HardwareInfo, hardware =>
                {
                    hardware.Property(h => h.Cpu).HasMaxLength(200);
                    hardware.Property(h => h.Motherboard).HasMaxLength(200);
                    hardware.Property(h => h.MotherboardSerial).HasMaxLength(100);
                    hardware.Property(h => h.BiosManufacturer).HasMaxLength(100);
                    hardware.Property(h => h.BiosVersion).HasMaxLength(100);
                    hardware.Property(h => h.BiosSerial).HasMaxLength(100);
                    
                    // Configure nested collections as owned
                    hardware.OwnsMany(h => h.RamModules, ram =>
                    {
                        ram.HasKey(r => r.Id);
                        ram.Property(r => r.Slot).HasMaxLength(50);
                        ram.Property(r => r.SpeedMHz).HasMaxLength(50);
                        ram.Property(r => r.Manufacturer).HasMaxLength(100);
                        ram.Property(r => r.PartNumber).HasMaxLength(100);
                        ram.Property(r => r.SerialNumber).HasMaxLength(100);
                    });
                    
                    hardware.OwnsMany(h => h.Disks, disk =>
                    {
                        disk.HasKey(d => d.Id);
                        disk.Property(d => d.DeviceId).HasMaxLength(200);
                    });
                    
                    hardware.OwnsMany(h => h.Gpus, gpu =>
                    {
                        gpu.HasKey(g => g.Id);
                        gpu.Property(g => g.Name).HasMaxLength(200);
                    });
                    
                    hardware.OwnsMany(h => h.NetworkAdapters, adapter =>
                    {
                        adapter.HasKey(a => a.Id);
                        adapter.Property(a => a.Description).HasMaxLength(200);
                        adapter.Property(a => a.MacAddress).HasMaxLength(17);
                        adapter.Property(a => a.IpAddress).HasMaxLength(15);
                    });
                });

                // Configure DeviceSoftwareInfo as owned entity
                entity.OwnsOne(e => e.SoftwareInfo, software =>
                {
                    software.Property(s => s.OperatingSystem).HasMaxLength(200);
                    software.Property(s => s.OsVersion).HasMaxLength(100);
                    software.Property(s => s.OsArchitecture).HasMaxLength(50);
                    software.Property(s => s.RegisteredUser).HasMaxLength(200);
                    software.Property(s => s.SerialNumber).HasMaxLength(100);
                    software.Property(s => s.ActiveUser).HasMaxLength(200);
                });
            });

            // Simple ChangeLog configuration
            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(c => c.ChangeType).HasMaxLength(100);
                entity.Property(c => c.ChangedBy).HasMaxLength(200);
                entity.Property(c => c.OldValue).HasMaxLength(500);
                entity.Property(c => c.NewValue).HasMaxLength(500);
                entity.HasIndex(c => c.ChangeDate);
                entity.HasIndex(c => c.DeviceId);
                
                // Configure foreign key relationship to Device
                entity.HasOne<Device>()
                    .WithMany(d => d.ChangeLogs)
                    .HasForeignKey(c => c.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}