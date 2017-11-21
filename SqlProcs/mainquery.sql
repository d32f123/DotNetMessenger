 DROP TABLE IF EXISTS [ChatUserInfos]
 DROP TABLE IF EXISTS [UserRoles]
 DROP TABLE IF EXISTS [ChatUsers];
 DROP TABLE IF EXISTS [Attachments];
 DROP TABLE IF EXISTS [AttachmentTypes];
 DROP TABLE IF EXISTS [MessagesDeleteQueue];
 DROP TABLE IF EXISTS [Messages];
 DROP TABLE IF EXISTS [ChatInfos];
 DROP TABLE IF EXISTS [Chats];
 DROP TABLE IF EXISTS [ChatTypes];
 DROP TABLE IF EXISTS [UserInfos];
 DROP TABLE IF EXISTS [Tokens];
 DROP TABLE IF EXISTS [Users];
 DROP TYPE IF EXISTS [GenderType];
 IF EXISTS (SELECT * FROM [sys].[fulltext_catalogs] WHERE name='CG_Messages') 
	DROP FULLTEXT CATALOG [CG_Messages];

 CREATE TYPE [GenderType] FROM CHAR(1) NULL;
 GO

	-- USERS table
 CREATE TABLE [Users](
	[ID]			INT IDENTITY(0, 1),
	[Username]		VARCHAR(50) UNIQUE NOT NULL,
	[Password]		VARCHAR(50) NOT NULL,
	[LastSeenDate]  DATETIME
		CONSTRAINT	[DF_LastSeenDate] DEFAULT GETDATE(),
	[RegisterDate]	DATETIME
		CONSTRAINT	[DF_RegisterDate] DEFAULT GETDATE(),
	CONSTRAINT		[PK_Users] PRIMARY KEY([ID]),
	CONSTRAINT		[CK_Users_Username] CHECK(LEN([Username]) >= 4)
	);
	-- INSERT DELETED USER
