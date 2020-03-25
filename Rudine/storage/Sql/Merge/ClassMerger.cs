using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Rudine.Web.Util;

namespace Rudine.Storage.Sql.Merge
{
    /// <summary>
    ///     Merges type property sets on property name. Designed specifically for building emitting Types that Entity Framework
    ///     Code First likes. Not suitable as a utility as self & cyclical referencing between peer types has not been
    ///     implemented. Two way references such as these are emitted as "one-way" references; right to left, first type
    ///     retains it's property references.
    /// </summary>
    public class ClassMerger
    {
        public static readonly ClassMerger Instance = new ClassMerger();
        private readonly Dictionary<ClassSignature, Type> classes;
        private readonly ModuleBuilder module;
        static ClassMerger() { } // Trigger lazy initialization of static fields

        private ClassMerger()
        {
            AssemblyName name = new AssemblyName("DynamicNamedClasses");
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

            module = assembly.DefineDynamicModule("Module");
            classes = new Dictionary<ClassSignature, Type>();
        }


        private void GenerateEquals(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(bool), new[]
                {
                    typeof (object)
                });
            ILGenerator gen = mb.GetILGenerator();
            LocalBuilder other = gen.DeclareLocal(tb);
            Label next = gen.DefineLabel();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, tb);
            gen.Emit(OpCodes.Stloc, other);
            gen.Emit(OpCodes.Ldloc, other);
            gen.Emit(OpCodes.Brtrue_S, next);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);
            gen.MarkLabel(next);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                next = gen.DefineLabel();
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(OpCodes.Ldloc, other);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("Equals", new[]
                {
                    ft, ft
                }), null);
                gen.Emit(OpCodes.Brtrue_S, next);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ret);
                gen.MarkLabel(next);
            }
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);
        }

        private void GenerateGetHashCode(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("GetHashCode",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            ILGenerator gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_0);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default"), null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("GetHashCode", new[]
                {
                    ft
                }), null);
                gen.Emit(OpCodes.Xor);
            }
            gen.Emit(OpCodes.Ret);
        }

        private static FieldInfo[] GenerateProperties(TypeBuilder tb, ClassProperty[] properties)
        {
            FieldInfo[] fields = new FieldBuilder[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                ClassProperty dp = properties[i];
                FieldBuilder fb = tb.DefineField("_" + dp.Name, dp.PropertyType, FieldAttributes.Private);
                PropertyBuilder pb = tb.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.PropertyType, null);
                MethodBuilder mbGet = tb.DefineMethod("get_" + dp.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, dp.PropertyType, Type.EmptyTypes);
                ILGenerator genGet = mbGet.GetILGenerator();
                genGet.Emit(OpCodes.Ldarg_0);
                genGet.Emit(OpCodes.Ldfld, fb);
                genGet.Emit(OpCodes.Ret);
                MethodBuilder mbSet = tb.DefineMethod("set_" + dp.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[]
                {
                    dp.PropertyType
                });
                ILGenerator genSet = mbSet.GetILGenerator();
                genSet.Emit(OpCodes.Ldarg_0);
                genSet.Emit(OpCodes.Ldarg_1);
                genSet.Emit(OpCodes.Stfld, fb);
                genSet.Emit(OpCodes.Ret);
                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i] = fb;
            }
            return fields;
        }

        public int CalcClassSignature(string nameSpace, Type[] sourceTypes, string newClassName = null, string[] PropertyNameExclutions = null, bool prettyNames = true, Type BaseType = null)
        {
            return new ClassSignature(
                nameSpace,
                ConsolidatePropertyNames(
                    nameSpace,
                    sourceTypes,
                    PropertyNameExclutions,
                    BaseType,
                    prettyNames)
                ).hashCode;
        }

        private static readonly object classesLock = new object();

        private Type Generate(string nameSpace, string typeName, IEnumerable<ClassProperty> classProperties, Type parrent = null)
        {
            ClassSignature signature = new ClassSignature(nameSpace, classProperties);

            if (!classes.ContainsKey(signature))
            {
                string typeFullName = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.{1}", nameSpace, typeName);
                TypeBuilder _TypeBuilder = parrent == null
                                               ? module.DefineType(typeFullName, TypeAttributes.Class | TypeAttributes.Public)
                                               : module.DefineType(typeFullName, TypeAttributes.Class | TypeAttributes.Public, parrent);
                FieldInfo[] _FieldInfoArray = GenerateProperties(_TypeBuilder, signature.properties);

                GenerateEquals(_TypeBuilder, _FieldInfoArray);
                GenerateGetHashCode(_TypeBuilder, _FieldInfoArray);

                lock (classesLock)
                    if (!classes.ContainsKey(signature))
                        classes[signature] = _TypeBuilder.CreateType();
            }

            return classes[signature];
        }

        /// <summary>
        ///     Recursively merges type properties yielding a single new type covering all the properties for all the types.
        /// </summary>
        /// <param name="nameSpace">
        ///     Stays consistent through recursion ensuring sourceTypes passed and all there property
        ///     child types are defined in the targetNamespace regardless of there source. Properties that are IEnumerable will
        ///     always be returned as arrays of the UnionOnName child type.
        /// </param>
        /// <param name="sourceTypes"></param>
        /// <returns>Emitted type with joined names of sourceTypes</returns>
        public Type MergeOnPropertyNames(string nameSpace, Type[] sourceTypes, string newClassName = null, string[] propertyNameExclutions = null, bool prettyNames = true, Type baseType = null)
        {
            if (baseType == null)
                baseType = DefaultBaseType(sourceTypes);

            if (string.IsNullOrWhiteSpace(newClassName))
                newClassName = string.Join("_",
                    sourceTypes
                    .Select(t => prettyNames ? pretty(t.Name) : t.Name)
                    .Distinct()
                    .OrderBy(s => s));

            return Generate(nameSpace, newClassName,
                ConsolidatePropertyNames(
                    nameSpace,
                    sourceTypes,
                    propertyNameExclutions,
                    baseType,
                    prettyNames), baseType);
        }

        public Type MergeOnPropertyNames(string nameSpace, ClassProperty[] classProperties, string newClassName, string[] propertyNameExclutions = null, bool prettyNames = true, Type baseType = null)
        {
            var _ConsolidatePropertyNames = ConsolidatePropertyNames(
                   nameSpace,
                   null,
                   propertyNameExclutions,
                   baseType,
                   prettyNames,
                   classProperties);

            return Generate(nameSpace, newClassName, _ConsolidatePropertyNames, baseType);
        }

        /// <summary>
        ///     PropertyType(s) are merged when there DeclaringType(s) have the same names & the properties between those
        ///     DeclaringType(s) match also. This will yield a class that given any of the sourceTypes serialized with json; can be
        ///     de-serialized with a class defining the properties return.
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="sourceTypes"></param>
        /// <param name="propertyNameExclutions"></param>
        /// <param name="BaseType"></param>
        /// <param name="prettyNames"></param>
        /// <param name="ClassProperties"></param>
        /// <returns></returns>
        private ClassProperty[] ConsolidatePropertyNames(string nameSpace, Type[] sourceTypes = null, string[] propertyNameExclutions = null, Type BaseType = null, bool prettyNames = true, ClassProperty[] ClassProperties = null)
        {
            if (ClassProperties == null)
                ClassProperties = new ClassProperty[] { };

            if (propertyNameExclutions == null)
                propertyNameExclutions = new string[] { };

            if (sourceTypes == null)
                sourceTypes = new Type[] { };
            else
            {
                if (BaseType == null)
                    BaseType = DefaultBaseType(sourceTypes);

                if (BaseType != null)
                    propertyNameExclutions =
                        BaseType
                            .GetProperties()
                            .Select(p => p.Name)
                            .Union(propertyNameExclutions)
                            .ToArray();
            }

            Dictionary<string, Type> dic = new Dictionary<string, Type>();

            // do simple value/quazi-value type properties first
            foreach (var _ClassProperty in
                FilterInvalidProperties(sourceTypes, propertyNameExclutions, prettyNames)
                    .Select(p => new ClassProperty(p.Name, p.PropertyType))
                    .Union(ClassProperties))
            {
                string propName = prettyNames
                                      ? StringTransform.PrettyMsSqlIdent(_ClassProperty.Name)
                                      : _ClassProperty.Name;

                if (_ClassProperty.PropertyType.hasConvert())
                {
                    // handle normal numeric, byte, string & datetime types
                    Type typeA = _ClassProperty.PropertyType;
                    Type typeB = dic.ContainsKey(propName)
                                     ? dic[propName]
                                     : _ClassProperty.PropertyType;
                    Type typeC = ImplicitTypeConversionExtension.TypeLcd(typeA.GetPrincipleType(), typeB.GetPrincipleType());

                    // there are many situations that will yield a nullable
                    if (Nullable.GetUnderlyingType(typeA) != null || Nullable.GetUnderlyingType(typeB) != null)
                        dic[propName] = typeof(Nullable<>).MakeGenericType(typeC);
                    else if (typeC.IsNullable() && sourceTypes.Count() > 0 && FilterInvalidProperties(sourceTypes, propertyNameExclutions, prettyNames).Count() != sourceTypes.Count())
                        dic[propName] = typeof(Nullable<>).MakeGenericType(typeC);
                    else
                        dic[propName] = typeC;
                }
                else if (dic.ContainsKey(propName) && dic[propName].hasConvert() != _ClassProperty.PropertyType.hasConvert())
                    throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture,"Property {0} is defined as both a primitive value data type & complex reference type amount properties defined in parent types {1}; automatic union of these property types can't be performed.", propName, string.Join(", ", sourceTypes.Select(t => t.FullName).ToArray())));
                else if (dic.ContainsKey(propName) && dic[propName].isEnumeratedType() != _ClassProperty.PropertyType.isEnumeratedType())
                    throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture,"Property {0} is defined as both a Enumerable & Non-Enumerable properties defined in parent types {1}; automatic union of these property types can't be performed.", propName, string.Join(", ", sourceTypes.Select(t => t.FullName).ToArray())));
                else if (_ClassProperty.PropertyType == typeof(byte[]) || _ClassProperty.PropertyType == typeof(string))
                    dic[propName] = _ClassProperty.PropertyType;
                else if (!dic.ContainsKey(propName))
                {
                    //TODO:stop recursive compiling off child objects as the names are the only thing we are looking for, not the types. far to many needless compiles are occurring when calling upon UnionOnName
                    ClassProperty[] dstTypes = FilterInvalidProperties(sourceTypes, propertyNameExclutions, prettyNames, new[] { _ClassProperty.Name }, ClassProperties);

                    if (dstTypes.Length > 0)
                    {
                        Type _UnionOnNameType = MergeOnPropertyNames(
                            nameSpace,
                            dstTypes.Select(prop => prop.PropertyType.GetPrincipleType()).ToArray(),
                            null,
                            propertyNameExclutions.Union(
                                dstTypes
                                    .Where(prop => !sourceTypes.Select(t => t.Name).Contains(prop.PropertyType.Name))
                                    .Select(prop => prop.Name)
                                ).ToArray(),
                            prettyNames);

                        // properties that are collections will yield as generic lists only
                        dic[propName] =
                            _ClassProperty.PropertyType.GetEnumeratedType() != null
                            && _ClassProperty.PropertyType != typeof(string)
                                ? typeof(List<>).MakeGenericType(_UnionOnNameType) 
                                : _UnionOnNameType;
                    }
                }
            }

            return dic.Select(m => new ClassProperty(m.Key, m.Value)).ToArray();
        }

        private static ClassProperty[] FilterInvalidProperties(Type[] sourceTypes, string[] PropertyNameExclutions, bool prettyNames, string[] targetPropertyNames = null, ClassProperty[] ClassPropertyInclusions = null)
        {
            return sourceTypes
                .SelectMany(t => t
                                     .GetProperties()
                                     .Where(_PropertyInfo => _PropertyInfo.PropertyType.IsSerializable && _PropertyInfo.CanRead && _PropertyInfo.CanWrite))
                .Select(p => new ClassProperty(p.Name, p.PropertyType))
                .Union(ClassPropertyInclusions ?? new ClassProperty[] { })
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
        private static string pretty(string Name) { return StringTransform.PrettyMsSqlIdent(Name); }
        private static string pretty(ClassProperty _ClassProperty) { return pretty(_ClassProperty.Name); }

        /// <summary>
        ///     gathers up types referenced by the o via properties that descend from the
        ///     BaseAutoIdent super-class designed to work with this generic repository implementation
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="exclusions"></param>
        /// <returns></returns>
        private static Type[] ListDeps(Type[] targets, Type[] exclusions = null)
        {
            return targets == null || targets.Length == 0
                       ? new Type[] { }
                       : ListDeps(
                           targets
                             .Distinct()
                             .SelectMany(t => t.GetProperties())
                             .Select(m => m.PropertyType.GetPrincipleType())
                             .Where(p =>
                                    p != typeof(object)
                                    && !p.hasConvert()
                                    && !targets.Contains(p)
                                    && (exclusions == null || !exclusions.Contains(p)))
                             .ToArray(),
                           targets.Union(exclusions ?? new Type[] { }).ToArray())
                             .Union(targets).Distinct().ToArray();
        }

        private static Type DefaultBaseType(Type[] sourceTypes)
        {
            return sourceTypes.OfType<Type>().Select(t => t.BaseType).Distinct().Count() == 1
                       ? sourceTypes.Select(t => t.BaseType).First()
                       : null;
        }
    }
}