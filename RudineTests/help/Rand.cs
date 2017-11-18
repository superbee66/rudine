using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rudine.Web.Util;

namespace Rudine.Tests.help
{
    /// <summary>
    ///     Hey Greg,
    ///     Not relevant to production activities between us anymore; I have a full blow version of that “Rand/Mock” poco
    ///     property filling factory class working. I wanted to test it out & show you it’s results as something relevant
    ///     between us. Can you paste or attach your poco to me?
    /// </summary>
    public class Rand
    {
        private readonly Dictionary<string, List<string>> _getSetText = new Dictionary<string, List<string>>();

        public bool bit(object seed = null) { return int32(seed, 0, 1) == 0; }

        public byte byte_(object seed = null, byte min = byte.MinValue, byte max = byte.MaxValue) { return (byte)int16(seed, min, max); }

        public DateTime date(object seed = null, DateTime? min = null, DateTime? max = null) { return datetime(seed, min, max).Date; }

        public DateTime datetime(object seed = null, DateTime? min = null, DateTime? max = null)
        {
            DateTime low = min ?? DateTime.MinValue;
            DateTime top = max ?? DateTime.MaxValue;

            return new DateTime(int64(seed, low.Ticks, top.Ticks));
        }

        //public string text(object seed = null, int min = 1, int max = 50) {
        //    int minwordcount = int32(new[] { seed }, min, max);
        //    int maxwordcount = int32(new[] { seed, seed }, minwordcount, max);
        //    return regex.matches(randomtext, string.format(@"(\w+[^\w]+){{{0},{1}}}", minwordcount, maxwordcount)).cast<match>().orderby(m => int32(new[] { seed, m })).firstordefault().tostring();
        //}

        public Guid guid(object seed = null)
        {
            return new Guid(
                int32(seed),
                int16(seed),
                int16(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed),
                byte_(seed));
        }

        public Int16 int16(object seed = null, int min = Int16.MinValue, Int16 max = Int16.MaxValue) { return (Int16)new Random(seed == null ? 0 : seed.GetHashCode()).Next(min, max); }

        public int int32(object seed = null, int min = int.MinValue, int max = int.MaxValue) { return new Random(seed == null ? 0 : seed.GetHashCode()).Next(min, max); }

        public long int64(object seed = null, long min = long.MinValue, long max = long.MaxValue)
        {
            float r = max - (float)min;
            r *= percent(seed);
            r += min;

            return (long)r;
        }

        public float percent(object seed = null) { return Math.Abs((float)int32(seed)) / int.MaxValue; }

        //private static readonly string RandomText = string.Join(" ", Directory.EnumerateFiles(Environment.ExpandEnvironmentVariables("%WINDIR%"), "*.txt").Take(5).Select(f => File.ReadAllText(f)).ToArray());
        public T obj<T>(T src, object seed = null, int min = 2, int max = 7) => obj_Internal(src, seed, min, max, new Type[] { });

        private T obj_Internal<T>(T src, object seed, int min, int max, Type[] stopTypes)
        {
            if (src != null)
            {
                Type srcType = Nullable.GetUnderlyingType(src.GetType()) ?? src.GetType();
                if (!stopTypes.Any(t => t.Equals(srcType)))
                    if (!srcType.IsArray)
                        if (!typeof(byte[]).Equals(srcType))
                            if (!typeof(IDictionary).IsAssignableFrom(srcType))

                                if (srcType == typeof(string))
                                    src = (T)(object)string.Format("{0} string property placeholder", StringTransform.Wordify(srcType.Name));
                                else if (srcType.IsEnum)
                                {
                                    Array enums = Enum.GetValues(srcType);
                                    src = (T)enums.GetValue(int32(new[] { seed, src, srcType }, 0, enums.Length - 1));
                                }
                                else if (typeof(IEnumerable).IsAssignableFrom(srcType))
                                {
                                    int i = int32(new[] { seed, srcType.Name }, min, max);
                                    IList iList = (IList)Activator.CreateInstance(srcType, i);
                                    Type enumeratedType = srcType.GetEnumeratedType();
                                    enumeratedType = Nullable.GetUnderlyingType(enumeratedType) ?? enumeratedType;

                                    if (!enumeratedType.IsAbstract)//TODO:initialize types that have no parameterless contructors
                                        while (i-- != 0)
                                            iList.Add(obj_Internal(
                                                Activator.CreateInstance(enumeratedType),
                                                new[] { seed, srcType },
                                                min,
                                                max,
                                                stopTypes));
                                    src = (T)iList;
                                }
                                else
                                {
                                    MethodInfo methodInfo = typeof(Rand).GetMethods().OrderBy(m => int32(m)).FirstOrDefault(m => m.ReturnType == srcType && m.Name != "Equals" && m.Name != "obj" && m.Name != "obj_Internal");

                                    if (methodInfo != null)
                                        src = (T)methodInfo.Invoke(this, methodInfo.GetParameters().Select(p => p.Name == "seed" ? new[] { seed, p.Name } : Type.Missing).ToArray());
                                    else
                                    {
                                        foreach (PropertyInfo propertyInfo in srcType.GetProperties())
                                            if (propertyInfo.CanRead)
                                                if (propertyInfo.CanWrite)
                                                    if (propertyInfo.GetValue(src) != null)
                                                        propertyInfo.SetValue(src, obj_Internal(propertyInfo.GetValue(src), new[] { seed, propertyInfo }, min, max, stopTypes.Union(new[] { Nullable.GetUnderlyingType(srcType) ?? srcType }).ToArray()), null);
                                                    else if (propertyInfo.PropertyType == typeof(string))
                                                        propertyInfo.SetValue(src, obj_Internal(string.Empty, new[] { seed, propertyInfo }, min, max, stopTypes), null);
                                                    else if (propertyInfo.PropertyType.GetConstructors().Any(m => m.GetParameters().Count() == 0))
                                                        propertyInfo.SetValue(
                                                            src,
                                                            obj_Internal(Activator.CreateInstance(propertyInfo.PropertyType),
                                                                new[] { seed, propertyInfo },
                                                                min,
                                                                max,
                                                                stopTypes.Union(new[] { Nullable.GetUnderlyingType(srcType) ?? srcType }).ToArray()),
                                                            null);
                                    }
                                }
            }
            return src;
        }
    }
}