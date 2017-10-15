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
    }
}