using System;
using System.Collections.Generic;
using System.Linq;

namespace Rudine.Storage.Sql.Merge
{
    public class ClassSignature : IEquatable<ClassSignature>
    {
        public readonly int hashCode;
        public readonly ClassProperty[] properties;

        public ClassSignature(string nameSpace, IEnumerable<ClassProperty> properties)
        {
            this.properties = properties.ToArray();
            hashCode = nameSpace.GetHashCode();
            foreach (ClassProperty p in properties.OrderBy(p => p.Name))
                hashCode ^= p.Name.GetHashCode() ^ p.PropertyType.GetHashCode();
        }

        public bool Equals(ClassSignature other)
        {
            if (properties.Length != other.properties.Length) return false;
            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Name != other.properties[i].Name ||
                    properties[i].PropertyType != other.properties[i].PropertyType)
                    return false;
            return true;
        }

        public override bool Equals(object obj) => obj is ClassSignature ? Equals((ClassSignature) obj) : false;

        public override int GetHashCode() => hashCode;
    }
}