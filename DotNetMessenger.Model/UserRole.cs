namespace DotNetMessenger.Model
{
    /// <summary>
    /// Information regarding
    /// what a specific role can do in a chat
    /// (i. e.) send messages, send attachments ...
    /// </summary>
    public class UserRole
    {
        public bool ReadPerm { get; set; }
        public bool WritePerm { get; set; }
        // Whether a user can edit topic, avatar of the chat et c. or not
        public bool ChatInfoPerm { get; set; }
        // Whether a user can send attachments or not
        public bool AttachPerm { get; set; }
        // Whether a user can kick/add users or not
        public bool ManageUsersPerm { get; set; }
    }
}