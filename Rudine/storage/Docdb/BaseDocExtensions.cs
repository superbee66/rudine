using Lucene.Net.Index;
using Rudine.Web;

namespace Rudine.Storage.Docdb
{
    internal static class BaseDocExtensions
    {
        public static Term docTermFromBaseDoc(this BaseDoc _BaseDoc) { return new Term("DocTerm", _BaseDoc.AsTermTxt()); }
    }
}