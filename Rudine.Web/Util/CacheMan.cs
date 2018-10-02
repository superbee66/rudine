using System;
using System.Linq;
using System.Runtime.Caching;

namespace Rudine.Web.Util
{
    internal static class CacheMan
    {
        private static MemoryCache _MemoryCache;

        /// <summary>
        ///     placeholder for null items to be cached since you can not feed MemoryCache null
        /// </summary>
        private static readonly object _MemoryCacheNullEntry = new object();

        static CacheMan()
        {
            Clear();
        }

        /// <summary>
        ///     grabs items from MemoryCache if they are available
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemFactory">>the factory to run if the item can't be found in cache</param>
        /// <param name="forcedRefresh">runs the itemFactory regardless</param>
        /// <param name="cacheKeyParts">things that will have there ToString() called then concatenated togather to make a key</param>
        /// <returns></returns>
        public static T Cache<T>(Func<T> itemFactory, bool forcedRefresh = false, params object[] cacheKeyParts) where T : class =>
            Cache(
                itemFactory,
                forcedRefresh,
                cacheKeyParts != null && cacheKeyParts.Length > 0
                    ? MakeKey(cacheKeyParts)
                    : null);

        /// <summary>
        ///     grabs items from MemoryCache if they are available
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemFactory">the factory to run if the item can't be found in cache</param>
        /// <param name="forcedRefresh">runs the itemFactory regardless</param>
        /// <param name="cacheKey">default is the type name of the itemFactory passed</param>
        /// <returns></returns>
        public static T Cache<T>(Func<T> itemFactory, bool forcedRefresh = false, string cacheKey = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                cacheKey = string.Format("{0}", typeof(T));

            if (!_MemoryCache.Contains(cacheKey) || forcedRefresh)
                _MemoryCache[cacheKey] = itemFactory.Invoke() ?? _MemoryCacheNullEntry;

            return _MemoryCache[cacheKey] == _MemoryCacheNullEntry
                ? null
                : (T) _MemoryCache[cacheKey];
        }

        public static void Clear()
        {
            _MemoryCache?.Dispose();
            _MemoryCache = new MemoryCache(typeof(CacheMan).Name);
        }

        /// <summary>
        ///     makes a single solid key string taking Array datatypes into consideration by performing recursive calls
        /// </summary>
        /// <param name="cacheKeyParts"></param>
        /// <returns></returns>
        private static string MakeKey(params object[] cacheKeyParts) =>
            string.Join(
                "__",
                cacheKeyParts.Select(o =>
                    string.Format("{0}",
                        o.GetType().IsArray && ((object[]) o).Length > 0
                            ? MakeKey(((Array) o).Cast<object>())
                            : o)));
    }
}