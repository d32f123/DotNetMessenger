using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.WebApi.Extensions;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters.Authentication
{
    /// <inheritdoc />
    /// <summary>
    /// Checks DB against the given username and password. Checks if combination is allowed.
    /// </summary>
    public class UserBasicAuthenticationAttribute : BasicAuthenticationAttribute
    {
        protected override async Task<IPrincipal> Authenticate(string userName, string password, CancellationToken cancellationToken)
        {
            NLogger.Logger.Debug("Authenticating user by login and pass. Login: \"{0}\"", userName);
            // check for username and password
            try
            {
                var user = await RepositoryBuilder.UsersRepository.GetUserByUsernameAsync(userName);
                var storedPassword = RepositoryBuilder.UsersRepository.GetPassword(user.Id);
                return PasswordHasher.ComparePasswordToHash(password, storedPassword) ? new UserPrincipal(user.Id, userName, Guid.Empty) : null;
            }
            catch (ArgumentException)
            {
                NLogger.Logger.Error("Authentication failed");
                return null;
            }
            
        }
    }
}