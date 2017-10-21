using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters.Authorization
{
    /// <inheritdoc />
    /// <summary>
    /// Checks that the user making the request has the same username as is
    /// specified in the URI. Username is extracted using
    /// <see cref="P:DotNetMessenger.WebApi.Filters.Authorization.UserNameIsNameFromUriAuthorizationAttribute.RegexString" />
    /// </summary>
    public class UserNameIsNameFromUriAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; }

        public string RegexString { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                if (string.IsNullOrEmpty(RegexString))
                {
                    Challenge(actionContext);
                    return;
                }

                // extract string matching regex
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success)
                {
                    Challenge(actionContext);
                    return;
                }
                // parse it to username
                var username = m.Groups[1].Value;

                // get principal
                var principal = Thread.CurrentPrincipal as UserPrincipal;
                if (principal == null)
                {
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                if (username != principal.Identity.Name)
                {
                    Challenge(actionContext);
                    return;
                }
                base.OnAuthorization(actionContext);
            }
            catch
            {
                Challenge(actionContext);
            }
        }

        private void Challenge(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
        }
    }
}