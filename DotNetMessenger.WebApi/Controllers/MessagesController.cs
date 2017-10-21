using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Model;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Authorization;
using DotNetMessenger.WebApi.Models;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/messages")]
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
            try
            {
                // if no attach permissions and attachments are not empty
                if ((RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(userId, chatId).Role.RolePermissions &
                     RolePermissions.AttachPerm) == 0 &&
                    (message.Attachments == null || message.Attachments.Count() != 0))
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                        "Not enough permissions"));
                }
                if (message.ExpirationDate == null)
                {
                    return RepositoryBuilder.MessagesRepository.StoreMessage(userId, chatId, message.Text,
                        message.Attachments);
                }
                return RepositoryBuilder.MessagesRepository.StoreTemporaryMessage(userId, chatId, message.Text,
                    (DateTime)message.ExpirationDate, message.Attachments);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Text and attachments cannot be both empty"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Ids are invalid"));
            }
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
            try
            {
                var message = RepositoryBuilder.MessagesRepository.GetMessage(messageId);
                if (message == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        "Id is invalid"));
                }

                return message;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Id is invalid"));
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
            try
            {
                var attachments = RepositoryBuilder.MessagesRepository.GetMessageAttachments(messageId) as Attachment[] ??
                            RepositoryBuilder.MessagesRepository.GetMessageAttachments(messageId).ToArray();
                return attachments;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such message exists"));
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
            try
            {
                return RepositoryBuilder.MessagesRepository.GetMessageExpirationDate(messageId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such message exists"));
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
            try
            {
                var messages = RepositoryBuilder.MessagesRepository.GetChatMessages(chatId) as Message[] ??
                               RepositoryBuilder.MessagesRepository.GetChatMessages(chatId);
                return messages;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat exists"));
            }
        }
        /// <summary>
        /// Gets chat messages in specified <paramref name="dateRange"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="dateRange"><see cref="DateRange"/> object instance</param>
        /// <returns>Messages in <see cref="DateRange"/> range</returns>
        [Route("chats/{chatId:int}/bydate")]
        [HttpGet]
        [ChatUserAuthorization(RegexString = @".*\/chats\/([^\/]+)\/?", Permissions = RolePermissions.ReadPerm)]
        public IEnumerable<Message> GetChatMessagesInRange(int chatId, [FromUri] DateRange dateRange)
        {
            try
            {
                var messages =
                    RepositoryBuilder.MessagesRepository.GetChatMessagesInRange(chatId, dateRange.DateFrom,
                        dateRange.DateTo);
                return messages;
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat exists"));
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
            try
            {
                var messages = RepositoryBuilder.MessagesRepository.SearchString(chatId, searchString) as Message[] ??
                               RepositoryBuilder.MessagesRepository.SearchString(chatId, searchString).ToArray();
                return messages;
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Search string cannot be empty"));
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "No such chat exists"));
            }
        }
    }
}
