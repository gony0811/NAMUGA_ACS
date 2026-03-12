using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp;

namespace ACS.Communication.Http.Handler
{
    public class ErrorHandler : IHttpRequestHandler
    {
        public Task Handle(IHttpContext context, System.Func<Task> next)
        {
            context.Response = new HttpResponse(HttpResponseCode.NotFound, "These are not the droids you are looking for.", true);
            return Task.Factory.GetCompleted();
        }
    }
}