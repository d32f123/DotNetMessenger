using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNetMessenger.Logger;

namespace DotNetMessenger.WebApi.Handlers
{
    public class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NLogger.Logger.Trace("Incoming request: {0}", request);

 
            //Allow the request to process further down the pipeline
            var response = await base.SendAsync(request, cancellationToken);

            NLogger.Logger.Trace("Response generated: {0}", response);
            return response;
        }
    }
}