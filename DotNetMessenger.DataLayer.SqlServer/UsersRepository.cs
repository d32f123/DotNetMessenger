using System.Data;
using System.Data.SqlClient;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class UsersRepository : IUsersRepository
    {
        private readonly string _connectionString;
        private readonly IChatsRepository _chatsRepository;

        public UsersRepository(string connectionString, IChatsRepository chatsRepository)
        {
            _connectionString = connectionString;
            _chatsRepository = chatsRepository;
        }

        public User CreateUser(string userName, string hash)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

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
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Just delete the user, everything else will cascade delete
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [UserInfos] WHERE [UserID] = @userId";
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

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Username], [Password] FROM [Users] WHERE [ID] = @userId";
                    command.Parameters.AddWithValue("@userId", userId);

                    var user = new User();
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        user.Id = reader.GetInt32(reader.GetOrdinal("[ID]"));
                        user.Hash = reader.GetString(reader.GetOrdinal("[Password]"));
                        user.Username = reader.GetString(reader.GetOrdinal("[Username]"));
                    }
                    user.Chats = _chatsRepository.GetUserChats(user.Id);
                    user.UserInfo = GetUserInfo(user.Id);
                    return user;
                }
            }
        }

        public UserInfo GetUserInfo(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

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
                            LastName = reader.GetString(reader.GetOrdinal("[LastName]")),
                            FirstName = reader.GetString(reader.GetOrdinal("[FirstName]")),
                            Phone = reader.GetString(reader.GetOrdinal("[Phone]")),
                            Email = reader.GetString(reader.GetOrdinal("[Email]")),
                            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("[DateOfBirth]")),
                            Avatar = reader[reader.GetOrdinal("[Avatar]")] as byte[]
                        };
                    }
                }
            }
        }

        public void SetUserInfo(int userId, UserInfo userInfo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    /* TODO: CHECK IF NO USERINFO EXISTS, INSERT IF YES */
                    command.CommandText = "UPDATE [UserInfos] SET [LastName] = @lastName, [FirstName] = @firstName, " +
                                          "[Phone] = @phone, [Email] = @email, [DateOfBirth] = @dateOfBirth, " +
                                          "[Avatar] = @avatar WHERE [UserID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@lastName", userInfo.LastName);
                    command.Parameters.AddWithValue("@firstName", userInfo.FirstName);
                    command.Parameters.AddWithValue("@phone", userInfo.Phone);
                    command.Parameters.AddWithValue("@email", userInfo.Email);
                    command.Parameters.AddWithValue("@dateOfBirth", userInfo.DateOfBirth);

                    var avatar =
                        new SqlParameter("@avatar", SqlDbType.VarBinary, userInfo.Avatar.Length)
                            { Value = userInfo.Avatar };
                    command.Parameters.Add(avatar);

                    command.ExecuteNonQuery();
                }
            }
        }

        public User PersistUser(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    
                    command.CommandText =
                        "UPDATE [Users] SET [Username] = @userName, [Password] = @password WHERE [ID] = @userId";

                    command.Parameters.AddWithValue("@userId", user.Id);
                    command.Parameters.AddWithValue("@userName", user.Username);
                    command.Parameters.AddWithValue("@password", user.Hash);

                    command.ExecuteNonQuery();
                    
                    if (user.UserInfo != null)
                        SetUserInfo(user.Id, user.UserInfo);
                    return user;
                }

            }
        }

        public ChatUserInfo GetChatSpecificInfo(int userId, int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var chatUserInfo = new ChatUserInfo();
                int roleId;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [Nickname], [UserRole] FROM [ChatUserInfos] WHERE [UserID] = @userId AND [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@chatId", chatId);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        chatUserInfo.Nickname = reader.GetString(reader.GetOrdinal("[Nickname]"));
                        roleId = reader.GetInt32(reader.GetOrdinal("[UserRole]"));
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [Name], [ReadPerm], [WritePerm], [ChatInfoPerm], [AttachPerm], [ManageUsersPerm]" +
                        " FROM [UserRoles] WHERE [ID] = @roleId";
                    command.Parameters.AddWithValue("@roleId", roleId);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        chatUserInfo.Role = new UserRole
                        {
                            RoleType = (UserRoles) roleId,
                            ReadPerm = reader.GetBoolean(reader.GetOrdinal("[ReadPerm]")),
                            WritePerm = reader.GetBoolean(reader.GetOrdinal("[WritePerm]")),
                            ChatInfoPerm = reader.GetBoolean(reader.GetOrdinal("[ChatInfoPerm]")),
                            AttachPerm = reader.GetBoolean(reader.GetOrdinal("[AttachPerm]")),
                            ManageUsersPerm = reader.GetBoolean(reader.GetOrdinal("[ManageUsersPerm]")),
                            RoleName = reader.GetString(reader.GetOrdinal("[Name]"))
                        };
                    }
                }

                return chatUserInfo;
            }
        }

        public void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    /* TODO: CHECK FOR MISSING CHAT USER INFO, IF MISSING, ADD */
                    command.CommandText = "UPDATE [ChatUserInfos] SET [Nickname] = @nickName";
                    if (updateRole)
                        command.CommandText += ", [UserRole] = @userRole";
                    command.CommandText += " WHERE [UserID] = @userId AND [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@nickName", userInfo.Nickname);
                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@userId", userId);
                    if (updateRole)
                    {
                        command.Parameters.AddWithValue("@userRole", (int)(userInfo.Role?.RoleType ?? UserRoles.Regular));
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        public ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    /* TODO: CHECK FOR MISSING CHAT USER INFO, IF MISSING, ADD */
                    command.CommandText = "UPDATE [ChatUserInfos] SET [UserRole] = @userRole " +
                                          "WHERE [UserID] = @userId AND [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@userRole", (int) userRole);
                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@userId", userId);

                    command.ExecuteNonQuery();
                    return GetChatSpecificInfo(userId, chatId);
                }
            }
        }

        public void SetPassword(int userId, string newHash)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

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
