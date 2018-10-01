using Rudine.Web;

namespace Rudine
{
    /// <summary>
    ///     Extensions for Rudine.Web.DocProcessingInstructions type as it's main cSharp file is shared between visual studio
    ///     csproj(s) via file link.
    ///     Gets and sets DocKey dictionary while applying encryption
    /// </summary>
    public static class DocProcessingInstructionsExtensions
    {
        public static string GetDocId(this DocProcessingInstructions o) => DocKeyEncrypter.DocIdFromKeys(o.DocIdKeys);

        public static void SetDocId(this DocProcessingInstructions o, string DocId)
        {
            o.DocIdKeys = DocKeyEncrypter.DocIdToKeys(DocId);
        }
    }
}