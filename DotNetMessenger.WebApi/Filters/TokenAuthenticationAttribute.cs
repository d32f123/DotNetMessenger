using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Principals;
using DotNetMessenger.WebApi.Results;

namespace DotNetMessenger.WebApi.Filters
{
    public class TokenAuthenticationAttribute : Attribute, IAuthenticationFilter
    {
        public string Realm { get; set; }

        public bool AllowMultiple => false;
        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;

            var tokensEnum = request.Headers.GetValues("Token");

            if (tokensEnum == null)
            {
                return;
            }

            var tokens = tokensEnum as string[] ?? tokensEnum.ToArray();
            if (!tokens.Any())
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing token", request);
            }

            try
            {
                var token = tokens.Single();
                if (string.IsNullOrEmpty(token))
                    context.ErrorResult = new AuthenticationFailureResult("Missing token", request);

                var userId = RepositoryBuilder.TokensRepository.GetUserIdByToken(Guid.Parse(token));
                if (userId == 0)
                    context.ErrorResult = new AuthenticationFailureResult("Invalid token", request);

                var userName = RepositoryBuilder.UsersRepository.GetUser(userId).Username;

                HttpContext.Current.User = new UserPrincipal(userId, userName);
                context.Principal = new UserPrincipal(userId, userName);
                Thread.CurrentPrincipal = new UserPrincipal(userId, userName);
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