namespace DotNetMessenger.Model
{
    /// <summary>
    /// Info regarding a specific
    /// chat that a user is in
    /// </summary>
    public class ChatUserInfo
    {
        public string Nickname { get; set; }
        public UserRole Role { get; set; }
    }
}