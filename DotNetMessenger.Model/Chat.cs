using System;
using System.Collections.Generic;

using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Class representing a single chat entity
    /// </summary>
    public class Chat : IEquatable<Chat>
    {
        public static readonly int InvalidId = -1;

        public int Id { get; set; }
        public ChatTypes ChatType { get; set; }
        /// <summary>
        /// The Id of the creator of the chat
        /// </summary>
        public int CreatorId { get; set; }
        public virtual ChatInfo Info { get; set; }
        /// <summary>
        /// List of users in this specific chat
        /// </summary>
        public virtual IEnumerable<int> Users { get; set; }

        public Chat()
        {
            Id = InvalidId;
            ChatType = ChatTypes.Dialog;
            CreatorId = User.InvalidId;
        }

        public bool Equals(Chat other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && ChatType == other.ChatType && CreatorId == other.CreatorId && Equals(Info, other.Info) && Equals(Users, other.Users);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Chat) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (int) ChatType;
                hashCode = (hashCode * 397) ^ CreatorId;
                hashCode = (hashCode * 397) ^ (Info != null ? Info.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
