using System;
using System.Text.RegularExpressions;

namespace Rudine.Web
{
    [Obsolete("SomeDocumentName Infopath technologies are no longer supported")]
    internal static class DocXmlParser
    {
        public static string GetFileName(string DocData) { return Regex.Match(DocData, "fileName=\"([^\"]+)\"").Groups[1].Value; }
    }
}