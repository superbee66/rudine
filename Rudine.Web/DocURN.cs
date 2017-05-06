using System;
using System.Xml.Serialization;

namespace Rudine.Web
{
    [XmlType(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocURN : BaseAutoIdent
    {
        public string DocTypeName { get; set; }
        public string solutionVersion { get; set; }
    }
}