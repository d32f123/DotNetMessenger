using DotNetMessenger.Model;

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
    }
}
