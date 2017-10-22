using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Extensions;
using DotNetMessenger.WebApi.Principals;
using DotNetMessenger.WebApi.Results;

namespace DotNetMessenger.WebApi.Filters.Authentication
{
    /// <inheritdoc cref="IAuthenticationFilter"/>
    /// <summary>
    /// Reads Base64 string from auth header and converts it to a token
    /// Sets <see cref="T:System.Security.Principal.IPrincipal" /> to the user using the token
    /// </summary>
    public class TokenAuthenticationAttribute : Attribute, IAuthenticationFilter
    {
        public string Realm { get; set; }

        public bool AllowMultiple => false;
        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var authorization = request.Headers.Authorization;

            if (authorization == null)
            {
                return;
            }

            if (authorization.Scheme != "Basic")
            {
                return;
            }

            if (string.IsNullOrEmpty(authorization.Parameter))
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing token", request);
                return;
            }

            var tokenStr = authorization.Parameter.FromBase64ToString();

            if (string.IsNullOrEmpty(tokenStr))
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);
                return;
            }

            // basic auth is like username:password
            // since we have only token and no pass it looks like this: token:[empty pass]
            // need toremove the colon (:)
            tokenStr = tokenStr.Remove(tokenStr.Length - 1, 1);

            try
            {
                var token = Guid.Parse(tokenStr);
                var userId = await RepositoryBuilder.TokensRepository.GetUserIdByTokenAsync(token);
                if (userId == 0)
                    context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);

                var userName = RepositoryBuilder.UsersRepository.GetUser(userId).Username;

                HttpContext.Current.User = new UserPrincipal(userId, userName, token);
                context.Principal = new UserPrincipal(userId, userName, token);
                Thread.CurrentPrincipal = new UserPrincipal(userId, userName, token);
            }
            catch
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            Challenge(context);
            return Task.FromResult(0);
        }

        private void Challenge(HttpAuthenticationChallengeContext context)
        {
            string parameter;

            if (string.IsNullOrEmpty(Realm))
            {
                parameter = null;
            }
            else
            {
                // A correct implementation should verify that Realm does not contain a quote character unless properly
                // escaped (precededed by a backslash that is not itself escaped).
                parameter = "realm=\"" + Realm + "\"";
            }

            context.ChallengeWith("Basic", parameter);
        }
    }
}