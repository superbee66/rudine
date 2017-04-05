using Rudine.Web;

namespace Rudine
{
    /// <summary>
    ///     gets and sets dockey dictionary while applying encryption
    /// </summary>
    public static class DocProcessingInstructionsExtensions
    {
        public static string GetDocId(this DocProcessingInstructions o) { return DocKeyEncrypter.DocIdFromKeys(o.DocKeys); }

        public static void SetDocId(this DocProcessingInstructions o, string DocId) { o.DocKeys = DocKeyEncrypter.DocIdToKeys(DocId); }
    }
}