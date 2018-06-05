using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rudine.Web;

namespace Rudine.Tests
{
    [TestFixture]
    public class InputTypeMapping
    {
        static string PropertyTypeNameEnd(Type t) =>
           string.Format("_{0}", string.Join("_",
                   new[]
                   {
                       (Nullable.GetUnderlyingType(t) ?? t).Name,
                       Nullable.GetUnderlyingType(t) != null ? nameof(Nullable) : string.Empty,
                       t.IsArray ? nameof(Array) : string.Empty
                   }.Where(s => !string.IsNullOrWhiteSpace(s)))
               .Trim('[', ']', '_')
               .Replace("]", string.Empty)
               .Replace("[", string.Empty));

        static IEnumerable<Type> PropertyTypes()
        {
            List<Type> propertyTypes = typeof(Convert)
                .GetMethods()
                .Where(m => m.GetParameters().Any())
                .Select(m => m.GetParameters()[0].ParameterType)
                .Where(p => p != typeof(Object))
                .Distinct()
                .ToList();

            Type[] enumerable = propertyTypes
                .Union(propertyTypes
                    .Where(type => type != typeof(string) && !type.IsArray)
                    .Select(type => typeof(Nullable<>).MakeGenericType(type))
                )
                .Distinct().ToArray();

            return enumerable;
        }

        [Test]
        [Combinatorial]
        public void InputFieldTypeMappings([DocDataSampleValues] string docTypeName, [ValueSource(nameof(PropertyTypes))] Type propertyType)
        {
            BaseDoc baseDoc = DocExchangeTests.Create(docTypeName);

            PropertyInfo[] properties = baseDoc.GetType().GetProperties();

            Assert.IsTrue(properties.Any(p => p.PropertyType == propertyType && p.Name.EndsWith(PropertyTypeNameEnd(propertyType), StringComparison.InvariantCultureIgnoreCase)), "field_name_here_" + PropertyTypeNameEnd(propertyType) + " to sample");

            Assert.IsFalse(properties.Any(p => p.PropertyType != propertyType && p.Name.EndsWith(PropertyTypeNameEnd(propertyType), StringComparison.InvariantCultureIgnoreCase)), "Invalid data type mapping interpreted");
        }
    }
}