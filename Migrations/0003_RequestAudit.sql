IF OBJECT_ID(N'[AssetRequestAudits]', N'U') IS NULL
BEGIN
    CREATE TABLE [AssetRequestAudits] (
      [Id] nvarchar(128) NOT NULL PRIMARY KEY,
      [RequestId] nvarchar(128) NOT NULL,
      [RequestNumber] nvarchar(40) NOT NULL,
      [RequestType] nvarchar(64) NOT NULL,
      [Action] nvarchar(64) NOT NULL,
      [ActorEmail] nvarchar(320) NOT NULL,
      [ActorRole] nvarchar(64) NULL,
      [FromStatus] nvarchar(64) NULL,
      [ToStatus] nvarchar(64) NULL,
      [Decision] nvarchar(64) NULL,
      [Comment] nvarchar(4000) NULL,
      [CreatedAt] datetimeoffset NOT NULL
    );

    CREATE INDEX [IX_AssetRequestAudits_RequestId] ON [AssetRequestAudits]([RequestId]);
    CREATE INDEX [IX_AssetRequestAudits_RequestNumber] ON [AssetRequestAudits]([RequestNumber]);
    CREATE INDEX [IX_AssetRequestAudits_ActorEmail] ON [AssetRequestAudits]([ActorEmail]);
    CREATE INDEX [IX_AssetRequestAudits_Action] ON [AssetRequestAudits]([Action]);
    CREATE INDEX [IX_AssetRequestAudits_CreatedAt] ON [AssetRequestAudits]([CreatedAt]);
END
