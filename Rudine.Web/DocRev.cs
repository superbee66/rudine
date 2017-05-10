using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        public const string KeyPart1 = "TargetDocTypeName";
        public const string KeyPart2 = "TargetDocTypeVer";
        public static string ManifestFileName = string.Format("{0}.json", nameof(DocURN));
        public static string SchemaFileName = string.Format("{0}.xsd", nameof(DocSchema));
        public static string PIFileName = string.Format("{0}.json", nameof(DocProcessingInstructions));

        public List<DocRevEntry> DocFiles { get; set; }

        public string DocFilesMD5
        {
            get
            {
                using (MD5 md5 = MD5.Create())
                {
                    foreach (DocRevEntry docRevEntry in DocFiles)
                        if (
                            !docRevEntry.Name.Equals(ManifestFileName, StringComparison.InvariantCultureIgnoreCase)
                            &&
                            !docRevEntry.Name.Equals(SchemaFileName, StringComparison.InvariantCultureIgnoreCase)
                            )
                        {
                            md5.TransformString(docRevEntry.Name ?? string.Empty);
                            md5.TransformBytes(docRevEntry.Bytes);
                        }
                    md5.TransformFinalBlock(new byte[0], 0, 0);
                    return BitConverter.ToString(md5.Hash);
                }
            }
        }

        public DocURN DocURN { get; set; }

        /// <summary>
        /// string literal that is valid XSD  used to compose an IDocModel & finally a BaseDoc from
        /// </summary>
        public string DocSchema { get; set; }

        /// <summary>
        ///     system reserved value management
        /// </summary>
        [XmlIgnore]
        public override Dictionary<string, string> DocKeys
        {
            get { return base.DocKeys; }
            set
            {
                if (value != default(Dictionary<string, string>))
                    if (string.Join(",", value.Keys.OrderBy(key => key)) != string.Join(",", MakeDocKeys(DocURN).Keys.OrderBy(key => key)))
                        throw PropertyValueException(nameof(DocKeys));

                base.DocKeys = value;
            }
        }



        [DefaultValue(MY_ONLY_DOC_NAME)]
        public override string DocTypeName
        {
            get { return base.DocTypeName; }

            set
            {
                if (value != MY_ONLY_DOC_NAME)
                    throw PropertyValueException(nameof(DocTypeName));
                base.DocTypeName = value;
            }
        }

        [DefaultValue(MY_ONLY_DOC_VERSION)]
        public override string solutionVersion
        {
            get { return base.solutionVersion; }

            set
            {
                if (value != default(string)) // serializers often set this to "" when they are spinning up
                {
                    if (value != MY_ONLY_DOC_VERSION)
                        throw PropertyValueException(nameof(solutionVersion));

                    base.solutionVersion = value;
                }
            }
        }

        public static Dictionary<string, string> MakeDocKeys(DocURN docUrn) =>
            new Dictionary<string, string>
            {
                { KeyPart1, docUrn.DocTypeName },
                { KeyPart2, docUrn.solutionVersion }
            };

        private static Exception PropertyValueException(string propertyName)
        {
            return new Exception(String.Format(
                @"{0} {1} values are system managed",
                nameof(DocRev),
                propertyName));
        }
    }
}