DROP TYPE IF EXISTS [AttachmentsListType];
CREATE TYPE [AttachmentsListType] AS TABLE(
	[Attachment] VARBINARY(MAX),
	[Type]		INT
);
GO

CREATE OR ALTER PROCEDURE Store_Message
	@senderId	INT,
	@chatId		INT,
	@text		VARCHAR(MAX),
	@expirationDate DATETIME NULL
AS
DECLARE @messageId INT
	-- store message
	BEGIN TRY
	INSERT [Messages] ([ChatID], [SenderID], [MessageText])
	VALUES (@chatId, @senderId, @text);
	SET @messageId = @@IDENTITY;
	-- store expiration date if needed
	IF @expirationDate IS NOT NULL
		INSERT [MessagesDeleteQueue] ([ExpireDate], [MessageID])
		VALUES (@expirationDate, @messageId);
	SELECT @messageId ID;
	END TRY
	BEGIN CATCH
		THROW;
	END CATCH
	RETURN;
GO

CREATE OR ALTER PROCEDURE Store_Message_WithAttachments
	@senderId	INT,
	@chatId		INT,
	@text		VARCHAR(MAX),
	@attachments [AttachmentsListType] READONLY,
	@expirationDate DATETIME NULL
AS
DECLARE @messageId INT
	BEGIN TRY
	-- store message
	EXECUTE @messageId = Store_Message @senderId, @chatId, @text, @expirationDate
	-- store attachments
	INSERT [Attachments] ([MessageID], [Type], [AttachFile]) 
	SELECT a.[ID], b.[Type], b.[Attachment] FROM 
		(SELECT @messageId ID) a 
		CROSS JOIN (SELECT att.[Attachment], att.[Type] FROM @attachments att) b;
	
	SELECT @messageId ID;
	RETURN;
	END TRY
	BEGIN CATCH
		THROW;
	END CATCH
GO

CREATE OR ALTER PROCEDURE Get_Message
	@messageId	INT
AS
	SELECT a.[Username], a.[Text], a.[Date], a.[ChatID], b.[ChatTitle], c.[ExpireDate] 
	FROM(SELECT u.[Username] Username, m.[MessageText] [Text], m.[MessageDate] [Date], m.[ChatID] ChatID
			FROM [Messages] m, [Users] u 
			WHERE m.[ID] = @messageId AND u.[ID] = m.[SenderID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title] ChatTitle FROM [ChatInfos] ci) b ON a.[ChatID] = b.[ChatID]
	LEFT JOIN (SELECT mdq.[MessageID], mdq.[ExpireDate] FROM [MessagesDeleteQueue] mdq) c ON c.[MessageID] = @messageId;
	IF @@ROWCOUNT = 0
		THROW 50000, 'id is invalid', 1
GO

CREATE OR ALTER PROCEDURE Get_All_Messages
AS
	SELECT a.[Username], a.[Text], a.[Date], a.[ChatID], b.[ChatTitle], c.[ExpireDate] 
	FROM(SELECT m.[ID], u.[Username] Username, m.[MessageText] [Text], m.[MessageDate] [Date], m.[ChatID] ChatID
			FROM [Messages] m, [Users] u 
			WHERE u.[ID] = m.[SenderID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title] ChatTitle FROM [ChatInfos] ci) b ON a.[ChatID] = b.[ChatID]
	LEFT JOIN (SELECT mdq.[MessageID], mdq.[ExpireDate] FROM [MessagesDeleteQueue] mdq) c ON c.[MessageID] = a.[ID];
	IF @@ROWCOUNT = 0
		THROW 50000, 'id is invalid', 1
GO

CREATE OR ALTER PROCEDURE Get_Message_Attachments
	@messageId	INT
AS
	IF NOT EXISTS (SELECT * FROM [Messages] WHERE [ID] = @messageId)
		THROW 50000, 'id is invalid', 1
	SELECT a.[ID], a.[AttachFile], [at].[FileFormat] FROM [Attachments] a, [AttachmentTypes] [at]
	WHERE a.[Type] = [at].[ID];
GO

CREATE OR ALTER PROCEDURE Get_Chat_Messages_InRange
	@chatId		INT,
	@rangeStart	DATETIME,
	@rangeEnd	DATETIME
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	SELECT a.[Username], a.[Text], a.[Date], a.[ChatID], b.[ChatTitle], c.[ExpireDate] 
	FROM(SELECT m.[ID], u.[Username] Username, m.[MessageText] [Text], m.[MessageDate] [Date], m.[ChatID] ChatID
			FROM [Messages] m, [Users] u 
			WHERE m.[ChatID] = @chatId 
				AND m.[MessageDate] >= @rangeStart 
				AND m.[MessageDate] <= @rangeEnd
				AND  u.[ID] = m.[SenderID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title] ChatTitle FROM [ChatInfos] ci) b ON a.[ChatID] = b.[ChatID]
	LEFT JOIN (SELECT mdq.[MessageID], mdq.[ExpireDate] FROM [MessagesDeleteQueue] mdq) c ON c.[MessageID] = a.[ID];
GO

