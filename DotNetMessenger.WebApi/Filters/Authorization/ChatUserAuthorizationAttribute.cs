using System;
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
    /// Authorizes based on user <see cref="Permissions"/> in the given chat.
    /// Chat id is extracted from the uri using <see cref="P:DotNetMessenger.WebApi.Filters.Authorization.ChatUserAuthorizationAttribute.RegexString" /> string
    /// </summary>
    public class ChatUserAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; } = "localhost";

        public string RegexString { get; set; }
        public RolePermissions Permissions { get; set; } = RolePermissions.ReadPerm;

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                NLogger.Logger.Debug("Authorizing user if user is in chat and has permission: {0}", Permissions);
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
                    NLogger.Logger.Error("Failed to parse chat id from uri");
                    Challenge(actionContext);
                    return;
                }
                // parse it to chat id
                NLogger.Logger.Debug("Parsing string chatId to int");
                var chatId = int.Parse(m.Groups[1].Value);

                // get principal
                if (!(Thread.CurrentPrincipal is UserPrincipal principal))
                {
                    NLogger.Logger.Fatal("Principal is not set. User is not authenticated?");
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                NLogger.Logger.Debug("Getting user role");
                var chatInfo = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(principal.UserId, chatId);
                var userRole = chatInfo?.Role ?? RepositoryBuilder.ChatsRepository.GetUserRole(UserRoles.Regular);
                NLogger.Logger.Debug("Fetched role: {0}", userRole);
                
                // check for permissions
                if (Enum.GetValues(typeof(RolePermissions)).Cast<RolePermissions>().Any(perm => (perm & Permissions) != 0 && (userRole.RolePermissions & perm) == 0))
                {
                    NLogger.Logger.Error("Not enough permission! Required: {0}. Got: {1}", Permissions, userRole.RolePermissions);
                    Challenge(actionContext);
                    return;
                }
                NLogger.Logger.Debug("Authorization of userID {0} is successful", principal.UserId);
                base.OnAuthorization(actionContext);
            }
            catch (Exception e)
            {
                NLogger.Logger.Error("No such user or incorrect chat type. E: {0}", (object)e);
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