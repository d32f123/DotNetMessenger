using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using DotNetMessenger.DataLayer.SqlServer.ModelProxies;
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
        /// <inheritdoc />
        /// <summary>
        /// Stores a <see cref="T:DotNetMessenger.Model.Message" /> by <paramref name="senderId" /> in <paramref name="chatId" />
        /// </summary>
        /// <param name="senderId">The id of the sender</param>
        /// <param name="chatId">Chat id</param>
        /// <param name="text">Text of the message</param>
        /// <param name="attachments">Any attachments</param>
        /// <returns><see cref="T:DotNetMessenger.Model.Message" /> object representing persisted message</returns>
        /// <exception cref="ArgumentNullException">Throws if text and attachments are both null</exception>
        /// <exception cref="ArgumentException">
        ///     Throws if any of the ids are invalid or if 
        ///     <paramref name="senderId"/> is not in <paramref name="chatId"/>
        /// </exception>
        public Message StoreMessage(int senderId, int chatId, string text, IEnumerable<Attachment> attachments = null)
        {
            try
            {
                if (string.IsNullOrEmpty(text) && attachments == null)
                    throw new ArgumentNullException();
                if (senderId == 0)
                    throw new ArgumentException();
                using (var scope = new TransactionScope())
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Insert the message
                        var message = new MessageSqlProxy {ChatId = chatId, SenderId = senderId, Text = text};
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "DECLARE @T TABLE (ID INT, MessageDate DATETIME)\n" +
                                                  "INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText]) " +
                                                  "OUTPUT INSERTED.[ID], INSERTED.[MessageDate] INTO @T " +
                                                  "VALUES (@chatId, @senderId, @messageText)\n" +
                                                  "SELECT [ID], [MessageDate] FROM @T";

                            command.Parameters.AddWithValue("@chatId", chatId);
                            command.Parameters.AddWithValue("@senderId", senderId);
                            if (text == null)
                                command.Parameters.AddWithValue("@messageText", DBNull.Value);
                            else
                                command.Parameters.AddWithValue("@messageText", text);

                            using (var reader = command.ExecuteReader())
                            {
                                reader.Read();
                                message.Id = reader.GetInt32(reader.GetOrdinal("ID"));
                                message.Date = reader.GetDateTime(reader.GetOrdinal("MessageDate"));
                            }
                        }

                        //Insert attachments if not null

                        if (attachments == null)
                        {
                            message.Attachments = null;
                        }
                        else
                        {
                            foreach (var attachment in attachments)
                                using (var command = connection.CreateCommand())
                                {
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
                        }
                        scope.Complete();
                        return message;
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
        /// Stores a <see cref="T:DotNetMessenger.Model.Message" /> by <paramref name="senderId" /> in <paramref name="chatId" />
        /// that will be deleted after <paramref name="expirationDate" />
        /// </summary>
        /// <param name="senderId">The id of the sender</param>
        /// <param name="chatId">Chat id</param>
        /// <param name="text">Text of the message</param>
        /// <param name="expirationDate">Expiration date of the message</param>
        /// <param name="attachments">Any attachments</param>
        /// <returns><see cref="T:DotNetMessenger.Model.Message" /> object representing persisted message</returns>
        /// <exception cref="T:System.ArgumentNullException">Throws if text and attachments are both null</exception>
        /// <exception cref="T:System.ArgumentException">
        ///     Throws if any of the ids are invalid or if 
        ///     <paramref name="senderId" /> is not in <paramref name="chatId" />
        /// </exception>
        public Message StoreTemporaryMessage(int senderId, int chatId, string text, DateTime expirationDate,
            IEnumerable<Attachment> attachments = null)
        {
            // if message already expired
            if (expirationDate <= DateTime.Now)
                return null;
            using (var scope = new TransactionScope())
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var message = (MessageSqlProxy)StoreMessage(senderId, chatId, text, attachments);
                    // Insert expiration date into queue
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [MessagesDeleteQueue] ([MessageID], [ExpireDate])" +
                                              "VALUES (@messageId, @expireDate)";

                        command.Parameters.AddWithValue("@messageId", message.Id);
                        command.Parameters.AddWithValue("@expireDate", expirationDate);

                        command.ExecuteNonQuery();
                    }

                    scope.Complete();
                    return message;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets a message given its <paramref name="messageId" />.
        /// Ret value can be null
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns>Null if no object found, else <see cref="Message"/> object representing persisted message</returns>
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
                        if (!reader.HasRows)
                            return null;
                        reader.Read();
                        return new MessageSqlProxy
                        {
                            Id = messageId,
                            ChatId = reader.GetInt32(reader.GetOrdinal("ChatID")),
                            SenderId = reader.GetInt32(reader.GetOrdinal("SenderID")),
                            Text = reader.IsDBNull(reader.GetOrdinal("MessageText")) ? null : reader.GetString(reader.GetOrdinal("MessageText")),
                            Date = reader.GetDateTime(reader.GetOrdinal("MessageDate")),
                        };
                    }
                }
            }
        }
        /// <summary>
        /// Gets message attachments given its <paramref name="messageId"/>
        /// </summary>
        /// <param name="messageId">The id of the message</param>
        /// <returns></returns>
        public IEnumerable<Attachment> GetMessageAttachments(int messageId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                /* TODO: CHECK IF EXISTS, IF NO THROW EXCEPTION */
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ID], [Type], [AttachFile] FROM [Attachments] " +
                                          "WHERE [MessageID] = @messageId";

                    command.Parameters.AddWithValue("@messageId", messageId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            yield break;
                        while (reader.Read())
                        {
                            yield return new Attachment
                            { 
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                Type = (AttachmentTypes) reader.GetInt32(reader.GetOrdinal("Type")),
                                File = reader[reader.GetOrdinal("AttachFile")] as byte[]
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

        public IEnumerable<Message> GetChatMessagesTo(int chatId, DateTime dateTo)
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
                            yield return new MessageSqlProxy
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                ChatId = chatId,
                                Text = reader.IsDBNull(reader.GetOrdinal("MessageText")) ? null : reader.GetString(reader.GetOrdinal("MessageText")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("SenderID")),
                                Date = reader.GetDateTime(reader.GetOrdinal("MessageDate")),
                            };
                        }
                    }
                }
            }
        }

        public IEnumerable<Message> SearchString(int chatId, string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
                yield break;
            using (var connection = new SqlConnection(_connectionString))
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
                            yield return new MessageSqlProxy
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                ChatId = chatId,
                                Text = reader.GetString(reader.GetOrdinal("MessageText")),
                                SenderId = reader.GetInt32(reader.GetOrdinal("SenderID")),
                                Date = reader.GetDateTime(reader.GetOrdinal("MessageDate")),
                            };
                        }
                    }
                }
            }
        }

        public DateTime? GetMessageExpirationDate(int messageId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT [ExpireDate] FROM [MessagesDeleteQueue] WHERE [MessageID] = @messageId";
                    command.Parameters.AddWithValue("@messageId", messageId);

                    return (DateTime?)command.ExecuteScalar();
                }
            }
        }

        public void DeleteExpiredMessages()
        {
            using (var connection = new SqlConnection(_connectionString))
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