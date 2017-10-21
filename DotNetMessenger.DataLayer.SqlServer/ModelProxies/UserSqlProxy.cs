using System.Collections.Generic;
using System.Linq;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer.SqlServer.ModelProxies
{
    public class UserSqlProxy : User
    {
        private bool _areChatsFetched;
        private IEnumerable<Chat> _chats;

        private bool _isUserInfoFetched;
        private UserInfo _userInfo;

        private bool _areChatUserInfosFetched;
        private IEnumerable<ChatUserInfo> _chatUserInfos;

        public override IEnumerable<Chat> Chats
        {
            get
            {
                if (_areChatsFetched)
                    return _chats;
                _chats = RepositoryBuilder.ChatsRepository.GetUserChats(Id);
                _areChatsFetched = true;
                return _chats;
            }
            set
            {
                _areChatsFetched = true;
                _chats = value;
            }
        }

        public override UserInfo UserInfo
        {
            get
            {
                if (_isUserInfoFetched)
                    return _userInfo;
                _userInfo = RepositoryBuilder.UsersRepository.GetUserInfo(Id);
                _isUserInfoFetched = true;
                return _userInfo;
            } 
        }

        public override IEnumerable<ChatUserInfo> ChatUserInfos
        {
            get
            {
                if (_areChatUserInfosFetched)
                    return _chatUserInfos;
                _chatUserInfos = RepositoryBuilder.ChatsRepository.GetUserChats(Id)
                    .Select(x => RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(Id, x.Id));
                _areChatUserInfosFetched = true;
                return _chatUserInfos;
            }
            set
            {
                _areChatUserInfosFetched = true;
                _chatUserInfos = value;
            }
        }
    }
}
