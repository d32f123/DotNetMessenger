using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Models;
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

        private ChatsRepository _chatsRepository;
        private IUsersRepository _usersRepository;

        [TestInitialize]
        public void InitRepos()
        {
            RepositoryBuilder.ConnectionString = ConnectionString;
            _usersRepository = RepositoryBuilder.UsersRepository;
            _chatsRepository = (ChatsRepository)RepositoryBuilder.ChatsRepository;
        }

        [TestMethod]
        public void Should_CreateGroupChat_When_ExistingUsers()
        {
            // arrange
            var usersCred = new List<UserCredentials> 
            {
                new UserCredentials { Username = "testuser13", Password = "asd" },
                new UserCredentials { Username = "testuser14", Password = "asd" },
                new UserCredentials {Username = "testuser15", Password = "asd"}
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);
            
            // assert
            Assert.AreEqual(chat.Info.Title, "newChat");
            Assert.AreEqual(chat.CreatorId, users[0].Id);

            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_InvalidUsers()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser16", Password = "asd" },
                new UserCredentials { Username = "testuser17", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            users.ForEach(x => _tempUsers.Add(x.Id));
            users.Insert(0, new User {Id = User.InvalidId, Username = "testuser18"});
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat"));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_DefaultUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser18", Password = "asd" },
                new UserCredentials { Username = "testuser19", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            users.ForEach((x) => _tempUsers.Add(x.Id));
            users.Insert(0, new User { Id = 0, Username = "testuser18"});
            Assert.ThrowsException<ArgumentException>(() =>_chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat"));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_GetUsersForInvalidChat()
        {
            // act
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.GetChatUsers(Chat.InvalidId).First());
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_GetChatsForInvalidUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.GetUserChats(User.InvalidId).First());
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_GetChatsForDefaultUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.GetUserChats(0).First());
        }

        [TestMethod]
        public void Should_CreateDialogChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser20", Password = "asd" },
                new UserCredentials { Username = "testuser21", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // assert
            var i = 0;
            foreach (var user in chat.Users)
            {
                Assert.IsTrue(user.Id == users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_AddUserToChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
                new UserCredentials {Username = "testuser24", Password = "asd"}
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
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
                Assert.IsTrue(user.Id == users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_AddInvalidUserToChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.AddUser(chat.Id, User.InvalidId));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_AddDefaultUserToChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.AddUser(chat.Id, 0));
        }

        [TestMethod]
        public void Should_ThrowChatTypeMismatchException_When_AddUserToDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
                new UserCredentials {Username = "testuser24", Password = "asd"}
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ChatTypeMismatchException>(() => _chatsRepository.AddUser(chat.Id, users[2].Id));
        }

        [TestMethod]
        public void Should_AddUsersToChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
                new UserCredentials {Username = "testuser24", Password = "asd"}
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
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
                Assert.IsTrue(user.Id == users[i++].Id);
            }
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_AddUsersContainsInvalidUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.AddUsers(chat.Id, new[] {User.InvalidId}));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_AddUsersContainsDefaultUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.AddUsers(chat.Id, new[] {0}));
        }

        [TestMethod]
        public void Should_ThrowChatTypeMismatchException_When_AddUsersToCDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser22", Password = "asd" },
                new UserCredentials { Username = "testuser23", Password = "asd" },
                new UserCredentials {Username = "testuser24", Password = "asd"}
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempChats.Add(chat.Id);

            // add user
            Assert.ThrowsException<ChatTypeMismatchException>(() => _chatsRepository.AddUsers(chat.Id, users.GetRange(2, 1).Select(x => x.Id)));
        }

        [TestMethod]
        public void Should_DeleteChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser25", Password = "asd" },
                new UserCredentials { Username = "testuser26", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.DeleteChat(chat.Id);
            chat = _chatsRepository.GetChat(chat.Id);

            // assert
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_DeleteInvalidChat()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.DeleteChat(Chat.InvalidId));
        }

        [TestMethod]
        public void Should_SetAndGetChatInfo()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser27", Password = "asd" },
                new UserCredentials { Username = "testuser28", Password = "asd" },
            };
            var chatInfo = new ChatInfo {Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar")};
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);

            // assert
            Assert.AreEqual(retChatInfo.Title, chatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(chatInfo.Avatar));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_GetInvalidChatInfo()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.GetChatInfo(Chat.InvalidId));
        }

        [TestMethod]
        public void Should_DeleteChatInfo()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser29", Password = "asd" },
                new UserCredentials { Username = "testuser30", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.DeleteChatInfo(chat.Id);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.IsNull(retChatInfo);
        }

        [TestMethod]
        public void Should_ThrowArgumentNullException_When_NoInfoOnDeleteChatInfo()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser31", Password = "asd" },
                new UserCredentials { Username = "testuser32", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            _chatsRepository.DeleteChatInfo(chat.Id);
            // try to delete already deleted info (should not fail)
            Assert.ThrowsException<ArgumentNullException>(() => _chatsRepository.DeleteChatInfo(chat.Id));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetChatInfoForInvalidChat()
        {
            // arrange
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };

            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatInfo(Chat.InvalidId, chatInfo));
        }

        [TestMethod]
        public void Should_SetChatInfo_When_InfoIsDeleted()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser33", Password = "asd" },
                new UserCredentials { Username = "testuser34", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            var newChatInfo = new ChatInfo { Title = "newTitle", Avatar = Encoding.UTF8.GetBytes("ava") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
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
        public void Should_ThrowArgumentNullException_When_InfoIsNull()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser35", Password = "asd" },
                new UserCredentials { Username = "testuser36", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            // assert
            Assert.ThrowsException<ArgumentNullException>(() => _chatsRepository.SetChatInfo(chat.Id, null));
        }

        [TestMethod]
        public void Should_SetInfo_When_InfoMembersAreNull()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser37", Password = "asd" },
                new UserCredentials { Username = "testuser38", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = null, Avatar = null };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
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
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser39", Password = "asd" },
                new UserCredentials { Username = "testuser40", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "", Avatar = Encoding.UTF8.GetBytes("") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetChatInfo(chat.Id, chatInfo);
            var retChatInfo = _chatsRepository.GetChatInfo(chat.Id);
            // assert
            Assert.AreEqual(retChatInfo.Title, chatInfo.Title);
            Assert.IsTrue(retChatInfo.Avatar.SequenceEqual(chatInfo.Avatar));
        }

        [TestMethod]
        public void Should_ThrowChatTypeMismatchException_When_SetInfoForDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser41", Password = "asd" },
                new UserCredentials { Username = "testuser42", Password = "asd" },
            };
            var chatInfo = new ChatInfo { Title = "someTitle", Avatar = Encoding.UTF8.GetBytes("heyIamAnAvatar") };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<ChatTypeMismatchException>(() => _chatsRepository.SetChatInfo(chat.Id, chatInfo));
        }

        [TestMethod]
        public void Should_SetCreator()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser43", Password = "asd" },
                new UserCredentials { Username = "testuser44", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.SetCreator(chat.Id, users[1].Id);
            chat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chat.CreatorId, users[1].Id);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_CreatorIsInvalid()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser45", Password = "asd" },
                new UserCredentials { Username = "testuser46", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetCreator(chat.Id, User.InvalidId));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_NewCreatorIsNotInChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser47", Password = "asd" },
                new UserCredentials { Username = "testuser48", Password = "asd" },
            };

            var otherUserCred = new UserCredentials() {Username = "John", Password = "asdh"};
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var otherUser = _usersRepository.CreateUser(otherUserCred.Username, otherUserCred.Password);

            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetCreator(chat.Id, otherUser.Id));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_CreatorIsDefault()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser49", Password = "asd" },
                new UserCredentials { Username = "testuser50", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();

            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "hey");

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetCreator(chat.Id, 0));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetCreatorForInvalidChat()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser51", Password = "asd" };

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);

            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetCreator(Chat.InvalidId, user.Id));

        }

        [TestMethod]
        public void Should_ThrowChatTypeMistmatchException_When_SetCreatorForDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser52", Password = "asd" },
                new UserCredentials { Username = "testuser53", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.ThrowsException<ChatTypeMismatchException>(() =>_chatsRepository.SetCreator(chat.Id, users[1].Id));
            
        }

        [TestMethod]
        public void Should_KickUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser54", Password = "asd" },
                new UserCredentials { Username = "testuser55", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");
            var chatCount = chat.Users.Count();

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUser(chat.Id, users[1].Id);
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chatCount - 1, newChat.Users.Count());
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KickUserNotInChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser56", Password = "asd" },
                new UserCredentials { Username = "testuser57", Password = "asd" },
            };
            var otherUserCred = new UserCredentials {Username = "testuser58", Password = "asd"};
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var otherUser = _usersRepository.CreateUser(otherUserCred.Username, otherUserCred.Password);
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);

            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUser(chat.Id, otherUser.Id));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KickDefaultUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser59", Password = "asd" },
                new UserCredentials { Username = "testuser60", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUser(chat.Id, 0));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KickFromInvalidChat()
        {
            // arrange 
            var userCred = new UserCredentials {Username = "testuser61", Password = "asd"};

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);

            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUser(Chat.InvalidId, user.Id));
        }

        [TestMethod]
        public void Should_ThrowChatTypeMismatchException_When_KickFromDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser62", Password = "asd" },
                new UserCredentials { Username = "testuser63", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.ThrowsException<ChatTypeMismatchException>(() => _chatsRepository.KickUser(chat.Id, users[1].Id));
        }

        [TestMethod]
        public void Should_ThrowUserIsCreatorException_When_KickCreator()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser64", Password = "asd" },
                new UserCredentials { Username = "testuser65", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));

            Assert.ThrowsException<UserIsCreatorException>(() => _chatsRepository.KickUser(chat.Id, chat.CreatorId));
        }

        [TestMethod]
        public void Should_KickUsers()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser66", Password = "asd" },
                new UserCredentials { Username = "testuser67", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");
            var chatCount = chat.Users.Count();

            users.ForEach(x => _tempUsers.Add(x.Id));

            _chatsRepository.KickUsers(chat.Id, new[] {users[1].Id});
            var newChat = _chatsRepository.GetChat(chat.Id);
            // assert
            Assert.AreEqual(chatCount - 1, newChat.Users.Count());
        }

        [TestMethod]
        public void Should_ThrowUserIsCreatorException_When_KickContainsCreator()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser64", Password = "asd" },
                new UserCredentials { Username = "testuser65", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<UserIsCreatorException>(() => _chatsRepository.KickUsers(chat.Id, new[] {chat.CreatorId}));
        }

        [TestMethod]
        public void Should_ThrowChatTypeMismatchException_When_KickUsersFromDialog()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser68", Password = "asd" },
                new UserCredentials { Username = "testuser69", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateDialog(users[0].Id, users[1].Id);

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<ChatTypeMismatchException>(() => _chatsRepository.KickUsers(chat.Id, users.GetRange(1, 1).Select(x => x.Id)));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KickContainsUserNotInChat()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser70", Password = "asd" },
                new UserCredentials { Username = "testuser71", Password = "asd" },
            };
            var otherUserCred = new UserCredentials { Username = "testuser72", Password = "asd" };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var otherUser = _usersRepository.CreateUser(otherUserCred.Username, otherUserCred.Password);
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            _tempUsers.Add(otherUser.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUsers(chat.Id, new [] {otherUser.Id}));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KickContainsDefaultUser()
        {
            // arrange
            var usersCred = new List<UserCredentials>
            {
                new UserCredentials { Username = "testuser73", Password = "asd" },
                new UserCredentials { Username = "testuser74", Password = "asd" },
            };
            // act
            var users = usersCred.Select(x => _usersRepository.CreateUser(x.Username, x.Password)).ToList();
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "asd");

            users.ForEach(x => _tempUsers.Add(x.Id));
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUsers(chat.Id, new[] {0}));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_KicksFromInvalidChat()
        {
            // arrange 
            var userCred = new UserCredentials { Username = "testuser75", Password = "asd" };

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.KickUsers(Chat.InvalidId, new[] {user.Id}));

        }

        [TestMethod]
        public void Should_ThrowArgumentNullException_When_KicksIsNull()
        {
            // arrange 
            var userCred = new UserCredentials { Username = "testuser75", Password = "asd" };

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            var chat = _chatsRepository.CreateGroupChat(new[] {user.Id}, "hey");
            // assert
            Assert.ThrowsException<ArgumentNullException>(() => _chatsRepository.KickUsers(chat.Id, null));
        }


        [TestMethod]
        public void Should_SetRole_When_SetRoleForChat()
        {
            // arrange
            var listenerUserCred = new UserCredentials { Username = "testuser76", Password = "asd" };
            var regularUserCred = new UserCredentials { Username = "testuser77", Password = "asd" };

            // act

            var listenerUser = _usersRepository.CreateUser(listenerUserCred.Username, listenerUserCred.Password);
            var regularUser = _usersRepository.CreateUser(regularUserCred.Username, regularUserCred.Password);
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
        public void Should_ThrowsArgumentException_When_SetRoleForUserNotInChat()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser78", Password = "asd" };
            var notChatUserCred = new UserCredentials { Username = "testuser79", Password = "asd" };

            // act

            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var notChatUser = _usersRepository.CreateUser(notChatUserCred.Username, notChatUserCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempUsers.Add(notChatUser.Id);
            _tempChats.Add(chat.Id);

            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatSpecificRole(notChatUser.Id, chat.Id, UserRoles.Moderator));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetChatSpecificRoleForDefaultUser()
        {
            // arrange
            var listenerUserCred = new UserCredentials { Username = "testuser76", Password = "asd" };
            var regularUserCred = new UserCredentials { Username = "testuser77", Password = "asd" };

            // act

            var listenerUser = _usersRepository.CreateUser(listenerUserCred.Username, listenerUserCred.Password);
            var regularUser = _usersRepository.CreateUser(regularUserCred.Username, regularUserCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { regularUser.Id, listenerUser.Id }, "newChat");

            _tempUsers.Add(listenerUser.Id);
            _tempUsers.Add(regularUser.Id);
            _tempChats.Add(chat.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatSpecificRole(0, chat.Id, UserRoles.Regular));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetRoleForInvalidChat()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser80", Password = "asd" };

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatSpecificRole(user.Id, Chat.InvalidId, UserRoles.Moderator));
        }

        [TestMethod]
        public void Should_ReturnNewRole_When_UpdateCurrentRole()
        {
            // arrange
            var regularUserCred = new UserCredentials { Username = "testuser81", Password = "asd" };

            // act
            var regularUser = _usersRepository.CreateUser(regularUserCred.Username, regularUserCred.Password);
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
        public void Should_ThrowArgumentException_When_DeleteChatSpecificInfoForDefaultUser()
        {
            // arrange
            var userCred = new UserCredentials {Username = "asdajklsd", Password = "asd"};

            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new [] {user.Id}, "newChat");
            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.DeleteChatSpecificInfo(0, chat.Id));

        }

        [TestMethod]
        public void Should_SetNewUserChatInfo()
        {
            // arrange
            var userCred = new UserCredentials {Username = "testuser82", Password = "asd"};
            var userInfo = new ChatUserInfo {Nickname = "alfred", Role = new UserRole {RoleType = UserRoles.Trusted}};
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
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
            var userCred = new UserCredentials { Username = "testuser83", Password = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
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
        public void Should_ThrowArgumentException_When_SetInfoForUserNotInChat()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser84", Password = "asd" };
            var otherCred = new UserCredentials {Username = "testuser85", Password = "ok"};
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var other = _usersRepository.CreateUser(otherCred.Username, otherCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempUsers.Add(other.Id);
            _tempChats.Add(chat.Id);

            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatSpecificInfo(other.Id, chat.Id, userInfo, true));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetInfoForDefaultUserInChat()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser84", Password = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            Assert.ThrowsException<ArgumentException>(() => _chatsRepository.SetChatSpecificInfo(0, chat.Id, userInfo));
        }

        [TestMethod]
        public void Should_ReturnNewInfo_When_SetChatUserInfoAfterDelete()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser82", Password = "asd" };
            var userInfo = new ChatUserInfo { Nickname = "alfred", Role = new UserRole { RoleType = UserRoles.Trusted } };
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
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
        public void Should_ThrowArgumentNullException_When_SetChatUserInfoIsNull()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser82", Password = "asd" };
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(user.Id, chat.Id);
            // assert
            Assert.ThrowsException<ArgumentNullException>(() => _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, null));
        }

        [TestMethod]
        public void Should_ThrowArgumentNullException_When_SetChatUserInfoRoleIsNullAndSetRole()
        {
            // arrange
            var userCred = new UserCredentials { Username = "testuser82", Password = "asd" };
            var chatUserInfo = new ChatUserInfo {Nickname = "asd", Role = null};
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat");

            _tempUsers.Add(user.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.DeleteChatSpecificInfo(user.Id, chat.Id);
            // assert
            Assert.ThrowsException<ArgumentNullException>(() => _chatsRepository.SetChatSpecificInfo(user.Id, chat.Id, chatUserInfo, true));
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

