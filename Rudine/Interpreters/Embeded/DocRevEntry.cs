using System.Xml.Serialization;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded
{
    [XmlType(Namespace = "urn:rudine.progablab.com")]
    public class DocRevEntry : BaseAutoIdent
    {
        private byte[] bytesField;

        private string nameField;

        [XmlElement(DataType = "base64Binary")]
        public byte[] Bytes
        {
            get { return bytesField; }
            set { bytesField = value; }
        }

        public string Name
        {
            get { return nameField; }
            set { nameField = value; }
        }
    }
}