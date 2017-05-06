using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     for storage & transmission Templified BaseDoc support content
    /// </summary>
    [XmlType(AnonymousType = true, Namespace = "urn:rudine.progablab.com")]
    [XmlRoot(Namespace = "urn:rudine.progablab.com", IsNullable = false)]
    [Serializable]
    public class DocRev : BaseDoc, IDocRev
    {
        public const string MY_ONLY_DOC_NAME = "DocRev";
        public const string MY_ONLY_DOC_VERSION = "1.0.0.0";

        /// <summary>
        ///     system reserved value management
        /// </summary>
        public override Dictionary<string, string> DocKeys
        {
            get { return MakeDocKeys(DocURN); }
            set
            {
                if (value != default(Dictionary<string, string>))
                    if (value != MakeDocKeys(DocURN))
                        throw PropertyValueException(nameof(DocKeys));
                base.DocKeys = value;
            }
        }

        public static Dictionary<string, string> MakeDocKeys(DocURN docUrn) =>
            new Dictionary<string, string>
            {
                    { "TargetDocTypeName", docUrn.DocTypeName },
                    { "TargetDocTypeVer", docUrn.solutionVersion }
                };

        /// <summary>
        ///     string literal that is valid XSD  used to compose an IDocModel & finally a BaseDoc from
        /// </summary>
        public string DocSchema { get; set; }

        [DefaultValue(MY_ONLY_DOC_NAME)]
        public override string DocTypeName
        {
            get { return base.DocTypeName; }

            set
            {
                if (value != DocTypeName)
                    throw PropertyValueException(nameof(DocTypeName));
                base.DocTypeName = value;
            }
        }

        public DocURN DocURN { get; set; }

        public List<DocRevEntry> DocFiles { get; set; }

        public string DocFilesMD5
        {
            get
            {
                using (MD5 md5 = MD5.Create())
                {
                    foreach (DocRevEntry docRevEntry in DocFiles)
                    {
                        md5.TransformString(docRevEntry.Name);
                        md5.TransformBytes(docRevEntry.Bytes);
                    }
                    return BitConverter.ToString(md5.Hash);
                }
            }
        }

        [DefaultValue(MY_ONLY_DOC_VERSION)]
        public override string solutionVersion
        {
            get { return base.solutionVersion; }

            set
            {
                if (value != solutionVersion)
                    throw PropertyValueException(nameof(solutionVersion));

                base.solutionVersion = value;
            }
        }

        private static Exception PropertyValueException(string propertyName)
        {
            return new Exception(String.Format(
                @"{0} {1} values are system managed",
                nameof(DocRev),
                propertyName));
        }
    }
}