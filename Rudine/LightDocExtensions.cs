using System.Collections.Generic;
using Rudine.Web;

namespace Rudine
{
    internal static class LightDocExtensions
    {
        public static Dictionary<string, string> GetDocKeys(this LightDoc lightdoc) { return DocKeyEncrypter.DocIdToKeys(lightdoc.DocId); }

        /// <summary>
        ///     useful to understand what a LightDoc for a DocRev's principle "Target Doc Type Name" is actually represents.
        /// </summary>
        /// <param name="lightdoc">LightDoc for a DocRev document</param>
        /// <returns>
        ///     for IDocRev_Gen2's: TargetDocTypeVer, IDocRev_Gen2's: DocTypeVer, anything not actually a DocRev
        ///     representative will simple be the DocTypeName
        /// </returns>
        public static string GetTargetDocName(this LightDoc lightdoc)
        {
            Dictionary<string, string> _DocKeys = lightdoc.GetDocKeys();
            return _DocKeys.ContainsKey(Properties.Resources.TargetDocTypeNameKey)
                       ? _DocKeys[Properties.Resources.TargetDocTypeNameKey]
                       : _DocKeys.ContainsKey("DocTypeName")
                           ? _DocKeys["DocTypeName"]
                           : lightdoc.DocTypeName;
        }

        /// <summary>
        ///     useful to understand what a LightDoc for a DocRev's principle "Target Doc Type Name" is actually represents.
        /// </summary>
        /// <param name="lightdoc">LightDoc for a DocRev document</param>
        /// <returns>
        ///     for IDocRev_Gen2's: TargetDocTypeVer, IDocRev_Gen2's: DocTypeVer, null if we are not dealing with a DocRev
        ///     LightDoc listing item
        /// </returns>
        public static string GetTargetDocVer(this LightDoc lightdoc)
        {
            Dictionary<string, string> _DocKeys = lightdoc.GetDocKeys();
            return _DocKeys.ContainsKey(Properties.Resources.TargetDocTypeVerKey)
                       ? _DocKeys[Properties.Resources.TargetDocTypeVerKey]
                       : _DocKeys.ContainsKey("DocTypeVer")
                           ? _DocKeys["DocTypeVer"]
                           : null;
        }
    }
}