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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811175346_InitialCreate'
)
BEGIN
    CREATE TABLE [Attendances] (
        [AttendanceID] int NOT NULL IDENTITY,
        [StudentID] int NOT NULL,
        [AttendanceDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Attendances] PRIMARY KEY ([AttendanceID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811175346_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [StudentID] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [EnrollmentNumber] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([StudentID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811175346_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811175346_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811182121_AddIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811182121_AddIdentity', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD [ClassId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD [FacultyId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD [SchoolClassId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD [UserId] nvarchar(450) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE TABLE [Faculties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_Faculties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE TABLE [Classes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [FacultyId] int NOT NULL,
        CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Classes_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE TABLE [AttendanceSessions] (
        [Id] int NOT NULL IDENTITY,
        [ClassId] int NOT NULL,
        [TeacherId] nvarchar(450) NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AttendanceSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceSessions_AspNetUsers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AttendanceSessions_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE TABLE [Teachers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [FacultyId] int NOT NULL,
        [ClassId] int NOT NULL,
        [SchoolClassId] int NOT NULL,
        CONSTRAINT [PK_Teachers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Teachers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Teachers_Classes_SchoolClassId] FOREIGN KEY ([SchoolClassId]) REFERENCES [Classes] ([Id]),
        CONSTRAINT [FK_Teachers_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Students_FacultyId] ON [Students] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Students_SchoolClassId] ON [Students] ([SchoolClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_AttendanceSessions_ClassId] ON [AttendanceSessions] ([ClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_AttendanceSessions_TeacherId] ON [AttendanceSessions] ([TeacherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Classes_FacultyId] ON [Classes] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Teachers_FacultyId] ON [Teachers] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Teachers_SchoolClassId] ON [Teachers] ([SchoolClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    CREATE INDEX [IX_Teachers_UserId] ON [Teachers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD CONSTRAINT [FK_Students_Classes_SchoolClassId] FOREIGN KEY ([SchoolClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    ALTER TABLE [Students] ADD CONSTRAINT [FK_Students_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911191031_AddTeacherAndAttendanceTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911191031_AddTeacherAndAttendanceTables', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [AttendanceSessions] ADD [Subject] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'Status');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Attendances] ALTER COLUMN [Status] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [Attendances] ADD [Method] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [Attendances] ADD [SessionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [Attendances] ADD [Subject] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    CREATE INDEX [IX_Attendances_SessionId] ON [Attendances] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    CREATE INDEX [IX_Attendances_StudentID] ON [Attendances] ([StudentID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_AttendanceSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AttendanceSessions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Students_StudentID] FOREIGN KEY ([StudentID]) REFERENCES [Students] ([StudentID]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918131257_AddAttendanceEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918131257_AddAttendanceEnhancements', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918143819_AddFaceRecognitionToStudent'
)
BEGIN
    ALTER TABLE [Students] ADD [FaceEmbedding] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918143819_AddFaceRecognitionToStudent'
)
BEGIN
    ALTER TABLE [Students] ADD [FaceImagePath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918143819_AddFaceRecognitionToStudent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918143819_AddFaceRecognitionToStudent', N'10.0.10');
END;

COMMIT;
GO

