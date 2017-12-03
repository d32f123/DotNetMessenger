using System;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Info regarding a specific
    /// chat that a user is in
    /// </summary>
    public class ChatUserInfo : IEquatable<ChatUserInfo>
    {
        public string Nickname { get; set; }
        public UserRole Role { get; set; }


        public bool Equals(ChatUserInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Nickname, other.Nickname) && Equals(Role, other.Role);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ChatUserInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Nickname != null ? Nickname.GetHashCode() : 0) * 397) ^ (Role != null ? Role.GetHashCode() : 0);
            }
        }
    }
}