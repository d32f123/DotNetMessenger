using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetMessenger.DataLayer.SqlServer.ModelProxies;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class ChatsRepositoryTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";
        private readonly List<int> _tempUsers = new List<int>();
        private readonly List<int> _tempChats = new List<int>();

        private IChatsRepository _chatsRepository;
        private IUsersRepository _usersRepository;

        [TestInitialize]
        public void InitRepos()
        {
            RepositoryBuilder.ConnectionString = ConnectionString;
            _usersRepository = RepositoryBuilder.UsersRepository;
            _chatsRepository = RepositoryBuilder.ChatsRepository;
        }

        [TestMethod]
        public void Should_CreateGroupChat_When_ExistingUsers()
        {
            // arrange
            var users = new List<User> 
            {
                new UserSqlProxy { Username = "testuser13", Hash = "asd" },
                new UserSqlProxy { Username = "testuser14", Hash = "asd" },
                new UserSqlProxy {Username = "testuser15", Hash = "asd"}
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);
            
            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);

            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotCreateChat_When_InvalidUsers()
        {
            // arrange
            var users = new List<User>
            {
                new UserSqlProxy { Username = "testuser16", Hash = "asd" },
                new UserSqlProxy { Username = "testuser17", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            users.Insert(0, new User {Id = User.InvalidId, Username = "testuser18", Hash = "asd"});
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_NotCreateChat_When_DefaultUser()
        {
            // arrange
            var users = new List<User>
            {
                new UserSqlProxy { Username = "testuser18", Hash = "asd" },
                new UserSqlProxy { Username = "testuser19", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            users.Insert(0, new UserSqlProxy { Id = 0, Username = "testuser18", Hash = "asd" });
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetUsersForInvalidChat()
        {
            // act
            var users = _chatsRepository.GetChatUsers(Chat.InvalidId);
            // assert
            Assert.IsTrue(!users.Any());
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetChatsForInvalidUser()
        {
            // act
            var chats = _chatsRepository.GetUserChats(User.InvalidId);
            // assert
            Assert.IsFalse(chats.Any());
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetChatsForDefaultUser()
        {
            // act
            var chats = _chatsRepository.GetUserChats(0);
            // assert
            Assert.IsFalse(chats.Any());
        }

        [TestMethod]
        public void Should_CreateDialogChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser20", Hash = "asd" },
                new User { Username = "testuser21", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // assert
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_AddUserToChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
                new User {Username = "testuser24", Hash = "asd"}
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.GetRange(0, 2).Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);
            
                // add user
            _chatsRepository.AddUser(chat.Id, users[2].Id);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);

            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUserToChat_When_InvalidUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUser(chat.Id, User.InvalidId);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUserToChat_When_DefaultUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUser(chat.Id, 0);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUserToChat_When_Dialogue()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
                new User {Username = "testuser24", Hash = "asd"}
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUser(chat.Id, users[2].Id);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_AddUsersToChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
                new User {Username = "testuser24", Hash = "asd"}
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.GetRange(0, 2).Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUsers(chat.Id, users.GetRange(2, 1).Select(x => x.Id));
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);

            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUsersToChat_When_ContainsInvalidUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUsers(chat.Id, new[] {User.InvalidId});
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUsersToChat_When_ContainsDefaultUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUsers(chat.Id, new[] {0});
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_NotAddUsersToChat_When_ChatIsDialogue()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser22", Hash = "asd" },
                new User { Username = "testuser23", Hash = "asd" },
                new User {Username = "testuser24", Hash = "asd"}
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            _chatsRepository.AddUsers(chat.Id, users.GetRange(2, 1).Select(x => x.Id));
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.AreEqual(chat.Users.Count(), 2);
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i].Id);
                Assert.IsTrue(user.Hash == users[i++].Hash);
            }
        }

        [TestMethod]
        public void Should_DeleteChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser25", Hash = "asd" },
                new User { Username = "testuser26", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.DeleteChat(chat.Id);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_NotDeleteChat_When_InvalidId()
        {
            // act
            _chatsRepository.DeleteChat(Chat.InvalidId);
            var chat = _chatsRepository.GetChat(Chat.InvalidId);

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_SetAndGetChatInfo()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser27", Hash = "asd" },
                new User { Username = "testuser28", Hash = "asd" },
            };
            var chatInfo = new ChatInfo {Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar")};
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);

            // assert
            Assert.AreEqual(retChatInfo.Title, chatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(chatInfo.Avatar));
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetInvalidChatInfo()
        {
            // act
            var retChatInfo = _chatsRepository.GetChatInfo(Chat.InvalidId);
            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_DeleteChatInfo()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser29", Hash = "asd" },
                new User { Username = "testuser30", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.DeleteChatInfo(chat.Id);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_DoNothing_When_NoInfoOnDeleteChatInfo()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser31", Hash = "asd" },
                new User { Username = "testuser32", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.DeleteChatInfo(chat.Id);
            // try to delete already deleted info (should not fail)
            _chatsRepository.DeleteChatInfo(chat.Id);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetChatInfoForInvalidChatAndGet()
        {
            // arrange
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };

            // act
            _chatsRepository.SetChatInfo(Chat.InvalidId, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(Chat.InvalidId);
            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_SetChatInfo_When_InfoIsDeleted()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser33", Hash = "asd" },
                new User { Username = "testuser34", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            var newChatInfo = new ChatInfo { Title = "newTitle", Avatar = Encoding.UTF8.GetBytes("ava") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.DeleteChatInfo(chat.Id);
            _chatsRepository.SetChatInfo(chat.Id, newChatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.AreEqual(retChatInfo.Title, newChatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(newChatInfo.Avatar));
        }

        [TestMethod]
        public void Should_DoNothing_When_InfoIsNull()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser35", Hash = "asd" },
                new User { Username = "testuser36", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.SetChatInfo(chat.Id, null);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.AreEqual(retChatInfo.Title, chatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(chatInfo.Avatar));
        }

        [TestMethod]
        public void Should_SetInfo_When_InfoMembersAreNull()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser37", Hash = "asd" },
                new User { Username = "testuser38", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = null, Avatar = null };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.IsNull(retChatInfo.Avatar);
            Assert.IsNull(retChatInfo.Title);
        }

        [TestMethod]
        public void Should_SetInfo_When_InfoMembersAreEmpty()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser39", Hash = "asd" },
                new User { Username = "testuser40", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "", Avatar = Encoding.UTF8.GetBytes("") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.AreEqual(retChatInfo.Title, chatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(chatInfo.Avatar));
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetInfoForDialog()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser41", Hash = "asd" },
                new User { Username = "testuser42", Hash = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);

            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_SetCreator()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser43", Hash = "asd" },
                new User { Username = "testuser44", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetCreator(chat.Id, users[1].Id);
            chat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.CreatorId, users[1].Id);
        }

        [TestMethod]
        public void Should_DoNothing_When_CreatorIsInvalid()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser45", Hash = "asd" },
                new User { Username = "testuser46", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetCreator(chat.Id, User.InvalidId);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.CreatorId, newChat.CreatorId);
        }

        [TestMethod]
        public void Should_DoNothing_When_NewCreatorIsNotInChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser47", Hash = "asd" },
                new User { Username = "testuser48", Hash = "asd" },
            };

            var otherUser = new User() {Username = "John", Hash = "asdh"};
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            otherUser = _usersRepository.CreateUser(otherUser.Username, otherUser.Hash);

            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);
            
            _chatsRepository.SetCreator(chat.Id, otherUser.Id);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.CreatorId, newChat.CreatorId);
        }

        [TestMethod]
        public void Should_DoNothing_When_CreatorIsDefault()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser49", Hash = "asd" },
                new User { Username = "testuser50", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();

            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetCreator(chat.Id, 0);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.CreatorId, newChat.CreatorId);
        }

        [TestMethod]
        public void Should_DoNothing_When_SetCreatorForInvalidChat()
        {
            // arrange
            var user = new User { Username = "testuser51", Hash = "asd" };

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);

            _chatsRepository.SetCreator(Chat.InvalidId, user.Id);
            var newChat = _chatsRepository.GetChat(Chat.InvalidId);
            // assert
            Assert.IsNull(newChat);
        }

        [TestMethod]
        public void Should_DoNothing_When_SetCreatorForDialog()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser52", Hash = "asd" },
                new User { Username = "testuser53", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetCreator(chat.Id, users[1].Id);
            var newCreator = _chatsRepository.GetChat(chat.Id).CreatorId;
            // assert
            Assert.AreEqual(chat.CreatorId, newCreator);
        }

        [TestMethod]
        public void Should_KickUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser54", Hash = "asd" },
                new User { Username = "testuser55", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");
            var chatCount = chat.Users.Count();

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUser(chat.Id, users[1].Id);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chatCount - 1, newChat.Users.Count());
        }

        [TestMethod]
        public void Should_DoNothing_When_KickUserNotInChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser56", Hash = "asd" },
                new User { Username = "testuser57", Hash = "asd" },
            };
            var otherUser = new User {Username = "testuser58", Hash = "asd"};
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            otherUser = _usersRepository.CreateUser(otherUser.Username, otherUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);

            _chatsRepository.KickUser(chat.Id, otherUser.Id);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickDefaultUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser59", Hash = "asd" },
                new User { Username = "testuser60", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUser(chat.Id, 0);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickFromInvalidChat()
        {
            // arrange 
            var user = new User {Username = "testuser61", Hash = "asd"};

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);

            _chatsRepository.KickUser(Chat.InvalidId, user.Id);
            var chat = _chatsRepository.GetChat(Chat.InvalidId);
            
            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_DoNothing_When_KickFromDialog()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser62", Hash = "asd" },
                new User { Username = "testuser63", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUser(chat.Id, users[1].Id);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickCreator()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser64", Hash = "asd" },
                new User { Username = "testuser65", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUser(chat.Id, chat.CreatorId);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_KickUsers()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser66", Hash = "asd" },
                new User { Username = "testuser67", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");
            var chatCount = chat.Users.Count();

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUsers(chat.Id, new[] {users[1].Id});
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chatCount - 1, newChat.Users.Count());
        }

        [TestMethod]
        public void Should_DoNothing_When_KickContainsCreator()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser64", Hash = "asd" },
                new User { Username = "testuser65", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUsers(chat.Id, new[] {chat.CreatorId});
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickUsersFromDialog()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser68", Hash = "asd" },
                new User { Username = "testuser69", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUsers(chat.Id, users.Select(x => x.Id));
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickContainsUserNotInChat()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser70", Hash = "asd" },
                new User { Username = "testuser71", Hash = "asd" },
            };
            var otherUser = new User { Username = "testuser72", Hash = "asd" };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            otherUser = _usersRepository.CreateUser(otherUser.Username, otherUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);

            _chatsRepository.KickUsers(chat.Id, new [] {otherUser.Id});
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KickContainsDefaultUser()
        {
            // arrange
            var users = new List<User>
            {
                new User { Username = "testuser73", Hash = "asd" },
                new User { Username = "testuser74", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUsers(chat.Id, new[] {0});
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
            var i = 0;
            foreach (var user in newChat.Users)
            {
                Assert.AreEqual(user.Id, users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_DoNothing_When_KicksFromInvalidChat()
        {
            // arrange 
            var user = new User { Username = "testuser75", Hash = "asd" };

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);

            _chatsRepository.KickUsers(Chat.InvalidId, new[] {user.Id});
            var chat = _chatsRepository.GetChat(Chat.InvalidId);

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_DoNothing_When_KicksIsNull()
        {
            // arrange 
            var user = new User { Username = "testuser75", Hash = "asd" };

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);
            var chat = _chatsRepository.CreateGroupChat(new[] {user.Id}, "hey");

            _chatsRepository.KickUsers(chat.Id, null);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.Users.Count(), newChat.Users.Count());
        }


        [TestMethod]
        public void Should_SetRole_When_SetRoleForChat()
        {
            // arrange
            var listenerUser = new User { Username = "testuser76", Hash = "asd" };
            var regularUser = new User { Username = "testuser77", Hash = "asd" };

            // act

            listenerUser = _usersRepository.CreateUser(listenerUser.Username, listenerUser.Hash);
            regularUser = _usersRepository.CreateUser(regularUser.Username, regularUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { regularUser.Id, listenerUser.Id }, "newChat");
            
            _tempUsers.Add(listenerUser.Id);
            _tempUsers.Add(regularUser.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificRole(listenerUser.Id, chat.Id, UserRoles.Listener);
            var listenerRole = _chatsRepository.GetChatSpecificInfo(listenerUser.Id, chat.Id).Role;
            var regularRole = _chatsRepository.GetChatSpecificInfo(regularUser.Id, chat.Id).Role;

            // assert
            Assert.AreEqual(listenerRole.RoleType, UserRoles.Listener);
            Assert.AreEqual(regularRole.RoleType, UserRoles.Regular);
            Assert.IsFalse(listenerRole.WritePerm);
            Assert.IsTrue(regularRole.WritePerm);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetRoleForUserNotInChat()
        {
            // arrange
            var user = new User { Username = "testuser78", Hash = "asd" };
            var notChatUser = new User { Username = "testuser79", Hash = "asd" };

            // act

            user = _usersRepository.CreateUser(user.Username, user.Hash);
            notChatUser = _usersRepository.CreateUser(notChatUser.Username, notChatUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempUsers.Add(notChatUser.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificRole(notChatUser.Id, chat.Id, UserRoles.Moderator);
            var role = _chatsRepository.GetChatSpecificInfo(notChatUser.Id, chat.Id)?.Role;

            // assert
            Assert.IsNull(role);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetChatSpecificRoleForDefaultUser()
        {
            // arrange
            var listenerUser = new User { Username = "testuser76", Hash = "asd" };
            var regularUser = new User { Username = "testuser77", Hash = "asd" };

            // act

            listenerUser = _usersRepository.CreateUser(listenerUser.Username, listenerUser.Hash);
            regularUser = _usersRepository.CreateUser(regularUser.Username, regularUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { regularUser.Id, listenerUser.Id }, "newChat");

            _tempUsers.Add(listenerUser.Id);
            _tempUsers.Add(regularUser.Id);
            _tempChats.Add(chat.Id);

            var nullVal = _chatsRepository.SetChatSpecificRole(0, chat.Id, UserRoles.Regular);
            // assert
            Assert.IsNull(nullVal);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetRoleForInvalidChat()
        {
            // arrange
            var user = new User { Username = "testuser80", Hash = "asd" };

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);
            _chatsRepository.SetChatSpecificRole(user.Id, Chat.InvalidId, UserRoles.Moderator);
            var role = _chatsRepository.GetChatSpecificInfo(user.Id, Chat.InvalidId)?.Role;

            // assert
            Assert.IsNull(role);
        }

        [TestMethod]
        public void Should_ReturnNewRole_When_UpdateCurrentRole()
        {
            // arrange
            var regularUser = new User { Username = "testuser81", Hash = "asd" };

            // act
            regularUser = _usersRepository.CreateUser(regularUser.Username, regularUser.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { regularUser.Id }, "newChat");
            _tempUsers.Add(regularUser.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificRole(regularUser.Id, chat.Id, UserRoles.Regular);
            _chatsRepository.DeleteChatSpecificInfo(regularUser.Id, chat.Id);
            _chatsRepository.SetChatSpecificRole(regularUser.Id, chat.Id, UserRoles.Moderator);
            var moderatorRole = _chatsRepository.GetChatSpecificInfo(regularUser.Id, chat.Id).Role;

            // assert
            Assert.AreEqual(moderatorRole.RoleType, UserRoles.Moderator);
            Assert.IsTrue(moderatorRole.ManageUsersPerm);
        }

        [TestMethod]
        public void Should_DoNothing_When_DeleteChatSpecificInfoForDefaultUser()
        {
            // arrange
            var user = new User {Username = "asdajklsd", Hash = "asd"};

            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new [] {user.Id}, "newChat");
            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(0, chat.Id);
            var chatSpecificInfo = _chatsRepository.GetChatSpecificInfo(0, chat.Id);

            // assert
            Assert.IsNull(chatSpecificInfo);

        }

        [TestMethod]
        public void Should_SetNewUserChatInfo()
        {
            // arrange
            var user = new User {Username = "testuser82", Hash = "asd"};
            var userInfo = new ChatUserInfo {Nickname = "alfred", Role = new UserRole {RoleType = UserRoles.Trusted}};
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, userInfo, true);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);

            // assert
            Assert.AreEqual(userInfo.Nickname, returnedInfo.Nickname);
            Assert.AreEqual(userInfo.Role.RoleType, returnedInfo.Role.RoleType);
        }

        [TestMethod]
        public void Should_NotChangeRole_When_SetNewChatInfoWithoutRole()
        {
            // arrange
            var user = new User { Username = "testuser83", Hash = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            var prevInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);
            _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, userInfo);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);

            // assert
            Assert.AreEqual(userInfo.Nickname, returnedInfo.Nickname);
            Assert.AreEqual(prevInfo.Role.RoleType, returnedInfo.Role.RoleType);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetInfoForUserNotInChat()
        {
            // arrange
            var user = new User { Username = "testuser84", Hash = "asd" };
            var other = new User {Username = "testuser85", Hash = "ok"};
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            other = _usersRepository.CreateUser(other.Username, other.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempUsers.Add(other.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificInfo(other.Id, chat.Id, userInfo, true);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(other.Id, chat.Id);

            // assert
            Assert.IsNull(returnedInfo);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetInfoForDefaultUserInChat()
        {
            // arrange
            var user = new User { Username = "testuser84", Hash = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificInfo(0, chat.Id, userInfo);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(0, chat.Id);

            // assert
            Assert.IsNull(returnedInfo);
        }

        [TestMethod]
        public void Should_ReturnNewInfo_When_SetChatUserInfoAfterDelete()
        {
            // arrange
            var user = new User { Username = "testuser82", Hash = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(user.Id, chat.Id);
            _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, userInfo, true);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);

            // assert
            Assert.AreEqual(userInfo.Nickname, returnedInfo.Nickname);
            Assert.AreEqual(userInfo.Role.RoleType, returnedInfo.Role.RoleType);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetChatUserInfoIsNull()
        {
            // arrange
            var user = new User { Username = "testuser82", Hash = "asd" };
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(user.Id, chat.Id);
            _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, null);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);

            // assert
            Assert.IsNull(returnedInfo);
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetChatUserInfoRoleIsNullAndSetRole()
        {
            // arrange
            var user = new User { Username = "testuser82", Hash = "asd" };
            var chatUserInfo = new ChatUserInfo {Nickname = "asd", Role = null};
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(user.Id, chat.Id);
            _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, chatUserInfo, true);
            var returnedInfo = _chatsRepository.GetChatSpecificInfo(user.Id, chat.Id);

            // assert
            Assert.IsNull(returnedInfo);
        }

        [TestCleanup]
        public void Clean()
        {
            foreach (var id in _tempUsers)
                _usersRepository.DeleteUser(id);
            foreach (var id in _tempChats)
                _chatsRepository.DeleteChat(id);
        }
    }
}
