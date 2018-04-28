using Hyland.Unity;
using Rudine.Web.Util;

namespace Rudine
{
    public static class KeywordTypeExtensions
    {
        public static string CSharpName(this KeywordType keywordType) =>
            keywordType.Name.PrettyCSharpIdent();
    }
}