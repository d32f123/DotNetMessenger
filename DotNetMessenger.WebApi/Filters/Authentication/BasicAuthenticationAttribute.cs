using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using DotNetMessenger.WebApi.Extensions;
using DotNetMessenger.WebApi.Results;

namespace DotNetMessenger.WebApi.Filters.Authentication
{
    /// <inheritdoc />
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
                context.ErrorResult = new AuthenticationFailureResult("Missing credentials", request);
                return;
            }

            var userNameAndPassword = ExtractUserNameAndPassword(authorization.Parameter);

            if (userNameAndPassword == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", request);
                return;
            }

            var username = userNameAndPassword.Item1;
            var password = userNameAndPassword.Item2;

            var principal = Authenticate(username, password, cancellationToken);

            if (principal == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid username or password", request);
            }
            else
            {
                HttpContext.Current.User = principal;
                context.Principal = principal;
                Thread.CurrentPrincipal = principal;
            }
        }

        protected abstract IPrincipal Authenticate(string userName, string password,
            CancellationToken cancellationToken);

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

        private static Tuple<string, string> ExtractUserNameAndPassword(string authorizationParameter)
        {
            var decodedCredentials = authorizationParameter.FromBase64ToString();

            if (string.IsNullOrEmpty(decodedCredentials))
            {
                return null;
            }

            var colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
            {
                return null;
            }

            var userName = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);
            return new Tuple<string, string>(userName, password);
        }
    }
}