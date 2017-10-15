using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Class represents a single user entity
    /// </summary>
    public class User
    {
        public static readonly int InvalidId = -1;

        /// <summary>
        /// Do not set ID field, it should automatically be filled by a call
        /// to GetIdByUsername method
        /// </summary>
        public int Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }

        /// <summary>
        /// Information regarding last name, first name, email, et c.
        /// </summary>
        public virtual UserInfo UserInfo { get; set; }

        /// <summary>
        /// List of chats in which the user is in
        /// </summary>
        public virtual IEnumerable<Chat> Chats { get; set; }

        /// <summary>
        /// Information regarding a specific chat the user is in
        /// e. g. role, nickname
        /// </summary>
        public virtual IEnumerable<ChatUserInfo> ChatUserInfos { get; set; }

        public User()
        {
            Id = InvalidId;
        }
    }
}
