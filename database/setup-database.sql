-- InventoryManagementSystem Database Schema
-- SQL Server / Azure SQL Database Compatible
-- Version: 1.0
-- Created: 2024

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
    PRINT 'Database InventoryDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database InventoryDB already exists.';
END
GO

USE InventoryDB;
GO

-- Create login and user for application
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'inventoryuser')
BEGIN
    CREATE LOGIN inventoryuser WITH PASSWORD = 'StrongPassword123!';
    PRINT 'Login inventoryuser created.';
END

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'inventoryuser')
BEGIN
    CREATE USER inventoryuser FOR LOGIN inventoryuser;
    ALTER ROLE db_owner ADD MEMBER inventoryuser;
    PRINT 'User inventoryuser created and added to db_owner role.';
END
GO

-- Drop tables if they exist (for clean installation)
IF OBJECT_ID('dbo.ChangeLogs', 'U') IS NOT NULL DROP TABLE dbo.ChangeLogs;
IF OBJECT_ID('dbo.InstalledApps', 'U') IS NOT NULL DROP TABLE dbo.InstalledApps;
IF OBJECT_ID('dbo.NetworkAdapters', 'U') IS NOT NULL DROP TABLE dbo.NetworkAdapters;
IF OBJECT_ID('dbo.RamModules', 'U') IS NOT NULL DROP TABLE dbo.RamModules;
IF OBJECT_ID('dbo.DiskInfo', 'U') IS NOT NULL DROP TABLE dbo.DiskInfo;
IF OBJECT_ID('dbo.GpuInfo', 'U') IS NOT NULL DROP TABLE dbo.GpuInfo;
IF OBJECT_ID('dbo.DeviceSoftwareInfo', 'U') IS NOT NULL DROP TABLE dbo.DeviceSoftwareInfo;
IF OBJECT_ID('dbo.DeviceHardwareInfo', 'U') IS NOT NULL DROP TABLE dbo.DeviceHardwareInfo;
IF OBJECT_ID('dbo.Devices', 'U') IS NOT NULL DROP TABLE dbo.Devices;
GO

-- Main Devices table
CREATE TABLE dbo.Devices (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    DeviceType NVARCHAR(50),
    Model NVARCHAR(200),
    Location NVARCHAR(200),
    Status INT DEFAULT 0, -- 0: Active, 1: Inactive, 2: Maintenance, 3: Retired
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE(),
    LastSeenDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Constraints
    CONSTRAINT CK_Devices_Status CHECK (Status BETWEEN 0 AND 3),
    CONSTRAINT CK_Devices_MacAddress CHECK (MacAddress IS NULL OR MacAddress LIKE '[0-9A-F][0-9A-F]:[0-9A-F][0-9A-F]:[0-9A-F][0-9A-F]:[0-9A-F][0-9A-F]:[0-9A-F][0-9A-F]:[0-9A-F][0-9A-F]'),
    CONSTRAINT CK_Devices_Name_NotEmpty CHECK (LEN(TRIM(Name)) > 0)
);
GO

-- DeviceHardwareInfo table
CREATE TABLE dbo.DeviceHardwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    
    -- CPU Information
    Cpu NVARCHAR(200),
    CpuCores INT,
    CpuLogical INT,
    CpuClockMHz INT,
    
    -- Motherboard and BIOS
    Motherboard NVARCHAR(200),
    MotherboardSerial NVARCHAR(100),
    BiosManufacturer NVARCHAR(100),
    BiosVersion NVARCHAR(100),
    BiosSerial NVARCHAR(100),
    
    -- Memory Information
    RamGB INT,
    
    -- Storage Information
    DiskGB INT,
    
    -- Timestamps
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_DeviceHardwareInfo_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_DeviceHardwareInfo_CpuCores CHECK (CpuCores IS NULL OR CpuCores > 0),
    CONSTRAINT CK_DeviceHardwareInfo_CpuLogical CHECK (CpuLogical IS NULL OR CpuLogical > 0),
    CONSTRAINT CK_DeviceHardwareInfo_RamGB CHECK (RamGB IS NULL OR RamGB > 0),
    CONSTRAINT CK_DeviceHardwareInfo_DiskGB CHECK (DiskGB IS NULL OR DiskGB > 0)
);
GO

-- DeviceSoftwareInfo table
CREATE TABLE dbo.DeviceSoftwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    
    -- Operating System Information
    OperatingSystem NVARCHAR(200),
    OsVersion NVARCHAR(100),
    OsArchitecture NVARCHAR(50),
    RegisteredUser NVARCHAR(200),
    SerialNumber NVARCHAR(100),
    
    -- User Information
    ActiveUser NVARCHAR(200),
    
    -- Timestamps
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_DeviceSoftwareInfo_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE
);
GO

