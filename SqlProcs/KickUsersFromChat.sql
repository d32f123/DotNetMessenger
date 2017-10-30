 DROP PROCEDURE [KickUsersFromChat];
 GO
 
 CREATE OR ALTER PROCEDURE [KickUsersFromChat] 
	@IDList IdListType READONLY, 
	@ChatID INT  
	AS
 DELETE FROM [ChatUsers] WHERE [ChatUsers].[UserID] IN 
	(SELECT [a].[ID] FROM @IDList AS a) AND [ChatUsers].[ChatID] = @ChatID;