using System;
using System.Collections.Generic;
using System.Linq;
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
            _usersRepository = new UsersRepository(ConnectionString);
            _chatsRepository = new ChatsRepository(ConnectionString, _usersRepository);
            ((UsersRepository)_usersRepository).ChatsRepository = _chatsRepository;
        }

        [TestMethod]
        public void Should_CreateGroupChat_When_ExistingUsers()
        {
            // arrange
            var users = new List<User> 
            {
                new User { Username = "testuser13", Hash = "asd" },
                new User { Username = "testuser14", Hash = "asd" },
                new User {Username = "testuser15", Hash = "asd"}
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
                new User { Username = "testuser16", Hash = "asd" },
                new User { Username = "testuser17", Hash = "asd" },
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
                new User { Username = "testuser18", Hash = "asd" },
                new User { Username = "testuser19", Hash = "asd" },
            };
            // act
            users = users.Select(x => _usersRepository.CreateUser(x.Username, x.Hash)).ToList();
            users.Insert(0, new User { Id = 0, Username = "testuser18", Hash = "asd" });
            var chat = _chatsRepository.CreateGroupChat(users.Select(x => x.Id), "newChat");

            users.ForEach(x => _tempUsers.Add(x.Id));

            // assert
            Assert.IsNull(chat);
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
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_NotDeleteChat_When_InvalidId()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_SetAndGetChatInfo()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetInvalidChatInfo()
        {
            
        }

        [TestMethod]
        public void Should_DeleteChatInfo()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_NoInfoOnDeleteChatInfo()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_ReturnNull_When_SetChatInfoForInvalidChatAndGet()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_SetChatInfo_When_InfoIsDeleted()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_ReturnNull_When_InfoIsNull()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_SetInfo_When_InfoMembersAreNull()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_SetInfo_When_InfoMembersAreEmpty()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_SetCreator()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_CreatorIsInvalid()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_CreatorIsDefault()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_ChatIsInavlid()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_KickUser()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KickUserNotInChat()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KickDefaultUser()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KickFromInvalidChat()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_KickUsers()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KickContainsUserNotInChat()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KickContainsDefaultUser()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Should_DoNothing_When_KicksFromInvalidChat()
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        public void Should_SetRole_When_SetRoleForChat()
        {
            // arrange
            var listenerUser = new User { Username = "testuser11", Hash = "asd" };
            var regularUser = new User { Username = "testuser12", Hash = "asd" };

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
