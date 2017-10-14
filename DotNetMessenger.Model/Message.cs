using System;
using System.Collections;
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
        public DateTime ExpirationDate { get; set; }
        public IEnumerable<Attachment> Attachments { get; set; }
    }
}
