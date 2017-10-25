using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using DotNetMessenger.Logger;
using DotNetMessenger.WebApi.Extensions;
using DotNetMessenger.WebApi.Results;

namespace DotNetMessenger.WebApi.Filters.Authentication
{
    /// <inheritdoc cref="IAuthenticationFilter"/>
    /// <summary>
    /// Reads Base64 string, converts it to username and password pair and executes 
    /// <see cref="M:DotNetMessenger.WebApi.Filters.Authentication.BasicAuthenticationAttribute.Authenticate(System.String,System.String,System.Threading.CancellationToken)" /> method
    /// for further authentication
    /// </summary>
    public abstract class BasicAuthenticationAttribute : Attribute, IAuthenticationFilter
    {
        public string Realm { get; set; }

        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var authorization = request.Headers.Authorization;

            NLogger.Logger.Debug("Authentication starting");

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
                context.ErrorResult = new AuthenticationFailureResult("Missing credentials", request);
                return;
            }

            var userNameAndPassword = ExtractUserNameAndPassword(authorization.Parameter);

            if (userNameAndPassword == null)
            {
                NLogger.Logger.Error("Invalid format of credentials. Forbidden");
                context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", request);
                return;
            }

            var username = userNameAndPassword.Item1;
            var password = userNameAndPassword.Item2;

            var principal = await Authenticate(username, password, cancellationToken);

            if (principal == null)
            {
                NLogger.Logger.Error("Invalid username or password. Forbidden");
                context.ErrorResult = new AuthenticationFailureResult("Invalid username or password", request);
            }
            else
            {
                NLogger.Logger.Debug("Successfully authenticated user. Username: \"{0}\"", principal.Identity.Name);
                HttpContext.Current.User = principal;
                context.Principal = principal;
                Thread.CurrentPrincipal = principal;
            }
        }

        protected abstract Task<IPrincipal> Authenticate(string userName, string password,
            CancellationToken cancellationToken);

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

        private static Tuple<string, string> ExtractUserNameAndPassword(string authorizationParameter)
        {
            var decodedCredentials = authorizationParameter.FromBase64ToString();
            NLogger.Logger.Debug("Credentials decoded from base64");
            if (string.IsNullOrEmpty(decodedCredentials))
            {
                NLogger.Logger.Debug("Credentials are empty");
                return null;
            }

            NLogger.Logger.Debug("Splitting username from password");
            var colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
            {
                NLogger.Logger.Warn("No delimeter found");
                return null;
            }

            var userName = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);
            NLogger.Logger.Debug("Successfully extracted username and password");
            return new Tuple<string, string>(userName, password);
        }
    }
}