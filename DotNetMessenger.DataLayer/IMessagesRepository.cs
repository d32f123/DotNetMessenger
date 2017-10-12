using System;
using System.Collections.Generic;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface IMessagesRepository
    {
        Message StoreMessage(Message message);
        Message GetMessage(int messageId);

        IEnumerable<Message> GetChatMessages(int chatId);
        IEnumerable<Message> GetChatMessagesFrom(int chatId, DateTime dateFrom);
        IEnumerable<Message> GetChatMessageTo(int chatId, DateTime dateTo);
        IEnumerable<Message> GetChatMessagesInRange(int chatId, DateTime dateFrom, DateTime dateTo);

        IEnumerable<Message> SearchString(int chatId, string searchString);

        // Execute sql-template for clearing messages that should be deleted
        void ExecuteQueueCleanUp();
    }
}