-- Phase: Request workflow tables
IF OBJECT_ID(N'[AssetRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequests] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestNumber] nvarchar(40) NOT NULL,
      [RequestType] nvarchar(64) NOT NULL,
      [RequesterEmail] nvarchar(320) NOT NULL,
      [RequesterName] nvarchar(255) NULL,
      [RequesterUserId] nvarchar(128) NULL,
      [RequestedForEmail] nvarchar(320) NULL,
      [Department] nvarchar(128) NOT NULL,
      [CostCenter] nvarchar(128) NULL,
      [Location] nvarchar(255) NOT NULL,
      [BusinessJustification] nvarchar(4000) NOT NULL,
      [Urgency] nvarchar(24) NOT NULL,
      [Status] nvarchar(64) NOT NULL,
      [CurrentApprovalLevel] nvarchar(32) NOT NULL,
      [PmApproverEmail] nvarchar(320) NOT NULL,
      [BossApproverEmail] nvarchar(320) NOT NULL,
      [DestinationUserEmail] nvarchar(320) NULL,
      [DestinationManagerEmail] nvarchar(320) NULL,
      [RelatedAssetId] nvarchar(128) NULL,
      [RequestedCategory] nvarchar(128) NULL,
      [RequestedConfigurationJson] nvarchar(max) NULL,
      [SecurityIncidentFlag] bit NOT NULL,
      [IncidentDate] datetimeoffset NULL,
      [IncidentLocation] nvarchar(255) NULL,
      [PoliceReportNumber] nvarchar(128) NULL,
      [CreatedAt] datetimeoffset NOT NULL,
      [UpdatedAt] datetimeoffset NOT NULL,
      [ClosedAt] datetimeoffset NULL
    );

    CREATE UNIQUE INDEX [UX_AssetRequests_RequestNumber] ON [AssetRequests]([RequestNumber]);
    CREATE INDEX [IX_AssetRequests_RequesterEmail] ON [AssetRequests]([RequesterEmail]);
    CREATE INDEX [IX_AssetRequests_Status] ON [AssetRequests]([Status]);
    CREATE INDEX [IX_AssetRequests_CurrentApprovalLevel] ON [AssetRequests]([CurrentApprovalLevel]);
    CREATE INDEX [IX_AssetRequests_PmApproverEmail] ON [AssetRequests]([PmApproverEmail]);
    CREATE INDEX [IX_AssetRequests_BossApproverEmail] ON [AssetRequests]([BossApproverEmail]);
END

IF OBJECT_ID(N'[AssetRequestApprovals]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequestApprovals] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestId] nvarchar(128) NOT NULL,
      [Level] nvarchar(32) NOT NULL,
      [ApproverEmail] nvarchar(320) NOT NULL,
      [Decision] nvarchar(64) NOT NULL,
      [Comment] nvarchar(4000) NULL,
      [DecidedAt] datetimeoffset NOT NULL,
      CONSTRAINT [FK_AssetRequestApprovals_AssetRequests] FOREIGN KEY([RequestId]) REFERENCES [AssetRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AssetRequestApprovals_RequestId] ON [AssetRequestApprovals]([RequestId]);
    CREATE INDEX [IX_AssetRequestApprovals_ApproverEmail] ON [AssetRequestApprovals]([ApproverEmail]);
END

IF OBJECT_ID(N'[AssetRequestNotifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequestNotifications] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestId] nvarchar(128) NOT NULL,
      [RecipientEmail] nvarchar(320) NOT NULL,
      [Channel] nvarchar(32) NOT NULL,
      [Type] nvarchar(128) NOT NULL,
      [SentAt] datetimeoffset NOT NULL,
      [Status] nvarchar(32) NOT NULL,
      CONSTRAINT [FK_AssetRequestNotifications_AssetRequests] FOREIGN KEY([RequestId]) REFERENCES [AssetRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AssetRequestNotifications_RequestId] ON [AssetRequestNotifications]([RequestId]);
    CREATE INDEX [IX_AssetRequestNotifications_RecipientEmail] ON [AssetRequestNotifications]([RecipientEmail]);
END

IF OBJECT_ID(N'[AssetRequestAttachments]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequestAttachments] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestId] nvarchar(128) NOT NULL,
      [FileName] nvarchar(260) NOT NULL,
      [BlobPath] nvarchar(1000) NOT NULL,
      [UploadedBy] nvarchar(320) NOT NULL,
      [UploadedAt] datetimeoffset NOT NULL,
      CONSTRAINT [FK_AssetRequestAttachments_AssetRequests] FOREIGN KEY([RequestId]) REFERENCES [AssetRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AssetRequestAttachments_RequestId] ON [AssetRequestAttachments]([RequestId]);
END

IF OBJECT_ID(N'[AssetRequestComments]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequestComments] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestId] nvarchar(128) NOT NULL,
      [AuthorEmail] nvarchar(320) NOT NULL,
      [Comment] nvarchar(4000) NOT NULL,
      [CreatedAt] datetimeoffset NOT NULL,
      CONSTRAINT [FK_AssetRequestComments_AssetRequests] FOREIGN KEY([RequestId]) REFERENCES [AssetRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AssetRequestComments_RequestId] ON [AssetRequestComments]([RequestId]);
END
