using System.Collections.Generic;
using Rudine.Web;

namespace Rudine
{
    /// <summary>
    ///     Extensions for Rudine.Web.LightDoc type as it's main cSharp file is shared between visual studio csproj(s) via file
    ///     link.
    /// </summary>
    internal static class LightDocExtensions
    {
        public static Dictionary<string, string> GetDocKeys(this LightDoc LightDoc) => 
            DocKeyEncrypter.DocIdToKeys(LightDoc.DocId);

        /// <summary>
        ///     useful to understand what a LightDoc for a DocRev's principle "Target Doc Type Name" is actually represents.
        /// </summary>
        /// <param name="LightDoc">LightDoc for a DocRev document</param>
        /// <returns>
        ///     for IDocRev_Gen2's: TargetDocTypeVer, IDocRev_Gen2's: DocTypeVer, anything not actually a DocRev
        ///     representative will simple be the DocTypeName
        /// </returns>
        public static string GetTargetDocName(this LightDoc LightDoc)
        {
            Dictionary<string, string> docKeys = LightDoc.GetDocKeys();
            return docKeys.ContainsKey(DocRev.KeyPart1)
                       ? docKeys[DocRev.KeyPart1]
                       : docKeys.ContainsKey(DocRev.KeyPart1)
                           ? docKeys[DocRev.KeyPart1]
                           : LightDoc.DocTypeName;
        }

        /// <summary>
        ///     useful to understand what a LightDoc for a DocRev's principle "Target Doc Type Name" is actually represents.
        /// </summary>
        /// <param name="LightDoc">LightDoc for a DocRev document</param>
        /// <returns>
        ///     for IDocRev_Gen2's: TargetDocTypeVer, IDocRev_Gen2's: DocTypeVer, null if we are not dealing with a DocRev
        ///     LightDoc listing item
        /// </returns>
        public static string GetTargetDocVer(this LightDoc LightDoc)
        {
            Dictionary<string, string> docKeys = LightDoc.GetDocKeys();
            return docKeys.ContainsKey(DocRev.KeyPart2)
                       ? docKeys[DocRev.KeyPart2]
                       : docKeys.ContainsKey(DocRev.KeyPart2)
                           ? docKeys[DocRev.KeyPart2]
                           : null;
        }
    }
}