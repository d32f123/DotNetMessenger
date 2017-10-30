using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DotNetMessenger.DataLayer.SqlServer.ModelProxies;
using DotNetMessenger.Model;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Logger;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class UsersRepository : IUsersRepository
    {
        private readonly string _connectionString;


        public UsersRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <inheritdoc />
        /// <summary>
        /// Create user given their username and password
        /// </summary>
        /// <param name="userName">Name of the user</param>
        /// <param name="password">Their password</param>
        /// <returns>User object</returns>
        /// <exception cref="UserAlreadyExistsException">Throws if username already exists</exception>
        public User CreateUser(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(userName));

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Does username exist?
                if (SqlHelper.DoesFieldValueExist(connection, "Users", "Username", userName, SqlDbType.VarChar, userName.Length))
                    throw new UserAlreadyExistsException($"User {userName} already exists");
                using (var transaction = connection.BeginTransaction())
                {
                    UserSqlProxy user;

                    // make an entry in users table
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO [Users] ([Username], [Password]) OUTPUT INSERTED.[ID] VALUES (@userName, @password)";

                        command.Parameters.AddWithValue("@userName", userName);
                        command.Parameters.AddWithValue("@password", password);
                        try
                        {
                            var id = (int) command.ExecuteScalar();
                            user = new UserSqlProxy {Id = id, Username = userName};
                        }
                        catch (SqlException)
                        {
                            NLogger.Logger.Error("Tried to insert invalid username");
                            throw new ArgumentException();
                        }
                    }
                    NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (Username:{1}, Password:x)", "[Users]", userName);


                    // make entry in userinfos table
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO [UserInfos] ([UserID], [LastName], [FirstName], [Phone], [Email], [DateOfBirth], [Avatar]) VALUES " +
                            "(@userId, NULL, NULL, NULL, NULL, NULL, NULL)";

                        command.Parameters.AddWithValue("@userId", user.Id);

                        command.ExecuteNonQuery();
                    }
                    NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (UserID:{1})", "[UserInfos]", user.Id);
                    transaction.Commit();

                    return user;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes user given their id
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <exception cref="T:System.ArgumentException">Throws if no such user exists</exception>
        public void DeleteUser(int userId)
        {
            // Default (deleted) user check
            if (userId == 0)
                throw new ArgumentException("No user found", nameof(userId));
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Just delete the user, everything else will cascade delete
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);
                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException("No user found", nameof(userId)); // if no user was deleted, throw an error
                }
                NLogger.Logger.Trace("DB:Deleted:{0}:WHERE UserID:{1}", "[Tokens]", userId);
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Returns a user given their id
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns><see cref="User"/> object</returns>
        /// <exception cref="ArgumentException">Throws if id is invalid</exception>
        public User GetUser(int userId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Username], [Password] FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);

                    var user = new UserSqlProxy();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        reader.Read();
                        user.Id = reader.GetInt32(reader.GetOrdinal("ID"));
                        user.Username = reader.GetString(reader.GetOrdinal("Username"));
                    }
                    return user;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets user by username
        /// </summary>
        /// <param name="userName">The name of the user</param>
        /// <returns>null if user not found, else User object</returns>
        /// <exception cref="ArgumentException">Throws if userName is invalid</exception>
        public User GetUserByUsername(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Password] FROM [Users] WHERE [Username] = @userName";
                    command.Parameters.AddWithValue("@userName", userName);

                    var user = new UserSqlProxy();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        reader.Read();
                        user.Id = reader.GetInt32(reader.GetOrdinal("ID"));
                        user.Username = userName;
                    }
                    return user.Id == 0 ? null : user;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Async version of <see cref="M:DotNetMessenger.DataLayer.SqlServer.UsersRepository.GetUserByUsername(System.String)" />
        /// </summary>
        /// <seealso cref="M:DotNetMessenger.DataLayer.SqlServer.UsersRepository.GetUserByUsername(System.String)" />
        public async Task<User> GetUserByUsernameAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Password] FROM [Users] WHERE [Username] = @userName";
                    command.Parameters.AddWithValue("@userName", userName);

                    var user = new UserSqlProxy();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        await reader.ReadAsync();
                        user.Id = reader.GetInt32(reader.GetOrdinal("ID"));
                        user.Username = userName;
                    }
                    return user.Id == 0 ? null : user;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes a user from the DB
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <exception cref="T:System.ArgumentException">If user is invalid or does not have userInfo</exception>
        public void DeleteUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // check if user and userinfo entry exist (for possible exception throw in the future)
                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int)
                    || !SqlHelper.DoesFieldValueExist(connection, "UserInfos", "UserID", userId, SqlDbType.Int))
                    throw new ArgumentException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [UserInfos] WHERE [UserID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                    NLogger.Logger.Trace("DB:Deleted:{0}:WHERE UserID:{1})", "[UserInfos]", userId);
                }
            }
        } 
        /// <inheritdoc />
        /// <summary>
        /// Returns user's info given their id
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>UserInfo object representing the information, null if no user or info found</returns>
        /// <exception cref="ArgumentException">Throws if no info found</exception>
        public UserInfo GetUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [LastName], [FirstName], [Phone], [Email], [DateOfBirth], [Avatar], [Gender]" +
                                          "FROM [UserInfos] WHERE [UserID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        reader.Read();
                        return new UserInfo
                        {
                            LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : (DateTime?) reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                            Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader[reader.GetOrdinal("Avatar")] as byte[],
                            Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : (Genders?)reader.GetString(reader.GetOrdinal("Gender")).First()
                        };
                    }
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Sets user info given id and UserInfo object
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="userInfo">UserInfo object representing info</param>
        /// <exception cref="ArgumentException">Throws if <paramref name="userId"/> is invalid</exception>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="userInfo"/> is null</exception>
        public void SetUserInfo(int userId, UserInfo userInfo)
        {
            if (userId == 0)
                throw new ArgumentException(nameof(userId));
            if (userInfo == null)
                throw new ArgumentNullException(nameof(userInfo));

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // check if entry of the user exists
                    if (SqlHelper.DoesFieldValueExist(connection, "UserInfos", "UserID", userId, SqlDbType.Int))
                    {
                        command.CommandText =
                            "UPDATE [UserInfos] SET [LastName] = @lastName, [FirstName] = @firstName, " +
                            "[Phone] = @phone, [Email] = @email, [DateOfBirth] = @dateOfBirth, " +
                            "[Avatar] = @avatar, [Gender] = @gender WHERE [UserID] = @userId";
                    }
                    else
                    {
                        command.CommandText =
                            "INSERT INTO [UserInfos] ([UserID], [LastName], [FirstName], [Phone], [Email], [DateOfBirth], " +
                            "[Avatar], [Gender]) VALUES (@userId, @lastName, @firstName, @phone, @email, @dateOfBirth, @avatar, @gender)";
                    }

                    command.Parameters.AddWithValue("@userId", userId);

                    if (userInfo.LastName == null)
                    {
                        command.Parameters.AddWithValue("@lastName", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@lastName", userInfo.LastName);
                    }

                    if (userInfo.FirstName == null)
                    {
                        command.Parameters.AddWithValue("@firstName", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@firstName", userInfo.FirstName);
                    }

                    if (userInfo.Phone == null)
                    {
                        command.Parameters.AddWithValue("@phone", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@phone", userInfo.Phone);
                    }

                    if (userInfo.Email == null)
                    {
                        command.Parameters.AddWithValue("@email", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@email", userInfo.Email);
                    }

                    if (userInfo.DateOfBirth == null)
                    {
                        command.Parameters.AddWithValue("@dateOfBirth", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@dateOfBirth", userInfo.DateOfBirth);
                    }

                    if (userInfo.Avatar == null)
                    {
                        command.Parameters.Add(
                            new SqlParameter("@avatar", SqlDbType.VarBinary) {Value = DBNull.Value});
                    }
                    else
                    {
                        var avatar =
                            new SqlParameter("@avatar", SqlDbType.VarBinary, userInfo.Avatar.Length)
                                {Value = userInfo.Avatar};
                        command.Parameters.Add(avatar);
                    }

                    if (userInfo.Gender == null)
                    {
                        command.Parameters.AddWithValue("@gender", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.Add(
                            new SqlParameter("@gender", SqlDbType.Char, 1) {Value = (char) userInfo.Gender});
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                        NLogger.Logger.Trace("DB:Inserted:{0}:Object:{1} WHERE UserID:{2}", "[UserInfos]", userInfo, userId);
                    }
                    catch (SqlException)
                    {
                        throw new ArgumentException(nameof(userId));
                    }
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Persists(updates) the user in the DB
        /// </summary>
        /// <param name="user">User object to be persisted</param>
        /// <returns>Persisted user(id may have changed)</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="user"/> is null</exception>
        /// <exception cref="ArgumentException">Throws if id is invalid</exception>
        /// <exception cref="UserAlreadyExistsException">Throws if tried to persist a new user and username is already taken</exception>
        public User PersistUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            // Check for default user change
            if (user.Id == 0)
                throw new ArgumentException(nameof(user.Id));

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if id already exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", user.Id, SqlDbType.Int))
                    throw new ArgumentException(nameof(user.Id));
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [Users] SET [Username] = @userName WHERE [ID] = @userId";

                    command.Parameters.AddWithValue("@userId", user.Id);
                    command.Parameters.AddWithValue("@userName", user.Username);

                    try
                    {
                        command.ExecuteNonQuery();
                        NLogger.Logger.Trace("DB:Updated:{0}:VALUES (Username:{1}) WHERE UserID:{2}", "[Users]", user.Username, user.Id);
                    }
                    catch (SqlException)
                    {
                        throw new UserAlreadyExistsException();
                    }

                    if (user.UserInfo != null)
                        SetUserInfo(user.Id, user.UserInfo);
                    return user;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Sets a new password for the user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="newPassword">The new password of the user</param>
        /// <exception cref="ArgumentException">Throws if <paramref name="userId"/> is invalid</exception>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="newPassword"/> is null or empty</exception>
        public void SetPassword(int userId, string newPassword)
        {
            if (userId == 0)
                throw new ArgumentException();
            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    throw new ArgumentException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [Users] SET [Password] = @password WHERE [ID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@password", newPassword);

                    command.ExecuteNonQuery();
                    NLogger.Logger.Trace("DB:Updated:{0}:VALUES (Password:x) WHERE UserID:{1}", "[Users]", userId);
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Returns user's password hash
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>The password hash</returns>
        public string GetPassword(int userId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    throw new ArgumentException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [Password] FROM [Users] WHERE [ID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);

                    return (string)command.ExecuteScalar();
                }
            }
        }
    }
}
