 CREATE OR ALTER PROCEDURE [DeleteExpiredMessages] AS
	DELETE m FROM [Messages] AS m, [MessagesDeleteQueue] AS mdq 
	WHERE [m].[ID] = [mdq].[MessageID] AND [mdq].[ExpireDate] <= GETDATE();