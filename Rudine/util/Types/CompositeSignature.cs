using System;
using System.Collections.Generic;
using System.Linq;
using Rudine.Web.Util;

namespace Rudine.Util.Types
{
    internal class CompositeSignature : IEquatable<CompositeSignature>
    {
        internal readonly Type[] _SourceTypes;
        internal readonly string[] _PropertyNameExclutions;
        internal readonly Type _BaseType;
        internal readonly bool _PrettyNames;
        internal readonly CompositeProperty[] _ClassProperties;
        internal readonly string _FullName;
        private Dictionary<string, Type> _MergedProperties;
        private int _HashCode;

        public Dictionary<string, Type> MergedProperties
        {
            get
            {
                if (_MergedProperties == null)
                    ConsolidateProperties();
                return _MergedProperties;
            }
        }

        public CompositeSignature(Type[] sourceTypes = null, string[] propertyNameExclutions = null, Type baseType = null, bool prettyNames = true, CompositeProperty[] ClassProperties = null)
        {
            if (ClassProperties == null)
                ClassProperties = new CompositeProperty[]
                    { };

            if (propertyNameExclutions == null)
                propertyNameExclutions = new string[]
                    { };

            if (sourceTypes == null)
                sourceTypes = new Type[]
                    { };

            else if (baseType == null)
                baseType = DefaultBaseType(sourceTypes);

            if (baseType != null)
                propertyNameExclutions =
                    baseType
                        .GetProperties()
                        .Select(p => p.Name)
                        .Union(propertyNameExclutions)
                        .ToArray();

            _SourceTypes = sourceTypes;
            _PropertyNameExclutions = propertyNameExclutions;
            _BaseType = baseType;
            _PrettyNames = prettyNames;
            _ClassProperties = ClassProperties;
        }

        private void ConsolidateProperties(bool calcMergedProperties = true)
        {
            Dictionary<string, Type> _mergedProperties = new Dictionary<string, Type>();
            Dictionary<string, int> _mergedPropertyHashes = new Dictionary<string, int>
            {
                { "Base_Type", _BaseType == null ? 0 : new CompositeSignature(new[] { _BaseType }, null, null, false).GetHashCode() }
            };

            var PropertiesByName = FilterInvalidProperties(_SourceTypes, _PropertyNameExclutions, _PrettyNames)
                .Select(p => new CompositeProperty(p.Name, p.PropertyType))
                .Union(_ClassProperties)
                .Select(classproperty => new
                {
                    MergeName = _PrettyNames ? pretty(classproperty.Name) : classproperty.Name,
                    SourceName = classproperty.Name,
                    SourceType = classproperty.PropertyType,
                    PrincipleType = classproperty.PropertyType.GetPrincipleType()
                })
                .Select(SourceProperty => new
                {
                    SourceProperty,
                    MergeFactors = new
                    {
                        isCollection = SourceProperty.SourceType.isCollection(),
                        SourceProperty.PrincipleType.IsEnum,
                        IsPrimitive = SourceProperty.PrincipleType.isSimple(),
                        SourceProperty.SourceType.IsArray
                    }
                })
                .GroupBy(m => m.SourceProperty.MergeName)
                .ToDictionary(m => m.Key);

            // do simple value/quazi-value type properties first
            // notice there is no OrderBy as we want to maintain the original layout of the properties as long as possible
            // only when the _hashcode is calculated do we ignore order
            foreach (string MergeName in PropertiesByName.Keys)
            {
                if (1 != PropertiesByName[MergeName].Select(m =>
                                                                m.MergeFactors.isCollection.GetHashCode()
                                                                ^
                                                                m.MergeFactors.IsEnum.GetHashCode()
                                                                ^
                                                                m.MergeFactors.IsPrimitive.GetHashCode()
                                                                ^
                                                                m.MergeFactors.IsArray.GetHashCode()
                    ).Distinct().Count())
                    throw new Exception(String.Format("{0} as the target Name for a new PropertyType can't be created. Merging between collection, enum & primitive (vs non) Types is not supported", MergeName));

                var MergeFactors = PropertiesByName[MergeName].Select(m => m.MergeFactors).First();

                if (MergeFactors.IsPrimitive)
                {
                    foreach (var _ClassProperty in PropertiesByName[MergeName])
                    {
                        // handle normal numeric, byte, string & datetime types
                        Type typeA = _ClassProperty.SourceProperty.SourceType;
                        Type typeB = _mergedProperties.ContainsKey(MergeName)
                                         ? _mergedProperties[MergeName]
                                         : _ClassProperty.SourceProperty.SourceType;
                        Type typeC = ImplicitTypeConversionExtension.TypeLcd(typeA.GetPrincipleType(), typeB.GetPrincipleType());

                        // there are many situations that will yield a nullable

                        if (typeC == typeof(string))
                            _mergedProperties[MergeName] = typeC;
                        else if (typeC == typeof(byte[]))
                            _mergedProperties[MergeName] = typeC;
                        else if (Nullable.GetUnderlyingType(typeA) != null || Nullable.GetUnderlyingType(typeB) != null)
                            _mergedProperties[MergeName] = typeof(Nullable<>).MakeGenericType(typeC);
                        else if (_SourceTypes.Any() && PropertiesByName[MergeName].Count() != _SourceTypes.Count())
                            _mergedProperties[MergeName] = typeof(Nullable<>).MakeGenericType(typeC);
                        else
                            _mergedProperties[MergeName] = typeC;
                    }
                    _mergedPropertyHashes[MergeName] = _mergedProperties[MergeName].GetHashCode();
                } else if (MergeFactors.IsEnum)
                {
                    //TODO:Check for enum item collision & apply merging of multiple enum type when everything is OK
                    _mergedProperties[MergeName] = PropertiesByName[MergeName].Select(m => m.SourceProperty.SourceType).First();
                    _mergedPropertyHashes[MergeName] = _mergedProperties[MergeName].GetHashCode();
                } else
                {
                    Type[] dstTypes = PropertiesByName[MergeName].Select(m => m.SourceProperty.PrincipleType).ToArray();
                    // create the CompositeType that represents the new property's Type only if it's specifically asked for
                    if (calcMergedProperties)
                    {
                        _mergedProperties[MergeName] = new CompositeType(
                            DefaultNamespace(dstTypes),
                            DefaultClassName(dstTypes, _PrettyNames),
                            dstTypes,
                            null,
                            _PropertyNameExclutions,
                            _PrettyNames);

                        if (MergeFactors.isCollection)
                            _mergedProperties[MergeName] = typeof(List<>).MakeGenericType(_mergedProperties[MergeName]);
                    }
                    _mergedPropertyHashes[MergeName] = new CompositeSignature(dstTypes, _PropertyNameExclutions, null, _PrettyNames).GetHashCode();
                }
            }

            _HashCode = 0;
            foreach (int HashCode in _mergedPropertyHashes.OrderBy(m => m.Key).Select(m1 => m1.Key.GetHashCode() ^ m1.Value))
                _HashCode ^= HashCode;

            if (calcMergedProperties)
                _MergedProperties = _mergedProperties;
        }

