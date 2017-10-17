﻿using System;
using System.Collections.Generic;

namespace DotNetMessenger.Model
{
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
            if (string.Compare(Hash, other.Hash, StringComparison.Ordinal) != 0)
                return string.Compare(Hash, other.Hash, StringComparison.Ordinal);
            return 0;
        }

        public override bool Equals(Object obj)
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
            if (string.Compare(Hash, other.Hash, StringComparison.Ordinal) != 0)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
