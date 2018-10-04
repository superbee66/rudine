using System;
using System.CodeDom.Compiler;

namespace Rudine.Web.Util
{
    public class CompositeProperty
    {
        private static readonly CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");

        public CompositeProperty(string name, Type type)
        {
            if (!codeDomProvider.IsValidIdentifier(name))
                throw new ArgumentNullException(nameof(name), "invalid C# identifier");

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Name = name;
            PropertyType = type;
        }

        public CompositeProperty()
        {
        }

        public string Name { get; set; }

        public Type PropertyType { get; set; }
    }
}