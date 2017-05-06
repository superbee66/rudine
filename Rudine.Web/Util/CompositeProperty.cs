using System;

namespace Rudine.Web.Util
{
    public class CompositeProperty
    {
        public CompositeProperty(string name, Type type)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            Name = name;
            PropertyType = type;
        }

        public string Name { get; private set; }

        public Type PropertyType { get; set; }
    }
}