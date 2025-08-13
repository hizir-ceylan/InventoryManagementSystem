using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeAndNotesToDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HardwareInfo_Cpu = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HardwareInfo_CpuCores = table.Column<int>(type: "INTEGER", nullable: true),
                    HardwareInfo_CpuLogical = table.Column<int>(type: "INTEGER", nullable: true),
                    HardwareInfo_CpuClockMHz = table.Column<int>(type: "INTEGER", nullable: true),
                    HardwareInfo_Motherboard = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HardwareInfo_MotherboardSerial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HardwareInfo_BiosManufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HardwareInfo_BiosVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HardwareInfo_BiosSerial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HardwareInfo_RamGB = table.Column<int>(type: "INTEGER", nullable: true),
                    HardwareInfo_DiskGB = table.Column<int>(type: "INTEGER", nullable: true),
                    SoftwareInfo_OperatingSystem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SoftwareInfo_OsVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SoftwareInfo_OsArchitecture = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SoftwareInfo_RegisteredUser = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SoftwareInfo_SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SoftwareInfo_InstalledApps = table.Column<string>(type: "TEXT", nullable: true),
                    SoftwareInfo_Updates = table.Column<string>(type: "TEXT", nullable: true),
                    SoftwareInfo_Users = table.Column<string>(type: "TEXT", nullable: true),
                    SoftwareInfo_ActiveUser = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BarcodeNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AgentInstalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManagementType = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscoveryMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkScanHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScanTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScanType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DevicesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NetworkRange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PortScanType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkScanHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedNetworkRanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NetworkRange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PortScanType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastScanTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScanCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedNetworkRanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiskInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TotalGB = table.Column<double>(type: "REAL", nullable: false),
                    FreeGB = table.Column<double>(type: "REAL", nullable: false),
                    DeviceHardwareInfoDeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiskInfo_Devices_DeviceHardwareInfoDeviceId",
                        column: x => x.DeviceHardwareInfoDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GpuInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MemoryGB = table.Column<float>(type: "REAL", nullable: true),
                    DeviceHardwareInfoDeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpuInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GpuInfo_Devices_DeviceHardwareInfoDeviceId",
                        column: x => x.DeviceHardwareInfoDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkAdapter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    DeviceHardwareInfoDeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkAdapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkAdapter_Devices_DeviceHardwareInfoDeviceId",
                        column: x => x.DeviceHardwareInfoDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RamModule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slot = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CapacityGB = table.Column<double>(type: "REAL", nullable: false),
                    SpeedMHz = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PartNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeviceHardwareInfoDeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RamModule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RamModule_Devices_DeviceHardwareInfoDeviceId",
                        column: x => x.DeviceHardwareInfoDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpdateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    KBNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CurrentVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LatestVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SizeInMB = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CanAutoInstall = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresRestart = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecurityBulletinId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemUpdates_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_ChangeDate",
                table: "ChangeLogs",
                column: "ChangeDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_DeviceId",
                table: "ChangeLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_BarcodeNumber",
                table: "Devices",
                column: "BarcodeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CreatedAt",
                table: "Devices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_IpAddress",
                table: "Devices",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_LastSeen",
                table: "Devices",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_LastUpdate",
                table: "Devices",
                column: "LastUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_MacAddress",
                table: "Devices",
                column: "MacAddress");

            migrationBuilder.CreateIndex(
                name: "IX_DiskInfo_DeviceHardwareInfoDeviceId",
                table: "DiskInfo",
                column: "DeviceHardwareInfoDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GpuInfo_DeviceHardwareInfoDeviceId",
                table: "GpuInfo",
                column: "DeviceHardwareInfoDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkAdapter_DeviceHardwareInfoDeviceId",
                table: "NetworkAdapter",
                column: "DeviceHardwareInfoDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScanHistories_CreatedAt",
                table: "NetworkScanHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScanHistories_NetworkRange",
                table: "NetworkScanHistories",
                column: "NetworkRange");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScanHistories_ScanTime",
                table: "NetworkScanHistories",
                column: "ScanTime");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScanHistories_Status",
                table: "NetworkScanHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedNetworkRanges_CreatedAt",
                table: "PredefinedNetworkRanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedNetworkRanges_IsActive",
                table: "PredefinedNetworkRanges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedNetworkRanges_LastScanTime",
                table: "PredefinedNetworkRanges",
                column: "LastScanTime");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedNetworkRanges_Name",
                table: "PredefinedNetworkRanges",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedNetworkRanges_NetworkRange",
                table: "PredefinedNetworkRanges",
                column: "NetworkRange");

            migrationBuilder.CreateIndex(
                name: "IX_RamModule_DeviceHardwareInfoDeviceId",
                table: "RamModule",
                column: "DeviceHardwareInfoDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_DetectedDate",
                table: "SystemUpdates",
                column: "DetectedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_DeviceId",
                table: "SystemUpdates",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_Priority",
                table: "SystemUpdates",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUpdates_Status",
                table: "SystemUpdates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeLogs");

            migrationBuilder.DropTable(
                name: "DiskInfo");

            migrationBuilder.DropTable(
                name: "GpuInfo");

            migrationBuilder.DropTable(
                name: "NetworkAdapter");

            migrationBuilder.DropTable(
                name: "NetworkScanHistories");

            migrationBuilder.DropTable(
                name: "PredefinedNetworkRanges");

            migrationBuilder.DropTable(
                name: "RamModule");

            migrationBuilder.DropTable(
                name: "SystemUpdates");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
