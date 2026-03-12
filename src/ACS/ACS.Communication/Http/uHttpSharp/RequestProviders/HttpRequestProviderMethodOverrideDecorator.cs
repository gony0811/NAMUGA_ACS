using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ACS.Communication.Http.uHttpSharp.RequestProviders
{
    public class HttpRequestProviderMethodOverrideDecorator : IHttpRequestProvider
    {
        private readonly IHttpRequestProvider _child;

        public HttpRequestProviderMethodOverrideDecorator(IHttpRequestProvider child)
        {
            _child = child;
        }

        public async Task<IHttpRequest> Provide(IStreamReader streamReader)
        {
            var childValue = await _child.Provide(streamReader).ConfigureAwait(false);

            if (childValue == null)
            {
                return null;
            }

            string methodName;
            if (!childValue.Headers.TryGetByName("X-HTTP-Method-Override", out methodName))
            {
                return childValue;
            }

            return new HttpRequestMethodDecorator(childValue, HttpMethodProvider.Default.Provide(methodName));
        }
    }
}