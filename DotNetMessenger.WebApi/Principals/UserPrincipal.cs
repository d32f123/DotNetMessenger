using System;
using System.Security.Principal;

namespace DotNetMessenger.WebApi.Principals
{
    public class UserPrincipal : IPrincipal
    {
        public UserPrincipal(int userId, string userName, Guid token)
        {
            Identity = new GenericIdentity(userName);
            UserId = userId;
            Token = token;
        }
        public bool IsInRole(string role)
        {
            return role.Equals("user");
        }

        public int UserId { get; }
        public Guid Token { get; }
        public IIdentity Identity { get; }
    }
}