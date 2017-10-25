using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.Logger;
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
                NLogger.Logger.Debug("Authorization successful if username from URI is the user making the request");
                if (string.IsNullOrEmpty(RegexString))
                {
                    NLogger.Logger.Fatal("RegexString to get username is not set. Cannot authorize");
                    Challenge(actionContext);
                    return;
                }

                // extract string matching regex
                NLogger.Logger.Debug("Parsing username using regex {0}", RegexString);
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success)
                {
                    NLogger.Logger.Error("Failed to parse username from uri");
                    Challenge(actionContext);
                    return;
                }
                // parse it to username
                var username = m.Groups[1].Value;

                // get principal
                if (!(Thread.CurrentPrincipal is UserPrincipal principal))
                {
                    NLogger.Logger.Fatal("Principal is not set. User is not authenticated?");
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                if (username != principal.Identity.Name)
                {
                    NLogger.Logger.Error("User \"{0}\" is not the same as in the URI. Forbidden", principal.Identity.Name);
                    Challenge(actionContext);
                    return;
                }
                NLogger.Logger.Debug("Authorization complete");
                base.OnAuthorization(actionContext);
            }
            catch (Exception e)
            {
                NLogger.Logger.Error("Some ids are invalid. E: {0}", (object)e);
                Challenge(actionContext);
            }
        }

        private void Challenge(HttpActionContext actionContext)
        {
            NLogger.Logger.Debug("Adding challenge");
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
        }
    }
}