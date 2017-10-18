using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Transactions;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.DataLayer.SqlServer.ModelProxies;


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

        /// <inheritdoc />
        /// <summary>
        /// Adds a user to a given chat
        /// </summary>
        /// <param name="chatId">The chat id</param>
        /// <param name="userId">The user id</param>
        /// <exception cref="ArgumentException">Throws if <paramref name="chatId"/> or <paramref name="userId"/> are invalid</exception>
        /// <exception cref="ChatTypeMismatchException">Throws if chat is of dialog type</exception>
        public void AddUser(int chatId, int userId)
        {
            // check for default user
            if (userId == 0)
                throw new ArgumentException();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "[ID]", chatId, "[ChatType]",
                        (int) ChatTypes.Dialog))
                        throw new ChatTypeMismatchException();

                    // make an entry in chatusers table
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [ChatUsers]([UserID], [ChatID]) VALUES " +
                                              "(@userId, @chatId)";
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@chatId", chatId);

                        command.ExecuteNonQuery();
                    }
                    // make an entry in chatuserinfos table
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
            catch (SqlException)
            {
                throw new ArgumentException();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Adds a list of users to a given chat
        /// </summary>
        /// <param name="chatId">The chat id</param>
        /// <param name="newUsers">The list of user ids</param>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="newUsers"/> is null</exception>
        /// <exception cref="ArgumentException">Throws if any of the ids are invalid</exception>
        /// <exception cref="ChatTypeMismatchException">Throws if chat is of dialog type</exception>
        public void AddUsers(int chatId, IEnumerable<int> newUsers)
        {
            if (newUsers == null)
                throw new ArgumentNullException();
            var idList = newUsers as int[] ?? newUsers.ToArray();
            if (idList.Contains(0))
                throw new ArgumentException();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "[ID]", chatId, "[ChatType]",
                        (int) ChatTypes.Dialog))
                        throw new ChatTypeMismatchException();
                    using (var command = connection.CreateCommand())
                    {
                        // call a stored procedure
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
            catch (SqlException)
            {
                throw new ArgumentException();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets a chat given its id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Null if no chat exists, otherwise chat object representing given entity</returns>
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
                        // if select returned no rows
                        if (!reader.HasRows)
                            return null;
                        reader.Read();
                        return new ChatSqlProxy
                        {
                            Id = chatId,
                            ChatType = (ChatTypes) reader.GetInt32(reader.GetOrdinal("ChatType")),
                            CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorID")),
                        };
                    }
                }
            }
        }
        /// <summary>
        /// Creates a chat of type <paramref name="chatType"/> with given <paramref name="title"/> and <paramref name="members"/>
        /// </summary>
        /// <param name="members">The members of the chat</param>
        /// <param name="title">The title of the new chat</param>
        /// <param name="chatType">The type of the new chat</param>
        /// <returns>Chat object</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="members"/> is null or empty</exception>
        /// <exception cref="ArgumentException">Throws if any of the <paramref name="members"/> ids is null</exception>
        private Chat CreateChat(IEnumerable<int> members, string title, ChatTypes chatType)
        {
            if (members == null)
                throw new ArgumentNullException();
            var membersList = members as int[] ?? members.ToArray();
            if (membersList.Length == 0)
                throw new ArgumentNullException();
            // check for default user
            if (membersList.Contains(0))
                throw new ArgumentException();

            try
            {
                using (var scope = new TransactionScope())
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var chatInfo = new ChatInfo {Title = title};
                        var userIds = members as int[] ?? membersList.ToArray();

                        var chat = new ChatSqlProxy
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
                        return chat;
                    }
                }
            }
            catch
            {
                throw new ArgumentException();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Creates a group chat with given <paramref name="title" /> and <paramref name="members" />
        /// </summary>
        /// <param name="members">The members of the chat</param>
        /// <param name="title">The title of the chat</param>
        /// <returns>Chat object</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="members"/> or <paramref name="title"/> are null or empty</exception>
        /// <exception cref="ArgumentException">Throws if any of the <paramref name="members"/> ids is null</exception>
        public Chat CreateGroupChat(IEnumerable<int> members, string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException();
            return CreateChat(members, title, ChatTypes.GroupChat);
        }
        /// <inheritdoc />
        /// <summary>
        /// Creates a dialog chat with <paramref name="member1"/> and <paramref name="member2"/>
        /// </summary>
        /// <param name="member1">The first participant</param>
        /// <param name="member2">The second participant</param>
        /// <returns>Chat object</returns>
        /// <exception cref="ArgumentException">Throws if any of the members ids is invalid</exception>
        public Chat CreateDialog(int member1, int member2)
        {
            return CreateChat(new[] {member1, member2}, string.Empty, ChatTypes.Dialog);
        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes a chat given its id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <exception cref="T:System.ArgumentException">Throws if id is invalid</exception>
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

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException();
                }
            }
            
        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes info of the chat given its id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <exception cref="ArgumentException">Throws if id is invalid</exception>
        public void DeleteChatInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [ChatInfos] WHERE [ChatInfos].[ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException();
                }
            }
            
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets the list of chats the user is in
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>A list of chats the user is in</returns>
        /// <exception cref="T:System.ArgumentException">Id is invalid</exception>
        public IEnumerable<Chat> GetUserChats(int userId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Users", "ID", userId, SqlDbType.Int))
                    throw new ArgumentException();
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
                            yield return new ChatSqlProxy
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                ChatType = (ChatTypes)reader.GetInt32(reader.GetOrdinal("ChatType")),
                                CreatorId = reader.GetInt32(reader.GetOrdinal("CreatorID")),
                            };
                        }
                    }
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets the list of users that are in the chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>List of users(can be empty)</returns>
        /// <exception cref="T:System.ArgumentException">Throws if <paramref name="chatId" /> is invalid</exception>
        public IEnumerable<User> GetChatUsers(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    throw new ArgumentException();
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
        /// <inheritdoc />
        /// <summary>
        /// Kicks a user from a given chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userId">The id of the user</param>
        /// <exception cref="T:System.ArgumentException">Throws if any of the ids are invalid or there is no such user in chat</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.UserIsCreatorException">Throws if tried to kick creator of the chat</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.ChatTypeMismatchException">Throws if chat is dialog</exception>
        public void KickUser(int chatId, int userId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if user is creator
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "CreatorID", userId))
                    throw new UserIsCreatorException();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "DELETE FROM [ChatUsers] WHERE [ChatUsers].[ChatID] = @chatId AND [ChatUsers].[UserID] = @userId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@userId", userId);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException();
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Kicks a list of users from a given chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="kickedUsers">List of user ids</param>
        /// <exception cref="T:System.ArgumentNullException">Throws if <paramref name="kickedUsers" /> is null</exception>
        /// <exception cref="T:System.ArgumentException">Throws if any of the ids are invalid or there are no such user in chat</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.UserIsCreatorException">Throws if tried to kick creator of the chat</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.ChatTypeMismatchException">Throws if chat is dialog</exception>
        public void KickUsers(int chatId, IEnumerable<int> kickedUsers)
        {
            if (kickedUsers == null)
                throw new ArgumentNullException();
            var idList = kickedUsers as int[] ?? kickedUsers.ToArray();
            if (idList.Contains(0))
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if creator is in range
                if (SqlHelper.IsSelectedRowFieldInRange(connection, "Chats", "ID", chatId, "CreatorID", idList))
                    throw new UserIsCreatorException();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "KickUsersFromChat";

                    var parameter = command.Parameters.AddWithValue("@IDList", SqlHelper.IdListToDataTable(idList));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";
                    command.Parameters.AddWithValue("@ChatID", chatId);
                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException();
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Sets info for the chat
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="info">The information to be set</param>
        /// <exception cref="T:System.ArgumentNullException">Throws if <paramref name="info" /> is null</exception>
        /// <exception cref="T:System.ArgumentException">Throws if <paramref name="chatId" /> is invalid</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.ChatTypeMismatchException">Throws if chat is dialog</exception>
        public void SetChatInfo(int chatId, ChatInfo info)
        {
            if (info == null)
                throw new ArgumentNullException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int) ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();
                // check if the chat exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    throw new ArgumentException();
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
        /// <inheritdoc />
        /// <summary>
        /// Gets chat information given chat id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Null if no info, else the <see cref="T:DotNetMessenger.Model.ChatInfo" /> object</returns>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.ChatTypeMismatchException">Throws if chat is dialog</exception>
        /// <exception cref="ArgumentException">Throws if chat does not exist</exception>
        public ChatInfo GetChatInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if the chat exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    throw new ArgumentException();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();

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
        /// <inheritdoc />
        /// <summary>
        /// Sets a new creator for the chat (the user must already be in the chat)
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="newCreator">The id of the creator</param>
        /// <exception cref="T:System.ArgumentException">One of the ids is invalid</exception>
        /// <exception cref="T:DotNetMessenger.DataLayer.SqlServer.Exceptions.ChatTypeMismatchException">Throws if the chat is dialog</exception>
        public void SetCreator(int chatId, int newCreator)
        {
            if (newCreator == 0)
                throw new ArgumentException();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // check if the chat exists
                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    throw new ArgumentException();
                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();
                // check if user is in chat
                if (!SqlHelper.DoesDoubleKeyExist(connection, "ChatUsers", "UserID", newCreator, "ChatID", chatId))
                    throw new ArgumentException();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [Chats] SET [CreatorID] = @newCreator WHERE [ID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@newCreator", newCreator);

                    command.ExecuteNonQuery();
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets information about the user specific to a given chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Null if no info, else <see cref="T:DotNetMessenger.Model.ChatUserInfo" /> object</returns>
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
                        reader.Read();
                        chatUserInfo.Nickname = reader.IsDBNull(reader.GetOrdinal("Nickname"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Nickname"));
                        roleId = reader.GetInt32(reader.GetOrdinal("UserRole"));
                    }
                }

                // get permissions
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
        /// <inheritdoc />
        /// <summary>
        /// Sets information about the user speicifc to a given chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userInfo">The information about the user</param>
        /// <param name="updateRole">Whether to update the user role or not</param>
        /// <exception cref="T:System.ArgumentNullException">Throws if <paramref name="userInfo" /> is null OR <paramref name="updateRole" /> 
        /// is true and <see cref="T:DotNetMessenger.Model.UserRole" /> is null</exception>
        /// <exception cref="T:System.ArgumentException">Throws if id is invalid</exception>
        public void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false)
        {
            if (userInfo == null)
                throw new ArgumentNullException();
            if (updateRole && userInfo.Role == null)
                throw new ArgumentNullException();
            if (userId == 0)
                throw new ArgumentException();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // is there an entry in the table already?
                    var infoExists = SqlHelper.DoesDoubleKeyExist(connection, "ChatUserInfos", "[UserID]", userId,
                        "[ChatID]", chatId);
                    using (var command = connection.CreateCommand())
                    {
                        var sb = new StringBuilder();
                        if (infoExists)
                        {
                            // if there is already an entry, update it
                            sb.Append("UPDATE [ChatUserInfos] SET [Nickname] = @nickName");
                            if (updateRole)
                                sb.Append(", [UserRole] = @userRole");
                            sb.Append(" WHERE [UserID] = @userId AND [ChatID] = @chatId");
                        }
                        else
                        {
                            // else create a new one
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
                            command.Parameters.AddWithValue("@userRole", (int) userInfo.Role.RoleType);
                        }

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                throw new ArgumentException();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Sets role to the user and returns their information for the chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="userRole">The role to set</param>
        /// <returns><see cref="T:DotNetMessenger.Model.ChatUserInfo" /> object</returns>
        /// <exception cref="T:System.ArgumentException">Throws if id is invalid</exception>
        public ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole)
        {
            if (userId == 0)
                throw new ArgumentException();
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
                // means that there is no such chat id or user id
                throw new ArgumentException();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes information about a user specific to a given chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        /// <exception cref="T:System.ArgumentNullException">Throws if no data found about the user</exception>
        /// <exception cref="T:System.ArgumentException">Throws if any of the ids are invalid or if the user is not in chat</exception>
        public void DeleteChatSpecificInfo(int userId, int chatId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if user is in chat
                if (!SqlHelper.DoesDoubleKeyExist(connection, "ChatUsers", "UserID", userId, "ChatID", chatId))
                    throw new ArgumentException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [ChatUserInfos] WHERE [UserID] = @userId AND [ChatID] = @chatId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@chatId", chatId);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentNullException();
                }
            }
        }
    }
}