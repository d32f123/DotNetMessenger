using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters.Authorization
{
    /// <inheritdoc />
    /// <summary>
    /// Checks whether the user has the same userId as the creator of the chat.
    /// ChatId is extracted from the URI using <see cref="P:DotNetMessenger.WebApi.Filters.Authorization.UserIsChatCreatorAuthorizationAttribute.RegexString" />
    /// </summary>
    public class UserIsChatCreatorAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; }

        public string RegexString { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                NLogger.Logger.Debug("Authorization successful if the user is the creator of the chat");
                if (string.IsNullOrEmpty(RegexString))
                {
                    NLogger.Logger.Fatal("RegexString to get chat id is not set. Cannot authorize");
                    Challenge(actionContext);
                    return;
                }

                // extract string matching regex
                NLogger.Logger.Debug("Parsing chat id using regex {0}", RegexString);
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success)
                {
                    NLogger.Logger.Error("Failed to parse id from uri");
                    Challenge(actionContext);
                    return;
                }
                // parse it to chat id
                NLogger.Logger.Debug("Parsing string id to int");
                var chatId = int.Parse(m.Groups[1].Value);

                // get principal
                if (!(Thread.CurrentPrincipal is UserPrincipal principal))
                {
                    NLogger.Logger.Fatal("Principal is not set. User is not authenticated?");
                    Challenge(actionContext);
                    return;
                }

                // check if the user is the creator
                if (RepositoryBuilder.ChatsRepository.GetChat(chatId).CreatorId != principal.UserId)
                {
                    NLogger.Logger.Error("User {0} is not chat creator. Forbidden", principal.UserId);
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

        protected void Challenge(HttpActionContext actionContext)
        {
            NLogger.Logger.Debug("Adding challenge");
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
        }
    }
}