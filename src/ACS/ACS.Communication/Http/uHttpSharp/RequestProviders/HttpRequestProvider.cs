using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp.Headers;
using ACS.Communication.Http.uHttpSharp.Logging;

namespace ACS.Communication.Http.uHttpSharp.RequestProviders
{
    public class HttpRequestProvider : IHttpRequestProvider
    {
        private static readonly char[] Separators = { '/' };

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        
        public async Task<IHttpRequest> Provide(IStreamReader reader)
        {
            // parse the http request
            var request = await reader.ReadLine().ConfigureAwait(false);        // "POST /acs_mod HTTP/1.1"

            if (request == null)
                return null;

            var firstSpace = request.IndexOf(' ');
            var lastSpace = request.LastIndexOf(' ');

            var tokens = new []
            {
                request.Substring(0, firstSpace),                                   // tokens[0]="Post"
                request.Substring(firstSpace + 1, lastSpace - firstSpace - 1),      // tokens[1]="acs_mod"
                request.Substring(lastSpace + 1)                                    // tokens[2]="HTTP/1.1"
            };

            if (tokens.Length != 3)
            {
                return null;
            }

            
            var httpProtocol = tokens[2];

            var url = tokens[1];
            var reqUri = url;
            var queryString = GetQueryStringData(ref url);
            var uri = new Uri(url, UriKind.Relative);

            var headersRaw = new List<KeyValuePair<string, string>>();

            // get the headers
            string line;

            while (!string.IsNullOrEmpty((line = await reader.ReadLine().ConfigureAwait(false))))
            {
                string currentLine = line;

                var headerKvp = SplitHeader(currentLine);
                headersRaw.Add(headerKvp);
            }

            IHttpHeaders headers = new HttpHeaders(headersRaw.ToDictionary(k => k.Key, k => k.Value, StringComparer.InvariantCultureIgnoreCase));
            IHttpPost post = await GetPostData(reader, headers).ConfigureAwait(false);      // Post의 Body 가져오기(Command Message... ex)MOVECMD..)

            string verb;
            if (!headers.TryGetByName("_method", out verb))
            {
                verb = tokens[0];
            }
            var httpMethod = HttpMethodProvider.Default.Provide(verb);
            return new HttpRequest(headers, httpMethod, httpProtocol, uri, reqUri,
                uri.OriginalString.Split(Separators, StringSplitOptions.RemoveEmptyEntries), queryString, post);
        }

        private static IHttpHeaders GetQueryStringData(ref string url)
        {
            var queryStringIndex = url.IndexOf('?');
            IHttpHeaders queryString;
            if (queryStringIndex != -1)
            {
                queryString = new QueryStringHttpHeaders(url.Substring(queryStringIndex + 1));
                url = url.Substring(0, queryStringIndex);
            }
            else
            {
                queryString = EmptyHttpHeaders.Empty;
            }
            return queryString;
        }

        private static async Task<IHttpPost> GetPostData(IStreamReader streamReader, IHttpHeaders headers)
        {
            int postContentLength;
            IHttpPost post;
            if (headers.TryGetByName("content-length", out postContentLength) && postContentLength > 0)
            {
                post = await HttpPost.Create(streamReader, postContentLength, Logger).ConfigureAwait(false);
            }
            else
            {
                post = EmptyHttpPost.Empty;
            }
            return post;
        }

        private KeyValuePair<string, string> SplitHeader(string header)
        {
            var index = header.IndexOf(": ", StringComparison.InvariantCultureIgnoreCase);
            return new KeyValuePair<string, string>(header.Substring(0, index), header.Substring(index + 2));
        }

    }
}