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
            });

            // Simple ChangeLog configuration
            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(c => c.ChangeType).HasMaxLength(100);
                entity.Property(c => c.ChangedBy).HasMaxLength(200);
                entity.HasIndex(c => c.ChangeDate);
            });
        }
    }
}