-- RAM Modules table (detailed RAM information)
CREATE TABLE dbo.RamModules (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    Slot NVARCHAR(50),
    CapacityGB INT,
    SpeedMHz NVARCHAR(20),
    Manufacturer NVARCHAR(100),
    PartNumber NVARCHAR(100),
    SerialNumber NVARCHAR(100),
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_RamModules_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_RamModules_CapacityGB CHECK (CapacityGB > 0)
);
GO

-- Disk Information table
CREATE TABLE dbo.DiskInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    DeviceId_Disk NVARCHAR(10), -- C:, D:, etc.
    TotalGB DECIMAL(10,2),
    FreeGB DECIMAL(10,2),
    DiskType NVARCHAR(50), -- HDD, SSD, etc.
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_DiskInfo_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_DiskInfo_TotalGB CHECK (TotalGB > 0),
    CONSTRAINT CK_DiskInfo_FreeGB CHECK (FreeGB >= 0),
    CONSTRAINT CK_DiskInfo_FreeGB_LTE_TotalGB CHECK (FreeGB <= TotalGB)
);
GO

-- GPU Information table
CREATE TABLE dbo.GpuInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200),
    MemoryGB DECIMAL(5,2),
    Manufacturer NVARCHAR(100),
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_GpuInfo_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_GpuInfo_MemoryGB CHECK (MemoryGB IS NULL OR MemoryGB > 0)
);
GO

-- Network Adapters table
CREATE TABLE dbo.NetworkAdapters (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    Description NVARCHAR(200),
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    AdapterType NVARCHAR(50), -- Ethernet, WiFi, etc.
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_NetworkAdapters_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE
);
GO

-- Installed Applications table
CREATE TABLE dbo.InstalledApps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    AppName NVARCHAR(200) NOT NULL,
    Version NVARCHAR(100),
    Publisher NVARCHAR(200),
    InstallDate DATETIME2,
    AppSize BIGINT, -- Size in bytes
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Foreign Key
    CONSTRAINT FK_InstalledApps_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_InstalledApps_AppName_NotEmpty CHECK (LEN(TRIM(AppName)) > 0),
    CONSTRAINT CK_InstalledApps_AppSize CHECK (AppSize IS NULL OR AppSize >= 0)
);
GO

-- Change Logs table
CREATE TABLE dbo.ChangeLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    ChangeDate DATETIME2 DEFAULT GETUTCDATE(),
    ChangeType NVARCHAR(100) NOT NULL,
    FieldName NVARCHAR(100),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    ChangedBy NVARCHAR(200) DEFAULT 'System',
    ChangeCategory NVARCHAR(50), -- Hardware, Software, Configuration, etc.
    Severity NVARCHAR(20) DEFAULT 'Info', -- Info, Warning, Critical
    
    -- Foreign Key
    CONSTRAINT FK_ChangeLogs_DeviceId FOREIGN KEY (DeviceId) REFERENCES dbo.Devices(Id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT CK_ChangeLogs_ChangeType_NotEmpty CHECK (LEN(TRIM(ChangeType)) > 0),
    CONSTRAINT CK_ChangeLogs_Severity CHECK (Severity IN ('Info', 'Warning', 'Critical'))
);
GO

-- Create indexes for performance
PRINT 'Creating indexes...';

-- Primary lookup indexes
CREATE INDEX IX_Devices_MacAddress ON dbo.Devices(MacAddress) WHERE MacAddress IS NOT NULL;
CREATE INDEX IX_Devices_IpAddress ON dbo.Devices(IpAddress) WHERE IpAddress IS NOT NULL;
CREATE INDEX IX_Devices_DeviceType ON dbo.Devices(DeviceType) WHERE DeviceType IS NOT NULL;
CREATE INDEX IX_Devices_Status ON dbo.Devices(Status);
CREATE INDEX IX_Devices_LastSeenDate ON dbo.Devices(LastSeenDate);

-- Foreign key indexes
CREATE INDEX IX_DeviceHardwareInfo_DeviceId ON dbo.DeviceHardwareInfo(DeviceId);
CREATE INDEX IX_DeviceSoftwareInfo_DeviceId ON dbo.DeviceSoftwareInfo(DeviceId);
CREATE INDEX IX_RamModules_DeviceId ON dbo.RamModules(DeviceId);
CREATE INDEX IX_DiskInfo_DeviceId ON dbo.DiskInfo(DeviceId);
CREATE INDEX IX_GpuInfo_DeviceId ON dbo.GpuInfo(DeviceId);
CREATE INDEX IX_NetworkAdapters_DeviceId ON dbo.NetworkAdapters(DeviceId);
CREATE INDEX IX_InstalledApps_DeviceId ON dbo.InstalledApps(DeviceId);
CREATE INDEX IX_ChangeLogs_DeviceId ON dbo.ChangeLogs(DeviceId);

