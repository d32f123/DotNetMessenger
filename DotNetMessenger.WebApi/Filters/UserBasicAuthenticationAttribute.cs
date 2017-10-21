using System;
using System.Security.Principal;
using System.Threading;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters
{
    public class UserBasicAuthenticationAttribute : BasicAuthenticationAttribute
    {
        protected override IPrincipal Authenticate(string userName, string password, CancellationToken cancellationToken)
        {
            // check for username and password
            var user = RepositoryBuilder.UsersRepository.GetUserByUsername(userName);
            if (user == null)
                return null;
            var userPassword = RepositoryBuilder.UsersRepository.GetPassword(user.Id);
            return userPassword != password ? null : new UserPrincipal(user.Id, userName, Guid.Empty);
        }
    }
}