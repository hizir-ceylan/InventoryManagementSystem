using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVMwareSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVirtual",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VMwareInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VMwareId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InstanceUuid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PowerState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    GuestOS = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GuestFullName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HostName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HostId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DatastoreName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DatastoreId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResourcePoolName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResourcePoolId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClusterName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ClusterId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CpuCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CoresPerSocket = table.Column<int>(type: "INTEGER", nullable: true),
                    MemoryMB = table.Column<long>(type: "INTEGER", nullable: true),
                    ProvisionedSpaceGB = table.Column<long>(type: "INTEGER", nullable: true),
                    UsedSpaceGB = table.Column<long>(type: "INTEGER", nullable: true),
                    VirtualHardwareVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VMwareToolsStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VMwareToolsVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VMwareToolsVersionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NetworkName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NetworkType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PortGroupName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Annotation = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Template = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastBootTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSuspendTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HasSnapshots = table.Column<bool>(type: "INTEGER", nullable: false),
                    SnapshotCount = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSnapshotDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    MemoryUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    NetworkUsageKBps = table.Column<double>(type: "REAL", nullable: true),
                    DiskUsageKBps = table.Column<double>(type: "REAL", nullable: true),
                    LastSyncDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SyncError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMwareInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VMwareInfos_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VMwareServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ServerAddress = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastConnection = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConnectionError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Build = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProductType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LicenseType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TotalMemoryMB = table.Column<long>(type: "INTEGER", nullable: true),
                    UsedMemoryMB = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalStorageGB = table.Column<long>(type: "INTEGER", nullable: true),
                    UsedStorageGB = table.Column<long>(type: "INTEGER", nullable: true),
                    FreeStorageGB = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalCpuCores = table.Column<int>(type: "INTEGER", nullable: true),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    TotalVMs = table.Column<int>(type: "INTEGER", nullable: true),
                    RunningVMs = table.Column<int>(type: "INTEGER", nullable: true),
                    StoppedVMs = table.Column<int>(type: "INTEGER", nullable: true),
                    SuspendedVMs = table.Column<int>(type: "INTEGER", nullable: true),
                    AutoSync = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastSyncError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMwareServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VMwareSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VMwareServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SyncStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    VirtualMachinesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    VirtualMachinesCreated = table.Column<int>(type: "INTEGER", nullable: false),
                    VirtualMachinesUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    VirtualMachinesDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorsEncountered = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    DataTransferredBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    SyncType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SyncTrigger = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMwareSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VMwareSyncLogs_VMwareServers_VMwareServerId",
                        column: x => x.VMwareServerId,
                        principalTable: "VMwareServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VMwareInfos_DeviceId",
                table: "VMwareInfos",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VMwareInfos_InstanceUuid",
                table: "VMwareInfos",
                column: "InstanceUuid");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareInfos_LastSyncDate",
                table: "VMwareInfos",
                column: "LastSyncDate");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareInfos_PowerState",
                table: "VMwareInfos",
                column: "PowerState");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareInfos_VMwareId",
                table: "VMwareInfos",
                column: "VMwareId");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareServers_IsActive",
                table: "VMwareServers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareServers_LastConnection",
                table: "VMwareServers",
                column: "LastConnection");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareServers_LastSyncDate",
                table: "VMwareServers",
                column: "LastSyncDate");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareServers_ServerAddress",
                table: "VMwareServers",
                column: "ServerAddress");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareSyncLogs_CreatedAt",
                table: "VMwareSyncLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareSyncLogs_Status",
                table: "VMwareSyncLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareSyncLogs_SyncStartTime",
                table: "VMwareSyncLogs",
                column: "SyncStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_VMwareSyncLogs_VMwareServerId",
                table: "VMwareSyncLogs",
                column: "VMwareServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VMwareInfos");

            migrationBuilder.DropTable(
                name: "VMwareSyncLogs");

            migrationBuilder.DropTable(
                name: "VMwareServers");

            migrationBuilder.DropColumn(
                name: "IsVirtual",
                table: "Devices");
        }
    }
}
