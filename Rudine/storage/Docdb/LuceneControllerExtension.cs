using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Rudine.Exceptions;
using Rudine.Interpreters;
using Rudine.Web;
using Directory = System.IO.Directory;

namespace Rudine.Storage.Docdb
{
    internal static class LuceneControllerExtension
    {
        private static readonly object RebuildLock = new object();

        /// <summary>
        ///     does an Lucene.Net.Index.IndexWriter.Optimize()
        /// </summary>
        /// <returns></returns>
        public static void Rebuild(this LuceneController o)
        {
            lock (RebuildLock)
            {
                Debug.WriteLine("Start", "Rebuild");
                if (Directory.Exists(o.DirectoryPath) && Directory.EnumerateFiles(o.DirectoryPath).Any())
                    using (StandardAnalyzer _StandardAnalyzer = new StandardAnalyzer(LuceneController.LUCENE_VERSION))
                    using (IndexWriter _CurrentIndexWriter = new IndexWriter(FSDirectory.Open(o.DirectoryPath),
                        _StandardAnalyzer,
                        false,
                        IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        foreach (string DocTypeName in o.List(new List<string> { DocRev.MyOnlyDocName }).Select(_LightDoc => _LightDoc.GetTargetDocName()).Distinct())
                        {
                            int PageIndex = 0, hashcode = 0;
                            List<Document> _ListDocument = new List<Document>();
                            do
                            {
                                foreach (Document _Document in o.ListDocuments(new List<string> { DocTypeName }, null, null, null, 1, PageIndex++))
                                {
                                    try
                                    {
                                        Dictionary<LightDoc, object> _DocSubmissions = _Document.AsDocSubmissions();

                                        BaseDoc _BaseDoc = _Document.AsDocSubmissions().Last().Key.DocIsBinary
                                                               ? DocInterpreter.Instance.Read((byte[]) _Document.AsDocSubmissions().Last().Value)
                                                               : DocInterpreter.Instance.Read((string) _Document.AsDocSubmissions().Last().Value);

                                        Debug.WriteLine(string.Format(_BaseDoc.DocTitle, "Rebuild"));

                                        // last step on the "Document -> AsDocSubmissions -> *****AsDocument*****" conversions is where storage efficiencies are gained with each release of this assembly
                                        _CurrentIndexWriter.UpdateDocument(_BaseDoc.docTermFromBaseDoc(), _DocSubmissions.AsDocument());
                                    } catch (InterpreterLocationException)
                                    {
                                        /* if developer does not have all the DocRevs there is a chance this will happen */
                                    }
                                    _CurrentIndexWriter.Commit();
                                }
                            }
                            while (_ListDocument.Count == LuceneController.PAGE_SIZE_DEFAULT);
                        }

                        _CurrentIndexWriter.Optimize();
                    }
            }
        }
    }
}