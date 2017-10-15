using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class RepositoryBuilderTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";

        [TestMethod]
        public void Should_CreateChatsFirst()
        {
            // act
            RepositoryBuilder.ConnectionString = ConnectionString;
            var usersRepository = RepositoryBuilder.UsersRepository;
            var chatsRepository = RepositoryBuilder.ChatsRepository;

            // assert
            Assert.IsNotNull(usersRepository);
            Assert.IsNotNull(chatsRepository);
        }

        [TestMethod]
        public void Should_CreateUsersFirst()
        {
            // act
            RepositoryBuilder.ConnectionString = ConnectionString;
            var chatsRepository = RepositoryBuilder.ChatsRepository;
            var usersRepository = RepositoryBuilder.UsersRepository;

            // assert
            Assert.IsNotNull(usersRepository);
            Assert.IsNotNull(chatsRepository);
        }

        [TestMethod]
        public void Should_GetConnectionString()
        {
            // act
            RepositoryBuilder.ConnectionString = ConnectionString;
            // assert
            Assert.AreEqual(ConnectionString, RepositoryBuilder.ConnectionString);
        }
    }
}
