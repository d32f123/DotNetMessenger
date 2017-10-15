using System;
using System.Collections;
using System.Collections.Generic;

using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class Chat
    {
        public static readonly int InvalidId = -1;
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

        public Chat()
        {
            Id = Chat.InvalidId;
            ChatType = ChatTypes.Dialog;
            CreatorId = User.InvalidId;
            Info = null;
        }
    }
}
