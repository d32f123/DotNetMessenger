DELETE FROM [Users];
DELETE FROM [Chats];

-- create a few users
EXECUTE Create_User 'd32f123', 'x';
EXECUTE Create_User 'mopkoffka', 'x';
EXECUTE Create_User 'admin123', 'x';

-- list the newly created users
EXECUTE Get_All_Users;

-- get a user by username
EXECUTE Get_User_By_Username 'd32f123';

-- set and get user info
EXECUTE Set_User_Info 1, 'Nesterov', NULL, '+79502704406', 'd32f123@yandex.ru', '06/05/1998', 'M';
EXECUTE Set_User_Info 2, 'Iunusov', 'Sanya', '+7999999999', 'mopkoffka97@yandex.ru', '31/05/1997', 'M';

EXECUTE Get_User_Info 1;
EXECUTE Get_User_Info 2;
-- delete user info
EXECUTE Delete_User_Info 1;
EXECUTE Get_User_Info 1;

EXECUTE Get_Chat_Messages 1;

EXECUTE CreateOrGet_Dialog 4, 2;

EXECUTE Get_All_Chats;

EXECUTE Get_Chat 3;

-- create some chats
DECLARE @idlist IDListType;
INSERT @idlist ([ID]) VALUES (1), (2), (4);
EXECUTE Create_Chat @idlist, 'chat title', 1;
-- create a dialog
DECLARE @idlist1 IDListType;
INSERT @idlist1 ([ID]) VALUES (1), (3);
EXECUTE Create_Chat @idlist1, 'dialog', 0;

DECLARE @cmList [ChatMessageType];
INSERT @cmList ([ChatID], [MessageID]) VALUES (0, -1), (3, -1);
EXECUTE Find_New_Messages_InChats @cmList;

-- easier way to create a dialog
EXECUTE CreateOrGet_Dialog 1, 2;
-- get the newly created chats
EXECUTE Get_All_Chats;
-- get users of a chat
EXECUTE Get_Chat_Users 3;
-- get users of a dialog
EXECUTE Get_Chat_Users 1;
EXECUTE Get_Chat_Users 2;

-- get user's chats
EXECUTE Get_User_Chats 1;

-- set chat's title
EXECUTE Set_Chat_Title 0, 'new tItle!';
EXECUTE Get_Chat 0;

-- set user's info and role in a chat
EXECUTE Set_ChatSpecific_UserInfo 0, 2, 'hey asd', 3;
EXECUTE Get_ChatSpecific_UserInfo 0, 2;

EXECUTE Get_All_Chats;
EXECUTE Get_All_Users;
-- now let's send some messages
EXECUTE Store_Message 1, 0, 'hello everyone', NULL;
EXECUTE Store_Message 2, 3, 'heyyyyy', NULL;

EXECUTE Get_Chat_Messages 0;

-- try creating a message that should self-delete
DECLARE @date DATETIME
SET @date = DATEADD(SECOND, 50, GETDATE());
EXECUTE Store_Message 1, 0, 'seld-deleting message', @date;

EXECUTE Get_Chat_Messages 0;

EXECUTE Get_All_Messages;

-- try searching for a message 
EXECUTE Search_Messages 0, 'hello';