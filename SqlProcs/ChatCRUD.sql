CREATE OR ALTER PROCEDURE Create_Chat
	@idlist		IDListType READONLY,
	@title		VARCHAR(30) NULL,
	@type		INT
AS
DECLARE @chatId INT, @creatorId INT
	SET @creatorId = (SELECT TOP(1) a.[ID] FROM @idlist a);
	BEGIN TRANSACTION
	BEGIN TRY
		INSERT INTO [Chats] ([ChatType], [CreatorID]) VALUES (@type, @creatorId);
		SET @chatId = @@IDENTITY;
		INSERT INTO [ChatUsers] ([ChatID], [UserID]) 
			SELECT b.[ChatID], a.[ID] FROM (
				SELECT [ID] FROM @idlist 
					EXCEPT SELECT TOP(1) [ID] FROM @idlist) a 
				CROSS JOIN (SELECT @chatId ChatID) b;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
		THROW;
	END CATCH
	COMMIT TRANSACTION
GO

CREATE OR ALTER PROCEDURE Get_Chat
	@id			INT
AS
	SELECT a.[ID], a.[ChatType], a.[Creator], b.[Title], b.[Avatar]
	FROM (
		SELECT c.[ID], ct.[Name] ChatType, u.[Username] Creator 
		FROM [Chats] c, [ChatTypes] ct, [Users] u 
		WHERE c.[ID] = @id
			AND ct.[ID] = c.[ChatType] 
			AND u.[ID] = c.[CreatorID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title], ci.[Avatar] FROM [ChatInfos] ci) b
	ON a.[ID] = b.[ChatID]
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1
	RETURN
GO

CREATE OR ALTER PROCEDURE Get_All_Chats
AS 
	SELECT a.[ID], a.[ChatType], a.[Creator], b.[Title], b.[Avatar]
	FROM (
		SELECT c.[ID], ct.[Name] ChatType, u.[Username] Creator 
		FROM [Chats] c, [ChatTypes] ct, [Users] u 
		WHERE ct.[ID] = c.[ChatType] 
			AND u.[ID] = c.[CreatorID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title], ci.[Avatar] FROM [ChatInfos] ci) b
	ON a.[ID] = b.[ChatID]
	RETURN
GO

CREATE OR ALTER PROCEDURE Delete_Chat
	@id			INT
AS
	DELETE FROM [Chats] WHERE [ID] = @id;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1
	RETURN
GO

CREATE OR ALTER PROCEDURE Add_User
	@chatId		INT,
	@userId		INT
AS
	INSERT INTO [ChatUsers] ([ChatID], [UserID]) VALUES (@chatId, @userId);
	RETURN;
GO

CREATE OR ALTER PROCEDURE Add_Users
	@chatId		INT,
	@userIds	[IDListType] READONLY
AS
	INSERT [ChatUsers] ([ChatID], [UserID]) SELECT c.ID, l.ID FROM (SELECT @chatId ID) c CROSS JOIN @userIds l;
	RETURN;
GO

CREATE OR ALTER PROCEDURE Kick_User
	@chatId		INT,
	@userId		INT
AS
	DELETE FROM [ChatUsers] WHERE [ChatID] = @chatId AND [UserID] = @userId;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1;
GO

CREATE OR ALTER PROCEDURE Kick_Users
	@chatId		INT,
	@userIds	[IDListType] READONLY
AS
	BEGIN TRANSACTION
	DELETE FROM [ChatUsers] WHERE [ChatID] = @chatId AND [UserID] IN (SELECT ids.[ID] FROM @userIds ids);
	IF (@@ROWCOUNT <> (SELECT COUNT(*) FROM @userIds))
	BEGIN
		ROLLBACK TRANSACTION;
		THROW 50000, 'some ids were invalid', 1;
	END
	COMMIT TRANSACTION
GO

CREATE OR ALTER PROCEDURE Get_Chat_Users
	@chatId		INT
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1;
	SELECT u.[ID], u.[Username], u.[Password], u.[LastSeenDate], u.[RegisterDate]  
	FROM [Users] u, [ChatUsers] cu WHERE cu.[ChatID] = @chatId AND u.[ID] = cu.[UserID];
	RETURN;
GO

CREATE OR ALTER PROCEDURE Get_User_Chats
	@userId		INT
AS
	IF (@userId = 0 OR NOT EXISTS(SELECT * FROM [Users] WHERE [ID] = @userId))
		THROW 50000, 'id is invalid', 1;
	SELECT c.[ID], u.[Username] Creator, ct.[Name] ChatType FROM [ChatUsers] cu, [Chats] c, [ChatTypes] ct, [Users] u
	WHERE cu.[UserID] = @userId
		AND c.[ID] = cu.[ChatID]
		AND u.[ID] = c.[CreatorID]
		AND ct.[ID] = c.[ChatType];
		RETURN;
GO

CREATE OR ALTER PROCEDURE Get_Chat_Info
	@chatId		INT
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	SELECT ci.[Title], ci.[Avatar] FROM [ChatInfos] ci WHERE ci.[ChatID] = @chatId;
	RETURN;
GO

CREATE OR ALTER PROCEDURE Set_Chat_Title
	@chatId		INT,
	@title		VARCHAR(30) NULL
