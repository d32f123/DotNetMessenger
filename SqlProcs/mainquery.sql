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
 IF EXISTS (SELECT * FROM [sys].[fulltext_catalogs] WHERE name='CG_Messages') 
	DROP FULLTEXT CATALOG [CG_Messages];

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
	[Avatar]		VARBINARY(MAX),
	CONSTRAINT		[PK_UserInfos] PRIMARY KEY([UserID]),
	CONSTRAINT		[FK_UserInfosToUsers] FOREIGN KEY ([UserID]) REFERENCES [Users]([ID]) ON DELETE CASCADE
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

	-- ChatInfos table
 CREATE TABLE [ChatInfos](
	[ChatID]		INT,
	[Title]			VARCHAR(30),
	[Avatar]		VARBINARY(MAX),
	CONSTRAINT		[PK_ChatInfos] PRIMARY KEY ([ChatID]),
	CONSTRAINT		[FK_ChatInfosChats] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE
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
	[ReadPerm]		BIT NOT NULL
		CONSTRAINT	[DF_ReadPerm] DEFAULT 1,
	[WritePerm]		BIT NOT NULL
		CONSTRAINT	[DF_WritePerm] DEFAULT 1,
	[ChatInfoPerm]	BIT NOT NULL
		CONSTRAINT	[DF_ChatInfoPerm] DEFAULT 0,
	[AttachPerm]	BIT NOT NULL
		CONSTRAINT	[DF_AttachPerm]	DEFAULT 0,
	[ManageUsersPerm]
					BIT NOT NULL
		CONSTRAINT	[DF_ManageUsersPerm] DEFAULT 0,
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
		REFERENCES	[UserRoles]([ID])
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
	[Type]			INT NOT NULL		-- 0 - image, 1 -- regular file
		CONSTRAINT DF_Type DEFAULT 0,
	[AttachFile]	VARBINARY(MAX) NOT NULL,
	[MessageID]		INT NOT NULL,  -- index candidate
	CONSTRAINT		[PK_Attachments] PRIMARY KEY ([ID]),
	CONSTRAINT		[FK_AttachmentsMessages] FOREIGN KEY ([MessageID]) REFERENCES [Messages]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_AttachmentsAttachmentTypes] FOREIGN KEY ([Type]) REFERENCES [AttachmentTypes]([ID]) ON DELETE SET DEFAULT
 );
 CREATE INDEX [IX_Attachments_MessageID] ON [Attachments]([MessageID]);

 --SELECT * FROM [Users];
 --SELECT * FROM [UserRoles];
 --SELECT * FROM [Chats];
 --SELECT * FROM [ChatUserInfos];
 --SELECT [Nickname], [UserRole] FROM [ChatUserInfos] WHERE [UserID] = 0 AND [ChatID] = 0;

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
	RAISERROR ('INSERT Messages Constraint FAILED', 16, 1)
	ROLLBACK TRANSACTION
END
GO

	-- a stored procedure that deletes expired messages
 CREATE OR ALTER PROCEDURE [DeleteExpiredMessages] AS
	DELETE m FROM [Messages] AS m, [MessagesDeleteQueue] AS mdq 
	WHERE [m].[ID] = [mdq].[MessageID] AND [mdq].[ExpireDate] <= GETDATE();
GO

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
GO
	-- create a stored procedure to clear bad tokens
 CREATE OR ALTER PROCEDURE [DeleteExpiredTokens] AS
	DELETE FROM [Tokens] 
	WHERE DATEADD(HOUR, [Tokens].[ExpireDays], [Tokens].[LastLoginDate]) > GETDATE();
GO

--INSERT INTO [Users] ([Username], [Password]) VALUES ('d32f123', 'asd');

--INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText]) VALUES (0, 0, 'xd'); 
---- INSERT INTO [ChatUserInfos] ([UserID], [ChatID], [UserRole]) VALUES (0, 0, 1);
--INSERT INTO [ChatUsers] ([UserID], [ChatID]) VALUES (0, 0);
--INSERT INTO [Chats] ([ChatType], [CreatorID]) VALUES (0, 0);
-- INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText])
--	OUTPUT INSERTED.[ID], INSERTED.[MessageDate]
--    VALUES (1, 0, 'hey');

-- INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText])
--	OUTPUT INSERTED.[ID], INSERTED.[MessageDate]
--    VALUES (1, 0, 'hey world asd');

--SELECT * FROM [Messages] WHERE CONTAINS([MessageText], '*hey*');

--SELECT * FROM [MessagesDeleteQueue];

--INSERT INTO [MessagesDeleteQueue] ([MessageID], [ExpireDate]) VALUES (1, GETDATE());

--EXEC [DeleteExpiredMessages];