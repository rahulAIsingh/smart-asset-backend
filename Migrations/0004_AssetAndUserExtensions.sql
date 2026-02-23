-- Phase: Asset + User extensions for role mapping and asset master fields
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF COL_LENGTH('Assets', 'DeviceSerialNumber') IS NULL
    ALTER TABLE [Assets] ADD [DeviceSerialNumber] nvarchar(255) NULL;
IF COL_LENGTH('Assets', 'Company') IS NULL
    ALTER TABLE [Assets] ADD [Company] nvarchar(255) NULL;
IF COL_LENGTH('Assets', 'Model') IS NULL
    ALTER TABLE [Assets] ADD [Model] nvarchar(255) NULL;
IF COL_LENGTH('Assets', 'Department') IS NULL
    ALTER TABLE [Assets] ADD [Department] nvarchar(128) NULL;
IF COL_LENGTH('Assets', 'WarrantyStart') IS NULL
    ALTER TABLE [Assets] ADD [WarrantyStart] nvarchar(64) NULL;
IF COL_LENGTH('Assets', 'WarrantyEnd') IS NULL
    ALTER TABLE [Assets] ADD [WarrantyEnd] nvarchar(64) NULL;
IF COL_LENGTH('Assets', 'WarrantyVendor') IS NULL
    ALTER TABLE [Assets] ADD [WarrantyVendor] nvarchar(255) NULL;
IF COL_LENGTH('Assets', 'Configuration') IS NULL
    ALTER TABLE [Assets] ADD [Configuration] nvarchar(4000) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Assets_DeviceSerialNumber'
      AND object_id = OBJECT_ID('Assets')
)
    CREATE INDEX [IX_Assets_DeviceSerialNumber] ON [Assets]([DeviceSerialNumber]);

IF COL_LENGTH('Users', 'ManagerEmail') IS NULL
    ALTER TABLE [Users] ADD [ManagerEmail] nvarchar(320) NULL;
IF COL_LENGTH('Users', 'BossEmail') IS NULL
    ALTER TABLE [Users] ADD [BossEmail] nvarchar(320) NULL;
IF COL_LENGTH('Users', 'ReportingToEmail') IS NULL
    ALTER TABLE [Users] ADD [ReportingToEmail] nvarchar(320) NULL;
IF COL_LENGTH('Users', 'BossApproverEmail') IS NULL
    ALTER TABLE [Users] ADD [BossApproverEmail] nvarchar(320) NULL;
