using System.Collections.Generic;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class ChatSqlProxy : Chat
    {
        private bool _areUsersFetched;
        private IEnumerable<User> _users;

        private bool _isInfoFetched;
        private ChatInfo _info;

        public override IEnumerable<User> Users
        {
            get
            {
                if (_areUsersFetched)
                    return _users;
                _users = RepositoryBuilder.ChatsRepository.GetChatUsers(Id);
                _areUsersFetched = true;
                return _users;
            }
        }

        public override ChatInfo Info
        {
            get
            {
                if (_isInfoFetched)
                    return _info;
                _info = RepositoryBuilder.ChatsRepository.GetChatInfo(Id);
                _isInfoFetched = true;
                return _info;
            }
        }
    }
}