using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer
{
    public interface IUsersRepository
    {
        User CreateUser(string userName, string hash);
        void DeleteUser(int userId);
        User GetUser(int userId);
        UserInfo GetUserInfo(int userId);

        // Update data in DB according to the user
        User PersistUser(User user);
        void SetPassword(int userId, string newHash);

        void SetUserInfo(int userId, UserInfo userInfo);
        void DeleteUserInfo(int userId);

        // Chat-specific user info
        ChatUserInfo GetChatSpecificInfo(int userId, int chatId);
        void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false);
        ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole);
    }
}
