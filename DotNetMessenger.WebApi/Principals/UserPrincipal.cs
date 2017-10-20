using System.Security.Principal;

namespace DotNetMessenger.WebApi.Principals
{
    public class UserPrincipal : IPrincipal
    {
        public UserPrincipal(int userId, string userName)
        {
            Identity = new GenericIdentity(userName);
            UserId = userId;
        }
        public bool IsInRole(string role)
        {
            return role.Equals("user");
        }

        public int UserId { get; }
        public IIdentity Identity { get; }
    }
}