using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rudine.Tests {
    public static class TypeExtensions
    {
        /// <summary>
        ///     If the given <paramref name="collectionType" /> is an array or some other collection
        ///     comprised of 0 or more instances of a "subtype", get that type
        /// </summary>
        /// <param name="collectionType">the source type</param>
        /// <returns></returns>
        public static Type GetEnumeratedType(this Type collectionType)
        {
            if (collectionType != typeof(byte[])
                && collectionType != typeof(string)
                && collectionType != typeof(byte[])
                && collectionType != typeof(string))
            {
                // provided by Array
                Type elType = !collectionType.IsArray ? null : collectionType.GetElementType();
                if (null != elType)
                    return elType;

                // otherwise provided by collection
                var types = collectionType.GetInterfaces()
                                          .Where(x => x.IsGenericType
                                                      && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                          .ToArray();
                // Only support collections that implement IEnumerable<T> once.
                return types.Length == 1 ? types[0].GetGenericArguments()[0] : null;
            }
            // otherwise is not an 'enumerated' type
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The underlying & possibly enumerated type when dealing with collections</returns>
        public static Type GetPrincipleType(this Type type)
        {
            return type.GetEnumeratedType() != null
                   && type != typeof(byte[])
                   && type != typeof(string)
                       ? GetPrincipleType(type.GetEnumeratedType())
                       : Nullable.GetUnderlyingType(type)
                         ?? type;
        }

        public static bool IsCastableTo(this Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
                return true;
            return from.GetMethods(BindingFlags.Public | BindingFlags.Static)
                       .Any(m => m.ReturnType == to &&
                                 (m.Name == "op_Implicit" ||
                                  m.Name == "op_Explicit"));
        }

        public static bool isCollection(this Type type)
        {
            return type != typeof(byte[])
                   && type != typeof(string)
                   && type != typeof(byte[])
                   && type != typeof(string)
                   && type.isEnumeratedType();
        }

        /// <summary>
        ///     If the given <paramref name="type" /> is an array or some other collection
        ///     comprised of 0 or more instances of a "subtype", get that type
        /// </summary>
        /// <param name="type">the source type</param>
        /// <returns></returns>
        public static bool isEnumeratedType(this Type type) { return type.GetEnumeratedType() != null; }

        public static bool IsNullable(this Type type)
        {
            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}