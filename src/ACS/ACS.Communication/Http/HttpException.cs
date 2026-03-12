using ACS.Communication.Http.uHttpSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Http
{
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
