using System.Collections.Generic;

namespace DotNetMessenger.Model
{
    public class User
    {
        public static readonly int InvalidId = -1;

        private bool _areChatsFetched;
        private IEnumerable<Chat> _chats;

        public int Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }

        private bool _isUserInfoFetched;
        private UserInfo _userInfo;

        public IEnumerable<Chat> Chats
        {
            get { return _chats; }
            set { _chats = value; }
        }

        public UserInfo UserInfo
        {
            get { return _userInfo; }
            set { _userInfo = value; }
        }

        public User()
        {
            Id = InvalidId;
        }
    }
}
