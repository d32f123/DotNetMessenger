using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Model;
using DotNetMessenger.WebApi.Models;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/messages")]
    public class MessagesController : ApiController
    {
        [Route("{chatId:int}/{userId:int}")]
        [HttpPost]
        public Message StoreMessage(int chatId, int userId, [FromBody] Message message)
        {
            try
            {
                if (message.ExpirationDate == null)
                    return RepositoryBuilder.MessagesRepository.StoreMessage(userId, chatId, message.Text,
                        message.Attachments);
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

        [Route("{messageId:int}")]
        [HttpGet]
        public Message GetMessage(int messageId)
        {
            var message = RepositoryBuilder.MessagesRepository.GetMessage(messageId);
            if (message != null)
                return message;
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                "Id is invalid"));
        }

        [Route("{messageId:int}/attachments")]
        [HttpGet]
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

        [Route("{messageId:int}/expirationdate")]
        [HttpGet]
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

        [Route("chats/{chatId:int}")]
        [HttpGet]
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

        [Route("chats/{chatId:int}/bydate")]
        [HttpGet]
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

        [Route("chats/{chatId:int}/bystring")]
        [HttpGet]
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
