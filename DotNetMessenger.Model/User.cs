using System;
using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    /// <inheritdoc />
    /// <summary>
    /// Class represents a single user entity
    /// </summary>
    public class User : IComparable<User>
    {
        public static readonly int InvalidId = -1;

        /// <summary>
        /// Do not set ID field, it should automatically be filled by a call
        /// to GetIdByUsername method
        /// </summary>
        public int Id { get; set; }
        public string Username { get; set; }

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
        public virtual Dictionary<int, ChatUserInfo> ChatUserInfos { get; set; }

        public User()
        {
            Id = InvalidId;
        }

        int IComparable<User>.CompareTo(User other)
        {
            if (other == null)
                return 1;
            if (Id < other.Id)
                return -1;
            if (Id > other.Id)
                return 1;
            if (string.Compare(Username, other.Username, StringComparison.Ordinal) != 0)
                return string.Compare(Username, other.Username, StringComparison.Ordinal);
            return 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as User;
            if (this == other)
                return true;

            if (other == null)
                return false;

            if (Id != other.Id)
                return false;
            if (string.Compare(Username, other.Username, StringComparison.Ordinal) != 0)
                return false;
            return true;
        }

        protected bool Equals(User other)
        {
            return Id == other.Id && string.Equals(Username, other.Username) && Equals(UserInfo, other.UserInfo) &&
                   Equals(Chats, other.Chats) && Equals(ChatUserInfos, other.ChatUserInfos);
        }

        public override int GetHashCode()
        {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Username != null ? Username.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UserInfo != null ? UserInfo.GetHashCode() : 0);
                return hashCode;
        }
    }
}
