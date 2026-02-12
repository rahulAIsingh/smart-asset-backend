-- Initial schema for Smart Asset Manager (Phase 1 bootstrap)
CREATE TABLE [Users] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Email] nvarchar(320) NOT NULL,
  [Role] nvarchar(32) NOT NULL,
  [Department] nvarchar(128) NULL,
  [Name] nvarchar(255) NULL,
  [Avatar] nvarchar(2048) NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_Users_Email] ON [Users]([Email]);

CREATE TABLE [Assets] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Name] nvarchar(255) NOT NULL,
  [Category] nvarchar(100) NOT NULL,
  [SerialNumber] nvarchar(255) NULL,
  [Location] nvarchar(max) NULL,
  [Status] nvarchar(50) NOT NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_Assets_SerialNumber] ON [Assets]([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;

CREATE TABLE [Issuances] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [AssetId] nvarchar(128) NOT NULL,
  [UserName] nvarchar(512) NOT NULL,
  [UserEmail] nvarchar(320) NOT NULL,
  [Status] nvarchar(64) NOT NULL,
  [IssueDate] datetimeoffset NOT NULL,
  [ReturnDate] datetimeoffset NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL,
  CONSTRAINT [FK_Issuances_Assets] FOREIGN KEY([AssetId]) REFERENCES [Assets]([Id])
);

CREATE TABLE [Maintenance] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [AssetId] nvarchar(128) NOT NULL,
  [Type] nvarchar(64) NOT NULL,
  [Description] nvarchar(4000) NOT NULL,
  [Cost] decimal(18,2) NULL,
  [WarrantyExpiry] date NULL,
  [Date] datetimeoffset NOT NULL,
  [PerformedBy] nvarchar(255) NOT NULL,
  CONSTRAINT [FK_Maintenance_Assets] FOREIGN KEY([AssetId]) REFERENCES [Assets]([Id])
);

CREATE TABLE [StockTransactions] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [AssetId] nvarchar(128) NOT NULL,
  [Type] nvarchar(16) NOT NULL,
  [Quantity] decimal(18,2) NOT NULL,
  [Reason] nvarchar(max) NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  CONSTRAINT [FK_StockTransactions_Assets] FOREIGN KEY([AssetId]) REFERENCES [Assets]([Id])
);

CREATE TABLE [Categories] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Label] nvarchar(128) NOT NULL,
  [Value] nvarchar(128) NOT NULL,
  [Icon] nvarchar(128) NOT NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_Categories_Value] ON [Categories]([Value]);

CREATE TABLE [Departments] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Name] nvarchar(128) NOT NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_Departments_Name] ON [Departments]([Name]);

CREATE TABLE [Vendors] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Name] nvarchar(128) NOT NULL,
  [CreatedAt] datetimeoffset NOT NULL,
  [UpdatedAt] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_Vendors_Name] ON [Vendors]([Name]);

CREATE TABLE [FinanceProfiles] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [Category] nvarchar(128) NOT NULL,
  [Method] nvarchar(64) NOT NULL,
  [UsefulLifeMonths] int NOT NULL,
  [SalvageType] nvarchar(64) NOT NULL,
  [SalvageValue] decimal(18,2) NOT NULL,
  [Frequency] nvarchar(64) NOT NULL,
  [ExpenseGl] nvarchar(128) NULL,
  [AccumDepGl] nvarchar(128) NULL,
  [Active] bit NOT NULL,
  [CreatedBy] nvarchar(255) NOT NULL,
  [CreatedDate] datetimeoffset NOT NULL,
  [UpdatedBy] nvarchar(255) NULL,
  [UpdatedDate] datetimeoffset NULL
);
CREATE UNIQUE INDEX [UX_FinanceProfiles_Category] ON [FinanceProfiles]([Category]);

CREATE TABLE [FinanceAssetOverrides] (
  [Id] nvarchar(128) NOT NULL PRIMARY KEY,
  [AssetId] nvarchar(128) NOT NULL,
  [Method] nvarchar(64) NOT NULL,
  [UsefulLifeMonths] int NOT NULL,
  [SalvageType] nvarchar(64) NOT NULL,
  [SalvageValue] decimal(18,2) NOT NULL,
  [EffectiveFrom] date NOT NULL,
  [CreatedBy] nvarchar(255) NOT NULL,
  [CreatedDate] datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX [UX_FinanceAssetOverrides_AssetId] ON [FinanceAssetOverrides]([AssetId]);
