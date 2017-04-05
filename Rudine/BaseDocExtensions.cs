using Rudine.Web;

namespace Rudine
{
    internal static class BaseDocExtensions
    {
        public static LightDoc ToLightDoc(this BaseDoc o, string DocSrc = null)
        {
            return new LightDoc
            {
                DocId = o.GetDocId(),
                DocSrc = DocSrc ?? Nav.ToUrl(o),
                DocStatus = o.DocStatus,
                DocTitle = o.DocTitle,
                DocTypeName = o.DocTypeName
            };
        }
    }
}