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
        public const string MyOnlyDocName = "DocRev";
        public const string MyOnlyDocVersion = "1.0.0.0";
        public const string KeyPart1 = "TargetDocTypeName";
        public const string KeyPart2 = "TargetDocTypeVer";

        public static readonly string ManifestFileName = String.Format("{0}.json", nameof(DocURN));
        public static readonly string SchemaFileName = String.Format("{0}.xsd", nameof(DocSchema));
        public static readonly string PIFileName = String.Format("{0}.json", nameof(DocProcessingInstructions));

        /// <summary>
        /// these files are not a factor when calculating DocFilesMD5 as they are system generated
        /// </summary>
        private static readonly string[] DocFileMD5Exclutions = { ManifestFileName, SchemaFileName, PIFileName };


        public List<DocRevEntry> DocFiles { get; set; }

        public string DocFilesMD5 => DocFilesMD5Calc(DocFiles);

        public static string DocFilesMD5Calc(List<DocRevEntry> docFiles)
        {
            using (MD5 md5 = MD5.Create())
            {
                foreach (DocRevEntry docRevEntry in docFiles
                    .Where(fileA => !DocFileMD5Exclutions.Any(fileB => fileA.Name.Equals(fileB, StringComparison.InvariantCultureIgnoreCase)))
                    .OrderBy(entry => entry.Name))
                {
                    md5.TransformString(docRevEntry.Name ?? String.Empty);
                    md5.TransformBytes(docRevEntry.Bytes);
                }
                md5.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(md5.Hash);
            }
        }

        /// <summary>
        ///     system reserved value management
        /// </summary>
        [XmlIgnore]
        public override Dictionary<string, string> DocIdKeys
        {
            get { return base.DocIdKeys; }
            set
            {
                if (value != default(Dictionary<string, string>))
                    if (String.Join(",", value.Keys.OrderBy(key => key)) != String.Join(",", MakeDocKeys(DocURN).Keys.OrderBy(key => key)))
                        throw PropertyValueException(nameof(DocIdKeys));

                base.DocIdKeys = value;
            }
        }

        /// <summary>
        ///     string literal that is valid XSD  used to compose an IDocModel & finally a BaseDoc from
        /// </summary>
        public string DocSchema { get; set; }

        [DefaultValue(MyOnlyDocName)]
        public override string DocTypeName
        {
            get { return base.DocTypeName; }

            set
            {
                if (value != MyOnlyDocName)
                    throw PropertyValueException(nameof(DocTypeName));
                base.DocTypeName = value;
            }
        }

        public DocURN DocURN { get; set; }

        [DefaultValue(MyOnlyDocVersion)]
        public override string solutionVersion
        {
            get { return base.solutionVersion; }

            set
            {
                if (value != default(string)) // serializers often set this to "" when they are spinning up
                {
                    if (value != MyOnlyDocVersion)
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