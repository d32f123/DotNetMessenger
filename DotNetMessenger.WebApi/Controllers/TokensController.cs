using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [RoutePrefix("api/tokens")]
    public class TokensController : ApiController
    {
        [Route("")]
        [HttpPost]
        [UserBasicAuthentication]
        [Authorize]
        public Guid GenerateUserToken()
        {
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));
            var principal = (UserPrincipal)Thread.CurrentPrincipal;

            try
            {
                return RepositoryBuilder.TokensRepository.GenerateToken(principal.UserId);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "No user found"));
            }
        }

        [Route("")]
        [HttpGet]
        [TokenAuthentication]
        [Authorize]
        public int GetUserIdFromToken()
        {
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));

            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            return principal.UserId;
        }

        [Route("")]
        [HttpDelete]
        [TokenAuthentication]
        [Authorize]
        public void DeleteUserToken()
        {
            if (!(Thread.CurrentPrincipal is UserPrincipal))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));

            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            try
            {
                RepositoryBuilder.TokensRepository.InvalidateToken(principal.Token);
            }
            catch (ArgumentException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Token is invalid"));
            }
        }
    }
}
