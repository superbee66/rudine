using System.Collections.Generic;
using Rudine.Util;
using Rudine.Web.Util;

namespace Rudine
{
    internal static class DocKeyEncrypter
    {
        //TODO:Need to move encryption items to another place, possibly where they can be changed like in the config
        private const string passPhrase = "Pasdf45ye"; //TODO:glean passPhrase from env
        private const string initVector = "fsd#$4561d234g7H8"; //TODO:must be 16 bytes, glean initVector from env
        private static readonly RijndaelEnhanced rijndaelKey = new RijndaelEnhanced(passPhrase, initVector);

        /// <summary>
        /// </summary>
        /// <param name="DocKeys"></param>
        /// <param name="ClearText">don't Rijndael encrypt</param>
        /// <returns>A jsoned & modified base64 string suitable for parameter UrlEncoding</returns>
        public static string DocIdFromKeys(Dictionary<string, string> DocKeys, bool ClearText = false)
        {
            return
                (ClearText ?
                     Serialize.Json.Serialize(DocKeys) :
                     rijndaelKey.Encrypt(Serialize.Json.Serialize(DocKeys)))
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "%3d");
            //TODO:compare Compression routines yielding the same string format used here in DocIdFromKeys
        }

        public static Dictionary<string, string> DocIdToKeys(string DocId)
        {
            return string.IsNullOrWhiteSpace(DocId) ? new Dictionary<string, string>() :
                       Serialize.Json.Deserialize<Dictionary<string, string>>(
                           rijndaelKey
                               .Decrypt(
                                   DocId
                                       .Replace("_", "/")
                                       .Replace("-", "+")
                                       .Replace("%3d", "=")));
        }
    }
}