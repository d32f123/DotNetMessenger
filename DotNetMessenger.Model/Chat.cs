using System;
using System.Collections;
using System.Collections.Generic;

using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class Chat
    {
        public static readonly int InvalidId = -1;

        public int Id { get; set; }
        public ChatTypes ChatType { get; set; }
        public int CreatorId { get; set; }
        public virtual ChatInfo Info { get; set; }
        public virtual IEnumerable<User> Users { get; set; }

        public Chat()
        {
            Id = Chat.InvalidId;
            ChatType = ChatTypes.Dialog;
            CreatorId = User.InvalidId;
        }
    }
}
