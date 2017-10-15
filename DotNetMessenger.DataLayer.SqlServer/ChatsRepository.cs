using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Transactions;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;


namespace DotNetMessenger.DataLayer.SqlServer
{
    public class ChatsRepository : IChatsRepository
    {
        private readonly string _connectionString;
        public IUsersRepository UsersRepository { get; set; }

        public ChatsRepository(string connectionString, IUsersRepository usersRepository)
        {
            _connectionString = connectionString;
            UsersRepository = usersRepository;
        }

        public ChatsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddUser(int chatId, int userId)
        {
            // check for default user
            if (userId == 0)
                return;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "[ID]", chatId, "[ChatType]",
                        (int) ChatTypes.Dialog))
                        return;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [ChatUsers]([UserID], [ChatID]) VALUES " +
                                              "(@userId, @chatId)";
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@chatId", chatId);

                        command.ExecuteNonQuery();
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            "INSERT INTO [ChatUserInfos] ([UserID], [ChatID]) VALUES (@userId, @chatId)";

                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@chatId", chatId);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException) {}
        }

        public void AddUsers(int chatId, IEnumerable<int> newUsers)
        {
            if (newUsers == null)
                return;
            var idList = newUsers as int[] ?? newUsers.ToArray();
            if (idList.Contains(0))
                return;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "[ID]", chatId, "[ChatType]",
                        (int)ChatTypes.Dialog))
                        return;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "AddUsersToChat";

                        var parameter =
                            command.Parameters.AddWithValue("@IDList", SqlHelper.IdListToDataTable(idList));
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "IdListType";
                        command.Parameters.AddWithValue("@ChatID", chatId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException) { }
        }

        public Chat GetChat(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [ChatType], [CreatorID] FROM [Chats] WHERE [ID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return null;
                        reader.Read();
                        return new Chat
                        {
                            Id = chatId,
                            ChatType = (ChatTypes) reader.GetInt32(reader.GetOrdinal("ChatType")),
                            CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorID")),
                            Info = GetChatInfo(chatId),
                            Users = GetChatUsers(chatId)
                        };
                    }
                }
            }
        }

        private Chat CreateChat(IEnumerable<int> members, string title, ChatTypes chatType)
        {
            if (members == null)
                return null;
            var membersList = members as int[] ?? members.ToArray();
            // check for default user
            if (membersList.Contains(0))
                return null;

            try
            {
                using (var scope = new TransactionScope())
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var chatInfo = new ChatInfo {Title = title};
                        var userIds = members as int[] ?? membersList.ToArray();

                        var chat = new Chat
                        {
                            ChatType = chatType,
                            CreatorId = userIds[0],
                            Info = chatInfo
                        };

                        // Create new chat
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO [Chats] ([ChatType], [CreatorID]) OUTPUT INSERTED.[ID] VALUES (@chatType, @creatorId)";

                            command.Parameters.AddWithValue("@chatType", (int) (chat.ChatType));
                            command.Parameters.AddWithValue("@creatorId", chat.CreatorId);

                            chat.Id = (int) command.ExecuteScalar();
                        }

                        // Add users to chat
                        foreach (var userId in userIds)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText =
                                    "INSERT INTO [ChatUsers] ([UserID], [ChatID]) VALUES (@userId, @chatId)";

                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@chatId", chat.Id);

                                command.ExecuteNonQuery();
                            }
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText =
                                    "INSERT INTO [ChatUserInfos] ([UserID], [ChatID]) VALUES (@userId, @chatId)";

                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@chatId", chat.Id);

                                command.ExecuteNonQuery();
                            }
                        }

                        // Add chat title
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO [ChatInfos] ([ChatID], [Title], [Avatar]) VALUES (@chatId, @title, NULL)";

                            command.Parameters.AddWithValue("@chatId", chat.Id);
                            command.Parameters.AddWithValue("@title", chatInfo.Title);

                            command.ExecuteNonQuery();
                        }

                        scope.Complete();
                        chat.Users = userIds.Select(x => UsersRepository.GetUser(x));
                        return chat;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public Chat CreateGroupChat(IEnumerable<int> members, string title)
        {
            return CreateChat(members, title, ChatTypes.GroupChat);
        }

        public Chat CreateDialog(int member1, int member2)
        {
            return CreateChat(new[] {member1, member2}, string.Empty, ChatTypes.Dialog);
        }

        public void DeleteChat(int chatId)
        {
            
            using (var connection = new SqlConnection(_connectionString))
            {
                    
                connection.Open();
                // Delete chat, ChatUsers entries should cascade
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [Chats] WHERE [Chats].[ID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    command.ExecuteNonQuery();
                }
            }
            
        }

        public void DeleteChatInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [ChatInfos] WHERE [ChatInfos].[ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    command.ExecuteNonQuery();
                }
            }
            
        }

        public IEnumerable<Chat> GetUserChats(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // get chat ids
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [Chats].[ID] ID, [Chats].[ChatType] ChatType, [Chats].[CreatorID] CreatorID " +
                        "FROM [ChatUsers], [Chats] WHERE [Chats].[ID] = [ChatUsers].[ChatID] AND [ChatUsers].[UserID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            yield break;
                        while (reader.Read())
                        {
                            var chatId = reader.GetInt32(reader.GetOrdinal("ID"));
                            yield return new Chat
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                ChatType = (ChatTypes)reader.GetInt32(reader.GetOrdinal("ChatType")),
                                CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorID")),
                                Info = GetChatInfo(chatId),
                                Users = GetChatUsers(chatId)
                            };
                        }
                    }
                }
            }
        }

        public IEnumerable<User> GetChatUsers(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [UserID] FROM [ChatUsers] WHERE [ChatUsers].[ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            yield break;
                        while (reader.Read())
                        {
                            yield return UsersRepository.GetUser(reader.GetInt32(reader.GetOrdinal("UserID")));
                        }
                    }
                }
            }
        }

        public void KickUser(int chatId, int userId)
        {
            if (userId == 0)
                return;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if user is creator
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "CreatorID", userId))
                    return;
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    return;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "DELETE FROM [ChatUsers] WHERE [ChatUsers].[ChatID] = @chatId AND [ChatUsers].[UserID] = @userId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@userId", userId);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void KickUsers(int chatId, IEnumerable<int> kickedUsers)
        {
            if (kickedUsers == null)
                return;
            var idList = kickedUsers as int[] ?? kickedUsers.ToArray();
            if (idList.Contains(0))
                return;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if creator is in range
                if (SqlHelper.IsSelectedRowFieldInRange(connection, "Chats", "ID", chatId, "CreatorID", idList))
                    return;
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    return;
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "KickUsersFromChat";

                    var parameter = command.Parameters.AddWithValue("@IDList", SqlHelper.IdListToDataTable(idList));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";
                    command.Parameters.AddWithValue("@ChatID", chatId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetChatInfo(int chatId, ChatInfo info)
        {
            if (info == null)
                return;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int) ChatTypes.Dialog))
                    return;
                // check if the chat exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    return;
                // check if info already exists (should then overwrite)
                var infoExists = SqlHelper.DoesFieldValueExist(connection, "ChatInfos", "ChatID", chatId, SqlDbType.Int);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = infoExists
                        ? "UPDATE [ChatInfos] SET [Title] = @title, [Avatar] = @avatar WHERE [ChatID] = @chatId"
                        : "INSERT INTO [ChatInfos] ([ChatID], [Title], [Avatar]) VALUES (@chatId, @title, @avatar)";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    if (info.Title == null)
                        command.Parameters.AddWithValue("@title", DBNull.Value);
                    else
                        command.Parameters.AddWithValue("@title", info.Title);

                    var avatar =
                        info.Avatar != null
                            ? new SqlParameter("@avatar", SqlDbType.VarBinary, info.Avatar.Length)
                                {Value = info.Avatar}
                            : new SqlParameter("@avatar", SqlDbType.VarBinary) {Value = DBNull.Value};
                    command.Parameters.Add(avatar);

                    command.ExecuteNonQuery();
                }
            }
        }

        public ChatInfo GetChatInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    return null;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [Title], [Avatar] FROM [ChatInfos] WHERE [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return null;
                        reader.Read();
                        var avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader[reader.GetOrdinal("Avatar")] as byte[];
                        return new ChatInfo
                        {
                            Avatar = avatar,
                            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title"))
                        };
                    }
                }
            }
        }

        public void SetCreator(int chatId, int newCreator)
        {
            if (newCreator == 0)
                return;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    // check if chat is dialog
                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                        return;
                    // check if user is in chat
                    if (!SqlHelper.DoesDoubleKeyExist(connection, "ChatUsers", "UserID", newCreator, "ChatID", chatId))
                        return;

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE [Chats] SET [CreatorID] = @newCreator WHERE [ID] = @chatId";

                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@newCreator", newCreator);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException) { }
        }

        public ChatUserInfo GetChatSpecificInfo(int userId, int chatId)
        {
            if (userId == 0)
                return null;
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
                        // Regular user (no info)
                        if (!reader.HasRows)
                        {
                            return null;
                        }
                        else
                        {
                            reader.Read();
                            chatUserInfo.Nickname = reader.IsDBNull(reader.GetOrdinal("Nickname"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Nickname"));
                            roleId = reader.GetInt32(reader.GetOrdinal("UserRole"));
                        }
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
                            RoleType = (UserRoles)roleId,
                            ReadPerm = reader.GetBoolean(reader.GetOrdinal("ReadPerm")),
                            WritePerm = reader.GetBoolean(reader.GetOrdinal("WritePerm")),
                            ChatInfoPerm = reader.GetBoolean(reader.GetOrdinal("ChatInfoPerm")),
                            AttachPerm = reader.GetBoolean(reader.GetOrdinal("AttachPerm")),
                            ManageUsersPerm = reader.GetBoolean(reader.GetOrdinal("ManageUsersPerm")),
                            RoleName = reader.GetString(reader.GetOrdinal("Name"))
                        };
                    }
                }

                return chatUserInfo;
            }
        }

        public void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false)
        {
            if (userInfo == null)
                return;
            if (updateRole && userInfo.Role == null)
                return;
            if (userId == 0)
                return;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var infoExists = SqlHelper.DoesDoubleKeyExist(connection, "ChatUserInfos", "[UserID]", userId,
                        "[ChatID]", chatId);
                    using (var command = connection.CreateCommand())
                    {
                        var sb = new StringBuilder();
                        if (infoExists)
                        {
                            sb.Append("UPDATE [ChatUserInfos] SET [Nickname] = @nickName");
                            if (updateRole)
                                sb.Append(", [UserRole] = @userRole");
                            sb.Append(" WHERE [UserID] = @userId AND [ChatID] = @chatId");
                        }
                        else
                        {
                            sb.Append("INSERT INTO [ChatUserInfos] ([UserID], [ChatID], [Nickname]");
                            sb.Append(updateRole ? ", [UserRole]) " : ") ");
                            sb.Append("VALUES (@userId, @chatId, @nickName");
                            sb.Append(updateRole ? ", @userRole)" : ")");
                        }
                        command.CommandText = sb.ToString();

                        command.Parameters.AddWithValue("@nickName", userInfo.Nickname);
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@userId", userId);
                        if (updateRole)
                        {
                            command.Parameters.AddWithValue("@userRole",
                                (int) (userInfo.Role?.RoleType ?? UserRoles.Regular));
                        }

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                // means that chatId is invalid
            }
        }

        public ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole)
        {
            if (userId == 0)
                return null;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var infoExists = SqlHelper.DoesDoubleKeyExist(connection, "ChatUserInfos", "[UserID]", userId,
                        "[ChatID]", chatId);
                    using (var command = connection.CreateCommand())
                    {
                        var sb = new StringBuilder();
                        if (infoExists)
                            sb.Append("UPDATE [ChatUserInfos] SET [UserRole] = @userRole ")
                                .Append("WHERE [UserID] = @userId AND [ChatID] = @chatId");
                        else
                            sb.Append("INSERT INTO [ChatUserInfos] ([UserID], [ChatID], [UserRole]) ")
                                .Append("VALUES (@userId, @chatId, @userRole)");
                        command.CommandText = sb.ToString();

                        command.Parameters.AddWithValue("@userRole", (int) userRole);
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@userId", userId);

                        command.ExecuteNonQuery();
                        return GetChatSpecificInfo(userId, chatId);
                    }
                }
            }
            catch (SqlException)
            {
                // means that there is no such chat id
                return null;
            }
        }

        public void DeleteChatSpecificInfo(int userId, int chatId)
        {
            if (userId == 0)
                return;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [ChatUserInfos] WHERE [UserID] = @userId AND [ChatID] = @chatId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@chatId", chatId);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}