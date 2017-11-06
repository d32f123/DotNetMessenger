using System.Collections.Generic;
using System.Linq;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer.SqlServer.ModelProxies
{
    public class ChatSqlProxy : Chat
    {
        private bool _areUsersFetched;
        private IEnumerable<int> _users;

        private bool _isInfoFetched;
        private ChatInfo _info;

        public override IEnumerable<int> Users
        {
            get
            {
                if (_areUsersFetched)
                    return _users;
                var temp = RepositoryBuilder.ChatsRepository.GetChatUsers(Id);
                _users =  (temp as List<User> ?? temp.ToList()).Select(x => x.Id);
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
                _info = ChatType != ChatTypes.Dialog ? RepositoryBuilder.ChatsRepository.GetChatInfo(Id) : null;
                _isInfoFetched = true;
                return _info;
            }
        }
    }
}