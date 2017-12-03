using System;
using System.Collections.Generic;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
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
        private Dictionary<int, ChatUserInfo> _chatUserInfos;

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
                try
                {
                    _userInfo = RepositoryBuilder.UsersRepository.GetUserInfo(Id);
                }
                catch (ArgumentException)
                {
                    _userInfo = null;
                }
                _isUserInfoFetched = true;
                return _userInfo;
            } 
        }

        public override Dictionary<int, ChatUserInfo> ChatUserInfos
        {
            get
            {
                if (_areChatUserInfosFetched)
                    return _chatUserInfos;
                _chatUserInfos = new Dictionary<int, ChatUserInfo>();
                foreach (var userChat in RepositoryBuilder.ChatsRepository.GetUserChats(Id))
                {
                    ChatUserInfo info;
                    try
                    {
                        info = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(Id, userChat.Id);
                    }
                    catch (ChatTypeMismatchException)
                    {
                        info = null;
                    }
                    _chatUserInfos.Add(userChat.Id, info);
                }
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
