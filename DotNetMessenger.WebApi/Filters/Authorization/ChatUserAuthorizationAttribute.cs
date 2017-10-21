using System;
using System.Linq;
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
                // parse it to chat id
                var chatId = int.Parse(m.Groups[1].Value);

                // get principal
                var principal = Thread.CurrentPrincipal as UserPrincipal;
                if (principal == null)
                {
                    Challenge(actionContext);
                    return;
                }

                // check if principal user id is the same as the id extracted from uri
                var chatInfo = RepositoryBuilder.ChatsRepository.GetChatSpecificInfo(principal.UserId, chatId);
                var userRole = chatInfo?.Role ?? RepositoryBuilder.ChatsRepository.GetUserRole(UserRoles.Regular);
                
                // check for permissions
                if (Enum.GetValues(typeof(RolePermissions)).Cast<RolePermissions>().Any(perm => (perm & Permissions) != 0 && (userRole.RolePermissions & perm) == 0))
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

        protected void Challenge(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
        }
    }
}