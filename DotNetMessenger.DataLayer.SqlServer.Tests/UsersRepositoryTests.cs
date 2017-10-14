using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class UsersRepositoryTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";
        private readonly List<int> _tempUsers = new List<int>();

        private IChatsRepository _chatsRepository;
        private IUsersRepository _usersRepository;

        [TestInitialize]
        public void InitRepos()
        {
            _chatsRepository = new ChatsRepository(ConnectionString);
            _usersRepository = new UsersRepository(ConnectionString, _chatsRepository);
            ((ChatsRepository) _chatsRepository).UsersRepository = _usersRepository;
        }

        [TestMethod]
        public void Should_CreateUser_When_ValidUser()
        {
            //arrange
            var user = new User
            {
                Username = "testuser1",
                Hash = "x"
            };

            //act
            var result = _usersRepository.CreateUser(user.Username, user.Hash);

            _tempUsers.Add(result.Id);

            //assert
            Assert.AreEqual(user.Username, result.Username);
            Assert.AreEqual(user.Hash, result.Hash);
        }

        [TestMethod]
        public void Should_ReturnCreatedUser_When_ValidUser()
        {
            //arrange
            var user = new User
            {
                Username = "testuser1",
                Hash = "x"
            };

            //act
            var result = _usersRepository.CreateUser(user.Username, user.Hash);
            result = _usersRepository.GetUser(result.Id);
            _tempUsers.Add(result.Id);

            //assert
            Assert.AreEqual(user.Username, result.Username);
            Assert.AreEqual(user.Hash, result.Hash);
        }

        [TestMethod]
        public void Should_ReturnNull_When_InvalidId()
        {
            // act
            var result = _usersRepository.GetUser(User.InvalidId);

            // assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Should_ReturnNull_When_EmptyLoginOrPassword()
        {
            // arrange
            var emptyUser = new User
            {
                Username = "",
                Hash = ""
            };

            // act 
            var result = _usersRepository.CreateUser(emptyUser.Username, emptyUser.Hash);
            
            // assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Should_ReturnNull_When_CreatingDuplicateUser()
        {
            // arrange
            var user = new User
            {
                Username = "testuser2",
                Hash = "x"
            };

            // act
            _tempUsers.Add(_usersRepository.CreateUser(user.Username, user.Hash).Id);
            var result = _usersRepository.CreateUser(user.Username, user.Hash);
            // assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Should_DeleteNewUser()
        {
            // arrange
            var user = new User
            {
                Username = "testuser3",
                Hash = "x"
            };

            // act
            var addedUser = _usersRepository.CreateUser(user.Username, user.Hash);
            _usersRepository.DeleteUser(addedUser.Id);
            var deletedUser = _usersRepository.GetUser(addedUser.Id);

            // assert
            Assert.IsNull(deletedUser);
        }

        [TestMethod]
        public void Should_NotDeleteUser_When_DefaultUser()
        {
            // act
            _usersRepository.DeleteUser(0);
            var defaultUser = _usersRepository.GetUser(0);

            // assert
            Assert.IsNotNull(defaultUser);
        }

        [TestMethod]
        public void Should_DoNothing_OnInvalidId()
        {
            // act
            _usersRepository.DeleteUser(User.InvalidId);
            Assert.IsTrue(true);
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
                Avatar = Encoding.UTF8.GetBytes("testAvatar")
            };
            var user = new User {Username = "testuser4", Hash = "x", UserInfo = userInfo};

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Hash).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);
            
            // assert
            Assert.AreEqual(user.Username, returnedUser.Username);
            Assert.AreEqual(user.Hash, returnedUser.Hash);
            Assert.AreEqual(user.UserInfo.LastName, returnedUser.UserInfo.LastName);
            Assert.AreEqual(user.UserInfo.FirstName, returnedUser.UserInfo.FirstName);
            Assert.AreEqual(user.UserInfo.Email, returnedUser.UserInfo.Email);
            Assert.IsTrue(user.UserInfo.Avatar.SequenceEqual(returnedUser.UserInfo.Avatar));
            Assert.AreEqual(user.UserInfo.DateOfBirth.Date, returnedUser.UserInfo.DateOfBirth);
            Assert.AreEqual(user.UserInfo.Phone, returnedUser.UserInfo.Phone);
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
            var user = new User {Username = "testuser5", Hash = "x", UserInfo = userInfo};

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Hash).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);

            // assert
            Assert.IsNull(returnedUser.UserInfo.LastName);
            Assert.IsNull(returnedUser.UserInfo.FirstName);
            Assert.IsNull(returnedUser.UserInfo.Email);
            Assert.AreEqual(user.UserInfo.DateOfBirth.Date, returnedUser.UserInfo.DateOfBirth);
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
                Email = "",
                FirstName = "",
                LastName = "",
                Phone = ""
            };
            var user = new User { Username = "testuser5", Hash = "x", UserInfo = userInfo };

            // act
            var userId = _usersRepository.CreateUser(user.Username, user.Hash).Id;
            _tempUsers.Add(userId);
            _usersRepository.SetUserInfo(userId, userInfo);
            var returnedUser = _usersRepository.GetUser(userId);

            // assert
            Assert.AreEqual(user.Username, returnedUser.Username);
            Assert.AreEqual(user.Hash, returnedUser.Hash);
            Assert.AreEqual(user.UserInfo.LastName, returnedUser.UserInfo.LastName);
            Assert.AreEqual(user.UserInfo.FirstName, returnedUser.UserInfo.FirstName);
            Assert.AreEqual(user.UserInfo.Email, returnedUser.UserInfo.Email);
            Assert.IsTrue(user.UserInfo.Avatar.SequenceEqual(returnedUser.UserInfo.Avatar));
            Assert.AreEqual(user.UserInfo.DateOfBirth.Date, returnedUser.UserInfo.DateOfBirth);
            Assert.AreEqual(user.UserInfo.Phone, returnedUser.UserInfo.Phone);
        }

        [TestMethod]
        public void Should_DoNothing_When_SetInfoForInvalidUser()
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
            _usersRepository.SetUserInfo(User.InvalidId, userInfo);
        }

        [TestMethod]
        public void Should_DoNothing_When_SetInfoForDefaultUser()
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
            _usersRepository.SetUserInfo(0, userInfo);
            var defaultUser = _usersRepository.GetUser(0);

            // assert
            Assert.IsNull(defaultUser.UserInfo);
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

            var user = new User {Username = "testuser6", Hash = "x"};
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
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
            Assert.AreEqual(userInfo2.DateOfBirth.Date, returnedInfo2.DateOfBirth);
        }

        [TestMethod]
        public void Should_DoNothing_When_DeleteInfoForInvalidUser()
        {
            // act
            _usersRepository.DeleteUserInfo(User.InvalidId);
        }

        [TestMethod]
        public void ShouldStartChatWithUser()
        {
            //arrange
            var user = new User
            {
                Username = "TODO:ASD",
                Hash = "x"
            };

            const string chatName = "tempChat";

            //act
            var chatsRepository = new ChatsRepository(ConnectionString);
            var usersRepository = new UsersRepository(ConnectionString, chatsRepository);
            chatsRepository.UsersRepository = usersRepository;

            var result = usersRepository.CreateUser(user.Username, user.Hash);

            _tempUsers.Add(result.Id);

            var chat = chatsRepository.CreateGroupChat(new[] { result.Id }, chatName);
            var userChats = chatsRepository.GetUserChats(result.Id);
            //assert
            Assert.AreEqual(chatName, chat.Info.Title);
            Assert.AreEqual(result.Id, chat.Users.Single().Id);
            Assert.AreEqual(chat.Id, userChats.Single().Id);
            Assert.AreEqual(chat.Info.Title, userChats.Single().Info.Title);
            Assert.AreEqual(chat.CreatorId, userChats.Single().CreatorId);
            Assert.AreEqual(chat.CreatorId, result.Id);
        }

        [TestMethod]
        public void Should_ChangePasswordForUser()
        {
            // arrange
            var user = new User { Username = "testuser6", Hash = "x" };
            const string newPassword = "newPassword";
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);

            _usersRepository.SetPassword(user.Id, newPassword);
            user = _usersRepository.GetUser(user.Id);

            // assert
            Assert.AreEqual(user.Hash, newPassword);
        }

        [TestMethod]
        public void Should_DoNothing_When_ChangePasswordForInvalidUser()
        {
            // act
            _usersRepository.SetPassword(User.InvalidId, "asd");
            var user = _usersRepository.GetUser(User.InvalidId);
            // assert
            Assert.IsNull(user);
        }

        [TestMethod]
        public void Should_DoNothing_When_ChangePasswordForDefaultUser()
        {
            // arrange
            var defaultUser = _usersRepository.GetUser(0);
            const string newPassword = "asd";

            // act
            _usersRepository.SetPassword(0, newPassword);
            var user = _usersRepository.GetUser(0);

            // assert
            Assert.AreEqual(user.Hash, defaultUser.Hash);
        }

        [TestMethod]
        public void Should_PersistUser_When_PersistNewUser()
        {
            // arrange
            var user = new User {Username = "testuser7", Hash = "asd"};
            var userInfo = new UserInfo {Email = "test@gmail.com"};
            user.UserInfo = userInfo;

            // act
            var persistedUser = _usersRepository.PersistUser(user);
            _tempUsers.Add(persistedUser.Id);
            // assert
            Assert.AreEqual(persistedUser.Username, user.Username);
            Assert.AreEqual(persistedUser.Hash, user.Hash);
            Assert.AreEqual(userInfo.Email, persistedUser.UserInfo.Email);
            Assert.IsNull(persistedUser.UserInfo.Avatar);
        }

        [TestMethod]
        public void Should_PersistUser_When_PersistExistingUser()
        {
            // arrange
            var user = new User { Username = "testuser8", Hash = "asd" };
            var userInfo = new UserInfo { Email = "test@gmail.com" };
            const string newUsername = "newUsername";
            const string newPassword = "newPassword";
            const string newEmail = "newEmail@gmail.com";
            // act
            user = _usersRepository.CreateUser(user.Username, user.Hash);
            _tempUsers.Add(user.Id);
            _usersRepository.SetUserInfo(user.Id, userInfo);
            user = _usersRepository.GetUser(user.Id);

            user.Username = newUsername;
            user.Hash = newPassword;
            user.UserInfo.Email = newEmail;
            var persistedUser = _usersRepository.PersistUser(user);

            // assert
            Assert.AreEqual(persistedUser.Id, user.Id);
            Assert.AreEqual(persistedUser.Username, newUsername);
            Assert.AreEqual(persistedUser.Hash, newPassword);
            Assert.AreEqual(persistedUser.UserInfo.Email, newEmail);
            Assert.IsNull(persistedUser.UserInfo.Avatar);
        }

        [TestMethod]
        public void Should_ReturnNull_When_PersistDefaultUser()
        {
            // arrange
            var user = new User { Id = 0, Username = "testuser9", Hash = "asd" };
            var userInfo = new UserInfo { Email = "test@gmail.com" };
            user.UserInfo = userInfo;

            // act
            var persistedUser = _usersRepository.PersistUser(user);
            // assert
            Assert.IsNull(persistedUser);
        }

        [TestMethod]
        public void Should_ReturnNull_When_PersistNewUserWithExistingUsername()
        {
            // arrange
            var user = new User { Username = "testuser10", Hash = "asd" };

            // act
            var existingUser = _usersRepository.CreateUser(user.Username, "somePassword");
            _tempUsers.Add(existingUser.Id);

            var persistedUser = _usersRepository.PersistUser(user);
            // assert
            Assert.IsNull(persistedUser);
        }

        [TestCleanup]
        public void Clean()
        {
            foreach (var id in _tempUsers)
                _usersRepository.DeleteUser(id);
        }
    }
}
