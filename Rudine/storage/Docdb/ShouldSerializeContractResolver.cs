using System.Reflection;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Rudine.Storage.Docdb
{
    /// <summary>
    ///     used by indexer to extract content from object while avoiding properties like signatures & bitmaps (human
    ///     unreadable). functionality provided is a quasi-iFilter for .Net objects.
    /// </summary>
    internal class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = instance
                                           => property.PropertyType != typeof(byte[])
                                              && property.PropertyType != typeof(XmlElement[])
                                              && property.PropertyType != typeof(bool);
            return property;
        }
    }
}