using System.Threading.Tasks;

namespace ACS.Communication.Http.uHttpSharp.Handlers.Compression
{
    public class GZipCompressor : ICompressor
    {
        public static readonly ICompressor Default = new GZipCompressor();

        public string Name
        {
            get { return "gzip"; }
        }
        public Task<IHttpResponse> Compress(IHttpResponse response)
        {
            return CompressedResponse.CreateGZip(response);
        }
    }
}