-- Specific search indexes
CREATE INDEX IX_ChangeLogs_ChangeDate ON dbo.ChangeLogs(ChangeDate);
CREATE INDEX IX_ChangeLogs_ChangeType ON dbo.ChangeLogs(ChangeType);
CREATE INDEX IX_ChangeLogs_Severity ON dbo.ChangeLogs(Severity);
CREATE INDEX IX_InstalledApps_AppName ON dbo.InstalledApps(AppName);
CREATE INDEX IX_NetworkAdapters_MacAddress ON dbo.NetworkAdapters(MacAddress) WHERE MacAddress IS NOT NULL;

-- Composite indexes for common queries
CREATE INDEX IX_Devices_Type_Status ON dbo.Devices(DeviceType, Status) WHERE DeviceType IS NOT NULL;
CREATE INDEX IX_ChangeLogs_Device_Date ON dbo.ChangeLogs(DeviceId, ChangeDate);
GO

-- Insert sample data
PRINT 'Inserting sample data...';

-- Sample devices
DECLARE @Device1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Device2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Device3Id UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Devices (Id, Name, MacAddress, IpAddress, DeviceType, Model, Location, Status)
VALUES 
    (@Device1Id, 'SERVER-001', '00:1B:44:11:3A:B7', '192.168.1.10', 'Server', 'Dell PowerEdge R720', 'Server Room', 0),
    (@Device2Id, 'PC-OFFICE-001', '00:1B:44:11:3A:B8', '192.168.1.100', 'PC', 'Dell OptiPlex 7090', 'Office-101', 0),
    (@Device3Id, 'LAPTOP-001', '00:1B:44:11:3A:B9', '192.168.1.150', 'Laptop', 'Lenovo ThinkPad X1 Carbon', 'Mobile', 0);

-- Sample hardware info
INSERT INTO dbo.DeviceHardwareInfo (DeviceId, Cpu, CpuCores, CpuLogical, CpuClockMHz, Motherboard, RamGB, DiskGB)
VALUES 
    (@Device1Id, 'Intel Xeon E5-2660 v2', 20, 40, 2200, 'Dell Inc. 08HPGT', 64, 2000),
    (@Device2Id, 'Intel Core i7-11700K', 8, 16, 3600, 'Dell Inc. 0M3F6V', 32, 1000),
    (@Device3Id, 'Intel Core i7-1165G7', 4, 8, 2800, 'LENOVO 20XW', 16, 512);

-- Sample software info
INSERT INTO dbo.DeviceSoftwareInfo (DeviceId, OperatingSystem, OsVersion, OsArchitecture, RegisteredUser, ActiveUser)
VALUES 
    (@Device1Id, 'Windows Server 2022 Standard', '10.0.20348', '64-bit', 'Administrator', 'Administrator'),
    (@Device2Id, 'Windows 11 Pro', '10.0.22631', '64-bit', 'John Doe', 'john.doe'),
    (@Device3Id, 'Windows 11 Pro', '10.0.22631', '64-bit', 'Jane Smith', 'jane.smith');

-- Sample change logs
INSERT INTO dbo.ChangeLogs (DeviceId, ChangeType, FieldName, OldValue, NewValue, ChangeCategory, Severity)
VALUES 
    (@Device1Id, 'Initial Registration', 'Device', '', 'Initial device registration', 'Configuration', 'Info'),
    (@Device2Id, 'Initial Registration', 'Device', '', 'Initial device registration', 'Configuration', 'Info'),
    (@Device3Id, 'Initial Registration', 'Device', '', 'Initial device registration', 'Configuration', 'Info');

PRINT 'Sample data inserted successfully.';
GO

PRINT '';
PRINT '==================================================';
PRINT 'INVENTORY MANAGEMENT SYSTEM DATABASE SETUP COMPLETE';
PRINT '==================================================';
PRINT 'Database: InventoryDB';
PRINT 'User: inventoryuser';
PRINT 'Password: StrongPassword123!';
PRINT '';
PRINT 'Connection String Examples:';
PRINT 'SQL Server: Server=localhost;Database=InventoryDB;User Id=inventoryuser;Password=StrongPassword123!;TrustServerCertificate=true;';
PRINT 'SQL Server (Trusted): Server=localhost;Database=InventoryDB;Trusted_Connection=true;TrustServerCertificate=true;';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Update your appsettings.json with the connection string';
PRINT '2. Start the Inventory API service';
PRINT '3. Configure and deploy agents to client machines';
PRINT '4. Access Swagger UI at: http://your-server/swagger';
GO