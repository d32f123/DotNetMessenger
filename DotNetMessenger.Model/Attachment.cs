using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    /// <summary>
    /// Class represents a single attachment file or image
    /// </summary>
    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public AttachmentTypes Type { get; set; }
        public byte[] File { get; set; }
    }
}