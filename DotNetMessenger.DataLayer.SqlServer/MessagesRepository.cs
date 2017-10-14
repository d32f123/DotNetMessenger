using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly string _connectionString;

        public MessagesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Message StoreMessage(int senderId, int chatId, string text, IEnumerable<Attachment> attachments = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    // Insert the message
                    var message = new Message {ChatId = chatId, SenderId = senderId, Text = text};
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText]) " +
                                              "OUTPUT INSERTED.[ID], INSERTED.[MessageDate] " +
                                              "VALUES (@chatId, @senderId, @messageText)";

                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@sednerId", senderId);
                        command.Parameters.AddWithValue("@messageText", text);

                        using (var reader = command.ExecuteReader())
                        {
                            reader.Read();
                            message.Id = reader.GetInt32(reader.GetOrdinal("[ID]"));
                            message.Date = reader.GetDateTime(reader.GetOrdinal("[MessageDate]"));
                        }
                    }

                    //Insert attachments if not null

                    if (attachments == null)
                    {
                        message.Attachments = null;
                    }
                    else
                    {
                        var messageAttachments = attachments as Attachment[] ?? attachments.ToArray();
                        foreach (var attachment in messageAttachments)
                            using (var command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText =
                                    "INSERT INTO [Attachments] ([Type], [AttachFile], [MessageID]) " +
                                    "OUTPUT INSERTED.[ID] " +
                                    "VALUES (@type, @attachFile, @messageId)";

                                command.Parameters.AddWithValue("@type", (int) attachment.Type);
                                command.Parameters.AddWithValue("@messageId", message.Id);
                                var attachFile =
                                    new SqlParameter("@attachFile", SqlDbType.VarBinary, attachment.File.Length)
                                        {Value = attachment.File};
                                command.Parameters.Add(attachFile);

                                attachment.Id = (int) command.ExecuteScalar();
                            }
                        message.Attachments = messageAttachments;
                    }
                    transaction.Commit();

                    return message;
                }
            }
        }

        public Message StoreTemporaryMessage(int senderId, int chatId, string text, DateTime expirationDate,
            IEnumerable<Attachment> attachments = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var message = StoreMessage(senderId, chatId, text, attachments);

                    // Insert expiration date into queue
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO [MessagesDeleteQueue] ([MessageID], [ExpireDate])" +
                            "VALUES (@messageId, @expireDate)";

                        command.Parameters.AddWithValue("@messageId", message.Id);
                        command.Parameters.AddWithValue("@expireDate", expirationDate);
                    }

                    transaction.Commit();
                    return message;
                }
            }
        }

        public Message GetMessage(int messageId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ChatID], [SenderID], [MessageText], [MessageDate] FROM [Messages] " +
                                          "WHERE [ID] = @messageId";

                    command.Parameters.AddWithValue("@messageId", messageId);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return new Message
                        {
                            Id = messageId,
                            ChatId = reader.GetInt32(reader.GetOrdinal("[ChatID]")),
                            SenderId = reader.GetInt32(reader.GetOrdinal("[SenderID]")),
                            Text = reader.GetString(reader.GetOrdinal("[MessageText]")),
                            Date = reader.GetDateTime(reader.GetOrdinal("[MessageDate]")),
                            Attachments = GetMessageAttachments(messageId),
                            ExpirationDate = GetMessageExpirationDate(messageId)
                        };
                    }
                }
            }
        }

        public IEnumerable<Attachment> GetMessageAttachments(int messageId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Type], [AttachFile] FROM [Attachments] " +
                                          "WHERE [MessageID] = @messageId";

                    command.Parameters.AddWithValue("@messageId", messageId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new Attachment
                            { 
                                Id = reader.GetInt32(reader.GetOrdinal("[ID]")),
                                Type = (AttachmentTypes) reader.GetInt32(reader.GetOrdinal("[Type]")),
                                File = reader[reader.GetOrdinal("[AttachFile]")] as byte[]
                            };
                        }
                    }
                }
            }
        }

        public IEnumerable<Message> GetChatMessages(int chatId)
        {
            return GetChatMessagesInRange(chatId, DateTime.MinValue, DateTime.MaxValue);
        }

        public IEnumerable<Message> GetChatMessagesFrom(int chatId, DateTime dateFrom)
        {
            return GetChatMessagesInRange(chatId, dateFrom, DateTime.MaxValue);
        }

        public IEnumerable<Message> GetChatMessageTo(int chatId, DateTime dateTo)
        {
            return GetChatMessagesInRange(chatId, DateTime.MinValue, dateTo);
        }

        public IEnumerable<Message> GetChatMessagesInRange(int chatId, DateTime dateFrom, DateTime dateTo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [SenderID], [MessageText], [MessageDate] FROM [Messages] " +
                                          "WHERE [ChatID] = @chatId";
                    command.Parameters.AddWithValue("@chatId", chatId);

                    if (dateFrom != DateTime.MinValue)
                    {
                        command.CommandText += " AND [MessageDate] >= @dateFrom";
                        command.Parameters.AddWithValue("@dateFrom", dateFrom);
                    }
                    if (dateTo != DateTime.MaxValue)
                    {
                        command.CommandText += " AND [MessageDate] <= @dateTo";
                        command.Parameters.AddWithValue("@dateTo", dateTo);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("[ID]")),
                                ChatId = chatId,
                                Text = reader.GetString(reader.GetOrdinal("[MessageText]")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("[SenderID]")),
                                Date = reader.GetDateTime(reader.GetOrdinal("[MessageDate]")),
                                Attachments = GetMessageAttachments(reader.GetInt32(reader.GetOrdinal("[ID]"))),
                                ExpirationDate = GetMessageExpirationDate(reader.GetInt32(reader.GetOrdinal("[ID]")))
                            };
                        }
                    }
                }
            }
        }

        public IEnumerable<Message> SearchString(int chatId, string searchString)
        {
            using (var connection = new SqlConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [SenderID], [MessageText], [MessageDate] FROM [Messages] " +
                                          "WHERE [ChatID] = @chatId AND CONTAINS([MessageText], @searchString)";

                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@searchString", "*" + searchString + "*");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("[ID]")),
                                ChatId = chatId,
                                Text = reader.GetString(reader.GetOrdinal("[MessageText]")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("[SenderID]")),
                                Date = reader.GetDateTime(reader.GetOrdinal("[MessageDate]")),
                                Attachments = GetMessageAttachments(reader.GetInt32(reader.GetOrdinal("[ID]"))),
                                ExpirationDate = GetMessageExpirationDate(reader.GetInt32(reader.GetOrdinal("[ID]")))
                            };
                        }
                    }
                }
            }
        }

        public DateTime GetMessageExpirationDate(int messageId)
        {
            using (var connection = new SqlConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [ExpireDate] FROM [MessagesDeleteQueue] WHERE [MessageID] = @messageId";
                    command.Parameters.AddWithValue("@messageId", messageId);

                    var date = (DateTime) command.ExecuteScalar();
                    return date;
                }
            }
        }

        public void DeleteExpiredMessages()
        {
            using (var connection = new SqlConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "DeleteExpiredMessages";

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}