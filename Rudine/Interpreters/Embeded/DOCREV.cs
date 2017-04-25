using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded
{
    [XmlType(AnonymousType = true, Namespace = "urn:rudine.progablab.com")]
    [XmlRoot(Namespace = "urn:rudine.progablab.com", IsNullable = false)]
    [Serializable]
    public class DOCREV : BaseDoc, IDocRev
    {
        private string mD5Field;

        private DocURN targetField;

        private List<DocRevEntry> fileListField;

        public string MD5
        {
            get { return mD5Field; }
            set { mD5Field = value; }
        }

        public DocURN Target
        {
            get { return targetField; }
            set { targetField = value; }
        }

        [XmlElement("FileList")]
        public List<DocRevEntry> FileList
        {
            get { return fileListField; }
            set { fileListField = value; }
        }
    }
}