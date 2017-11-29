using System;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Information regarding
    /// what a specific role can do in a chat
    /// (i. e.) send messages, send attachments ...
    /// </summary>
    public class UserRole : IEquatable<UserRole>
    {
        public UserRoles RoleType { get; set; }
        public string RoleName { get; set; }
        public RolePermissions RolePermissions;

        public bool Equals(UserRole other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RolePermissions == other.RolePermissions && RoleType == other.RoleType && string.Equals(RoleName, other.RoleName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserRole) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) RolePermissions;
                hashCode = (hashCode * 397) ^ (int) RoleType;
                hashCode = (hashCode * 397) ^ (RoleName != null ? RoleName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
    [Flags]
    public enum RolePermissions
    {
        NaN = 0,
        ReadPerm = 1,
        WritePerm = 2,
        ChatInfoPerm = 4,
        AttachPerm = 8,
        ManageUsersPerm = 16
    }
}