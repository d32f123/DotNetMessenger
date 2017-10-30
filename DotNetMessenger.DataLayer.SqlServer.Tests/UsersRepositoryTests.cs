using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Models;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class UsersRepositoryTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";
        private readonly List<int> _tempUsers = new List<int>();
        private readonly List<int> _tempChats = new List<int>();

        private IUsersRepository _usersRepository;
        private IChatsRepository _chatsRepository;

        [TestInitialize]
        public void InitRepos()
        {
            RepositoryBuilder.ConnectionString = ConnectionString;
            _usersRepository = RepositoryBuilder.UsersRepository;
            _chatsRepository = RepositoryBuilder.ChatsRepository;
        }

        [TestMethod]
        public void Should_CreateUser_When_ValidUser()
        {
            //arrange
            var user = new UserCredentials
            {
                Username = "testuser1",
                Password = "x"
            };

            //act
            var result = _usersRepository.CreateUser(user.Username, user.Password);

            _tempUsers.Add(result.Id);

            //assert
            Assert.AreEqual(user.Username, result.Username);
        }

        [TestMethod]
        public void Should_ReturnCreatedUser_When_ValidUser()
        {
            //arrange
            var user = new UserCredentials
            {
                Username = "testuser1",
                Password = "x"
            };

            //act
            var result = _usersRepository.CreateUser(user.Username, user.Password);
            result = _usersRepository.GetUser(result.Id);
            _tempUsers.Add(result.Id);

            //assert
            Assert.AreEqual(user.Username, result.Username);
            Assert.AreEqual(user.Password, _usersRepository.GetPassword(result.Id));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_InvalidId()
        {
            // act
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.GetUser(User.InvalidId));
        }

        [TestMethod]
        public void Should_ThrowArgumentNullException_When_EmptyLoginOrPassword()
        {
            // arrange
            var emptyUser = new UserCredentials
            {
                Username = "",
                Password = ""
            };

            // act and assert
            Assert.ThrowsException<ArgumentNullException>(() => _usersRepository.CreateUser(emptyUser.Username, emptyUser.Password));
        }

        [TestMethod]
        public void Should_ThrowUserAlreadyExistsException_When_CreatingDuplicateUser()
        {
            // arrange
            var user = new UserCredentials
            {
                Username = "testuser2",
                Password = "x"
            };

            // act
            _tempUsers.Add(_usersRepository.CreateUser(user.Username, user.Password).Id);
            // assert
            Assert.ThrowsException<UserAlreadyExistsException>(() =>_usersRepository.CreateUser(user.Username, user.Password));
        }

        [TestMethod]
        public void Should_DeleteNewUser()
        {
            // arrange
            var user = new UserCredentials
            {
                Username = "testuser3",
                Password = "x"
            };

            // act
            var addedUser = _usersRepository.CreateUser(user.Username, user.Password);
            _usersRepository.DeleteUser(addedUser.Id);
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.GetUser(addedUser.Id));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_DeleteDefaultUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() =>_usersRepository.DeleteUser(0));
            
        }

        [TestMethod]
        public void Should_ThrowArgumentException_OnInvalidId()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.DeleteUser(User.InvalidId));
        }

        [TestMethod]
        public void Should_CreateAndReturnUserInfo()
        {
            // arrange
            var userInfo = new UserInfo
            {
                FirstName = "Vladmir",
                DateOfBirth = DateTime.Parse("06/05/1996"),
                Email = "xxx@yandex.ru",
                Avatar = Encoding.UTF8.GetBytes("testAvatar"),
                Gender = Genders.Female
            };
            var user = new UserCredentials{Username = "testuser4", Password = "x"};

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Password).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);
            
            // assert
            Assert.AreEqual(user.Username, returnedUser.Username);
            Assert.AreEqual(userInfo.LastName, returnedUser.UserInfo.LastName);
            Assert.AreEqual(userInfo.FirstName, returnedUser.UserInfo.FirstName);
            Assert.AreEqual(userInfo.Email, returnedUser.UserInfo.Email);
            Assert.IsTrue(userInfo.Avatar.SequenceEqual(returnedUser.UserInfo.Avatar));
            Assert.AreEqual(userInfo.DateOfBirth?.Date, returnedUser.UserInfo.DateOfBirth);
            Assert.AreEqual(userInfo.Phone, returnedUser.UserInfo.Phone);
            Assert.AreEqual(userInfo.Gender, returnedUser.UserInfo.Gender);
        }

        [TestMethod]
        public void Should_ReturnNullInfo_When_InfoIsNull()
        {
            // arrange
            var userInfo = new UserInfo
            {
                Avatar = null,
                DateOfBirth = DateTime.Now,
                Email = null,
                FirstName = null,
                LastName = null,
                Phone = null
            };
            var user = new UserCredentials{Username = "testuser5", Password = "x"};

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Password).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);

            // assert
            Assert.IsNull(returnedUser.UserInfo.LastName);
            Assert.IsNull(returnedUser.UserInfo.FirstName);
            Assert.IsNull(returnedUser.UserInfo.Email);
            Assert.AreEqual(userInfo.DateOfBirth?.Date, returnedUser.UserInfo.DateOfBirth);
            Assert.IsNull(returnedUser.UserInfo.Phone);
        }

        [TestMethod]
        public void Should_ReturnEmptyStrings_When_InfoIsEmptyString()
        {
            // arrange
            var userInfo = new UserInfo
            {
                Avatar = Encoding.UTF8.GetBytes(""),
                DateOfBirth = DateTime.Now,
                FirstName = "",
                LastName = "",
                Phone = ""
            };
            var user = new UserCredentials{ Username = "testuser5", Password = "x" };

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Password).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);

            // assert
            Assert.AreEqual(user.Username, returnedUser.Username);
            Assert.AreEqual(userInfo.LastName, returnedUser.UserInfo.LastName);
            Assert.AreEqual(userInfo.FirstName, returnedUser.UserInfo.FirstName);
            Assert.IsTrue(userInfo.Avatar.SequenceEqual(returnedUser.UserInfo.Avatar));
            Assert.AreEqual(userInfo.DateOfBirth?.Date, returnedUser.UserInfo.DateOfBirth);
            Assert.AreEqual(userInfo.Phone, returnedUser.UserInfo.Phone);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetInfoForInvalidUser()
        {
            // arrange
            var userInfo = new UserInfo
            {
                FirstName = "Vladmir",
                DateOfBirth = DateTime.Parse("06/05/1996"),
                Email = "xxx@yandex.ru",
                Avatar = Encoding.UTF8.GetBytes("testAvatar")
            };

            // act
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.SetUserInfo(User.InvalidId, userInfo));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SetInfoForDefaultUser()
        {
            // arrange
            var userInfo = new UserInfo
            {
                FirstName = "Vladmir",
                DateOfBirth = DateTime.Parse("06/05/1996"),
                Email = "xxx@yandex.ru",
                Avatar = Encoding.UTF8.GetBytes("testAvatar")
            };

            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.SetUserInfo(0, userInfo));
        }

        [TestMethod]
        public void Should_ReplaceInfo_When_Deleted()
        {
            // arrange
            var userInfo1 = new UserInfo
            {
                FirstName = "Vladmir",
                DateOfBirth = DateTime.Parse("06/05/1996"),
                Email = "xxx@yandex.ru",
                Avatar = Encoding.UTF8.GetBytes("testAvatar")
            };
            var userInfo2 = new UserInfo
            {
                FirstName = "Asen"
            };

            var userCred = new UserCredentials{Username = "testuser6", Password = "x"};
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            _usersRepository.SetUserInfo(user.Id, userInfo1);
            var returnedInfo1 = _usersRepository.GetUserInfo(user.Id);
            _usersRepository.DeleteUserInfo(user.Id);
            _usersRepository.SetUserInfo(user.Id, userInfo2);
            var returnedInfo2 = _usersRepository.GetUserInfo(user.Id);
            // assert
            Assert.IsNull(returnedInfo1.LastName);
            Assert.IsNull(returnedInfo2.LastName);
            Assert.AreEqual(userInfo1.FirstName, returnedInfo1.FirstName);
            Assert.AreEqual(userInfo2.FirstName, returnedInfo2.FirstName);
            Assert.IsNull(returnedInfo2.Email);
            Assert.IsNull(returnedInfo2.Avatar);
            Assert.AreEqual(userInfo2.DateOfBirth?.Date, returnedInfo2.DateOfBirth);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_DeleteInfoForInvalidUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.DeleteUserInfo(User.InvalidId));
        }

        [TestMethod]
        public void Should_GetUserByUsername()
        {
            // arrange
            const string userName = "someequaluser";
            var userCred = new UserCredentials{Username = userName, Password = "asd"};
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            var retUser = _usersRepository.GetUserByUsername(user.Username);
            // assert
            Assert.AreEqual(user, retUser);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_OnInvalidUsername()
        {
            // act
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.GetUserByUsername("null"));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_OnNullUsername()
        {
            // act
            Assert.ThrowsException<ArgumentException>(() =>_usersRepository.GetUserByUsername(null));
        }

        [TestMethod]
        public void ShouldStartChatWithUser()
        {
            //arrange
            var user = new UserCredentials
            {
                Username = "TODO:ASD",
                Password = "x"
            };

            const string chatName = "tempChat";

            //act
            var chatsRepository = new ChatsRepository(ConnectionString);
            var usersRepository = new UsersRepository(ConnectionString);
            chatsRepository.UsersRepository = usersRepository;

            var result = usersRepository.CreateUser(user.Username, user.Password);

            _tempUsers.Add(result.Id);

            var chat = chatsRepository.CreateGroupChat(new[] { result.Id }, chatName);
            var userChats = chatsRepository.GetUserChats(result.Id);
            //assert
            Assert.AreEqual(chatName, chat.Info.Title);
            Assert.AreEqual(result.Id, chat.Users.Single());

            var enumerable = userChats as Chat[] ?? userChats.ToArray();
            Assert.AreEqual(chat.Id, enumerable.Single().Id);
            Assert.AreEqual(chat.Info.Title, enumerable.Single().Info.Title);
            Assert.AreEqual(chat.CreatorId, enumerable.Single().CreatorId);
            Assert.AreEqual(chat.CreatorId, result.Id);
        }

        [TestMethod]
        public void Should_ChangePasswordForUser()
        {
            // arrange
            var userCred = new UserCredentials{ Username = "testuser6", Password = "x" };
            const string newPassword = "newPassword";
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);

            _usersRepository.SetPassword(user.Id, newPassword);
            // assert
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_ChangePasswordForInvalidUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.SetPassword(User.InvalidId, "asd"));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_ChangePasswordForDefaultUser()
        {
            // arrange
            const string newPassword = "asd";
            // act
            // assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.SetPassword(0, newPassword));
        }

        [TestMethod]
        public void Should_PersistUser_When_PersistExistingUser()
        {
            // arrange
            var userCred = new UserCredentials{ Username = "testuser8", Password = "asd" };
            var userInfo = new UserInfo { Email = "test@gmail.com" };
            const string newUsername = "newUsername";
            const string newEmail = "newEmail@gmail.com";
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            _tempUsers.Add(user.Id);
            _usersRepository.SetUserInfo(user.Id, userInfo);
            user = _usersRepository.GetUser(user.Id);

            user.Username = newUsername;
            user.UserInfo.Email = newEmail;
            var persistedUser = _usersRepository.PersistUser(user);

            // assert
            Assert.AreEqual(persistedUser.Id, user.Id);
            Assert.AreEqual(persistedUser.Username, newUsername);
            Assert.AreEqual(persistedUser.UserInfo.Email, newEmail);
            Assert.IsNull(persistedUser.UserInfo.Avatar);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_PersistDefaultUser()
        {
            // arrange
            var user = new User{ Id = 0, Username = "testuser9" };
            var userInfo = new UserInfo { Email = "test@gmail.com" };
            user.UserInfo = userInfo;
            // assert
            Assert.ThrowsException<ArgumentException>(() => _usersRepository.PersistUser(user));
        }

        [TestMethod]
        public void Should_ThrowUserAlreadyExistsException_When_PersistNewUserWithExistingUsername()
        {
            // arrange
            var userCred = new UserCredentials{ Username = "testuser10", Password = "asd" };
            var user2Cred = new UserCredentials {Username = "someotherusername", Password = "asdf"};
            // act
            var existingUser = _usersRepository.CreateUser(userCred.Username, "somePassword");
            var tempUser = _usersRepository.CreateUser(user2Cred.Username, user2Cred.Password);
            _tempUsers.Add(existingUser.Id);
            _tempUsers.Add(tempUser.Id);
            tempUser.Username = existingUser.Username;
            // assert
            Assert.ThrowsException<UserAlreadyExistsException>(() =>_usersRepository.PersistUser(tempUser));
        }

        [TestMethod]
        public void Should_ReturnUserChats()
        {
            // arrange
            var userCred = new UserCredentials{Username = "hey world", Password = "asd"};
            // act
            var user = _usersRepository.CreateUser(userCred.Username, userCred.Password);
            var chats = new List<Chat> {
                _chatsRepository.CreateGroupChat(new[] { user.Id }, "newChat"),
                _chatsRepository.CreateGroupChat(new[] { user.Id }, "ok") 
            };

            _tempUsers.Add(user.Id);
            chats.ForEach(x => _tempChats.Add(x.Id));

            // assert
            Assert.AreEqual(2, user.Chats.Count());

            var i = 0;
            foreach (var chat in user.Chats)
            {
                Assert.AreEqual(chat.Id, chats[i].Id);
                Assert.AreEqual(chat.CreatorId, chats[i].CreatorId);
                Assert.AreEqual(chat.Users.Single(), chats[i++].Users.Single());
            }
        }

        [TestMethod]
        public void Should_ReturnChatSpecificInfoForUser()
        {
            // arrange
            var listenerUserCred = new UserCredentials{ Username = "testuser76", Password = "asd" };
            var regularUserCred = new UserCredentials{ Username = "testuser77", Password = "asd" };

            // act

            var listenerUser = _usersRepository.CreateUser(listenerUserCred.Username, listenerUserCred.Password);
            var regularUser = _usersRepository.CreateUser(regularUserCred.Username, regularUserCred.Password);
            var chat = _chatsRepository.CreateGroupChat(new[] { regularUser.Id, listenerUser.Id }, "newChat");

            _tempUsers.Add(listenerUser.Id);
            _tempUsers.Add(regularUser.Id);
            _tempChats.Add(chat.Id);

            _chatsRepository.SetChatSpecificRole(listenerUser.Id, chat.Id, UserRoles.Listener);

            // assert
            Assert.AreEqual(listenerUser.ChatUserInfos.Single().Role.RoleType, UserRoles.Listener);
            Assert.AreEqual(regularUser.ChatUserInfos.Single().Role.RoleType, UserRoles.Moderator);
            Assert.IsFalse((listenerUser.ChatUserInfos.Single().Role.RolePermissions & RolePermissions.WritePerm) != 0);
            Assert.IsTrue((regularUser.ChatUserInfos.Single().Role.RolePermissions & RolePermissions.WritePerm) != 0);
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
