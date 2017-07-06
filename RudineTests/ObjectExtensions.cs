using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RudineTests
{
    public static class ObjectExtensions
    {
        public static T Overlay<T, TT>(this T o, string propertyNameWildcardExpression, out HashSet<TT> existingValues) where T : class
        {
            existingValues = new HashSet<TT>();
            return Walk(o, propertyNameWildcardExpression, ref existingValues, Type.Missing);
        }

        public static T Overlay<T, TT>(this T o, string propertyNameWildcardExpression, TT newValue) where T : class
        {
            HashSet<TT> hashSet = new HashSet<TT>();
            return Walk(o, propertyNameWildcardExpression, ref hashSet, newValue);
        }

        private static T Walk<T, TT>(T o, string propertyNameWildcardExpression, ref HashSet<TT> distinct, object newValue) where T : class
        {
            if (o != null)
                foreach (PropertyInfo p in o.GetType().GetProperties())
                    if (p.CanRead)
                        if (p.CanWrite)
                        {
                            var existing = p.GetValue(o, null);

                            if (GlobMatch(p.Name, propertyNameWildcardExpression) && (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == typeof(TT))
                            {
                                if (existing != null)
                                    distinct.Add((TT)existing);
                                p.SetValue(o, newValue, null);
                            }
                            else if (typeof(IList).IsAssignableFrom(p.PropertyType) && existing != null)
                            {
                                IList l = (IList)existing;
                                for (int i = 0; i < l.Count; i++)
                                    l[i] = Walk(l[i], propertyNameWildcardExpression, ref distinct, newValue);
                            }
                            else if (typeof(IDictionary).IsAssignableFrom(p.PropertyType) && existing != null)
                            {
                                IDictionary d = (IDictionary)existing;
                                var keys = d.Keys.OfType<object>().ToArray();
                                foreach (var key in keys)
                                    d[key] = Walk(d[key], propertyNameWildcardExpression, ref distinct, newValue);
                            }
                            else
                            {
                                p.SetValue(o, Walk(existing, propertyNameWildcardExpression, ref distinct, newValue), null);
                            }
                        }
            return o;
        }

        public static bool GlobMatch(string input, string wildcardExpression) =>
            input == wildcardExpression ||
            Regex.IsMatch(input,
              Regex.Escape(wildcardExpression)
                .Replace(@"\*", ".*").Replace(@"\?", "."));

    }
}