CREATE OR ALTER PROCEDURE Get_Chat_Messages
	@chatId		INT
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	SELECT a.[Username], a.[Text], a.[Date], a.[ChatID], b.[ChatTitle], c.[ExpireDate] 
	FROM(SELECT m.[ID], u.[Username] Username, m.[MessageText] [Text], m.[MessageDate] [Date], m.[ChatID] ChatID
			FROM [Messages] m, [Users] u 
			WHERE m.[ChatID] = @chatId 
				AND  u.[ID] = m.[SenderID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title] ChatTitle FROM [ChatInfos] ci) b ON a.[ChatID] = b.[ChatID]
	LEFT JOIN (SELECT mdq.[MessageID], mdq.[ExpireDate] FROM [MessagesDeleteQueue] mdq) c ON c.[MessageID] = a.[ID];
GO

CREATE OR ALTER PROCEDURE Get_Chat_Messages_From
	@chatId		INT,
	@dateFrom	DATETIME
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	DECLARE @dateTo DATETIME
	SET @dateTo = GETDATE();
	EXECUTE Get_Chat_Messages_InRange @chatId, @dateFrom, @dateTo;
GO

CREATE OR ALTER PROCEDURE Get_Chat_Messages_To
	@chatId		INT,
	@dateTo		DATETIME
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	DECLARE @dateFrom DATETIME
	SET @dateFrom = CONVERT(DATETIME, '01/01/1800');
	EXECUTE Get_Chat_Messages_InRange @chatId, @dateFrom, @dateTo;
GO

CREATE OR ALTER PROCEDURE Search_Messages
	@chatId		INT,
	@string		VARCHAR(255)
AS
	IF NOT EXISTS (SELECT * FROM [Chats] WHERE [ID] = @chatId)
		THROW 50000, 'id is invalid', 1
	SELECT a.[Username], a.[Text], a.[Date], a.[ChatID], b.[ChatTitle], c.[ExpireDate] 
	FROM(SELECT m.[ID], u.[Username] Username, m.[MessageText] [Text], m.[MessageDate] [Date], m.[ChatID] ChatID
			FROM [Messages] m, [Users] u 
			WHERE m.[ChatID] = @chatId 
				AND CONTAINS(m.[MessageText], @string)
				AND u.[ID] = m.[SenderID]) a
	LEFT JOIN (SELECT ci.[ChatID], ci.[Title] ChatTitle FROM [ChatInfos] ci) b ON a.[ChatID] = b.[ChatID]
	LEFT JOIN (SELECT mdq.[MessageID], mdq.[ExpireDate] FROM [MessagesDeleteQueue] mdq) c ON c.[MessageID] = a.[ID];
GO

-- a stored procedure that deletes expired messages
 CREATE OR ALTER PROCEDURE [DeleteExpiredMessages] AS
	DELETE m FROM [Messages] AS m, [MessagesDeleteQueue] AS mdq 
	WHERE [m].[ID] = [mdq].[MessageID] AND [mdq].[ExpireDate] <= GETDATE();
GO

-- JOB
-- set up the queue for activation

-- IF NO BROKER ENABLED:
-- ALTER DATABASE [messenger] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE
-- GO
DROP SERVICE Timers;
DROP QUEUE Timers;
CREATE QUEUE Timers;
CREATE SERVICE Timers ON QUEUE Timers ([DEFAULT]);
GO

-- the activated procedure
CREATE OR ALTER PROCEDURE ActivatedTimers
AS
BEGIN
DECLARE @mt SYSNAME, @h UNIQUEIDENTIFIER;
BEGIN TRANSACTION;
    RECEIVE TOP (1)
        @mt = message_type_name
        , @h = CONVERSATION_HANDLE
        FROM Timers;

    IF @@ROWCOUNT = 0
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @mt IN (N'http://schemas.microsoft.com/SQL/ServiceBroker/Error'
        , N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
    BEGIN
        END CONVERSATION @h;
    END
    ELSE IF @mt = N'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer'
    BEGIN
        EXEC DeleteExpiredMessages;
		EXEC DeleteExpiredTokens;
        -- set a new timer after 2s
        BEGIN CONVERSATION timer (@h) TIMEOUT = 2;
    END
COMMIT
END
GO

-- attach the activated procedure to the queue
ALTER QUEUE Timers WITH ACTIVATION (
    STATUS = ON
    , MAX_QUEUE_READERS = 1
    , EXECUTE AS OWNER
    , PROCEDURE_NAME = ActivatedTimers);
go  


-- seed a conversation to start activating every 2s
DECLARE @h UNIQUEIDENTIFIER;
BEGIN dialog CONVERSATION @h
    FROM SERVICE [Timers]
    TO SERVICE N'Timers', N'current database'
    WITH ENCRYPTION = OFF;
BEGIN CONVERSATION timer (@h) TIMEOUT= 1;



DECLARE @date DATETIME
SET @date = CONVERT(DATETIME, '11:21:00', 108);
EXECUTE Store_Message 1, 1, 'hey', @date;
EXECUTE Get_Message 1012;
EXECUTE Get_Message_Attachments 4;

EXEC DeleteExpiredMessages;

EXECUTE Get_All_Messages;

DECLARE @startDate DATETIME, @endDate DATETIME
SET @startDate = CONVERT(DATETIME, '10/28/2017', 120);
SET @endDate = CONVERT(DATETIME, '10/30/2017', 120);
EXECUTE Get_Chat_Messages_To 1, @endDate;
EXECUTE Get_Chat_Messages_From 1, @startDate;
EXECUTE Get_Chat_Messages_InRange 1, @startDate, @endDate;
EXECUTE Get_Chat_Messages 1;

EXECUTE Search_Messages 1, 'hey';