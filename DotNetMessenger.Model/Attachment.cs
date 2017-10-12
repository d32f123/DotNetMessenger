namespace DotNetMessenger.Model
{
    public class Attachment
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public byte[] File { get; set; }
    }
}