using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters.Authorization
{
    /// <inheritdoc />
    /// <summary>
    /// Checks whether the user is in the same chat as the message.
    /// MessageId is extracted from the URI using <see cref="P:DotNetMessenger.WebApi.Filters.Authorization.MessageFromChatUserAuthorizationAttribute.RegexString" />
    /// </summary>
    public class MessageFromChatUserAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; } = "localhost";

        public string RegexString { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                NLogger.Logger.Debug("Authorizing user if message sender is in chat");
                if (string.IsNullOrEmpty(RegexString))
                {
                    NLogger.Logger.Fatal("RegexString to get message id is not set. Cannot authorize");
                    Challenge(actionContext);
                    return;
                }

                // extract string matching regex
                NLogger.Logger.Debug("Parsing message id using regex {0}", RegexString);
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success)
                {
                    NLogger.Logger.Error("Failed to parse message id from uri");
                    Challenge(actionContext);
                    return;
                }
                // parse it to message id
                NLogger.Logger.Debug("Parsing string messageId to int");
                var messageId = int.Parse(m.Groups[1].Value);

                // get principal
                if (!(Thread.CurrentPrincipal is UserPrincipal principal))
                {
                    NLogger.Logger.Fatal("Principal is not set. User is not authenticated?");
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                NLogger.Logger.Debug("Fetching message from repository. MessageID: {0}", messageId);
                var message = RepositoryBuilder.MessagesRepository.GetMessage(messageId);

                NLogger.Logger.Debug("Checking if the user is in chat. UserID: {0}", principal.UserId);
                if (RepositoryBuilder.ChatsRepository.GetChatUsers(message.ChatId).All(x => x.Id != principal.UserId))
                {
                    NLogger.Logger.Error("User {0} is not in chat. Forbidden", principal.UserId);
                }

                NLogger.Logger.Debug("Authorization of user {0} is successful", principal.UserId);
                base.OnAuthorization(actionContext);
            }
            catch
            {
                // throws if chat is dialog or user is not in chat or no such chat exists
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