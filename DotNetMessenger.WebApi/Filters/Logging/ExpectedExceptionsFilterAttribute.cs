using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using DotNetMessenger.DataLayer.SqlServer.Exceptions;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Filters.Logging
{
    public class ExpectedExceptionsFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            var exception = actionExecutedContext.Exception;
            switch (exception)
            {
                case ArgumentNullException ane:
                    NLogger.Logger.Error("Parameter missing or null: {0}", ane);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "Required parameter missing or null"));
                case ArgumentException ae:
                    NLogger.Logger.Error("Parameter was invalid: {0}", ae);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        "Some ids are invalid"));
                case ChatTypeMismatchException ctme:
                    NLogger.Logger.Error("Chat type is invalid for the given operation: {0}", ctme);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        "Cannot do that for this chat typ!"));
                case UserIsCreatorException uice:
                    NLogger.Logger.Error("Cannot do this action on chat creator: {0}", uice);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden,
                        "Cannot perform this action on creator"));
                case UserAlreadyExistsException uaee:
                    NLogger.Logger.Error("User already exists: {0}", uaee);
                    throw new HttpResponseException(
                        actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.Conflict,
                            "User already exists"));
            }
        }
    }
}