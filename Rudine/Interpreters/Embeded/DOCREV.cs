using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Embeded
{
    [XmlType(AnonymousType = true, Namespace = "urn:rudine.progablab.com")]
    [XmlRoot(Namespace = "urn:rudine.progablab.com", IsNullable = false)]
    [Serializable]
    public class DocRev : BaseDoc, IDocRev
    {
        public string MD5 {
            get {
                using (MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    foreach (DocRevEntry docRevEntry in FileList)
                    {
                        md5.TransformString(docRevEntry.Name);
                        md5.TransformBytes(docRevEntry.Bytes);
                    }
                    return BitConverter.ToString(md5.Hash);
                }
            }
        }

        public DocURN Target { get; set; }

        [XmlElement("FileList")]
        public List<DocRevEntry> FileList { get; set; }
    }
}