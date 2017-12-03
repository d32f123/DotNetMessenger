using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Logger;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Filters.Logging;
using DotNetMessenger.WebApi.Models;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/messages")]
    [ExpectedExceptionsFilter]
    [TokenAuthentication]
    [Authorize]
    public class MessagesController : ApiController
    {
        private const string RegexString = @".*\/messages\/([^\/]+)\/?";
        /// <summary>
        /// Stores a new <paramref name="message"/> in <paramref name="chatId"/> from <paramref name="userId"/>.
        /// User performing the request must have <see cref="RolePermissions.WritePerm"/> permission.
        /// <remarks>If user does not have <see cref="RolePermissions.AttachPerm"/> but tries to attach something, not enough permissions error is thrown</remarks>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="message">Message</param>
        /// <returns>Stored message (including the generated id)</returns>
        [Route("{chatId:int}/{userId:int}")]
        [HttpPost]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.WritePerm)]
        public Message StoreMessage(int chatId, int userId, [FromBody] Message message)
        {
            NLogger.Logger.Debug("Called with arguments CID:{0}, UID:{1}, MSG:{2}", chatId, userId, message);

            // if no attach permissions and attachments are not empty
            RolePermissions perms;
            try
            {
                perms = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(userId, chatId)?.Role?.RolePermissions
                        ?? RolePermissions.WritePerm;
            }
            catch (ChatTypeMismatchException)
            {
                perms = RolePermissions.WritePerm | RolePermissions.AttachPerm;
            }

            if ((perms & RolePermissions.AttachPerm) == 0 &&
                message.Attachments != null && message.Attachments.Count() != 0)
            {
                NLogger.Logger.Error(
                    "User does not have enough permissions to store a message! UserID: {0}, ChatID: {1}, Message: {2}",
                    userId, chatId, message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                    "Not enough permissions"));
            }
            if (message.ExpirationDate == null)
            {
                using (var timeLogger =
                    new ChronoLogger("Storing message. UID: {0}, CID: {1}, Text: {2}, Attachments: {3}",
                        userId, chatId, message.Text, message.Attachments))
                {
                    timeLogger.Start();
                    var msg = RepositoryBuilder.MessagesRepository.StoreMessage(userId, chatId, message.Text,
                        message.Attachments?.Where(x => x != null));
                    NLogger.Logger.Info("Successfully stored message from UID:{0} to CID:{1}. Message: {2}",
                        userId, chatId, msg);
                    return msg;
                }
            }
            using (var timeLogger =
                new ChronoLogger("Storing message with expiration date. UID: {0}, CID: {1}, Message: {2}",
                    userId, chatId, message))
            {
                timeLogger.Start();
                var msg = RepositoryBuilder.MessagesRepository.StoreTemporaryMessage(userId, chatId, message.Text,
                    ((DateTime) message.ExpirationDate).ToLocalTime(), message.Attachments?.Where(x => x != null));
                NLogger.Logger.Info("Successfully stored message with e.d from UID: {0} to CID:{1}. Message: {2}", userId, chatId, msg);
                return msg;
            }
        }
        /// <summary>
        /// Gets the last message in the chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Last message</returns>
        [Route("{chatId:int}/last")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = RegexString, Permissions = RolePermissions.ReadPerm)]
        public Message GetLastChatMessage(int chatId)
        {
            NLogger.Logger.Debug("Called with argument: {0}", chatId);
            var message = RepositoryBuilder.MessagesRepository.GetLastChatMessage(chatId);
            NLogger.Logger.Info("Successfully fetched message with id: {0}", message?.Id);
            return message;
        }
        /// <summary>
        /// Gets a message given its <paramref name="messageId"/>. User performing the request must be in the same 
        /// chat as the message and the user must have <see cref="RolePermissions.ReadPerm"/> permission
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>Full message object</returns>
        [Route("{messageId:int}")]
        [HttpGet]
        [MessageFromChatUserAuthorization(RegexString = RegexString)]
        public Message GetMessage(int messageId)
        {
            NLogger.Logger.Debug("Called with argument MID:{0}", messageId);
            using (var timeLogger = new ChronoLogger("Fetching message with id: {0}", messageId))
            {
                timeLogger.Start();
                var message = RepositoryBuilder.MessagesRepository.GetMessage(messageId);
                NLogger.Logger.Info("Successfully fetched message with id: {0}", messageId);
                return message;
            }
        }
        /// <summary>
        /// Gets the attachments of <paramref name="messageId"/>. User performing the request must be in the same chat as the message
        /// and have <see cref="RolePermissions.ReadPerm"/> permission
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>List of attachments of the message</returns>
        [Route("{messageId:int}/attachments")]
        [HttpGet]
        [MessageFromChatUserAuthorization(RegexString = RegexString)]
        public IEnumerable<Attachment> GetMessageAttachments(int messageId)
        {
            NLogger.Logger.Debug("Called with argument MID:{0}", messageId);
            using (var timeLogger = new ChronoLogger("Fetching message attachments for message {0}",
                messageId))
            {
                timeLogger.Start();
                var attachments =
                    RepositoryBuilder.MessagesRepository.GetMessageAttachments(messageId) as Attachment[] ??
                    RepositoryBuilder.MessagesRepository.GetMessageAttachments(messageId).ToArray();
                NLogger.Logger.Info("Successfully fetched message attachments for message {0}. Total of {1}",
                    messageId, attachments.Length);
                return attachments;
            }
        }
        /// <summary>
        /// Gets expiration date of the message. Returns null if message does not expire.
        /// User performing the request must have <see cref="RolePermissions.ReadPerm"/> permission
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>Expiration date of the message</returns>
        [Route("{messageId:int}/expirationdate")]
        [HttpGet]
        [MessageFromChatUserAuthorization(RegexString = RegexString)]
        public DateTime? GetMessageExpirationDate(int messageId)
        {
            NLogger.Logger.Debug("Called with argument MID:{0}", messageId);
            using (var timeLogger = new ChronoLogger("Fetching message expiration date for message {0}", messageId))
            {
                timeLogger.Start();
                var expirationDate = RepositoryBuilder.MessagesRepository.GetMessageExpirationDate(messageId);
                NLogger.Logger.Info("Successfully fetched message expiration date for message {0}", messageId);
                return expirationDate;
            }
        }
        /// <summary>
        /// Gets all messages from a given <paramref name="chatId"/>.
        /// User performing the request must have <see cref="RolePermissions.ReadPerm"/> permission
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>List of messages</returns>
        [Route("chats/{chatId:int}")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = @".*\/chats\/([^\/]+)\/?", Permissions = RolePermissions.ReadPerm)]
        public IEnumerable<Message> GetChatMessages(int chatId)
        {
            NLogger.Logger.Debug("Called with argument CID:{0}", chatId);
            using (var timeLogger = new ChronoLogger("Fetching chat messages for chat {0}", chatId))
            {
                timeLogger.Start();
                var messages = RepositoryBuilder.MessagesRepository.GetChatMessages(chatId) as Message[] ??
                               RepositoryBuilder.MessagesRepository.GetChatMessages(chatId);
                var chatMessages = messages as Message[] ?? messages.ToArray();
                NLogger.Logger.Info("Successfully fetched messages for chat {0}. Found {1} messages",
                    chatId, chatMessages.Length);
                return chatMessages;
            }
        }
        /// <summary>
        /// Gets chat messages in specified <paramref name="dateRange"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="dateRange"><see cref="DateRange"/> object instance</param>
        /// <returns>Messages in <see cref="DateRange"/> range</returns>
        [Route("chats/{chatId:int}/bydate")]
        [HttpPut]
        [ChatUserAuthorization(RegexString = @".*\/chats\/([^\/]+)\/?", Permissions = RolePermissions.ReadPerm)]
        public IEnumerable<Message> GetChatMessagesInRange(int chatId, [FromBody] DateRange dateRange)
        {
            NLogger.Logger.Debug("Called with arguments CID:{0}, Range:{1}", chatId, dateRange);
            if (dateRange == null)
            {
                NLogger.Logger.Debug("Called with null argument. Returning null");
                return null;
            }

            using (var timeLogger = new ChronoLogger("Fetching chat messages for chat {0} in range: {1}",
                chatId, dateRange))
            {
                timeLogger.Start();
                var messages =
                    RepositoryBuilder.MessagesRepository.GetChatMessagesInRange(chatId, dateRange.DateFrom,
                        dateRange.DateTo);
                var chatMessagesInRange = messages as Message[] ?? messages.ToArray();
                NLogger.Logger.Info("Successfully fetched messages of chat {0} in range {1}. Found {2} messages",
                    chatId, dateRange, chatMessagesInRange.Length);
                return chatMessagesInRange;
            }
        }
        /// <summary>
        /// Gets chat messages starting from id <paramref name="messageId"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="messageId">The id of the first message to be included</param>
        /// <returns>A list of messages</returns>
        [Route("chats/{chatId:int}/from/{messageId:int}")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = @".*\/chats\/([^\/]+)\/?", Permissions = RolePermissions.ReadPerm)]
        public IEnumerable<Message> GetChatMessagesFromId(int chatId, int messageId)
        {
            NLogger.Logger.Debug("Called with arguments: CID:{0}, MID:{1}", chatId, messageId);

            using (var timeLogger = new ChronoLogger("Fetching chat messages for chat {0} in range: {1}",
                chatId, messageId))
            {
                timeLogger.Start();
                var messages = RepositoryBuilder.MessagesRepository.GetChatMessagesFrom(chatId, messageId)
                    .Where(x => x.Id != messageId).ToArray();
                NLogger.Logger.Info("Successfully fetched messages of chat {0} in range {1}. Found {2} messages",
                    chatId, messageId, messages.Length);
                return messages;
            }
        }
        /// <summary>
        /// Searches messages in given <paramref name="chatId"/> by <paramref name="searchString"/>.
        /// User performing the request must have <see cref="RolePermissions.ReadPerm"/>
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        [Route("chats/{chatId:int}/bystring")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = @".*\/chats\/([^\/]+)\/?", Permissions = RolePermissions.ReadPerm)]
        public IEnumerable<Message> SearchMessagesInChat(int chatId, [FromUri] string searchString)
        {
            NLogger.Logger.Debug("Called with arguments CID:{0}, Search:{1}", chatId, searchString);
            using (var timeLogger = new ChronoLogger("Searching messages in chat {0}. Search query: {1}",
                chatId, searchString))
            {
                timeLogger.Start();
                var messages = RepositoryBuilder.MessagesRepository.SearchString(chatId, searchString) as Message[] ??
                               RepositoryBuilder.MessagesRepository.SearchString(chatId, searchString).ToArray();
                NLogger.Logger.Info("Searched for \"{0}\" in chat {1}. Found {2} messages",
                    searchString, chatId, messages.Length);
                return messages;
            }
        }
    }
}
