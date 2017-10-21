using System;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Information regarding
    /// what a specific role can do in a chat
    /// (i. e.) send messages, send attachments ...
    /// </summary>
    public class UserRole
    {
        public UserRoles RoleType { get; set; }
        public string RoleName { get; set; }
        public RolePermissions RolePermissions;
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