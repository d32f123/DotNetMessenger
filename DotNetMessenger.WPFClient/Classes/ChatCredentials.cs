using System.Collections.Generic;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.WPFClient.Classes
{
    public class ChatCredentials
    {
        public ChatTypes ChatType { get; set; }
        public string Title { get; set; }
        public IEnumerable<int> Members { get; set; }
    }
}