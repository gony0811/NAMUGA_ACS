using System.Threading.Tasks;

namespace ACS.Communication.Http.uHttpSharp.Handlers
{
    public interface IResponseProvider
    {

        Task<IHttpResponse> Provide(object value, HttpResponseCode responseCode = HttpResponseCode.Ok);

    }
}