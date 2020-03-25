using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.Advanced;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Pdf
{
    public class CompositePropertyAndValue : CompositeProperty
    {
        public CompositePropertyAndValue(string name, Type type, object value) : base(name, type) { Value = value; }
        public object Value { get; }
    }

    public static class PdfAcroFieldExtensions
    {
        static readonly string[] _pathsOfInterest = { "/Fields", "/AA", "/F", "/JS" };

        public static CompositePropertyAndValue AsCompositeProperty(this PdfAcroField o)
        {
            if (o is PdfPushButtonField)
                return null;

            CompositePropertyAndValue _CompositeProperty = asCompositePropertyAndValue((dynamic)o);

            if (o.Flags != PdfAcroFieldFlags.Required)
                if (_CompositeProperty.PropertyType != typeof(string))
                    if (_CompositeProperty.PropertyType != typeof(byte[]))
                        _CompositeProperty.PropertyType = typeof(Nullable<>).MakeGenericType(Nullable.GetUnderlyingType(_CompositeProperty.PropertyType) ?? _CompositeProperty.PropertyType);

            return _CompositeProperty;
        }

        private static CompositePropertyAndValue asCompositePropertyAndValue(PdfAcroField o)
        {
            Type _Type = GetPdfFieldType(o);
            string _Value = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}", o.Value);
            DateTime _DateTime;
            DateTime.TryParse(_Value, out _DateTime);

            return DateTime.TryParse(_Value, out _DateTime)
                ? new CompositePropertyAndValue(o.NameofCSharpProperty(), _Type, _DateTime)
                : new CompositePropertyAndValue(o.NameofCSharpProperty(), _Type, null);
        }

        private static CompositePropertyAndValue asCompositePropertyAndValue(PdfCheckBoxField _PdfCheckBoxField)
        {
            return new CompositePropertyAndValue(
                _PdfCheckBoxField.NameofCSharpProperty(),
                typeof(bool),
                string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}", _PdfCheckBoxField.Value).Equals(_PdfCheckBoxField.CheckedName)
                    ? true
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}", _PdfCheckBoxField.Value).Equals(_PdfCheckBoxField.UncheckedName)
                        ? (ValueType)false
                        : null);
        }

        //private static CompositePropertyAndValue asCompositePropertyAndValue(PdfTextField o) =>
        //    new CompositePropertyAndValue(o.NameofCSharpProperty(), GetPdfFieldType(o));

        private static CompositePropertyAndValue asCompositePropertyAndValue(PdfSignatureField o) =>
            new CompositePropertyAndValue(o.NameofCSharpProperty(), typeof(byte[]), null);

        private static readonly ModuleBuilder EnumTypeModuleBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicEnumNamedClasses"), AssemblyBuilderAccess.Run).DefineDynamicModule("EnumModule");

        /// <summary>
        ///     one bool per radio button
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static CompositePropertyAndValue asCompositePropertyAndValue(PdfRadioButtonField o)
        {
            if (!o.HasKids)
                return null;

            string _EnumName = o.NameofCSharpProperty();
            Type _T = null;
            int? _SelectValue = null;

            lock (EnumTypeModuleBuilder)
            {
                _T = EnumTypeModuleBuilder.GetTypes().FirstOrDefault(t => t.Name == _EnumName);

                if (_T == null)
                {
                    EnumBuilder _EnumBuilder = EnumTypeModuleBuilder.DefineEnum(
                                 o.NameofCSharpProperty(),
                                 TypeAttributes.Public,
                                 typeof(int));

                    foreach (PdfReference choice in ((PdfArray)o.Elements["/Kids"]).OfType<PdfReference>())
                    {
                        string _EnumItemName = GetEnumItemName(choice);
                        if (!string.IsNullOrWhiteSpace(_EnumItemName))
                            _EnumBuilder.DefineLiteral(_EnumItemName, _EnumItemName.GetHashCode());
                    }

                    _T = _EnumBuilder.CreateType();
                }
            }

            if (_T == null)
                return null;

            int _I = 0;
            foreach (PdfReference choice in ((PdfArray)o.Elements["/Kids"]).OfType<PdfReference>())
                if (o.SelectedIndex == _I++)
                    _SelectValue = GetEnumItemName(choice).GetHashCode();


            return new CompositePropertyAndValue(_EnumName, _T, _SelectValue);


        }

        private static string GetEnumItemName(PdfReference choice)
        {
            string _TrimStart = ((PdfDictionary)((PdfDictionary)((PdfItem[])((PdfDictionary)choice.Value).Elements.Values)[0]).Elements["/N"]).Elements.First().Key.TrimStart('/');
            return StringTransform.PrettyCSharpIdent(_TrimStart);
        }

        internal static string detectDateTimeFormat(PdfAcroField o) =>
            getPaths(o).Where(path => path.IndexOf("AFDate_FormatEx") != -1).Select(s => s.Split('(', ')')[1].Trim('"')).FirstOrDefault();

        private static string[] getPaths(PdfItem o, string parrentPath = null) =>
            new[] { parrentPath };

        private static string[] getPaths(PdfArray o, string parrentPath = null)
        {
            List<string> _Paths = new List<string>();
            foreach (var _Element in o.Elements)
                _Paths.AddRange(getPaths((dynamic)_Element, string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}", parrentPath)));
            return _Paths.Distinct().ToArray();
        }

        private static string[] getPaths(PdfReference o, string parrentPath = null) =>
            getPaths((dynamic)o.Value, parrentPath);

        private static string[] getPaths(PdfName o, string parrentPath = null) =>
            new[] { string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}/{1}", parrentPath, o.Value) };

        private static string[] getPaths(PdfString o, string parrentPath = null) =>
            new[] { string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}/{1}", parrentPath, o.Value) };

        private static string[] getPaths(PdfDictionary o, string parrentPath = null)
        {
            List<string> _Paths = new List<string>();
            foreach (KeyValuePair<string, PdfItem> _Element in o.Elements.Where(e => _pathsOfInterest.Any(p => p == e.Key)))
                _Paths.AddRange(getPaths((dynamic)_Element.Value, string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}{1}", parrentPath, _Element.Key)));
            return _Paths.Distinct().ToArray();
        }

        private static Type GetPdfFieldType(PdfAcroField o) =>
            detectDateTimeFormat(o) != null
                ? typeof(DateTime)
                : o.Value?.GetType().GetProperty("Value")?.PropertyType ?? typeof(string);

        private static bool getValue(PdfBoolean o) => o.Value;
        private static bool getValue(PdfBooleanObject o) => o.Value;
        private static DateTime getValue(PdfDate o) => o.Value;
        private static double getValue(PdfReal o) => o.Value;
        private static double getValue(PdfRealObject o) => o.Value;
        private static string getValue(PdfString o) => o.Value;
        private static string getValue(PdfStringObject o) => o.Value;
        private static uint getValue(PdfUInteger o) => o.Value;
        private static uint getValue(PdfUIntegerObject o) => o.Value;
        public static object GetValue(this PdfItem o) => getValue((dynamic)o);

        /// <summary>
        ///     takes special consideration for radio button fields
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string NameofCSharpProperty(this PdfAcroField o) =>
            StringTransform.PrettyCSharpIdent(o.Name);
    }
}