using System;
using System.Collections.Generic;

using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class MessagesRepository : IMessagesRepository
    {
        public Message StoreMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public Message GetMessage(int messageId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Message> GetChatMessages(int chatId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Message> GetChatMessagesFrom(int chatId, DateTime dateFrom)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Message> GetChatMessageTo(int chatId, DateTime dateTo)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Message> GetChatMessagesInRange(int chatId, DateTime dateFrom, DateTime dateTo)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Message> SearchString(int chatId, string searchString)
        {
            throw new NotImplementedException();
        }

        public void ExecuteQueueCleanUp()
        {
            throw new NotImplementedException();
        }
    }
}