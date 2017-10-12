using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class User
    {
        /* TODO: LAZY BINDING */
        private IEnumerable<Chat> _chats;

        public int Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }

        public UserInfo UserInfo { get; set; }

        public IEnumerable<Chat> Chats
        {
            get { return _chats; }
            set { _chats = value; }
        }
    }
}
