using System;
using System.Collections.Generic;
using System.Linq;
using Rudine.Web.Util;

namespace Rudine.Storage.Sql.Merge
{
    /// <summary>
    ///     TODO:Clean this up
    /// </summary>
    public static class ImplicitTypeConversionExtension
    {
        public static readonly Dictionary<Type, Type[]> ImplicitConversions = new Dictionary<Type, Type[]>
        {
            {typeof(double), new[] {typeof(double), typeof(string)}},
            {typeof(string), new[] {typeof(string)}},
            {typeof(DateTime), new[] {typeof(string)}},
            {typeof(sbyte), new[] {typeof(string), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(byte), new[] {typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(short), new[] {typeof(string), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(ushort), new[] {typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(int), new[] {typeof(string), typeof(long), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(uint), new[] {typeof(string), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(long), new[] {typeof(string), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(char), new[] {typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(float), new[] {typeof(string), typeof(double)}},
            {typeof(ulong), new[] {typeof(string), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(bool), new[] {typeof(decimal), typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(long), typeof(short), typeof(string), typeof(uint), typeof(ulong), typeof(ushort)}}
        };

        public static bool ConvertsToImplicitly(this Type T, Type targetType)
        {
            return
                T == targetType
                ||
                T == (Nullable.GetUnderlyingType(targetType) ?? targetType)
                ||
                ImplicitConversions.ContainsKey(Nullable.GetUnderlyingType(T) ?? T)
                &&
                ImplicitConversions[Nullable.GetUnderlyingType(T) ?? T]
                    .Any(t => t == (Nullable.GetUnderlyingType(targetType) ?? targetType));
        }

        public static bool hasConvert(this Type T)
        {
            return ImplicitConversions.Keys.Any(m => m.IsCastableTo(Nullable.GetUnderlyingType(T) ?? T));
        }

        public static Type TypeLcd(params Type[] samples)
        {
            return samples.Where(a => !samples.Any(b => !ConvertsToImplicitly(a, b))).Distinct().FirstOrDefault();
        }
    }
}