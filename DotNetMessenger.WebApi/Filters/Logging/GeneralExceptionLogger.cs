using System.Web.Http.ExceptionHandling;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Filters.Logging
{
    public class GeneralExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            NLogger.Logger.Error("Unhandled error, caught by {0}: {1}", GetType().Name, context.Exception);
        }
    }
}