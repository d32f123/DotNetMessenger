namespace DotNetMessenger.Model
{
    public class ChatInfo
    {
        public string Title { get; set; }
        public byte[] Avatar { get; set; }

        public ChatInfo()
        {
            Title = string.Empty;
            Avatar = null;
        }

        public ChatInfo(string title)
        {
            Title = title;
            Avatar = null;
        }

        public ChatInfo(string title, byte[] avatar)
        {
            Title = title;
            Avatar = avatar;
        }
    }
}