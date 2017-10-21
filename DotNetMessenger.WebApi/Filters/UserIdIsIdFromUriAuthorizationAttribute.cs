using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Filters
{
    public class UserIdIsIdFromUriAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public override bool AllowMultiple => false;

        public string Realm { get; set; }

        public string RegexString { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                if (string.IsNullOrEmpty(RegexString))
                    Challenge(actionContext);

                // extract string matching regex
                var r = new Regex(RegexString);
                var m = r.Match(actionContext.Request.RequestUri.AbsolutePath);
                // if there is any content
                if (!m.Success) Challenge(actionContext);
                // parse it to user id
                var userId = int.Parse(m.Groups[1].Value);

                // get principal
                var principal = Thread.CurrentPrincipal as UserPrincipal;
                if (principal == null)
                    Challenge(actionContext);

                // check if principal user id is the same as the id extracted from uri
                if (userId != principal.UserId)
                    Challenge(actionContext);
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