        private static CompositeProperty[] FilterInvalidProperties(Type[] sourceTypes, string[] PropertyNameExclutions, bool prettyNames, string[] targetPropertyNames = null, CompositeProperty[] ClassPropertyInclusions = null)
        {
            return sourceTypes
                .SelectMany(t => t
                                .GetProperties()
                                .Where(_PropertyInfo => _PropertyInfo.PropertyType.IsSerializable && _PropertyInfo.CanRead && _PropertyInfo.CanWrite))
                .Select(p => new CompositeProperty(p.Name, p.PropertyType))
                .Union(ClassPropertyInclusions ?? new CompositeProperty[]
                           { })
                .Where(ClassProperty =>
                           (prettyNames
                                ? !PropertyNameExclutions.Any(Name => pretty(Name) == pretty(ClassProperty))
                                : !PropertyNameExclutions.Any(Name => Name == ClassProperty.Name))
                           &&
                           (targetPropertyNames == null || targetPropertyNames.Length == 0 || (prettyNames
                                                                                                   ? targetPropertyNames.Any(Name => pretty(Name) == pretty(ClassProperty))
                                                                                                   : targetPropertyNames.Any(Name => Name == ClassProperty.Name))))
                .Distinct()
                .ToArray();
        }

        private static readonly Dictionary<string, string> _prettyDictionary = new Dictionary<string, string>();

        private static string pretty(string name) => 
            _prettyDictionary.ContainsKey(name)
                                                         ? _prettyDictionary[name]
                                                         : (_prettyDictionary[name] = StringTransform.PrettyMsSqlIdent(name));

        private static string pretty(CompositeProperty _ClassProperty) =>
            pretty(_ClassProperty.Name);

        internal string DefaultClassName(Type[] sourceTypes, bool prettyNames = true) =>
            string.Join("_",
            sourceTypes
                .Select(t => prettyNames ? pretty(t.Name) : t.Name)
                .Distinct()
                .OrderBy(s => s));

        public static Type DefaultBaseType(Type[] sourceTypes)
        {
            IEnumerable<Type> _BaseTypes = sourceTypes
                .Select(t => t.BaseType)
                .Where(BaseType => BaseType != typeof(object) && BaseType != typeof(Object))
                .Distinct();

            return _BaseTypes.Count() == 1 ? _BaseTypes.First() : null;
        }

        public static string DefaultNamespace(Type[] sourceTypes) =>
            string.Join(".", sourceTypes.Select(t => t.Namespace).Distinct().ToArray());

        public override int GetHashCode()
        {
            if (_HashCode == 0)
                ConsolidateProperties(false);
            return _HashCode;
        }

        public override bool Equals(object obj) =>
            obj is CompositeSignature ? Equals((CompositeSignature) obj) : false;
        public bool Equals(CompositeSignature other) => 
            GetHashCode().Equals(other.GetHashCode());
    }
}