 CREATE OR ALTER PROCEDURE [AddUsersToChat] 
	@IDList IdListType READONLY, 
	@ChatID INT  
	AS
 INSERT INTO [ChatUsers] ([UserID], [ChatID])
	SELECT [a].[ID], @ChatID FROM @IDList AS a;
 INSERT INTO [ChatUserInfos] ([UserID], [ChatID])
	SELECT [a].[ID], @chatID FROM @IDList AS a;