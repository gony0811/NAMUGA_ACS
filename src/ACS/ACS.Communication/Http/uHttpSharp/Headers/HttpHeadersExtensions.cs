using System;
using System.Text;

namespace ACS.Communication.Http.uHttpSharp.Headers
{
    public static class HttpHeadersExtensions
    {
        public static bool KeepAliveConnection(this IHttpHeaders headers)
        {
            //기존 부분
            //string value;
            //return headers.TryGetByName("connection", out value)
            //    && value.Equals("Keep-Alive", StringComparison.InvariantCultureIgnoreCase);

            //SJH 2019.03.15. : Read Stream에서 Request 대기중에 Exception발생으로 해당 부분에서 대기하지 않도록 수정.. 모니터링 필요..
            return false;
        }

        public static bool TryGetByName<T>(this IHttpHeaders headers, string name, out T value)
        {
            string stringValue;
            
            if (headers.TryGetByName(name, out stringValue))
            {
                value = (T) Convert.ChangeType(stringValue, typeof(T));
                return true;
            }

            value = default(T);
            return false;
        }

        public static T GetByName<T>(this IHttpHeaders headers, string name)
        {
            T value;
            headers.TryGetByName(name, out value);
            return value;
        }

        public static T GetByNameOrDefault<T>(this IHttpHeaders headers, string name, T defaultValue)
        {
            T value;
            if (headers.TryGetByName(name, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public static string ToUriData(this IHttpHeaders headers)
        {
            var builder = new StringBuilder();

            foreach (var header in headers)
            {
                builder.AppendFormat("{0}={1}&", Uri.EscapeDataString(header.Key), Uri.EscapeDataString(header.Value));
            }

            return builder.ToString(0, builder.Length - 1);
        }
    }
}