using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Rudine.Util.Types
{
    /// <summary>
    ///     supports runtime created self & child-parent type referencing properties defined at runtime (chicken before the egg
    ///     situation).
    /// </summary>
    public class CompositeType : Type
    {
        private static readonly Dictionary<int, Type> classes = new Dictionary<int, Type>();

        private static readonly object GenerateLock = new object();
        private static readonly ModuleBuilder MergeTypeModuleBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicNamedClasses"), AssemblyBuilderAccess.Run).DefineDynamicModule("Module");

        public CompositeType(string targetNamespace, string targetName, Type[] sourceTypes, Type targetBaseType = null, string[] sourcePropertyNameExclutions = null, bool prettyNames = true) : this(targetNamespace, targetName, sourceTypes, null, targetBaseType, sourcePropertyNameExclutions, prettyNames) { }
        public CompositeType(string targetNamespace, string targetName, CompositeProperty[] sourceProperties = null, Type targetBaseType = null, string[] sourcePropertyNameExclutions = null, bool prettyNames = true) : this(targetNamespace, targetName, null, sourceProperties, targetBaseType, sourcePropertyNameExclutions, prettyNames) { }

        public CompositeType(string targetNamespace, string targetName, Type[] sourceTypes, CompositeProperty[] sourceProperties, Type targetBaseType = null, string[] sourcePropertyNameExclutions = null, bool prettyNames = true)
        {
            Namespace = targetNamespace;
            Name = targetName;
            FullName = string.Join(".", targetNamespace, targetName);
            MergedTypeSignature = new CompositeSignature(sourceTypes, sourcePropertyNameExclutions, targetBaseType, prettyNames, sourceProperties);
        }

        private CompositeSignature MergedTypeSignature { get; }
        public override bool Equals(object obj) { return obj is CompositeType ? Equals((CompositeType) obj) : false; }
        public bool Equals(CompositeType other) { return GetHashCode().Equals(other.GetHashCode()); }

        private void GenerateEquals(TypeBuilder tb, FieldInfo[] fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(bool), new[]
                {
                    typeof(object)
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
                typeof(int), EmptyTypes);
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

        private static FieldInfo[] GenerateProperties(TypeBuilder tb, Dictionary<string, Type> properties)
        {
            FieldInfo[] fields = new FieldBuilder[properties.Count()];
            int i = 0;

            foreach (KeyValuePair<string, Type> dp in properties)
            {
                FieldBuilder fb = tb.DefineField("_" + dp.Key, dp.Value, FieldAttributes.Private);
                PropertyBuilder pb = tb.DefineProperty(dp.Key, PropertyAttributes.HasDefault, dp.Value, null);
                MethodBuilder mbGet = tb.DefineMethod("get_" + dp.Value, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, dp.Value, EmptyTypes);
                ILGenerator genGet = mbGet.GetILGenerator();
                genGet.Emit(OpCodes.Ldarg_0);
                genGet.Emit(OpCodes.Ldfld, fb);
                genGet.Emit(OpCodes.Ret);
                MethodBuilder mbSet = tb.DefineMethod("set_" + dp.Value, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[]
                {
                    dp.Value
                });
                ILGenerator genSet = mbSet.GetILGenerator();
                genSet.Emit(OpCodes.Ldarg_0);
                genSet.Emit(OpCodes.Ldarg_1);
                genSet.Emit(OpCodes.Stfld, fb);
                genSet.Emit(OpCodes.Ret);
                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i++] = fb;
            }

            return fields;
        }

        public override int GetHashCode() { return FullName.GetHashCode() ^ MergedTypeSignature.GetHashCode(); }

        #region TypeAbstracts

        public override object[] GetCustomAttributes(bool inherit) { return UnderlyingSystemType.GetCustomAttributes(inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return UnderlyingSystemType.IsDefined(attributeType, inherit); }
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { return UnderlyingSystemType.GetConstructors(bindingAttr); }
        public override Type GetInterface(string name, bool ignoreCase) { return UnderlyingSystemType.GetInterface(name, ignoreCase); }
        public override Type[] GetInterfaces() { return UnderlyingSystemType.GetInterfaces(); }
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) { return UnderlyingSystemType.GetEvent(name, bindingAttr); }
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) { return UnderlyingSystemType.GetEvents(bindingAttr); }
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) { return UnderlyingSystemType.GetNestedTypes(bindingAttr); }
        public override Type GetNestedType(string name, BindingFlags bindingAttr) { return UnderlyingSystemType.GetNestedType(name, bindingAttr); }
        public override Type GetElementType() { return UnderlyingSystemType.GetElementType(); }
        protected override bool HasElementTypeImpl() { return UnderlyingSystemType.IsArray || UnderlyingSystemType.IsByRef || UnderlyingSystemType.IsPointer; }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            //TODO:Full implementation needed
            return UnderlyingSystemType.GetProperty(name);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { return UnderlyingSystemType.GetProperties(bindingAttr); }
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { return UnderlyingSystemType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers); }
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { return UnderlyingSystemType.GetMethods(bindingAttr); }
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) { return UnderlyingSystemType.GetField(name, bindingAttr); }
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) { return UnderlyingSystemType.GetFields(bindingAttr); }
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) { return UnderlyingSystemType.GetMembers(bindingAttr); }
        protected override TypeAttributes GetAttributeFlagsImpl() { return UnderlyingSystemType.Attributes; }
        protected override bool IsArrayImpl() { return UnderlyingSystemType.IsArray; }
        protected override bool IsByRefImpl() { return UnderlyingSystemType.IsByRef; }
        protected override bool IsPointerImpl() { return UnderlyingSystemType.IsPointer; }
        protected override bool IsPrimitiveImpl() { return UnderlyingSystemType.IsPrimitive; }
        protected override bool IsCOMObjectImpl() { return UnderlyingSystemType.IsCOMObject; }
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) { return UnderlyingSystemType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters); }

        public override Type UnderlyingSystemType
        {
            get
            {
                try
                {
                    if (!classes.ContainsKey(GetHashCode()))
                    {
                        TypeBuilder _TypeBuilder = MergedTypeSignature._BaseType == null
                                                       ? MergeTypeModuleBuilder.DefineType(FullName, TypeAttributes.Class | TypeAttributes.Public)
                                                       : MergeTypeModuleBuilder.DefineType(FullName, TypeAttributes.Class | TypeAttributes.Public, MergedTypeSignature._BaseType);

                        GenerateProperties(_TypeBuilder, MergedTypeSignature.MergedProperties);

                        lock (GenerateLock)
                            if (!classes.ContainsKey(GetHashCode()))
                                classes[GetHashCode()] = _TypeBuilder.CreateType();
                    }
                    return classes[GetHashCode()];
                } catch (Exception)
                {
                    throw;
                }
            }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { return UnderlyingSystemType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers); }
        public override string Name { get; }

        public override Guid GUID
        {
            get { return UnderlyingSystemType.GUID; }
        }

        public override Module Module
        {
            get { return MergeTypeModuleBuilder; }
        }

        public override Assembly Assembly
        {
            get { return MergeTypeModuleBuilder.Assembly; }
        }

        public override string FullName { get; }
        public override string Namespace { get; }

        public override string AssemblyQualifiedName
        {
            get { return UnderlyingSystemType.AssemblyQualifiedName; }
        }

        public override Type BaseType
        {
            get { return MergedTypeSignature._BaseType; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return UnderlyingSystemType.GetCustomAttributes(attributeType, inherit); }

        #endregion
    }
}