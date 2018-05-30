using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Rudine.Interpreters;
using Rudine.Util.Zips;
using Rudine.Web;

namespace Rudine.Storage.Docdb
{
    internal static class DocumentExtensions
    {
        public static Dictionary<LightDoc, object> AsDocSubmissions(this Document _Document) { return Compressor.Decompress<Dictionary<LightDoc, object>>(_Document.GetBinaryValue(Parm.Submissions)); }

        public static Document AsDocument(this Dictionary<LightDoc, object> _DocSubmissions)
        {
            LightDoc _LightDoc = _DocSubmissions.Keys.Last();

            BaseDoc _BaseDoc = _LightDoc.DocIsBinary
                                   ? DocInterpreter.Instance.Read(_DocSubmissions.Values.OfType<byte[]>().Last(), true)
                                   : DocInterpreter.Instance.Read(_DocSubmissions.Values.OfType<string>().Last(), true);

            Term _Term = _BaseDoc.docTermFromBaseDoc();
            Document _Document = new Document();

            // TODO:convert the Submissions to a real non-datacontracted property
            _Document.Add(new Field(Parm.Submissions, Compressor.Compress(_DocSubmissions), Field.Store.YES));

            // BUG:For what ever reason, NOT_ANALYZED_NO_NORMS does not allow UpdateDocument to work properly; never executing DeleteDocument spawning duplicates
            _Document.Add(new Field(_Term.Field,
                _Term.Text,
                Field.Store.NO,
                Field.Index.NOT_ANALYZED));

            // DocTypeName will always be skipped over by GetFormObjectMappedProperties when it's dropping default valued fields
            _Document.Add(new Field(Parm.DocTypeName,
                _LightDoc.DocTypeName,
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            // searches items returned will in reverse chronological order
            _Document.Add(new Field(Parm.LogSequenceNumber,
                string.Format("{0}",
                    _LightDoc.LogSequenceNumber),
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            // Don't compress this field as it will slow down query results returned
            _Document.Add(
                new Field(
                    Parm.LightDoc,
                    _LightDoc.ToBytes(),
                    Field.Store.YES));

            //TODO:Find a more elegant way of making the documents DocKeys searchable. Currently they are simply concatenated to the DocData
            _Document.Add(
                new Field(Parm.DocData,
                    string.Format(@"{0}\n\r{1}",
                        _Term.Text,
                        JsonConvert.SerializeObject(
                            _BaseDoc,
                            Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ContractResolver = ShouldSerializeContractResolver.Instance,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            })),
                    Field.Store.NO,
                    Field.Index.ANALYZED,
                    Field.TermVector.WITH_POSITIONS_OFFSETS));

            // Add individual doc keys
            foreach (KeyValuePair<string, string> _DocKey in _BaseDoc.DocKeys)
                _Document.Add(new Field(_DocKey.Key,
                    _DocKey.Value,
                    Field.Store.NO,
                    Field.Index.NOT_ANALYZED));

            //TODO:Be selective about the column store like Raven does. Record if there is a query filter against it, only then should it be broken out as a field
            foreach (PropertyInfo p in _BaseDoc.GetType().GetProperties()
                .Where(m =>
                           _Document.GetFieldable(m.Name) == null
                           && m.PropertyType != typeof(byte[])))

                _Document.Add(
                    new Field(
                        p.Name,
                        string.Format("{0}", p.GetValue(_BaseDoc, null)),
                        (p.Name == Parm.DocChecksum || p.Name == Parm.DocStatus)
                            ? Field.Store.YES
                            : Field.Store.NO,
                        Field.Index.NOT_ANALYZED));

            return _Document;
        }
    }
}