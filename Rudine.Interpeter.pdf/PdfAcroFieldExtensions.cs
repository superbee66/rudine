using System;
using PdfSharp.Pdf.AcroForms;
using Rudine.Util.Types;

namespace Rudine.Interpreters.Pdf
{
    public static class PdfAcroFieldExtensions
    {
        public static CompositeProperty AsCompositeProperty(this PdfAcroField o)
        {
            CompositeProperty _CompositeProperty = asCompositeProperty((dynamic) o);

            if (o.Flags == PdfAcroFieldFlags.Required)
                if (_CompositeProperty.PropertyType != typeof(string))
                    if (_CompositeProperty.PropertyType != typeof(byte[]))
                        _CompositeProperty.PropertyType = typeof(Nullable<>).MakeGenericType(Nullable.GetUnderlyingType(_CompositeProperty.PropertyType) ?? _CompositeProperty.PropertyType);

            return _CompositeProperty;
        }

        private static CompositeProperty asCompositeProperty(PdfAcroField o) { return new CompositeProperty(o.Name, typeof(string)); }

        private static CompositeProperty asCompositeProperty(PdfCheckBoxField o) { return new CompositeProperty(o.Name, typeof(bool)); }

        private static CompositeProperty asCompositeProperty(PdfTextField o) { return new CompositeProperty(o.Name, typeof(string)); }

        private static CompositeProperty asCompositeProperty(PdfSignatureField o) { return new CompositeProperty(o.Name, typeof(byte[])); }

        //TODO:Finish translating the datatypes
    }
}