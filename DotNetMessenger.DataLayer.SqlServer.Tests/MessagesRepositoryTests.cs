using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetMessenger.DataLayer.SqlServer.Tests
{
    [TestClass]
    public class MessagesRepositoryTests
    {
        private const string ConnectionString = @"Data Source=DESKTOP-L5T6BNQ;
                Initial Catalog=messenger;
                Integrated Security=True;";

        private int _userId;
        private int _chatId;
        private readonly List<int> _tempUsers = new List<int>();

        private ChatsRepository _chatsRepository;
        private UsersRepository _usersRepository;
        private MessagesRepository _messagesRepository;

        [TestInitialize]
        public void InitRepos()
        {
            RepositoryBuilder.ConnectionString = ConnectionString;
            _usersRepository = RepositoryBuilder.UsersRepository;
            _chatsRepository = RepositoryBuilder.ChatsRepository;
            _messagesRepository = RepositoryBuilder.MessagesRepository;

            _userId = _usersRepository.CreateUser("botAlfred", "hey man").Id;
            _chatId = _chatsRepository.CreateGroupChat(new[] {_userId}, "somechat").Id;
        }


        [TestMethod]
        public void Should_StoreAndReturnMessage()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, text);
            var msg = _messagesRepository.GetChatMessages(_chatId).Single();
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            Assert.IsTrue(!msg.Attachments.Any());
            Assert.IsNull(msg.ExpirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_StoreAndReturnLastMessage()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, text);
            var msg = _messagesRepository.GetLastChatMessage(_chatId);
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            Assert.IsTrue(!msg.Attachments.Any());
            Assert.IsNull(msg.ExpirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_ThrowArgumentNullException_When_StoreEmptyMessage()
        {
            // act and assert
            Assert.ThrowsException<ArgumentNullException>(() =>_messagesRepository.StoreMessage(_userId, _chatId, null));
        }

        [TestMethod]
        public void Should_StoreMessageWithAttachmentAndText()
        {
            // arrange
            const string text = "Some text right here alright";
            var attachment = new Attachment
            {
                File = Encoding.UTF8.GetBytes("hey i am an attachment"),
                Type = AttachmentTypes.RegularFile,
                FileName = "asd"
            };
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, text, new[] {attachment});
            var msg = _messagesRepository.GetChatMessages(_chatId).Single();
            var retAttach = msg.Attachments.Single();
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            
            Assert.AreEqual(retAttach.Type, attachment.Type);
            Assert.IsTrue(retAttach.File.SequenceEqual(attachment.File));

            Assert.IsNull(msg.ExpirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_StoreMessageWithAttachment()
        {
            // arrange
            var attachment = new Attachment
            {
                File = Encoding.UTF8.GetBytes("hey i am an attachment"),
                FileName = "somefile",
                Type = AttachmentTypes.RegularFile
            };
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, null, new[] { attachment });
            var msg = _messagesRepository.GetChatMessages(_chatId).Single();
            var retAttach = msg.Attachments.Single();
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);

            Assert.AreEqual(retAttach.Type, attachment.Type);
            Assert.IsTrue(retAttach.File.SequenceEqual(attachment.File));

            Assert.IsNull(msg.ExpirationDate);
            Assert.IsNull(msg.Text);
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SentByDefaultUser()
        {
            // arrange
            const string text = "Some text right here alright";
            // act and assert
            Assert.ThrowsException<ArgumentException>(() =>  _messagesRepository.StoreMessage(0, _chatId, text));
            
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SentByUserNotInChat()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            var user = _usersRepository.CreateUser("joshua", "asd");
            _tempUsers.Add(user.Id);
            // assert
            Assert.ThrowsException<ArgumentException>(() => _messagesRepository.StoreMessage(user.Id, _chatId, text));
        }

        [TestMethod]
        public void Should_ThrowArgumentException_When_SentToInvalidChat()
        {
            // arrange
            const string text = "Some text right here alright";
            // act and assert
            Assert.ThrowsException<ArgumentException>(() => _messagesRepository.StoreMessage(_userId, Chat.InvalidId, text));
            
        }

        [TestMethod]
        public void Should_SendMessageWithExpirationDate()
        {
            // arrange
            const string text = "Some text right here alright";
            var expirationDate = DateTime.Now.AddSeconds(30);
            expirationDate = expirationDate.AddTicks(-(expirationDate.Ticks % TimeSpan.TicksPerSecond));
            // act
            _messagesRepository.StoreTemporaryMessage(_userId, _chatId, text, expirationDate);
            var msg = _messagesRepository.GetChatMessages(_chatId).Single();
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            Assert.IsTrue(!msg.Attachments.Any());
            Assert.AreEqual(msg.ExpirationDate, expirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_DoNothing_When_SentMessageWithExpirationDateLessThanNow()
        {
            // arrange
            const string text = "Some text right here alright";
            var expirationDate = DateTime.Now;
            expirationDate = expirationDate.AddTicks(-(expirationDate.Ticks % TimeSpan.TicksPerSecond));
            // act
            var msg = _messagesRepository.StoreTemporaryMessage(_userId, _chatId, text, expirationDate);
            // assert
            Assert.IsNull(msg);
        }

        [TestMethod]
        public void Should_GetMessageById()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            var msgId = _messagesRepository.StoreMessage(_userId, _chatId, text).Id;
            var msg = _messagesRepository.GetMessage(msgId);
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            Assert.IsTrue(!msg.Attachments.Any());
            // once more for other type of fetching
            Assert.IsTrue(!msg.Attachments.Any());
            Assert.IsNull(msg.ExpirationDate);
            // once more for other type of fetching
            Assert.IsNull(msg.ExpirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_GetMessagesBeforeNow()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, text);
            var msg = _messagesRepository.GetChatMessagesTo(_chatId, DateTime.Now.AddSeconds(2)).Single();
            // assert
            Assert.IsNotNull(msg);
            Assert.AreEqual(msg.SenderId, _userId);
            Assert.AreEqual(msg.ChatId, _chatId);
            Assert.IsTrue(!msg.Attachments.Any());
            Assert.IsNull(msg.ExpirationDate);
            Assert.AreEqual(msg.Text, text);
        }

        [TestMethod]
        public void Should_ReturnNull_When_GetMessagesAfterNow()
        {
            // arrange
            const string text = "Some text right here alright";
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, text);
            var msg = _messagesRepository.GetChatMessagesFrom(_chatId, DateTime.Now.AddSeconds(2));
            // assert
            Assert.IsTrue(!msg.Any());
        }

        [TestMethod]
        public void Should_DeleteExpiredMessages()
        {
            // arrange
            const string text = "Some text right here alright";
            var expirationDate = DateTime.Now.AddSeconds(1);
            expirationDate = expirationDate.AddTicks(-(expirationDate.Ticks % TimeSpan.TicksPerSecond));
            // act
            var msg = _messagesRepository.StoreTemporaryMessage(_userId, _chatId, text, expirationDate);
            Thread.Sleep(1000);
            _messagesRepository.DeleteExpiredMessages();
            Assert.ThrowsException<ArgumentException>(() => _messagesRepository.GetMessage(msg.Id));
        }

        [TestMethod]
        public void Should_FindMessages_With_SearchString()
        {
            // arrange
            string[] texts =
            {
                "Some text right here alright",
                "other text xdddd what"
            };
            // act
            _messagesRepository.StoreMessage(_userId, _chatId, texts[0]);
            _messagesRepository.StoreMessage(_userId, _chatId, texts[1]);
            Thread.Sleep(8000);
            var msgs = _messagesRepository.SearchString(_chatId, "text");
            // assert
            var messages = msgs as Message[] ?? msgs.ToArray();
            Assert.IsTrue(messages.Length == 2);

            var i = 0;
            foreach (var msg in messages)
            {
                Assert.AreEqual(texts[i++], msg.Text);
                Assert.AreEqual(_userId, msg.SenderId);
                Assert.AreEqual(_chatId, msg.ChatId);
            }
        }

        [TestCleanup]
        public void Clean()
        {
            _usersRepository.DeleteUser(_userId);
            _chatsRepository.DeleteChat(_chatId);
            foreach (var user in _tempUsers)
                _usersRepository.DeleteUser(user);
        }
    }
}
