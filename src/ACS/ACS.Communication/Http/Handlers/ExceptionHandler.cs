using System;
using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp;

namespace ACS.Communication.Http.Handler
{
    public class ExceptionHandler : IHttpRequestHandler
    {
        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (HttpException e)
            {
                context.Response = new HttpResponse(e.ResponseCode, "Error while handling your request. " + e.Message, false);
            }
            catch (Exception e)
            {
                context.Response = new HttpResponse(HttpResponseCode.InternalServerError, "Error while handling your request. " + e, false);
            }
        }
    }

    public class HttpException : Exception
    {
        private readonly HttpResponseCode _responseCode;

        public HttpResponseCode ResponseCode
        {
            get { return _responseCode; }
        }

        public HttpException(HttpResponseCode responseCode)
        {
            _responseCode = responseCode;
        }
        public HttpException(HttpResponseCode responseCode, string message) : base(message)
        {
            _responseCode = responseCode;
        }
    }
}