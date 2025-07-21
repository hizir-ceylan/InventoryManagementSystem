-- SQL Server initialization script
USE master;
GO

-- Create the inventory database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
END
GO

USE InventoryDB;
GO

-- Create tables if they don't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Devices' AND xtype='U')
BEGIN
    CREATE TABLE Devices (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        MacAddress NVARCHAR(17),
        IpAddress NVARCHAR(15),
        DeviceType NVARCHAR(50),
        Model NVARCHAR(200),
        Location NVARCHAR(200),
        Status INT DEFAULT 0,
        CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedDate DATETIME2 DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_Devices_MacAddress ON Devices(MacAddress);
    CREATE INDEX IX_Devices_IpAddress ON Devices(IpAddress);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DeviceHardwareInfo' AND xtype='U')
BEGIN
    CREATE TABLE DeviceHardwareInfo (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        Cpu NVARCHAR(200),
        CpuCores INT,
        CpuLogical INT,
        CpuClockMHz INT,
        Motherboard NVARCHAR(200),
        MotherboardSerial NVARCHAR(100),
        BiosManufacturer NVARCHAR(100),
        BiosVersion NVARCHAR(100),
        BiosSerial NVARCHAR(100),
        RamGB INT,
        DiskGB INT,
        CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DeviceSoftwareInfo' AND xtype='U')
BEGIN
    CREATE TABLE DeviceSoftwareInfo (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        OperatingSystem NVARCHAR(200),
        OsVersion NVARCHAR(100),
        OsArchitecture NVARCHAR(50),
        RegisteredUser NVARCHAR(200),
        SerialNumber NVARCHAR(100),
        ActiveUser NVARCHAR(200),
        CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ChangeLogs' AND xtype='U')
BEGIN
    CREATE TABLE ChangeLogs (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        ChangeDate DATETIME2 DEFAULT GETUTCDATE(),
        ChangeType NVARCHAR(100),
        OldValue NVARCHAR(MAX),
        NewValue NVARCHAR(MAX),
        ChangedBy NVARCHAR(200),
        FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ChangeLogs_DeviceId ON ChangeLogs(DeviceId);
    CREATE INDEX IX_ChangeLogs_ChangeDate ON ChangeLogs(ChangeDate);
END
GO

-- Insert sample data for testing
IF NOT EXISTS (SELECT * FROM Devices WHERE Name = 'DOCKER-TEST-001')
BEGIN
    INSERT INTO Devices (Name, MacAddress, IpAddress, DeviceType, Model, Location, Status)
    VALUES 
        ('DOCKER-TEST-001', '02:42:AC:14:00:02', '172.20.0.2', 'Container', 'Docker Container', 'Docker Network', 0),
        ('API-SERVER', '02:42:AC:14:00:03', '172.20.0.3', 'Server', 'Docker API Server', 'Docker Network', 0);
END
GO

PRINT 'Database initialization completed successfully!';
GO