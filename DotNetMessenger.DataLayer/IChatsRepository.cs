using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface IChatsRepository
    {
        // WARN: The first member will be used as the creator
        Chat CreateChat(IEnumerable<int> members, string title);
        Chat CreateDialog(int member1, int member2);
        IEnumerable<Chat> GetUserChats(int userId);
        void DeleteChat(int chatId);

        void SetCreator(int chatId, int newCreator);
        void AddUser(int chatId, int userId);
        void KickUser(int chatId, int userId);
        void AddUsers(int chatId, IEnumerable<int> newUsers);
        void KickUsers(int chatId, IEnumerable<int> kickedUsers);
        void SetChatInfo(int chatId, ChatInfo info);
        void DeleteChatInfo(int chatId);
    }
}
