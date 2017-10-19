using System;
using System.Collections.Generic;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface IMessagesRepository
    {
        /// <summary>
        /// Stores a new message in DB
        /// </summary>
        /// <param name="senderId">Sender of the message (should be in chat)</param>
        /// <param name="chatId">The receiving chat Id</param>
        /// <param name="text">Text of the message</param>
        /// <param name="attachments">Any attachments with the message</param>
        /// <returns>Null if invalid ids or if both text and attachments are null, else object representing given message</returns>
        Message StoreMessage(int senderId, int chatId, string text, IEnumerable<Attachment> attachments = null);
        /// <summary>
        /// Stores a new message in DB with expiration timer
        /// </summary>
        /// <param name="senderId">Sender of the message (should be in chat)</param>
        /// <param name="chatId">The receiving chat Id</param>
        /// <param name="text">Text of the message</param>
        /// <param name="expirationDate">Expiration date of the message</param>
        /// <param name="attachments">Any attachments with the message</param>
        /// <returns>Null if invalid ids, expirationDate is smaller than current date
        ///  or if both text and attachments are null, else object representing given message</returns>
        Message StoreTemporaryMessage(int senderId, int chatId, string text, DateTime expirationDate,
            IEnumerable<Attachment> attachments = null);

        /// <summary>
        /// Gets a message given the message Id
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>Null on invalid id, else object representing a given message</returns>
        Message GetMessage(int messageId);
        /// <summary>
        /// Gets all attachments for a given <paramref name="messageId"/>
        /// </summary>
        /// <param name="messageId">Id of the message</param>
        /// <returns>Empty list on invalid id, else all attachments that belong to the message</returns>
        IEnumerable<Attachment> GetMessageAttachments(int messageId);

        /// <summary>
        /// Gets expiration date of the given message
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>Null if does not expire, else expiration date</returns>
        DateTime? GetMessageExpirationDate(int messageId);

        /// <summary>
        /// Get all messages in a chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Empty list if invalid id, else list of all messages in that chat</returns>
        /// <seealso cref="GetChatMessagesInRange"/>
        IEnumerable<Message> GetChatMessages(int chatId);
        /// <summary>
        /// Get all messages in a chat starting from <paramref name="dateFrom"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="dateFrom">The starting point</param>
        /// <returns>Empty list if invalid chatid, else messages sent after <paramref name="dateFrom"/></returns>
        /// <seealso cref="GetChatMessagesInRange"/>
        IEnumerable<Message> GetChatMessagesFrom(int chatId, DateTime dateFrom);
        /// <summary>
        /// Get all messages in a chat before <paramref name="dateTo"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="dateTo">The end point</param>
        /// <returns>Empty list if invalid chatid, else list of messages prior to <paramref name="dateTo"/></returns>
        /// <seealso cref="GetChatMessagesInRange"/>
        IEnumerable<Message> GetChatMessagesTo(int chatId, DateTime dateTo);
        /// <summary>
        /// Get messages for a specific chat in a specific time range
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns>Empty list if invalid chatId, else list of messages between <paramref name="dateFrom"/> and <paramref name="dateTo"/></returns>
        /// <remarks>Call with both dates == null is equivalent to <see cref="GetChatMessages"/> call</remarks>
        IEnumerable<Message> GetChatMessagesInRange(int chatId, DateTime? dateFrom, DateTime? dateTo);
        /// <summary>
        /// Searches for a given string in a chat (regexp: ^*<paramref name="searchString"/>*$)
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="searchString">The string to be searched</param>
        /// <returns>A list of messages containing <paramref name="searchString"/></returns>
        IEnumerable<Message> SearchString(int chatId, string searchString);

        // Execute sql-template for clearing messages that should be deleted
        /// <summary>
        /// Deletes all messages that are expired
        /// </summary>
        void DeleteExpiredMessages();
    }
}