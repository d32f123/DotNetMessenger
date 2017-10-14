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
 DROP TABLE IF EXISTS [Users];
 IF EXISTS (SELECT * FROM [sys].[fulltext_catalogs] WHERE name='CG_Messages') 
	DROP FULLTEXT CATALOG [CG_Messages];

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

INSERT INTO [Users] ([Username], [Password]) VALUES ('deleted', 'x'); -- a deleted user

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

 CREATE TABLE [ChatTypes](
	[ID]			INT IDENTITY(0,1),
	[Name]			VARCHAR(20),
	CONSTRAINT		[PK_ChatTypes] PRIMARY KEY ([ID])
 );

 INSERT INTO [ChatTypes] VALUES ('Dialog');
 INSERT INTO [ChatTypes] VALUES ('GroupChat');

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

 CREATE TABLE [ChatInfos](
	[ChatID]		INT,
	[Title]			VARCHAR(30),
	[Avatar]		VARBINARY(MAX),
	CONSTRAINT		[PK_ChatInfos] PRIMARY KEY ([ChatID]),
	CONSTRAINT		[FK_ChatInfosChats] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE
 );

 CREATE TABLE [ChatUsers](
	[UserID]		INT,
	[ChatID]		INT,
	CONSTRAINT		[PK_ChatUsers] PRIMARY KEY ([UserID], [ChatID]),
	CONSTRAINT		[FK_ChatUsersUsers] FOREIGN KEY ([UserID]) REFERENCES [Users]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_ChatUsersChats] FOREIGN KEY ([ChatID]) REFERENCES [Chats]([ID]) ON DELETE CASCADE
 );

 CREATE TABLE [UserRoles](
	[ID]			TINYINT IDENTITY(0, 1),
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

 INSERT INTO [UserRoles] VALUES ('Listener', 1, 0, 0, 0, 0), ('Regular', 1, 1, 0, 0, 0), 
	('Trusted', 1, 1, 1, 1, 0), ('Moderator', 1, 1, 1, 1, 1);

 CREATE TABLE [ChatUserInfos](
	[UserID]		INT,
	[ChatID]		INT,
	[Nickname]		VARCHAR(30),
	[UserRole]		TINYINT
		CONSTRAINT	[DF_UserRole] DEFAULT 1,
	CONSTRAINT		[PK_ChatUserInfos] PRIMARY KEY ([UserID], [ChatID]),
	CONSTRAINT		[FK_ChatUserInfosChatUsers] FOREIGN KEY ([UserID], [ChatID]) 
		REFERENCES	[ChatUsers]([UserID], [ChatID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_ChatUserInfosUserRoles] FOREIGN KEY ([UserRole])
		REFERENCES	[UserRoles]([ID])
 );

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

 CREATE FULLTEXT CATALOG [CG_Messages]
	WITH ACCENT_SENSITIVITY = OFF;

 CREATE FULLTEXT INDEX ON [Messages]([MessageText])
	KEY INDEX [PK_Messages] ON [CG_Messages];

CREATE TABLE [MessagesDeleteQueue](
	[MessageID]		INT,
	[ExpireDate]	DATETIME NOT NULL,
	CONSTRAINT		[PK_MessagesDeleteQueue] PRIMARY KEY ([MessageID]),
	CONSTRAINT		[FK_MessagesDeleteQueueMessages] FOREIGN KEY ([MessageID]) REFERENCES [Messages]([ID]) ON DELETE CASCADE
);
CREATE INDEX [IX_MessagesDeleteQueue_ExpireDate] ON [MessagesDeleteQueue]([ExpireDate]);


CREATE TABLE [AttachmentTypes](
	[ID]			TINYINT IDENTITY(0, 1),
	[FileFormat]	VARCHAR(30),
	CONSTRAINT		[PK_AttachmentTypes] PRIMARY KEY ([ID])
);

INSERT INTO [AttachmentTypes] VALUES ('Regular file'), ('Image');

 CREATE TABLE [Attachments](
	[ID]			INT IDENTITY(0, 1),
	[Type]			TINYINT NOT NULL		-- 0 - image, 1 -- regular file
		CONSTRAINT DF_Type DEFAULT 0,
	[AttachFile]	VARBINARY(MAX) NOT NULL,
	[MessageID]		INT NOT NULL,  -- index candidate
	CONSTRAINT		[PK_Attachments] PRIMARY KEY ([ID]),
	CONSTRAINT		[FK_AttachmentsMessages] FOREIGN KEY ([MessageID]) REFERENCES [Messages]([ID]) ON DELETE CASCADE,
	CONSTRAINT		[FK_AttachmentsAttachmentTypes] FOREIGN KEY ([Type]) REFERENCES [AttachmentTypes]([ID]) ON DELETE SET DEFAULT
 );
 CREATE INDEX [IX_Attachments_MessageID] ON [Attachments]([MessageID]);

 INSERT INTO [Chats] ([ChatType], [CreatorID]) VALUES (0, 0);
 INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText])
	OUTPUT INSERTED.[ID], INSERTED.[MessageDate]
    VALUES (1, 0, 'hey');

 INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText])
	OUTPUT INSERTED.[ID], INSERTED.[MessageDate]
    VALUES (1, 0, 'hey world asd');

SELECT * FROM [Messages] WHERE CONTAINS([MessageText], '*hey*');

SELECT * FROM [MessagesDeleteQueue];

INSERT INTO [MessagesDeleteQueue] ([MessageID], [ExpireDate]) VALUES (1, GETDATE());

EXEC [DeleteExpiredMessages];