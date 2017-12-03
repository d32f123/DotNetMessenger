using System;
using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class ChatInfo : IEquatable<ChatInfo>
    {
        public string Title { get; set; }
        public byte[] Avatar { get; set; }

        public ChatInfo()
        {
            Title = string.Empty;
            Avatar = null;
        }

        public bool Equals(ChatInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Title, other.Title) && Equals(Avatar, other.Avatar);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ChatInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Title != null ? Title.GetHashCode() : 0) * 397) ^ (Avatar != null ? ByteArrayEqualityComparer.Default.GetHashCode(Avatar): 0);
            }
        }
    }

    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();
        private ByteArrayEqualityComparer() { }

        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (x.Length != y.Length)
                return false;
            for (var i = 0; i < x.Length; i++)
                if (x[i] != y[i])
                    return false;
            return true;
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null || obj.Length == 0)
                return 0;
            var hashCode = 0;
            for (var i = 0; i < obj.Length; i++)
                // Rotate by 3 bits and XOR the new value.
                hashCode = (hashCode << 3) | (hashCode >> (29)) ^ obj[i];
            return hashCode;
        }
    }
}