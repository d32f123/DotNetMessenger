using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;


namespace DotNetMessenger.DataLayer.SqlServer
{
    public class ChatsRepository : IChatsRepository
    {
        private readonly string _connectionString;
        private readonly IUsersRepository _usersRepository;

        public ChatsRepository(string connectionString, IUsersRepository usersRepository)
        {
            _connectionString = connectionString;
            _usersRepository = usersRepository;
        }

        public void AddUser(int chatId, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [ChatUsers]([UserID], [ChatID]) VALUES " +
                                          "(@userId, @chatId)";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@chatId", chatId);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddUsers(int chatId, IEnumerable<int> newUsers)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "AddUsersToChat";

                    var parameter = command.Parameters.AddWithValue("@IDList", DataTableHelper.IdListToDataTable(newUsers));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";
                    command.Parameters.AddWithValue("@ChatID", chatId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Chat CreateGroupChat(IEnumerable<int> members, string title)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var chatInfo = new ChatInfo { Title = title };
                    var userIds = members as int[] ?? members.ToArray();

                    var chat = new Chat{
                        ChatType = ChatTypes.GroupChat,
                        CreatorId = userIds[0],
                        Info = chatInfo
                    };

                    // Create new chat
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO [Chats] ([ChatType], [CreatorID]) OUTPUT INSERTED.[ID] VALUES (@chatType, @creatorId)";

                        command.Parameters.AddWithValue("@chatType", (int)(chat.ChatType));
                        command.Parameters.AddWithValue("@creatorId", chat.CreatorId);

                        chat.Id = (int) command.ExecuteScalar();
                    }

                    // Add users to chat
                    foreach (var userId in userIds)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText =
                                "INSERT INTO [ChatUsers] ([UserID], [ChatID]) VALUES (@userId, @chatId)";

                            command.Parameters.AddWithValue("@userId", userId);
                            command.Parameters.AddWithValue("@chatId", chat.Id);

                            command.ExecuteNonQuery();
                        }
                    }

                    // Add chat title
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO [ChatInfos] ([ChatID], [Title], [Avatar]) VALUES (@chatId, @title, NULL)";

                        command.Parameters.AddWithValue("@chatId", chat.Id);
                        command.Parameters.AddWithValue("@title", chatInfo.Title);

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    chat.Users = userIds.Select(x => _usersRepository.GetUser(x));
                    return chat;
                }
            }
        }

        public Chat CreateDialog(int member1, int member2)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var chat = new Chat { ChatType = ChatTypes.Dialog, CreatorId = member1 };

                    // Create new chat
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText =
                            "INSERT INTO [Chats] ([ChatType], [CreatorID]) OUTPUT INSERTED.[ID] VALUES (@chatType, @creatorId)";

                        command.Parameters.AddWithValue("@chatType", (int) chat.ChatType);
                        command.Parameters.AddWithValue("@creatorId", member1);

                        chat.Id = (int) command.ExecuteScalar();
                    }

                    // Add users to chat
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText =
                            "INSERT INTO [ChatUsers] ([UserID], [ChatID]) VALUES (@firstId, @chatId), (@secondId, @chatId)";

                        command.Parameters.AddWithValue("@firstId", member1);
                        command.Parameters.AddWithValue("@secondId", member2);
                        command.Parameters.AddWithValue("@chatId", chat.Id);

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    var members = new[] {member1, member2};
                    chat.Users = members.Select(x => _usersRepository.GetUser(x));
                    return chat;
                }
            }
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
                        "SELECT [Chats].[ID], [Chats].[ChatType], [Chats].[CreatorID] " +
                        "FROM [ChatUsers], [Chats] WHERE [Chats].[ID] = [ChatUsers].[ChatID] AND [ChatUsers].[UserID] = @userId";

                    command.Parameters.AddWithValue("@userId", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var chatId = reader.GetInt32(reader.GetOrdinal("[Chats].[ID]"));
                            yield return new Chat
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("[Chats].[ID]")),
                                ChatType = (ChatTypes)reader.GetInt32(reader.GetOrdinal("[Chats].[ChatType]")),
                                CreatorId = reader.GetInt32(reader.GetOrdinal("[Chats].[CreatorID]")),
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
                        while (reader.Read())
                        {
                            yield return _usersRepository.GetUser(reader.GetInt32(reader.GetOrdinal("[UserID]")));
                        }
                    }
                }
            }
        }

        public void KickUser(int chatId, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

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
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "KickUsersFromChat";

                    var parameter = command.Parameters.AddWithValue("@IDList", DataTableHelper.IdListToDataTable(kickedUsers));
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "IdListType";
                    command.Parameters.AddWithValue("@ChatID", chatId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetChatInfo(int chatId, ChatInfo info)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    /* TODO: CHECK IF NO USERINFO EXISTS, INSERT IF YES */
                    command.CommandText =
                        "UPDATE [ChatInfos] SET [Title] = @title, [Avatar] = @avatar WHERE [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@title", info.Title);

                    var avatar =
                        new SqlParameter("@avatar", SqlDbType.VarBinary, info.Avatar.Length)
                        { Value = info.Avatar };
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

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [Title], [Avatar] FROM [ChatInfos] WHERE [ChatID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        var avatar = reader[reader.GetOrdinal("[Avatar]")] as byte[];
                        return new ChatInfo {Avatar = avatar, Title = reader.GetString(reader.GetOrdinal("[Title]")) };
                    }
                }
            }
        }

        public void SetCreator(int chatId, int newCreator)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE [Chats] SET [CreatorID] = @newCreator WHERE [ID] = @chatId";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@newCreator", newCreator);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}