CREATE OR ALTER PROCEDURE Create_User
	@username	VARCHAR(50), 
	@password	VARCHAR(50)
AS
	INSERT INTO [Users] ([Username], [Password]) OUTPUT INSERTED.ID VALUES (@username, @password);
	RETURN;
GO

CREATE OR ALTER PROCEDURE Delete_User
	@id	INT
AS
IF (@id = 0)
BEGIN;
	THROW 50000, 'id is invalid', 1
END
	DELETE FROM [Users] WHERE [ID] = @id;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1
GO

CREATE OR ALTER PROCEDURE Change_User
	@id INT,
	@username VARCHAR(50),
	@password VARCHAR(50)
AS
	UPDATE [Users] SET [Username] = @username, [Password] = @password
	WHERE [ID] = @id;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1
	RETURN	
GO

CREATE OR ALTER PROCEDURE Get_All_Users
AS
	SELECT [ID], [Username], [Password], [RegisterDate], [LastSeenDate] FROM [Users] WHERE [ID] <> 0;
	RETURN;
GO

CREATE OR ALTER PROCEDURE Get_User
	@id INT
AS
	SELECT * FROM [Users] WHERE [ID] = @id;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'id is invalid', 1
	RETURN
GO

CREATE OR ALTER PROCEDURE Get_User_By_Username
	@username VARCHAR(50)
AS
	SELECT * FROM [Users] WHERE [Username] = @username;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'username is invalid', 1
	RETURN;
GO

CREATE OR ALTER PROCEDURE Set_User_Info
	@id			INT,
	@lastname	VARCHAR(30) NULL,
	@firstname	VARCHAR(30) NULL,
	@phone		VARCHAR(15) NULL,
	@email		VARCHAR(40) NULL,
	@dateofbirth DATE NULL,
	@gender		CHAR(1) NULL
AS
	IF (NOT EXISTS(SELECT * FROM [UserInfos] WHERE [UserID] = @id))
	BEGIN
		INSERT INTO [UserInfos] ([UserID], [LastName], [FirstName], [Phone], [Email], [DateOfBirth], [Gender])
		VALUES (@id, @lastname, @firstname, @phone, @email, @dateofbirth, @gender);
	END
	ELSE
		UPDATE [UserInfos] SET [LastName] = @lastname, [FirstName] = @firstname, 
			[Phone] = @phone, [Email] = @email, [DateOfBirth] = @dateofbirth, [Gender] = @gender
		WHERE [UserID] = @id;
GO

CREATE OR ALTER PROCEDURE Get_User_Info
	@id			INT
AS
	IF (NOT EXISTS(SELECT * FROM [Users] WHERE [ID] = @id))
		THROW 50000, 'id is invalid', 1
	SELECT * FROM [UserInfos] WHERE [UserID] = @id;
	RETURN;
GO

CREATE OR ALTER PROCEDURE Delete_User_Info
	@id			INT
AS
	IF (NOT EXISTS(SELECT * FROM [Users] WHERE [ID] = @id))
		THROW 50000, 'id is invalid', 1
	DELETE FROM [UserInfos] WHERE [UserID] = @id;
	IF (@@ROWCOUNT = 0)
		THROW 50000, 'no info', 1
	RETURN;
GO	

CREATE OR ALTER PROCEDURE Set_User_Avatar
	@id			INT,
	@avatar		VARBINARY(MAX)
AS
	IF (NOT EXISTS(SELECT * FROM [UserInfos] WHERE [UserID] = @id))
	BEGIN
		INSERT INTO [UserInfos] ([UserID], [Avatar])
		VALUES (@id, @avatar);
	END
	ELSE
		UPDATE [UserInfos] SET [Avatar] = @avatar
		WHERE [UserID] = @id;
GO

CREATE OR ALTER PROCEDURE Get_Users_InRange
	@startId	INT,
	@endId		INT
AS
	SELECT [ID], [Username], [Password], [RegisterDate], [LastSeenDate] FROM [Users] 
	WHERE [ID] <> 0 AND [ID] BETWEEN @startId AND @endId;
	RETURN;
GO

CREATE OR ALTER PROCEDURE Get_Users_By_Id
	@idlist		IDListType READONLY
AS
	SELECT [ID], [Username], [Password], [RegisterDate], [LastSeenDate] FROM [Users]
	WHERE [ID] IN (SELECT list.[ID] FROM @idlist list);
	RETURN;
GO

EXECUTE Get_All_Users;
EXECUTE Create_User 'adminasdf', 'x';
EXECUTE Delete_User 2;
EXECUTE Get_User 2;
EXECUTE Get_User_By_Username 'd32f1234';
EXECUTE Change_User 1, 'd32f123', 'asdasdasddas'

EXECUTE Set_User_Info 1, 'Nesterov', NULL, '+79502704406', 'd32f123@yandex.ru', '06/05/1998', 'M';
EXECUTE Get_User_Info 1;
EXECUTE Delete_User_Info 1;

DECLARE @avatar VARBINARY(MAX);
SET @avatar = CONVERT(VARBINARY(MAX), 'asdasdasd');
EXECUTE Set_User_Avatar 1, @avatar;