﻿using System;
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
using DotNetMessenger.Logger;


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
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        throw new ArgumentException();
                    }
                }
                NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (UserID:{1}, ChatID:{2})", "[ChatUsers]", userId, chatId);
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

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        throw new ArgumentException();
                    }
                }
                NLogger.Logger.Trace("DB:Called stored procedure {0}: ChatID:{1}", "[AddUsersToChat]", chatId);

            }

        }
        /// <inheritdoc />
        /// <summary>
        /// Gets a chat given its id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Chat object representing given entity</returns>
        /// <exception cref="ArgumentException">Throws if <paramref name="chatId"/> is invalid</exception>
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
                            throw new ArgumentException();
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
        /// <inheritdoc />
        /// <summary>
        /// Gets a dialog between two users
        /// </summary>
        /// <param name="member1">User 1</param>
        /// <param name="member2">User 2</param>
        /// <returns>Dialog between the two users</returns>
        public Chat CreateOrGetDialog(int member1, int member2)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXECUTE CreateOrGet_Dialog @user1, @user2";

                    command.Parameters.AddWithValue("@user1", member1);
                    command.Parameters.AddWithValue("@user2", member2);

                    using (var reader = command.ExecuteReader())
                    {
                        var id = SqlHelper.GetLastResult<int>(reader, "ID");
                        NLogger.Logger.Trace("DB:CreateOrGet:{0}:{1}", member1, member2);
                        return GetChat(id);
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

            using (var scope = new TransactionScope())
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var chatInfo = new ChatInfo {Title = title};

                    var chat = new ChatSqlProxy
                    {
                        ChatType = chatType,
                        CreatorId = membersList[0],
                        Info = chatInfo
                    };

                    // Create new chat
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"DECLARE @T TABLE (ID INT)
                                INSERT INTO [Chats] ([ChatType], [CreatorID]) 
                                OUTPUT INSERTED.[ID] INTO @T VALUES (@chatType, @creatorId)
                               SELECT [ID] FROM @T";

                        command.Parameters.AddWithValue("@chatType", (int) (chat.ChatType));
                        command.Parameters.AddWithValue("@creatorId", chat.CreatorId);

                        try
                        {
                            chat.Id = (int) command.ExecuteScalar();
                        }
                        catch
                        {
                            // means that the creator is invalid
                            throw new ArgumentException();
                        }
                    }

                    NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (ChatType:{1}, CreatorID:{2})", "[Chats]", chat.ChatType, chat.CreatorId);

                    // Add users to chat
                    foreach (var userId in membersList.Skip(1))
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO [ChatUsers] ([UserID], [ChatID]) VALUES (@userId, @chatId)";

                            command.Parameters.AddWithValue("@userId", userId);
                            command.Parameters.AddWithValue("@chatId", chat.Id);
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch
                            {
                                // means that some ids are invalid
                                throw new ArgumentException();
                            }
                        }
                        NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (UserID:{1}, ChatID:{2})", "[ChatUsers]", userId, chat.Id);
                    }

                    if (chatType != ChatTypes.Dialog)
                    {
                        // Add chat title
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO [ChatInfos] ([ChatID], [Title], [Avatar]) VALUES (@chatId, @title, NULL)";

                            command.Parameters.AddWithValue("@chatId", chat.Id);
                            command.Parameters.AddWithValue("@title", chatInfo.Title);

                            command.ExecuteNonQuery();
                        }
                        NLogger.Logger.Trace("DB:Inserted:{0}:VALUES (ChatID:{1}, Title:{2})", "[ChatInfos]", chat.Id,
                            chatInfo.Title);
                        SetChatSpecificRole(membersList[0], chat.Id, UserRoles.Moderator);
                    }

                    scope.Complete();
                    return chat;
                }
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
                NLogger.Logger.Trace("DB:Deleted:{0}:By (ChatID:{1})", "[Chats]", chatId);
            }

        }
        /// <inheritdoc />
        /// <summary>
        /// Deletes info of the chat given its id
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <exception cref="ArgumentException">Throws if id is invalid</exception>
        /// <exception cref="ArgumentNullException">Throws if no info exists</exception>
        public void DeleteChatInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                    throw new ArgumentException();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [ChatInfos] WHERE [ChatInfos].[ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentNullException();
                }
                NLogger.Logger.Trace("DB:Deleted:{0}:By (ChatID:{1})", "[ChatInfos]", chatId);
            }
            
        }
        /// <inheritdoc />
        /// <summary>
        /// Checks if a selected user is in chat
        /// </summary>
        /// <param name="userId">User to check</param>
        /// <param name="chatId">Chat to check</param>
        /// <returns></returns>
        public bool CheckForChatUser(int userId, int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Check_For_ChatUser";

                    command.Parameters.AddWithValue("@userID", userId);
                    command.Parameters.AddWithValue("@chatID", chatId);

                    var ret = (int?)command.ExecuteScalar();
                    return ret != null;
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
                    NLogger.Logger.Trace("DB:Deleted:{0}:By (ChatID:{1}, UserID:{2})", "[ChatUsers]", chatId, userId);
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
                    command.CommandText = "Kick_Users";

                    var parameter = command.Parameters.AddWithValue("@userIds", SqlHelper.IdListToDataTable(idList));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";
                    command.Parameters.AddWithValue("@chatId", chatId);
                    try
                    {
                        if (command.ExecuteNonQuery() == 0)
                            throw new ArgumentException();
                    }
                    catch (Exception e)
                    {
                        NLogger.Logger.Trace(e);
                    }
                    NLogger.Logger.Trace("DB:Called stored procedure:{0}", "[KickUsersFromChat]");
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
                    NLogger.Logger.Trace("DB:Updated:{0}:VALUES (Title:{1}, Avatar:{2}) WHERE ChatID:{3}",
                        "[ChatInfos]", info.Title ?? "", info.Avatar ?? Encoding.UTF8.GetBytes(""), chatId);
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

            using (var scope = new TransactionScope())
            {

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    // check if the chat exists
                    if (!SqlHelper.DoesFieldValueExist(connection, "Chats", "ID", chatId, SqlDbType.Int))
                        throw new ArgumentException();
                    // check if chat is dialog
                    if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType",
                        (int) ChatTypes.Dialog))
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
                        NLogger.Logger.Trace("DB:Updated:{0}:VALUES (CreatorID:{1}) WHERE ChatID:{2}", "[Chats]", newCreator, chatId);
                    }
                }

                SetChatSpecificRole(newCreator, chatId, UserRoles.Moderator);

                scope.Complete();
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets information about the user specific to a given chat
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="chatId">The id of the chat</param>
        /// <returns>Null if no info, else <see cref="T:DotNetMessenger.Model.ChatUserInfo" /> object</returns>
        /// <exception cref="ArgumentException">Throws if userid is invalid or user is not in chat</exception>
        /// <exception cref="ChatTypeMismatchException">Throws if chat is dialog</exception>
        public ChatUserInfo GetChatSpecificInfo(int userId, int chatId)
        {
            if (userId == 0)
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
                if (!SqlHelper.DoesDoubleKeyExist(connection, "ChatUsers", "UserID", userId, "ChatID", chatId))
                    throw new ArgumentException();

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
                            RolePermissions = (reader.GetBoolean(reader.GetOrdinal("WritePerm")) ? RolePermissions.WritePerm : RolePermissions.NaN) |
                                                (reader.GetBoolean(reader.GetOrdinal("ReadPerm")) ? RolePermissions.ReadPerm : RolePermissions.NaN) |
                                                (reader.GetBoolean(reader.GetOrdinal("ChatInfoPerm")) ? RolePermissions.ChatInfoPerm : RolePermissions.NaN) |
                                                (reader.GetBoolean(reader.GetOrdinal("AttachPerm")) ? RolePermissions.AttachPerm : RolePermissions.NaN) |
                                                (reader.GetBoolean(reader.GetOrdinal("ManageUsersPerm")) ? RolePermissions.ManageUsersPerm : RolePermissions.NaN),
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
        /// <exception cref="ChatTypeMismatchException">Throws if chat is dialog</exception>
        public void SetChatSpecificInfo(int userId, int chatId, ChatUserInfo userInfo, bool updateRole = false)
        {
            if (userInfo == null)
                throw new ArgumentNullException();
            if (updateRole && userInfo.Role == null)
                throw new ArgumentNullException();
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();

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

                    try
                    {
                        command.ExecuteNonQuery();
                        NLogger.Logger.Trace(
                            "DB:Updated:{0}:VALUES (Nickname:{3}" + (updateRole ? "UserRole:{4}" : "") +
                            ") WHERE ChatID:{1}, UserID:{2}", "[ChatUserInfos]", userId, chatId, userInfo.Nickname, userInfo.Role.RoleType);
                    }
                    catch (SqlException)
                    {
                        throw new ArgumentException();
                    }
                }
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
        /// <exception cref="ChatTypeMismatchException">Throws if chat is dialog</exception>
        public ChatUserInfo SetChatSpecificRole(int userId, int chatId, UserRoles userRole)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();

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

                    try
                    {
                        command.ExecuteNonQuery();
                        NLogger.Logger.Trace(
                            "DB:Updated:{0}:VALUES (UserRole:{3})) WHERE ChatID:{1}, UserID:{2}",
                            "[ChatUserInfos]", userId, chatId, userRole);

                    }
                    catch (SqlException)
                    {
                        // means that there is no such chat id or user id
                        throw new ArgumentException();
                    }
                    return GetChatSpecificInfo(userId, chatId);
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets the user role given its id
        /// </summary>
        /// <returns>Object representing the user role</returns>
        public UserRole GetUserRole(UserRoles roleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [Name], [ReadPerm], [WritePerm], [ChatInfoPerm], [AttachPerm], [ManageUsersPerm]" +
                                          " FROM [UserRoles] WHERE [ID] = @roleId";
                    command.Parameters.AddWithValue("@roleId", (int)roleId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        reader.Read();
                        return  new UserRole
                        {
                            RoleType = 0,
                            RolePermissions = (reader.GetBoolean(reader.GetOrdinal("WritePerm")) ? RolePermissions.WritePerm : RolePermissions.NaN) |
                                              (reader.GetBoolean(reader.GetOrdinal("ReadPerm")) ? RolePermissions.ReadPerm : RolePermissions.NaN) |
                                              (reader.GetBoolean(reader.GetOrdinal("ChatInfoPerm")) ? RolePermissions.ChatInfoPerm : RolePermissions.NaN) |
                                              (reader.GetBoolean(reader.GetOrdinal("AttachPerm")) ? RolePermissions.AttachPerm : RolePermissions.NaN) |
                                              (reader.GetBoolean(reader.GetOrdinal("ManageUsersPerm")) ? RolePermissions.ManageUsersPerm : RolePermissions.NaN),
                            RoleName = reader.GetString(reader.GetOrdinal("Name"))
                        };
                    }
                }
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
        /// <exception cref="ChatTypeMismatchException">Throws if chat is dialog</exception>
        public void ClearChatSpecificInfo(int userId, int chatId)
        {
            if (userId == 0)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // check if chat is dialog
                if (SqlHelper.DoesDoubleKeyExist(connection, "Chats", "ID", chatId, "ChatType", (int)ChatTypes.Dialog))
                    throw new ChatTypeMismatchException();
                // check if user is in chat
                if (!SqlHelper.DoesDoubleKeyExist(connection, "ChatUsers", "UserID", userId, "ChatID", chatId))
                    throw new ArgumentException();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [ChatUserInfos] SET [Nickname] = NULL, [UserRole] = DEFAULT" +
                                          " WHERE [UserID] = @userId AND [ChatID] = @chatId";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@chatId", chatId);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentNullException();
                    NLogger.Logger.Trace("DB:Updated:{0}:WHERE UserID:{1} AND ChatID:{2}", "[ChatUserInfos]", userId, chatId);

                }
            }
        }
        /// <summary>
        /// Gets ids and roles of the users of the chat, that are not listed in <paramref name="usersList"/>
        /// </summary>
        /// <param name="chatId">The id of the chat</param>
        /// <param name="usersList">The ids that should not be included in the output</param>
        /// <returns>A list of pairs of id and chatuserinfo of users that are not listed in <paramref name="usersList"/></returns>
        public IEnumerable<int> GetNotListedChatMembers(int chatId, IEnumerable<int> usersList)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "Get_Not_Listed_Chat_Members";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@chatId", chatId);
                    var parameter =
                        command.Parameters.AddWithValue("@idlist", SqlHelper.IdListToDataTable(usersList));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            yield break;
                        while (reader.Read())
                        {
                            yield return
                                reader.GetInt32(reader.GetOrdinal("UserID"));
                        }
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<int, ChatUserInfo>> GetChatMembersInfo(int chatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "Get_Chat_Members_With_ChatUserInfo";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@chatId", chatId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            yield break;
                        while (reader.Read())
                        {
                            yield return new KeyValuePair<int, ChatUserInfo>(
                                reader.GetInt32(reader.GetOrdinal("UserID")), new ChatUserInfo
                                {
                                    Nickname = reader.IsDBNull(reader.GetOrdinal("Nickname"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Nickname")),
                                    Role = new UserRole
                                    {
                                        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                        RolePermissions =
                                            (reader.GetBoolean(reader.GetOrdinal("WritePerm"))
                                                ? RolePermissions.WritePerm
                                                : RolePermissions.NaN) |
                                            (reader.GetBoolean(reader.GetOrdinal("ReadPerm"))
                                                ? RolePermissions.ReadPerm
                                                : RolePermissions.NaN) |
                                            (reader.GetBoolean(reader.GetOrdinal("ChatInfoPerm"))
                                                ? RolePermissions.ChatInfoPerm
                                                : RolePermissions.NaN) |
                                            (reader.GetBoolean(reader.GetOrdinal("AttachPerm"))
                                                ? RolePermissions.AttachPerm
                                                : RolePermissions.NaN) |
                                            (reader.GetBoolean(reader.GetOrdinal("ManageUsersPerm"))
                                                ? RolePermissions.ManageUsersPerm
                                                : RolePermissions.NaN),
                                        RoleType = (UserRoles)reader.GetInt32(reader.GetOrdinal("RoleID"))
                                    }
                                }
                            );
                        }
                    }
                }
            }
        }
    }
}