AS
	UPDATE [ChatInfos] SET [Title] = @title WHERE [ChatID] = @chatId;
	IF @@ROWCOUNT = 0
		INSERT [ChatInfos] ([ChatID], [Avatar], [Title]) VALUES (@chatId, NULL, @title)
GO

CREATE OR ALTER PROCEDURE Set_Chat_Avatar
	@chatId		INT,
	@avatar		VARBINARY(MAX) NULL
AS
	UPDATE [ChatInfos] SET [Avatar] = @avatar WHERE [ChatID] = @chatId;
	IF @@ROWCOUNT = 0
		INSERT [ChatInfos] ([ChatID], [Avatar], [Title]) VALUES (@chatId, @avatar, NULL)	
GO

CREATE OR ALTER PROCEDURE Delete_Chat_Info
	@chatId		INT
AS
	DELETE FROM [ChatInfos] WHERE [ChatID] = @chatId;
	IF @@ROWCOUNT = 0
		THROW 50000, 'no info exists', 1
GO

CREATE OR ALTER PROCEDURE Set_Creator
	@chatId		INT,
	@newCreator	INT
AS
	UPDATE [Chats] SET [CreatorID] = @newCreator WHERE [ID] = @chatId;
	IF @@ROWCOUNT = 0
		THROW 50000, 'no chat exists', 1
GO

CREATE OR ALTER PROCEDURE Set_ChatSpecific_UserInfo
	@chatId		INT,
	@userId		INT,
	@nickname	VARCHAR(30),
	@roleId		INT NULL
AS
	IF @roleId IS NOT NULL
	BEGIN
		UPDATE [ChatUserInfos] SET [Nickname] = @nickname, [UserRole] = @roleId
		WHERE [ChatID] = @chatId AND [UserID] = @userId;
		IF @@ROWCOUNT = 0
			INSERT [ChatUserInfos] ([ChatID], [UserID], [Nickname], [UserRole])
			VALUES (@chatId, @userId, @nickname, @roleId);
	END
	ELSE
	BEGIN
		UPDATE [ChatUserInfos] SET [Nickname] = @nickname
		WHERE [ChatID] = @chatId AND [UserID] = @userId;
		IF @@ROWCOUNT = 0
			INSERT [ChatUserInfos] ([ChatID], [UserID], [Nickname])
			VALUES (@chatId, @userId, @nickname);
	END
GO

CREATE OR ALTER PROCEDURE Get_ChatSpecific_UserInfo
	@chatId		INT,
	@userId		INT
AS
	IF NOT EXISTS (SELECT * FROM [ChatUsers] WHERE [ChatID] = @chatId AND [UserID] = @userId)
		THROW 50000, 'id is invalid', 1;
	SELECT cui.[Nickname], ur.[Name], ur.[ReadPerm], ur.[WritePerm], ur.[ChatInfoPerm], ur.[AttachPerm], ur.[ManageUsersPerm] 
	FROM [ChatUserInfos] cui, [UserRoles] ur
	WHERE cui.ChatID = @chatId AND cui.UserID = @userId AND ur.ID = cui.UserRole;
	RETURN
GO

CREATE OR ALTER PROCEDURE Set_ChatSpecific_UserRole
	@chatId		INT,
	@userId		INT,
	@roleId		INT
AS
	IF NOT EXISTS (SELECT * FROM [ChatUsers] WHERE [ChatID] = @chatId AND [UserID] = @userId)
		THROW 50000, 'id is invalid', 1;
	UPDATE [ChatUserInfos] SET [UserRole] = @roleId WHERE [ChatID] = @chatId AND [UserID] = @userId;
GO

CREATE OR ALTER PROCEDURE Clear_ChatSpecific_UserInfo
	@chatId		INT,
	@userId		INT
AS
	UPDATE [ChatUserInfos] SET [Nickname] = NULL, [UserRole] = DEFAULT WHERE [ChatID] = @chatId AND [UserID] = @userId;
	IF @@ROWCOUNT = 0
		THROW 50000, 'id is invalid', 1;
GO

--TODO: ChatSpecificInfo, Role

DECLARE @idlist IDListType;
INSERT @idlist ([ID]) VALUES (1), (2);
EXECUTE Create_Chat @idlist, 'nice', 1;
EXECUTE Get_Chat 1;
EXECUTE Get_All_Chats;
EXECUTE Delete_Chat 7;
EXECUTE Add_User 1, 1;

DECLARE @idlist1 IDListType;
INSERT @idlist1 ([ID]) VALUES (1), (3);
EXECUTE Add_Users 9, @idlist1;

DECLARE @idlist2 IDListType;
INSERT @idlist2 ([ID]) VALUES (1), (0);
EXECUTE Kick_Users 9, @idlist2;
EXECUTE Kick_User 1, 1;

EXECUTE Get_Chat_Users 1003;
EXECUTE Get_User_Chats 6;

EXECUTE Get_Chat_Info 1;
EXECUTE Set_Chat_Title 1, 'real nice';
EXECUTE Delete_Chat_Info 2;

EXECUTE Set_ChatSpecific_UserInfo 2, 1, 'hey asd', 3;
EXECUTE Get_ChatSpecific_UserInfo 2, 1;

EXECUTE Clear_ChatSpecific_UserInfo 2, 1;

DELETE [ChatUserInfos] WHERE [ChatID] = 1 AND [UserID] = 1