INSERT INTO [Users] ([Username], [Password]) VALUES ('deleted', 'x'); -- a deleted user
	-- USERINFO table
 CREATE TABLE [UserInfos](
	[UserID]		INT,
	[LastName]		VARCHAR(30), -- index candidate
	[FirstName]		VARCHAR(30), -- index candidate
	[Phone]			VARCHAR(15),
	[Email]			VARCHAR(40), -- index candidate
	[DateOfBirth]	DATE,
	[Gender]		[GenderType],
	[Avatar]		VARBINARY(MAX),
	CONSTRAINT		[PK_UserInfos] PRIMARY KEY([UserID]),
	CONSTRAINT		[FK_UserInfosToUsers] FOREIGN KEY ([UserID]) REFERENCES [Users]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[CK_UserInfos_Gender] CHECK ([Gender] IN ('F', 'M', 'U')),
	CONSTRAINT		[CK_UserInfos_Email] CHECK ([Email] LIKE '%@%.%')
	);
 CREATE INDEX [IX_UserInfos_LastNameFirstNameEmail] ON [UserInfos]([LastName], [FirstName], [Email]);

	-- CHATTYPES TABLE
 CREATE TABLE [ChatTypes](
	[ID]			INT IDENTITY(0,1),
	[Name]			VARCHAR(20),
	CONSTRAINT		[PK_ChatTypes] PRIMARY KEY ([ID])
 );
	-- TWO TYPES: DIALOG AND GROUP
 INSERT INTO [ChatTypes] VALUES ('Dialog');
 INSERT INTO [ChatTypes] VALUES ('GroupChat');

	-- CHATS Table
 CREATE TABLE [Chats](
	[ID]			INT IDENTITY(0, 1),
	[ChatType]		INT NOT NULL,
	[CreatorID]		INT NOT NULL -- index candidate
		CONSTRAINT	[DF_CreatorID] DEFAULT 0,
	CONSTRAINT		[PK_Chats] PRIMARY KEY([ID]),
	CONSTRAINT		[FK_ChatsUsers] FOREIGN KEY ([CreatorID]) REFERENCES [Users]([ID]) ON DELETE SET DEFAULT,
	CONSTRAINT		[FK_ChatsChatTypes] FOREIGN KEY ([ChatType]) REFERENCES [ChatTypes]([ID])
 );
 CREATE INDEX [IX_Chats_CreatorID] ON [Chats]([CreatorID]);
 CREATE INDEX [IX_Chats_ChatType] ON [Chats]([ChatType]);

	-- ChatInfos table
 CREATE TABLE [ChatInfos](
	[ChatID]		INT,
	[Title]			VARCHAR(30),
	[Avatar]		VARBINARY(MAX),
	CONSTRAINT		[PK_ChatInfos] PRIMARY KEY ([ChatID]),
	CONSTRAINT		[FK_ChatInfosChats] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE,
 );

	-- ChatUsers table
 CREATE TABLE [ChatUsers](
	[UserID]		INT,
	[ChatID]		INT,
	CONSTRAINT		[PK_ChatUsers] PRIMARY KEY ([UserID], [ChatID]),
	CONSTRAINT		[FK_ChatUsersUsers] FOREIGN KEY ([UserID]) REFERENCES [Users]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_ChatUsersChats] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE
 );

	-- UserRoles in group chat
 CREATE TABLE [UserRoles](
	[ID]			INT IDENTITY(0, 1),
	[Name]			VARCHAR(20),
	[ReadPerm]		BIT NOT NULL,
	[WritePerm]		BIT NOT NULL,
	[ChatInfoPerm]	BIT NOT NULL,
	[AttachPerm]	BIT NOT NULL,
	[ManageUsersPerm]
					BIT NOT NULL,
	CONSTRAINT		[PK_UserRoles] PRIMARY KEY ([ID]),
	CONSTRAINT		[UQ_UserRolesPermissions] UNIQUE ([ReadPerm], [WritePerm],
							[ChatInfoPerm], [AttachPerm], [ManageUsersPerm])
 );

	-- Various user roles
 INSERT INTO [UserRoles] VALUES ('Listener', 1, 0, 0, 0, 0), ('Regular', 1, 1, 0, 0, 0), 
	('Trusted', 1, 1, 1, 1, 0), ('Moderator', 1, 1, 1, 1, 1);

	-- ChatUserInfos table
 CREATE TABLE [ChatUserInfos](
	[UserID]		INT,
	[ChatID]		INT,
	[Nickname]		VARCHAR(30),
	[UserRole]		INT NOT NULL
		CONSTRAINT	[DF_UserRole] DEFAULT 1,
	CONSTRAINT		[PK_ChatUserInfos] PRIMARY KEY ([UserID], [ChatID]),
	CONSTRAINT		[FK_ChatUserInfosChatUsers] FOREIGN KEY ([UserID], [ChatID]) 
		REFERENCES	[ChatUsers]([UserID], [ChatID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_ChatUserInfosUserRoles] FOREIGN KEY ([UserRole])
		REFERENCES	[UserRoles]([ID]),
	CONSTRAINT		[CK_ChatUserInfos_Nickname] CHECK([Nickname] IS NULL OR LEN([Nickname]) >= 1)
 );

	-- Messages table
 CREATE TABLE [Messages](
	[ID]			INT IDENTITY(0, 1),
	[ChatID]		INT NOT NULL, -- index candidate
	[SenderID]		INT NOT NULL  -- index candidate
		CONSTRAINT [DF_SenderID] DEFAULT 0,
	[MessageText]	VARCHAR(MAX),
	[MessageDate]	DATETIME NOT NULL-- index candidate
		CONSTRAINT	[DF_MessageDate] DEFAULT GETDATE(),
	CONSTRAINT	[PK_Messages] PRIMARY KEY	([ID]),
	CONSTRAINT	[FK_MessagesChat] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE,
	CONSTRAINT	[FK_MessagesUser] FOREIGN KEY ([SenderID]) REFERENCES [Users]([ID]) ON DELETE SET DEFAULT,
 );
 CREATE INDEX [IX_Messages_ChatID] ON [Messages]([ChatID]);
 CREATE INDEX [IX_Messages_SenderID] ON [Messages]([SenderID]);
 CREATE INDEX [IX_Messages_MessageDate] ON [Messages]([MessageDate]);

	-- create catalog for indexed string search
 CREATE FULLTEXT CATALOG [CG_Messages]
	WITH ACCENT_SENSITIVITY = OFF;

	-- create indexed string search
 CREATE FULLTEXT INDEX ON [Messages]([MessageText])
	KEY INDEX [PK_Messages] ON [CG_Messages];

	-- table that keeps the expiration times of messages
CREATE TABLE [MessagesDeleteQueue](
	[MessageID]		INT,
	[ExpireDate]	DATETIME NOT NULL,
	CONSTRAINT		[PK_MessagesDeleteQueue] PRIMARY KEY ([MessageID]),
	CONSTRAINT		[FK_MessagesDeleteQueueMessages] FOREIGN KEY ([MessageID]) REFERENCES [Messages]([ID]) ON DELETE CASCADE
);
CREATE INDEX [IX_MessagesDeleteQueue_ExpireDate] ON [MessagesDeleteQueue]([ExpireDate]);

	-- types of attachments
CREATE TABLE [AttachmentTypes](
	[ID]			INT IDENTITY(0, 1),
	[FileFormat]	VARCHAR(30),
	CONSTRAINT		[PK_AttachmentTypes] PRIMARY KEY ([ID])
);

	-- two types: regular and image
INSERT INTO [AttachmentTypes] VALUES ('Regular file'), ('Image');

	-- Attachments table
 CREATE TABLE [Attachments](
	[ID]			INT IDENTITY(0, 1),
	[FileName]		VARCHAR(60) NOT NULL,
	[Type]			INT NOT NULL		-- 0 - image, 1 -- regular file
		CONSTRAINT DF_Type DEFAULT 0,
	[AttachFile]	VARBINARY(MAX) NOT NULL,
	[MessageID]		INT NOT NULL,  -- index candidate
	CONSTRAINT		[PK_Attachments] PRIMARY KEY ([ID]),
	CONSTRAINT		[FK_AttachmentsMessages] FOREIGN KEY ([MessageID]) REFERENCES [Messages]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_AttachmentsAttachmentTypes] FOREIGN KEY ([Type]) REFERENCES [AttachmentTypes]([ID]) ON DELETE SET DEFAULT
 );
 CREATE INDEX [IX_Attachments_MessageID] ON [Attachments]([MessageID]);
 CREATE INDEX [IX_Attachments_Type] ON [Attachments]([Type]);

 	-- a table that keeps various access tokens
 CREATE TABLE [Tokens] (
	[Token]			VARCHAR(36),
	[UserID]		INT NOT NULL,
	[LastLoginDate] DATETIME NOT NULL
		CONSTRAINT	[DF_LastLoginDate] DEFAULT GETDATE(),
	[ExpireDays]	TINYINT NOT NULL
		CONSTRAINT	[DF_ExpireDays]	DEFAULT 10,
	CONSTRAINT		[PK_Tokens] PRIMARY KEY ([Token]),
	CONSTRAINT		[FK_TokensUsers] FOREIGN KEY ([UserID]) REFERENCES [Users]([ID]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_Tokens_UserID] ON [Tokens]([UserID]);
GO
	-- create a stored procedure to clear bad tokens
 CREATE OR ALTER PROCEDURE [DeleteExpiredTokens] AS
	DELETE FROM [Tokens] 
	WHERE DATEADD(HOUR, [Tokens].[ExpireDays], [Tokens].[LastLoginDate]) <= GETDATE();
GO

	-- checks whether the specified user is in the specified chat
CREATE OR ALTER FUNCTION Check_For_ChatUser_Combination (
    @ChatID INT,
	@UserID INT
) RETURNS BIT
AS
BEGIN
    IF EXISTS (SELECT * FROM [ChatUsers] WHERE [UserID] = @UserID AND [ChatID] = @ChatID)
        RETURN 1
    RETURN 0
END
GO

	-- trigger that checks if user's info is being deleted while user is not
 CREATE OR ALTER TRIGGER [TR_ChatUserInfos_Delete]
	ON [ChatUserInfos]
 FOR DELETE
 AS
 DECLARE @chatId INT, @userId INT
	SELECT @chatId = d.[ChatID], @userId = d.[UserID] FROM DELETED d
	IF EXISTS (SELECT * FROM [ChatUsers] WHERE [ChatID] = @chatId AND [UserID] = @userId)
	BEGIN
		ROLLBACK TRANSACTION;
		THROW 50000, 'Cannot DELETE ChatUserInfo entry', 1
	END
 GO
	-- trigger that checks if inserting chatspecific info for dialog
 CREATE OR ALTER TRIGGER [TR_ChatUserInfos_Insert]
	ON [ChatUserInfos]
 FOR INSERT
 AS
 DECLARE @chatId INT, @chatType INT
	SELECT @chatId = i.[ChatID] FROM INSERTED i;
	SELECT @chatType = c.[ChatType] FROM [Chats] c WHERE c.[ID] = @chatId;
	IF (@chatType = 0)
	BEGIN
		ROLLBACK TRANSACTION;
		THROW 50000, 'Cannot INSERT ChatUserInfo entry for dialog', 1
	END
GO
	-- trigger that checks that there are no more than 2 users for dialog
 CREATE OR ALTER TRIGGER [TR_ChatUsers_Insert]
	ON [ChatUsers]
 FOR INSERT
 AS
	DECLARE @chatId INT, @userCount INT, @chatType INT, @userId INT
	SELECT @chatId = i.[ChatID], @userId = i.[UserID] FROM INSERTED i;
	SELECT @userCount = COUNT(*) FROM [ChatUsers] WHERE [ChatID] = @chatId;
	SELECT @chatType = [ChatType] FROM [Chats] WHERE [ID] = @chatId;
	IF (@userCount > 2 AND @chatType = 0)
	BEGIN
		RAISERROR('Cannot INSERT more than 2 users for dialog', 16, 1)
		ROLLBACK TRANSACTION
	END
	-- insert a chatuserinfo entry
	IF (@chatType <> 0)
		INSERT [ChatUserInfos] ([ChatID], [UserID]) VALUES (@chatId, @userId);
 GO
	-- trigger that checks that you cannot add or remove deleted user (id 0) to the chat
 CREATE OR ALTER TRIGGER [TR_ChatUsers_InsertDelete_DefaultUser]
	ON [ChatUsers]
 FOR INSERT, DELETE
 AS
	DECLARE @userId INT
	SELECT @userId = i.[UserID] FROM INSERTED i;
	IF (@userId = 0)
	BEGIN
		RAISERROR('Cannot INSERT or DELETE default user from chat', 16, 1)
		ROLLBACK TRANSACTION
	END
 GO

	-- trigger that makes sure we cannot kick creator of the chat
 CREATE OR ALTER TRIGGER [TR_ChatUsers_Delete_Creator]
	ON [ChatUsers]
 FOR DELETE
 AS
	DECLARE @userId INT, @chatId INT
	SELECT @userId = i.[UserID], @chatId = i.[ChatID] FROM INSERTED i;
	IF (@userId = (SELECT c.[CreatorID] FROM [Chats] c WHERE c.[ID] = @chatId))
	BEGIN
		RAISERROR('Cannot DELETE creator from chat', 16, 1);
		ROLLBACK TRANSACTION;
	END
 GO
	-- trigger that checks that we cannot delete users from dialog
 CREATE OR ALTER TRIGGER [TR_ChatUsers_Delete]
	ON [ChatUsers]
 FOR DELETE
 AS
	DECLARE @chatId INT, @chatType INT
	SELECT @chatId = i.[ChatID] FROM INSERTED i;
	SELECT @chatType = [ChatType] FROM [Chats] WHERE [ID] = @chatId;
	IF (@chatType = 0)
	BEGIN
		RAISERROR('Cannot DELETE from dialog', 16, 1)
		ROLLBACK TRANSACTION
	END
 GO
	-- trigger that checks that new creator is in chat
 CREATE OR ALTER TRIGGER [TR_Chats_Insert]
	ON [Chats]
 AFTER INSERT
 AS
 DECLARE @chatId INT, @userId INT
	SELECT @chatId = i.[ID],
		@userId = i.[CreatorID]
	FROM INSERTED i;
	INSERT INTO [ChatUsers] ([ChatID], [UserID]) VALUES (@chatId, @userId);
 GO
	-- trigger that checks that chat info is not being set on dialog
 CREATE OR ALTER TRIGGER [TR_ChatInfos_Insert]
	ON [ChatInfos]
 FOR INSERT, UPDATE, DELETE
 AS
 DECLARE @chatId INT, @chatType INT
	SELECT @chatId = i.[ChatID] FROM INSERTED i;
	SELECT @chatType = [Chats].[ChatType] FROM [Chats] WHERE [Chats].[ID] = @chatId;
	IF (@chatType = 0)
	BEGIN
		RAISERROR ('Cannot INSERT, UPDATE or DELETE Dialog info', 16, 1)
		ROLLBACK TRANSACTION
	END
 GO

	-- trigger that launches on every message insert and checks
	-- whether the user is in chat or not
CREATE OR ALTER TRIGGER [TR_Messages_Insert]
	ON [Messages]
FOR INSERT
AS
DECLARE @chatId INT,
   @senderId INT
SELECT @chatId = i.[ChatID],
	@senderId = i.[SenderID]
FROM INSERTED i
IF ([dbo].Check_For_ChatUser_Combination(@chatId, @senderId) = 0)
BEGIN
	ROLLBACK TRANSACTION;
	THROW 50000, 'INSERT Messages Constraint FAILED', 1
END
IF (@senderId = 0)
BEGIN
	ROLLBACK TRANSACTION;
	THROW 50000, 'INSERT Message sender cannot be 0', 1
END
GO

-- functions
--DROP TYPE IF EXISTS IDListType;
--CREATE TYPE IDListType AS TABLE (
--	ID	INT UNIQUE
--	);

 DROP PROCEDURE [AddUsersToChat];
 GO

 CREATE OR ALTER PROCEDURE [AddUsersToChat] 
	@IDList IdListType READONLY, 
	@ChatID INT  
	AS
 INSERT INTO [ChatUsers] ([UserID], [ChatID])
	SELECT [a].[ID], @ChatID FROM @IDList AS a;

 DROP PROCEDURE [KickUsersFromChat];
 GO
 
 CREATE OR ALTER PROCEDURE [KickUsersFromChat] 
	@IDList IdListType READONLY, 
	@ChatID INT  
	AS
 DELETE FROM [ChatUsers] WHERE [ChatUsers].[UserID] IN 
	(SELECT [a].[ID] FROM @IDList AS a) AND [ChatUsers].[ChatID] = @ChatID;