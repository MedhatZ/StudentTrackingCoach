BEGIN TRANSACTION;
GO

ALTER TABLE [dbo].[Student] ADD [TenantId] int NOT NULL DEFAULT 1;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Interventions]') AND [c].[name] = N'Status');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Interventions] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [dbo].[Interventions] ALTER COLUMN [Status] nvarchar(450) NOT NULL;
GO

ALTER TABLE [dbo].[Interventions] ADD [TenantId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [AdvisorNotes] ADD [TenantId] int NOT NULL DEFAULT 1;
GO

IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AdminAuditLogs', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[AdminAuditLogs] ADD [TenantId] int NOT NULL CONSTRAINT [DF_AdminAuditLogs_TenantId] DEFAULT 1;
    END
END
GO

CREATE TABLE [Tenants] (
    [TenantId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(100) NOT NULL,
    [ConnectionString] nvarchar(2000) NULL,
    [PassingGrade] int NOT NULL,
    [HighRiskThreshold] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([TenantId])
);
GO

CREATE TABLE [TenantFeatures] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [FeatureKey] nvarchar(120) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [FeatureValue] nvarchar(500) NULL,
    CONSTRAINT [PK_TenantFeatures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantFeatures_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE CASCADE
);
GO

CREATE TABLE [TenantUserRoles] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [RoleName] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_TenantUserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TenantUserRoles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Student_TenantId_StudentId] ON [dbo].[Student] ([TenantId], [StudentId]);
GO

CREATE INDEX [IX_Interventions_TenantId_StudentId_Status] ON [dbo].[Interventions] ([TenantId], [StudentId], [Status]);
GO

CREATE INDEX [IX_AdvisorNotes_TenantId_StudentId_CreatedAt] ON [AdvisorNotes] ([TenantId], [StudentId], [CreatedAt]);
GO

IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_AdminAuditLogs_TenantId_CreatedAt'
          AND object_id = OBJECT_ID(N'[dbo].[AdminAuditLogs]')
    )
    BEGIN
        CREATE INDEX [IX_AdminAuditLogs_TenantId_CreatedAt] ON [dbo].[AdminAuditLogs] ([TenantId], [CreatedAt]);
    END
END
GO

CREATE UNIQUE INDEX [IX_TenantFeatures_TenantId_FeatureKey] ON [TenantFeatures] ([TenantId], [FeatureKey]);
GO

CREATE UNIQUE INDEX [IX_Tenants_Slug] ON [Tenants] ([Slug]);
GO

CREATE UNIQUE INDEX [IX_TenantUserRoles_TenantId_UserId_RoleName] ON [TenantUserRoles] ([TenantId], [UserId], [RoleName]);
GO

CREATE INDEX [IX_TenantUserRoles_UserId] ON [TenantUserRoles] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260308074649_AddMultiTenantSupport', N'8.0.0');
GO

COMMIT;
GO

