using System;

namespace dCForm.Core.Storage.Sql.Merge
{
    public class ClassProperty
    {
        public ClassProperty(string name, Type type)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (type == null) throw new ArgumentNullException("type");
            Name = name;
            PropertyType = type;
        }

        public string Name { get; }

        public Type PropertyType { get; }
    }
}