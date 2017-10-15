using System.Collections.Generic;

using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer
{
    public interface IChatsRepository
    {
        // WARN: The first member will be used as the creator
        Chat CreateGroupChat(IEnumerable<int> members, string title);
        Chat CreateDialog(int member1, int member2);
        Chat GetChat(int chatId);
        IEnumerable<User> GetChatUsers(int chatId);
        IEnumerable<Chat> GetUserChats(int userId);
        void DeleteChat(int chatId);

        void SetCreator(int chatId, int newCreator);
        void AddUser(int chatId, int userId);
        void KickUser(int chatId, int userId);
        void AddUsers(int chatId, IEnumerable<int> newUsers);
        void KickUsers(int chatId, IEnumerable<int> kickedUsers);
        void SetChatInfo(int chatId, ChatInfo info);
        ChatInfo GetChatInfo(int chatId);
        void DeleteChatInfo(int chatId);

        // Chat-specific user info
        ChatUserInfo GetChatSpecificInfo(int userId, int chatId);
        void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false);
        ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole);
        void DeleteChatSpecificInfo(int userId, int chatId);
    }
}
