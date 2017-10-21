using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters.Authorization
{
    public class UserIsInChatAuthorizationAttribute : AuthorizationFilterAttribute
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
                // parse it to chat id
                var chatId = int.Parse(m.Groups[1].Value);

                // get principal
                var principal = Thread.CurrentPrincipal as UserPrincipal;
                if (principal == null)
                {
                    Challenge(actionContext);
                    return;
                }

                // check if user is in chat
                if (!RepositoryBuilder.ChatsRepository.GetChat(chatId).Users.Contains(principal.UserId))
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