 DROP TABLE IF EXISTS ChatUsers;
 DROP TABLE IF EXISTS Attachments;
 DROP TABLE IF EXISTS Messages;
 DROP TABLE IF EXISTS Chats;
 DROP TABLE IF EXISTS UserInfos;
 DROP TABLE IF EXISTS Users;
 IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name='CG_Messages') 
	DROP FULLTEXT CATALOG CG_Messages;

 CREATE TABLE Users(
	ID			INT IDENTITY(0,1),
	Username	VARCHAR(50) UNIQUE NOT NULL,
	Password	VARCHAR(50) NOT NULL,
	CONSTRAINT PK_Users PRIMARY KEY(ID),	
	);

INSERT INTO Users VALUES ('deleted', 'x'); -- a deleted user

 CREATE TABLE UserInfos(
	UserID		INT,
	LastName	VARCHAR(30), -- index candidate
	FirstName	VARCHAR(30), -- index candidate
	Phone		VARCHAR(15),
	Email		VARCHAR(40), -- index candidate
	DateOfBirth DATE,
	Avatar		VARBINARY(MAX),
	CONSTRAINT	PK_UserInfos PRIMARY KEY(UserID),
	CONSTRAINT	FK_UserInfosToUsers FOREIGN KEY (UserID) REFERENCES Users(ID) ON DELETE CASCADE
	);
 CREATE INDEX IX_UserInfos_LastNameFirstNameEmail ON UserInfos(LastName, FirstName, Email);

 CREATE TABLE Chats(
	ID			INT IDENTITY,
	ChatType	INT NOT NULL,
	CreatorID	INT NOT NULL -- index candidate
		CONSTRAINT	DF_CreatorID DEFAULT 0,
	CONSTRAINT	PK_Chats PRIMARY KEY(ID),
	CONSTRAINT	FK_ChatsUsers FOREIGN KEY (CreatorID) REFERENCES Users(ID) ON DELETE SET DEFAULT,
 );
 CREATE INDEX IX_Chats_CreatorID ON Chats(CreatorID);

 CREATE TABLE ChatUsers(
	UserID		INT
		CONSTRAINT DF_UserID DEFAULT 0,
	ChatID		INT,
	CONSTRAINT	PK_ChatUsers PRIMARY KEY (UserID, ChatID),
	CONSTRAINT	FK_ChatUsersUsers FOREIGN KEY (UserID) REFERENCES Users(ID) ON DELETE SET DEFAULT,
	CONSTRAINT	FK_ChatUsersChats FOREIGN KEY (ChatID) REFERENCES Chats(ID) ON DELETE CASCADE
 );

 CREATE TABLE Messages(
	ID			INT IDENTITY,
	ChatID		INT NOT NULL, -- index candidate
	SenderID	INT NOT NULL  -- index candidate
		CONSTRAINT DF_SenderID DEFAULT 0,
	MessageText VARCHAR(MAX),
	MessageDate DATE NOT NULL,-- index candidate
	CONSTRAINT	PK_Messages PRIMARY KEY	(ID),
	CONSTRAINT	FK_MessagesChat FOREIGN KEY (ChatID) REFERENCES Chats(ID) ON DELETE CASCADE,
	CONSTRAINT	FK_MessagesUser FOREIGN KEY (SenderID) REFERENCES Users(ID) ON DELETE SET DEFAULT,
 );
 CREATE INDEX IX_Messages_ChatID ON Messages(ChatID);
 CREATE INDEX IX_Messages_SenderID ON Messages(SenderID);
 CREATE INDEX IX_Messages_MessageDate ON Messages(MessageDate);

 CREATE FULLTEXT CATALOG CG_Messages
	WITH ACCENT_SENSITIVITY = OFF;

 CREATE FULLTEXT INDEX ON Messages(MessageText)
	KEY INDEX PK_Messages ON CG_Messages;

 CREATE TABLE Attachments(
	ID			INT IDENTITY,
	Type		CHAR(1) NOT NULL		-- 0 - image, 1 -- regular file
		CONSTRAINT DF_Type DEFAULT 0,
	AttachFile	VARBINARY(MAX) NOT NULL,
	MessageID	INT NOT NULL,  -- index candidate
	CONSTRAINT	PK_Attachments PRIMARY KEY (ID),
	CONSTRAINT	FK_AttachmentsMessages FOREIGN KEY (MessageID) REFERENCES Messages(ID) ON DELETE CASCADE
 );
 CREATE INDEX IX_Attachments_MessageID ON Attachments(MessageID);