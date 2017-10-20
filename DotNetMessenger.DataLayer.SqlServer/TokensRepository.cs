using System;
using System.Data;
using System.Data.SqlClient;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public class TokensRepository : ITokensRepository
    {
        private readonly string _connectionString;

        public TokensRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <inheritdoc />
        /// <summary>
        /// Gets user id given user's <paramref name="token" />
        /// </summary>
        /// <param name="token">The token of the user</param>
        /// <returns>The id of the user, 0 if no user was found</returns>
        /// <exception cref="ArgumentException">Throws if token is invalid</exception>
        public int GetUserIdByToken(Guid token)
        {
            if (token == Guid.Empty)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [UserID] FROM [Tokens] WHERE [Token] = @token";

                    command.Parameters.AddWithValue("@token", token);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            throw new ArgumentException();
                        reader.Read();
                        return reader.GetInt32(reader.GetOrdinal("UserID"));
                    }
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Generates a new token for given <paramref name="userId" />. Returns <see cref="F:System.Guid.Empty" /> 
        /// if no such user exists
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>New guid for the user. <see cref="Guid.Empty"/> if no such user exists</returns>
        /// <exception cref="ArgumentException">Throws if <paramref name="userId"/> is invalid</exception>
        public Guid GenerateToken(int userId)
        {
            if (userId == 0)
                throw new ArgumentException();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [Tokens]([Token], [UserID]) VALUES (@token, @userId)";

                    var token = Guid.NewGuid();
                    command.Parameters.AddWithValue("@token", token);
                    command.Parameters.AddWithValue("@userId", userId);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        // means user does not exist
                        throw new ArgumentException();
                    }
                    return token;
                }
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Invalidates given token
        /// </summary>
        /// <param name="token">Token to be invalidated</param>
        /// <exception cref="T:System.ArgumentException">Throws if token does not exist</exception>
        public void InvalidateToken(Guid token)
        {
            if (token == Guid.Empty)
                throw new ArgumentException();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM [Tokens] WHERE [Token] = @token";

                    command.Parameters.AddWithValue("@token", token);

                    if (command.ExecuteNonQuery() == 0)
                        throw new ArgumentException(); // means token did not exist
                }
            }
        }
    }
}