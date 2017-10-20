using System.Security.Principal;
using System.Threading;
using DotNetMessenger.DataLayer.SqlServer;

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
            if (userPassword != password)
                return null;
            // if succeeded
            var identity = new GenericIdentity(userName);
            return new GenericPrincipal(identity, null);
        }
    }
}