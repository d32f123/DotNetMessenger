using System;
using DotNetMessenger.Model;

namespace DotNetMessenger.DataLayer
{
    public interface ITokensRepository
    {
        /// <summary>
        /// Gets user id by a given token. Returns 0 if no user was found
        /// </summary>
        /// <param name="token">User's token</param>
        /// <returns>Id of the user, 0 if no user was found</returns>
        int GetUserIdByToken(Guid token);
        /// <summary>
        /// Generates a token given user's id
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>A newly generated token</returns>
        Guid GenerateToken(int userId);
        /// <summary>
        /// Invalidates given token
        /// </summary>
        /// <param name="token">Token to be invalidated</param>
        void InvalidateToken(Guid token);
    }
}