using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using DotNetMessenger.DataLayer.SqlServer;
using DotNetMessenger.Logger;
using DotNetMessenger.WebApi.Filters.Authentication;
using DotNetMessenger.WebApi.Filters.Logging;
using DotNetMessenger.WebApi.Principals;

namespace DotNetMessenger.WebApi.Controllers
{
    [ExpectedExceptionsFilter]
    [RoutePrefix("api/tokens")]
    public class TokensController : ApiController
    {
        /// <summary>
        /// Generates a new token for the user. Use basic authentication
        /// </summary>
        /// <returns>A new token for other API controllers</returns>
        [Route("")]
        [HttpPost]
        [UserBasicAuthentication]
        [Authorize]
        public Guid GenerateUserToken()
        {
            NLogger.Logger.Debug("Called");
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("Principal is not set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));
            }
            var principal = (UserPrincipal)Thread.CurrentPrincipal;

            using (var timeLog = new ChronoLogger("Generating token for user {0}", principal.UserId))
            {
                timeLog.Start();
                var token = RepositoryBuilder.TokensRepository.GenerateToken(principal.UserId);
                NLogger.Logger.Info("Token generated for user {0}", principal.UserId);
                return token;
            }
        }
        /// <summary>
        /// Gets the user id from the token. Use token authentication
        /// </summary>
        /// <returns>User id</returns>
        [Route("")]
        [HttpGet]
        [TokenAuthentication]
        [Authorize]
        public int GetUserIdFromToken()
        {
            NLogger.Logger.Debug("Called");
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("Principal is not set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));
            }

            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            NLogger.Logger.Info("UserID successfully fetched: {0}", principal.UserId);
            return principal.UserId;
        }
        /// <summary>
        /// Invalidates given token. Use on logout
        /// </summary>
        [Route("")]
        [HttpDelete]
        [TokenAuthentication]
        [Authorize]
        public void DeleteUserToken()
        {
            NLogger.Logger.Debug("Called");
            if (!(Thread.CurrentPrincipal is UserPrincipal))
            {
                NLogger.Logger.Fatal("Principal is not set");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Server broken"));
            }

            var principal = (UserPrincipal)Thread.CurrentPrincipal;
            RepositoryBuilder.TokensRepository.InvalidateToken(principal.Token);
            NLogger.Logger.Info("User token invalidated: {0}", principal.Token);
        }
    }
}
