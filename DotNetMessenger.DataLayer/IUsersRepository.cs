using System.Collections.Generic;

using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface IUsersRepository
    {
        User CreateUser(User user);
        void DeleteUser(int userId);
        User GetUser(int userId);

        // Update data in DB according to the user
        User PersistUser(User user);
        void SetPassword(string newHash);

        void SetUserInfo(int userId, UserInfo userInfo);
        void DeleteUserInfo(int userId);
        IEnumerable<User> GetChatUsers(int chatId);

        // Chat-specific user info
        UserInfo GetChatSpecificInfo(int userId, int chatId);
        UserInfo SetChatSpecificInfo(int userId, int chatId, UserInfo userInfo);
        UserInfo SetChatSpecificRole(int userId, int chatId, UserRole userRole);
    }
}
