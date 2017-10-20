using System;
using DotNetMessenger.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class TokensRepositoryTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";
        private int _userId;

        private UsersRepository _usersRepository;
        private TokensRepository _tokensRepository;

        [TestInitialize]
        public void InitRepos()
        {
            RepositoryBuilder.ConnectionString = ConnectionString;
            _usersRepository = RepositoryBuilder.UsersRepository;
            _tokensRepository = RepositoryBuilder.TokensRepository;

            _userId = _usersRepository.CreateUser("botAlfred", "hey man").Id;
        }

        [TestMethod]
        public void Should_GetNewToken_When_CreateNewToken()
        {
            // act
            var token = _tokensRepository.GenerateToken(_userId);
            // assert
            Assert.AreEqual(_userId, _tokensRepository.GetUserIdByToken(token));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_CreateTokenForInvalidUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _tokensRepository.GenerateToken(User.InvalidId));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_CreateTokenForDefaultUser()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _tokensRepository.GenerateToken(0));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_InvalidateNewToken()
        {
            // act
            var token = _tokensRepository.GenerateToken(_userId);
            _tokensRepository.InvalidateToken(token);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _tokensRepository.GetUserIdByToken(token));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_GetUserFromInvalidToken()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _tokensRepository.GetUserIdByToken(Guid.NewGuid()));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_DeleteInvalidToken()
        {
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _tokensRepository.InvalidateToken(Guid.NewGuid()));
        }

        [TestCleanup]
        public void Clean()
        {
            _usersRepository.DeleteUser(_userId);
        }
    }
}
