using Rudine.Web;

namespace Rudine
{
    /// <summary>
    ///     Extensions for Rudine.Web.BaseDoc type as it's main cSharp file is shared between visual studio csproj(s) via file
    ///     link.
    /// </summary>
    internal static class BaseDocExtensions
    {
        public static LightDoc ToLightDoc(this BaseDoc BaseDoc, string DocSrc = null) =>
            new LightDoc
            {
                DocId = BaseDoc.GetDocId(),
                DocSrc = DocSrc ?? Nav.ToUrl(BaseDoc),
                DocStatus = BaseDoc.DocStatus,
                DocTitle = BaseDoc.DocTitle,
                DocTypeName = BaseDoc.DocTypeName
            };
    }
}