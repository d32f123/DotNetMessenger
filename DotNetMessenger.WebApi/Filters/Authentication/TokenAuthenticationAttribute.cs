using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
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

            NLogger.Logger.Debug("Authentication started");


            if (authorization == null)
            {
                NLogger.Logger.Warn("No authorization header included in the request. Skipping");
                return;
            }

            if (authorization.Scheme != "Basic")
            {
                NLogger.Logger.Warn("Auth scheme is not {0}. Skipping", "Basic");
                return;
            }

            if (string.IsNullOrEmpty(authorization.Parameter))
            {
                NLogger.Logger.Error("Credentials missing in the header. Forbidden");
                context.ErrorResult = new AuthenticationFailureResult("Missing token", request);
                return;
            }

            NLogger.Logger.Debug("Converting token from Base64");
            var tokenStr = authorization.Parameter.FromBase64ToString();
            NLogger.Logger.Debug("Token converted from Base64");
            if (string.IsNullOrEmpty(tokenStr))
            {
                NLogger.Logger.Error("Credentials empty. Forbidden");
                context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);
                return;
            }

            // basic auth is like username:password
            // since we have only token and no pass it looks like this: token:[empty pass]
            // need toremove the colon (:)
            tokenStr = tokenStr.Remove(tokenStr.Length - 1, 1);

            try
            {
                NLogger.Logger.Debug("Parsing token");
                var token = Guid.Parse(tokenStr);

                NLogger.Logger.Debug("Token parsed. Getting user id");
                var userId = await RepositoryBuilder.TokensRepository.GetUserIdByTokenAsync(token);

                NLogger.Logger.Debug("Id is {0}. Getting username", userId);
                var userName = RepositoryBuilder.UsersRepository.GetUser(userId).Username;

                NLogger.Logger.Debug("Authentication successful. Setting principal");
                HttpContext.Current.User = new UserPrincipal(userId, userName, token);
                context.Principal = new UserPrincipal(userId, userName, token);
                Thread.CurrentPrincipal = new UserPrincipal(userId, userName, token);
            }
            catch
            {
                NLogger.Logger.Error("Token is invalid. Forbidden");
                context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            NLogger.Logger.Debug("Adding challenge to response");
            Challenge(context);
            NLogger.Logger.Debug("Added challenge to response");
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