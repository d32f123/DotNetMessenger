using System;
using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public virtual DateTime? ExpirationDate { get; set; }
        public virtual IEnumerable<Attachment> Attachments { get; set; }
    }
}
