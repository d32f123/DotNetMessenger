using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
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
                // parse it to message id
                var messageId = int.Parse(m.Groups[1].Value);

                // get principal
                var principal = Thread.CurrentPrincipal as UserPrincipal;
                if (principal == null)
                {
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                var message = RepositoryBuilder.MessagesRepository.GetMessage(messageId);

                var chatInfo = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(principal.UserId, message.ChatId);
                var userRole = chatInfo?.Role ?? RepositoryBuilder.ChatsRepository.GetUserRole(UserRoles.Regular);

                if ((userRole.RolePermissions & RolePermissions.ReadPerm) == 0)
                {
                    Challenge(actionContext);
                    return;
                }
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
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
        }
    }
}