using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class User
    {
        public static readonly int InvalidId = -1;

        public int Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }

        public virtual UserInfo UserInfo { get; set; }

        public virtual IEnumerable<Chat> Chats { get; set; }

        public virtual IEnumerable<ChatUserInfo> ChatUserInfos { get; set; }

        public User()
        {
            Id = InvalidId;
        }
    }
}
