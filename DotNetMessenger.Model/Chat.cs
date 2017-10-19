using System.Collections.Generic;

using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Class representing a single chat entity
    /// </summary>
    public class Chat
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
    }
}
