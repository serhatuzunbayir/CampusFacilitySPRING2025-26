SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [FacilityTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(64) NOT NULL,
        [RequiresApproval] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FacilityTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] nvarchar(450) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [EntityType] nvarchar(64) NOT NULL,
        [EntityId] nvarchar(64) NULL,
        [TimestampUtc] datetime2 NOT NULL,
        [Details] nvarchar(2000) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [RecipientUserId] nvarchar(450) NOT NULL,
        [Kind] int NOT NULL,
        [Message] nvarchar(512) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [Facilities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NOT NULL,
        [FacilityTypeId] int NOT NULL,
        [Capacity] int NOT NULL,
        [Location] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Facilities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Facilities_FacilityTypes_FacilityTypeId] FOREIGN KEY ([FacilityTypeId]) REFERENCES [FacilityTypes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] int NOT NULL IDENTITY,
        [FacilityId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Date] date NOT NULL,
        [TimeSlot] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Facilities_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [Facilities] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE TABLE [MaintenanceIssues] (
        [Id] int NOT NULL IDENTITY,
        [FacilityId] int NOT NULL,
        [ReporterId] nvarchar(450) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Severity] int NOT NULL,
        [PhotoPath] nvarchar(512) NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [AssigneeId] nvarchar(450) NULL,
        [AssignedById] nvarchar(450) NULL,
        [AssignedAtUtc] datetime2 NULL,
        [StartedAtUtc] datetime2 NULL,
        [ResolvedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_MaintenanceIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceIssues_AspNetUsers_AssignedById] FOREIGN KEY ([AssignedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenanceIssues_AspNetUsers_AssigneeId] FOREIGN KEY ([AssigneeId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenanceIssues_AspNetUsers_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenanceIssues_Facilities_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [Facilities] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId] ON [AuditLogs] ([ActorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TimestampUtc] ON [AuditLogs] ([TimestampUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bookings_FacilityId_Date_TimeSlot] ON [Bookings] ([FacilityId], [Date], [TimeSlot]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_Facilities_FacilityTypeId] ON [Facilities] ([FacilityTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_Facilities_Name] ON [Facilities] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FacilityTypes_Name] ON [FacilityTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceIssues_AssignedById] ON [MaintenanceIssues] ([AssignedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceIssues_AssigneeId] ON [MaintenanceIssues] ([AssigneeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceIssues_FacilityId] ON [MaintenanceIssues] ([FacilityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceIssues_ReporterId] ON [MaintenanceIssues] ([ReporterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceIssues_Status] ON [MaintenanceIssues] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId_IsRead] ON [Notifications] ([RecipientUserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503133541_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503133541_Init', N'8.0.10');
END;
GO

COMMIT;
GO

