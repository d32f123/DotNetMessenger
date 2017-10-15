using System;
using System.Data;
using System.Data.SqlClient;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class UsersRepository : IUsersRepository
    {
        private readonly string _connectionString;
        public IChatsRepository ChatsRepository { get; set; }
        

        public UsersRepository(string connectionString, IChatsRepository chatsRepository)
        {
            _connectionString = connectionString;
            ChatsRepository = chatsRepository;
        }

        public UsersRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public User CreateUser(string userName, string hash)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(hash))
                return null;
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (SqlHelper.DoesFieldValueExist(connection, "Users", "Username", userName, SqlDbType.VarChar, userName.Length))
                    return null;
                using (var transaction = connection.BeginTransaction())
                {
                    User user;
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO [Users] ([Username], [Password]) OUTPUT INSERTED.[ID] VALUES (@userName, @hash)";

                        command.Parameters.AddWithValue("@userName", userName);
                        command.Parameters.AddWithValue("@hash", hash);

                        var id = (int) command.ExecuteScalar();
                        user = new User {Chats = null, Hash = hash, Id = id, UserInfo = null, Username = userName};
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO [UserInfos] ([UserID], [LastName], [FirstName], [Phone], [Email], [DateOfBirth], [Avatar]) VALUES " +
                            "(@userId, NULL, NULL, NULL, NULL, NULL, NULL)";

                        command.Parameters.AddWithValue("@userId", user.Id);

                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();

                    return user;
                }
            }
        }

        public void DeleteUser(int userId)
        {
            // Default (deleted) user check
            if (userId == 0)
                return;
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                //Check if id exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    return;
                // Just delete the user, everything else will cascade delete
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public User GetUser(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Username], [Password] FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);

                    var user = new User();
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        user.Id = reader.GetInt32(reader.GetOrdinal("ID"));
                        user.Hash = reader.GetString(reader.GetOrdinal("Password"));
                        user.Username = reader.GetString(reader.GetOrdinal("Username"));
                    }
                    user.Chats = ChatsRepository.GetUserChats(user.Id);
                    user.UserInfo = GetUserInfo(user.Id);
                    return user;
                }
            }
        }

        public void DeleteUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int)
                    || !SqlHelper.DoesFieldValueExist(connection, "UserInfos", "UserID", userId, SqlDbType.Int))
                    return;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [UserInfos] WHERE [UserID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        } 

        public UserInfo GetUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "UserInfos", "UserID", userId, SqlDbType.Int))
                    return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [LastName], [FirstName], [Phone], [Email], [DateOfBirth], [Avatar]" +
                                          "FROM [UserInfos] WHERE [UserID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return new UserInfo
                        {
                            LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                            Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader[reader.GetOrdinal("Avatar")] as byte[]
                        };
                    }
                }
            }
        }

        public void SetUserInfo(int userId, UserInfo userInfo)
        {
            if (userId == 0)
                return;
            if (userInfo == null)
                return;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    return;
                using (var command = connection.CreateCommand())
                {
                    if (SqlHelper.DoesFieldValueExist(connection, "UserInfos", "UserID", userId, SqlDbType.Int))
                    {
                        command.CommandText = "UPDATE [UserInfos] SET [LastName] = @lastName, [FirstName] = @firstName, " +
                                          "[Phone] = @phone, [Email] = @email, [DateOfBirth] = @dateOfBirth, " +
                                          "[Avatar] = @avatar WHERE [UserID] = @userId";
                    }
                    else
                    {
                        command.CommandText =
                            "INSERT INTO [UserInfos] ([UserID], [LastName], [FirstName], [Phone], [Email], [DateOfBirth], " +
                            "[Avatar]) VALUES (@userId, @lastName, @firstName, @phone, @email, @dateOfBirth, @avatar)";
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

                    command.Parameters.AddWithValue("@dateOfBirth", userInfo.DateOfBirth);

                    if (userInfo.Avatar == null)
                    {
                        command.Parameters.Add(new SqlParameter("@avatar", SqlDbType.VarBinary) {Value = DBNull.Value});
                    }
                    else
                    {
                        var avatar =
                            new SqlParameter("@avatar", SqlDbType.VarBinary, userInfo.Avatar.Length)
                                {Value = userInfo.Avatar};
                        command.Parameters.Add(avatar);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        public User PersistUser(User user)
        {
            if (user == null)
                return null;
            // Check for default user change
            if (user.Id == 0)
                return null;
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var idExists = SqlHelper.DoesFieldValueExist(connection, "Users", "ID", user.Id, SqlDbType.Int);
                // If ID does not exist, but username exists, then we cannot create a new user!
                if (!idExists && SqlHelper.DoesFieldValueExist(connection, "Users", "Username", user.Username, SqlDbType.VarChar,
                        user.Username.Length))
                    return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = idExists ? 
                        "UPDATE [Users] SET [Username] = @userName, [Password] = @password WHERE [ID] = @userId" 
                        : "INSERT INTO [Users] ([Username], [Password]) OUTPUT INSERTED.[ID] VALUES (@userName, @password)";

                    if (idExists)
                        command.Parameters.AddWithValue("@userId", user.Id);
                    command.Parameters.AddWithValue("@userName", user.Username);
                    command.Parameters.AddWithValue("@password", user.Hash);

                    if (idExists)
                        command.ExecuteNonQuery();
                    else
                        user.Id = (int) command.ExecuteScalar();    

                    if (user.UserInfo != null)
                        SetUserInfo(user.Id, user.UserInfo);
                    return user;
                }

            }
        }

        public void SetPassword(int userId, string newHash)
        {
            if (userId == 0)
                return;
            if (string.IsNullOrEmpty(newHash))
                return;
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    return;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [Users] SET [Password] = @password WHERE [ID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@password", newHash);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
