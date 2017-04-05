using System;
using System.Linq;
using System.Runtime.Caching;

//TODO:change public visibility of Util classes as they are not what the consuming developer should concentrate on within inteli-sense

namespace Rudine.Web.Util
{
    internal static class CacheMan
    {
        private static MemoryCache _MemoryCache;

        /// <summary>
        ///     makes a single solid key string taking Array datatypes into consideration by performing recursive calls
        /// </summary>
        /// <param name="cacheKeyParts"></param>
        /// <returns></returns>
        private static string MakeKey(params object[] cacheKeyParts)
        {
            return string.Join(
                "__",
                cacheKeyParts.Select(o =>
                                         string.Format("{0}",
                                             o.GetType().IsArray && ((object[]) o).Length > 0
                                                 ? MakeKey(((Array) o).Cast<object>())
                                                 : o)));
        }

        static CacheMan() { Clear(); }

        public static T Cache<T>(Func<T> itemFactory, bool forcedRefresh = false, params object[] cacheKeyParts) where T : class
        {
            string key = MakeKey(cacheKeyParts);
            return Cache(
                itemFactory,
                forcedRefresh,
                cacheKeyParts != null && cacheKeyParts.Length > 0
                    ? key
                    : null);
        }

        /// <summary>
        ///     grabs items from MemoryCache if they are available
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemFactory">the factory to run if the item can't be found in cache</param>
        /// <param name="forcedRefresh">runs the itemFactory regardless</param>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public static T Cache<T>(Func<T> itemFactory, bool forcedRefresh = false, string cacheKey = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                cacheKey = string.Format("{0}", typeof(T));

            if (!_MemoryCache.Contains(cacheKey) || forcedRefresh)
            {
                //TODO:Potential performance issure with conditionally caching values only if there not null
                T o = itemFactory.Invoke();
                if (o == null)
                    return null;
                _MemoryCache[cacheKey] = o;
            }
            return (T) _MemoryCache[cacheKey];
        }

        public static void Clear()
        {
            if (_MemoryCache != null)
                _MemoryCache.Dispose();
            _MemoryCache = new MemoryCache(typeof(CacheMan).Name);
        }
    }
}