using System.Xml.Serialization;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded
{
    [XmlType(Namespace = "urn:rudine.progablab.com")]
    public class DocURN : BaseAutoIdent
    {
        private string docTypeNameField;

        private string solutionVersionField;

        public string DocTypeName
        {
            get { return docTypeNameField; }
            set { docTypeNameField = value; }
        }

        public string solutionVersion
        {
            get { return solutionVersionField; }
            set { solutionVersionField = value; }
        }
    }
}