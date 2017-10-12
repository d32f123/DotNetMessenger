using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class Chat
    {
        /* TODO: LAZY BINDING */
        private IEnumerable<User> _users;

        public int Id { get; set; }
        public int ChatType { get; set; }
        public User Creator { get; set; }
        public ChatInfo Info { get; set; }
        public IEnumerable<User> Users
        {
            get { return _users; }
            set { _users = value; }
        }

        public Chat(int id, int chatType, User creator, ChatInfo info)
        {
            Id = id;
            ChatType = chatType;
            Creator = creator;
            Info = info;
        }

        public Chat(int id, int chatType, User creator)
        {
            Id = id;
            ChatType = chatType;
            Creator = creator;
            Info = null;
        }

        public Chat(int id)
        {
            Id = id;
            ChatType = 0;
            Creator = null;
            Info = null;
        }
    }
}
