using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Rudine.Util
{
    internal static class DictionaryExtensions
    {
        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> d)
        {
            NameValueCollection _NameValueCollection = new NameValueCollection();
            if (d != null)
                if (d.Count > 0)
                    foreach (KeyValuePair<string, string> item in d)
                        _NameValueCollection[item.Key] = item.Value;
            return _NameValueCollection;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, List<string>> d)
        {
            NameValueCollection _NameValueCollection = new NameValueCollection();
            foreach (string key in d.Keys)
            foreach (string val in d[key])
                _NameValueCollection.Add(key, val);

            return _NameValueCollection;
        }

        public static Type GetEnumeratedType<T>(this IEnumerable<T> _) { return typeof(T); }
    }
}