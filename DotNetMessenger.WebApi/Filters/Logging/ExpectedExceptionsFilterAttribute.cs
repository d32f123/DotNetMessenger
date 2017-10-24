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
                    NLogger.Logger.Error("{0}: Parameter missing or null: {1}", nameof(ExpectedExceptionsFilterAttribute), ane);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "Required parameter missing or null"));
                case ArgumentException ae:
                    NLogger.Logger.Error("{0}: Parameter was invalid: {1}", nameof(ExpectedExceptionsFilterAttribute), ae);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        "Some ids are invalid"));
                case ChatTypeMismatchException ctme:
                    NLogger.Logger.Error("{0}: Chat type is invalid for the given operation: {1}", nameof(ExpectedExceptionsFilterAttribute), ctme);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        "Cannot do that for this chat typ!"));
                case UserIsCreatorException uice:
                    NLogger.Logger.Error("{0}: Cannot do this action on chat creator: {1}", nameof(ExpectedExceptionsFilterAttribute), uice);
                    throw new HttpResponseException(actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden,
                        "Cannot perform this action on creator"));
            }
        }
    }
}