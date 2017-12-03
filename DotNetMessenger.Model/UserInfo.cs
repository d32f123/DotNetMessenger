using System;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class UserInfo : IEquatable<UserInfo>
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string LastAndFirstName => LastName + " " + FirstName;

        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Genders? Gender { get; set; }
        public byte[] Avatar { get; set; }

        public UserInfo()
        {
            DateOfBirth = null;
        }

        public bool Equals(UserInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(LastName, other.LastName) && string.Equals(FirstName, other.FirstName) &&
                   string.Equals(Phone, other.Phone) && string.Equals(Email, other.Email) &&
                   DateOfBirth.Equals(other.DateOfBirth) && Gender == other.Gender && Equals(Avatar, other.Avatar);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UserInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = LastName != null ? LastName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Phone != null ? Phone.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Email != null ? Email.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DateOfBirth != null ? DateOfBirth.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Gender != null ? (int) Gender : 0);
                hashCode = (hashCode * 397) ^ (Avatar != null ? ByteArrayEqualityComparer.Default.GetHashCode(Avatar) : 0);
                return hashCode;
            }
        }
    }
}