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
    /// Checks that the user that is making the request has the same UserId
    /// as is in the URI. UserId from URI is extracted using <see cref="P:DotNetMessenger.WebApi.Filters.Authorization.UserIdIsIdFromUriAuthorizationAttribute.RegexString" />
    /// </summary>
    public class UserIdIsIdFromUriAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; }

        public string RegexString { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                NLogger.Logger.Debug("Authorization successful if the user making the request is the same as in the URI");
                if (string.IsNullOrEmpty(RegexString))
                {
                    NLogger.Logger.Fatal("RegexString to get id is not set. Cannot authorize");
                    Challenge(actionContext);
                    return;
                }

                // extract string matching regex
                NLogger.Logger.Debug("Parsing user id using regex {0}", RegexString);
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success)
                {
                    NLogger.Logger.Error("Failed to parse id from uri");
                    Challenge(actionContext);
                    return;
                }
                // parse it to user id
                NLogger.Logger.Debug("Parsing string id to int");
                var userId = int.Parse(m.Groups[1].Value);

                // get principal
                if (!(Thread.CurrentPrincipal is UserPrincipal principal))
                {
                    NLogger.Logger.Fatal("Principal is not set. User is not authenticated?");
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                if (userId != principal.UserId)
                {
                    NLogger.Logger.Error("User id is not the same as in the URI. Forbidden");
                    Challenge(actionContext);
                    return;
                }
                NLogger.Logger.Debug("Authorization of user {0} is successful", principal.UserId);
                base.OnAuthorization(actionContext);
            }
            catch (Exception e)
            {
                NLogger.Logger.Error("Some ids were invalid. E: {0}", (object)e);
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