using System;

namespace DotNetMessenger.Model.Enums
{
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