using System;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace Rudine.Web
{
    /// <summary>
    ///     Represents documents who's render source exists outside the Rudine document system
    /// </summary>
    [XmlType(AnonymousType = true, Namespace = "urn:rudine.progablab.com")]
    [XmlRoot(Namespace = "urn:rudine.progablab.com", IsNullable = false)]
    [Serializable]
    public class ExternalDoc : BaseDoc, IExternalDoc
    {
        public const string MyOnlyDocRev = "1.0.0.0";
        public static readonly string SubmissionFileName = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.dat", nameof(ExternalDoc));
        public static readonly string PropertiesFileName = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.json", nameof(ExternalDoc));

        public static readonly string PIFileName = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.json", "pi");
        //[ScriptIgnore]
        public byte[] RawBytes { get; set; }
    }
}