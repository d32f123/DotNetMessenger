CREATE OR ALTER PROCEDURE Get_User_ByToken
	@token		VARCHAR(36)
AS
	SELECT u.[ID], u.[Username], u.[Password], u.[LastSeenDate], u.[RegisterDate] 
	FROM [Tokens] t, [Users] u
	WHERE t.[Token] = @token AND u.[ID] = t.[UserID]
	IF @@ROWCOUNT = 0
		THROW 50000, 'id is invalid', 1
	RETURN
GO

CREATE OR ALTER PROCEDURE Get_All_Tokens
AS
	SELECT t.[Token], t.[LastLoginDate], t.[ExpireDays], u.[ID], u.[Username], u.[Password], u.[LastSeenDate], u.[RegisterDate] 
	FROM [Tokens] t, [Users] u
	WHERE  u.[ID] = t.[UserID]
GO

CREATE OR ALTER PROCEDURE Create_Token
	@userId		INT,
	@token		VARCHAR(36)
AS
	INSERT [Tokens] ([Token], [UserID]) VALUES (@token, @userId);
	IF @@ROWCOUNT = 0
		THROW 50000, 'id is invalid', 1
GO

CREATE OR ALTER PROCEDURE Delete_Token
	@token		VARCHAR(36)
AS
	DELETE FROM [Tokens] WHERE [Token] = @token
	IF @@ROWCOUNT = 0
		THROW 50000, 'token is invalid', 1
GO

EXEC Create_Token 1, 'asdasdasd';

EXEC Get_All_Tokens;