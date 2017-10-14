using System;
using System.Collections;
using System.Collections.Generic;

using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class Chat
    {
        /* TODO: LAZY BINDING */
        private IEnumerable<User> _users;

        public int Id { get; set; }
        public ChatTypes ChatType { get; set; }
        public int CreatorId { get; set; }
        public ChatInfo Info { get; set; }
        public IEnumerable<User> Users
        {
            get { return _users; }
            set { _users = value; }
        }

        public Chat(int id, ChatTypes chatType, int creatorId, ChatInfo info)
        {
            Id = id;
            ChatType = chatType;
            CreatorId = creatorId;
            Info = info;
        }

        public Chat(int id, ChatTypes chatType, int creatorId)
        {
            Id = id;
            ChatType = chatType;
            CreatorId = creatorId;
            Info = null;
        }

        public Chat(int id)
        {
            Id = id;
            ChatType = ChatTypes.Dialog;
            CreatorId = 0;
            Info = null;
        }

        public Chat()
        {
            Id = 0;
            ChatType = ChatTypes.Dialog;
            CreatorId = 0;
            Info = null;
        }
    }
}
