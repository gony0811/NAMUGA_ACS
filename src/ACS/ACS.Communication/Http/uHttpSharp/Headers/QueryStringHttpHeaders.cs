using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;namespace ACS.Communication.Http.uHttpSharp.Headers
{
    [DebuggerDisplay("{Count} Query String Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    internal class QueryStringHttpHeaders : IHttpHeaders
    {
        private readonly HttpHeaders _child;
        //private static readonly char[] Seperators = {'&', '='};

        private static readonly char[] Seperators1 = { '&' };
        private static readonly char[] Seperators2 = { '=' };

        private readonly int _count;

        public QueryStringHttpHeaders(string query)
        {
            string[] splittedKeyValues = query.Split(Seperators1, StringSplitOptions.RemoveEmptyEntries);
            var values = new Dictionary<string, string>(splittedKeyValues.Length, StringComparer.InvariantCultureIgnoreCase);

            //var splittedKeyValues = query.Split(Seperators, StringSplitOptions.RemoveEmptyEntries);
            //var values = new Dictionary<string, string>(splittedKeyValues.Length / 2, StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < splittedKeyValues.Length; i++)
            {
                var splitted = splittedKeyValues[i].Split(Seperators2, StringSplitOptions.RemoveEmptyEntries);
                int j = 0;
                var key = Uri.UnescapeDataString(splitted[j]);
                string value = null;
                if (splitted.Length == 1)
                    value = "";
                else
                    value = Uri.UnescapeDataString(splitted[j + 1]).Replace('+', ' ');

                values[key] = value;
            }

            //for (int i = 0; i < splittedKeyValues.Length; i += 2)
            //{
            //    var key = Uri.UnescapeDataString(splittedKeyValues[i]);
            //    string value = null;
            //    if (splittedKeyValues.Length > i + 1)
            //    {
            //        value = Uri.UnescapeDataString(splittedKeyValues[i + 1]).Replace('+', ' ');    
            //    }

            //    values[key] = value;
            //}

            _count = values.Count;
            _child = new HttpHeaders(values);
        }

        public string GetByName(string name)
        {
            return _child.GetByName(name);
        }
        public bool TryGetByName(string name, out string value)
        {
            return _child.TryGetByName(name, out value);
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _child.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count
        {
            get
            {
                return _count;
            }
        }
    }
}