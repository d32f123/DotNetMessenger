using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.Model
{
    public class Attachment
    {
        public int Id { get; set; }
        public AttachmentTypes Type { get; set; }
        public byte[] File { get; set; }
    }
}