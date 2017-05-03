using System;
using System.Xml.Serialization;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded
{
    [XmlType(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocRevEntry : BaseAutoIdent
    {
        [XmlElement(DataType = "base64Binary")]
        public byte[] Bytes { get; set; }
        /// <summary>
        /// path of file compatible with ZipEntry
        /// </summary>
        public string Name { get; set; }
        public DateTime ModDate { get; set; }
    }
}