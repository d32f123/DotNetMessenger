using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface IUsersRepository
    {
        /// <summary>
        /// Creates a user in DB given their <paramref name="userName"/> and <paramref name="password"/>
        /// </summary>
        /// <param name="userName">The username of the new user</param>
        /// <param name="password">Hash of the password of the new user</param>
        /// <returns>Null if username already exists, else object representing a newly created user</returns>
        User CreateUser(string userName, string password);
        /// <summary>
        /// Deletes a user from DB
        /// </summary>
        /// <param name="userId">Id of the user</param>
        void DeleteUser(int userId);
        /// <summary>
        /// Gets a user from DB given their <paramref name="userId"/>
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>Null on invalidId, else object representing given user</returns>
        User GetUser(int userId);
        /// <summary>
        /// Gets a user from DB given their <paramref name="userName"/>
        /// </summary>
        /// <param name="userName">The username of the user</param>
        /// <returns>Null on invalid username, else object representing given user</returns>
        User GetUserByUsername(string userName);
        /// <summary>
        /// Gets information about the user (last name et c.)
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>Null on invalidId, else information of the user</returns>
        UserInfo GetUserInfo(int userId);
        /// <summary>
        /// Persists all changes in the <see cref="User"/> in DB
        /// </summary>
        /// <param name="user">The user object to be persisted</param>
        /// <returns>Null if failed, same object if successful</returns>
        User PersistUser(User user);
        /// <summary>
        /// Sets a new password for the user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="newPassword">The new password</param>
        void SetPassword(int userId, string newPassword);
        /// <summary>
        /// Gets user's password hash
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>The hash</returns>
        string GetPassword(int userId);
        /// <summary>
        /// Sets info for a given user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="userInfo">The information about the user</param>
        void SetUserInfo(int userId, UserInfo userInfo);
        /// <summary>
        /// Deletes information about a specific user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        void DeleteUserInfo(int userId);
    }
}
