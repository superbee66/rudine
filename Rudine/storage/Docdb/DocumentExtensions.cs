using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IFilterTextReader;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Rudine.Interpreters;
using Rudine.Util;
using Rudine.Util.Zips;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Storage.Docdb
{
    internal static class DocumentExtensions
    {
        public static Dictionary<LightDoc, object> AsDocSubmissions(this Document _Document) =>
            Compressor.Decompress<Dictionary<LightDoc, object>>(_Document.GetBinaryValue(Parm.Submissions));

        /// <summary>
        ///     Utilizes IFilterTextReader to extract text from document when possible. If this can't be achieved the BaseDoc is
        ///     simply serialized to json and that is used for the full text indexing. IFilter capabilities 
        /// </summary>
        /// <param name="_DocSubmissions"></param>
        /// <returns></returns>
        public static Document AsDocument(this Dictionary<LightDoc, object> _DocSubmissions)
        {
            LightDoc _LightDoc = _DocSubmissions.Keys.Last();

            using (MemoryStream _MemoryStream = _LightDoc.DocIsBinary
                ? new MemoryStream(_DocSubmissions.Values.OfType<byte[]>().Last())
                : _DocSubmissions.Values.OfType<string>().Last().AsMemoryStream())

            {
                BaseDoc _BaseDoc = _MemoryStream.Spork(
                    Bytes => DocInterpreter.Instance.Read(Bytes, true),
                    String => DocInterpreter.Instance.Read(String, true));

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


                string _Text = string.Empty;
                foreach (MemoryStream _IFilterSourceMemoryStream in _BaseDoc.IsDocRev()
                    ? ((DocRev) _BaseDoc).DocFiles.Select(docFile => new MemoryStream(docFile.Bytes))
                    : new[] {_MemoryStream})
                {
                    _IFilterSourceMemoryStream.Position = 0;
                    FileTypeFileInfo _FileTypeFileInfo = FileTypeSelector.GetFileTypeFileInfo(_IFilterSourceMemoryStream.AsBytes());
                    _IFilterSourceMemoryStream.Position = 0;


                    if (_FileTypeFileInfo != null)
                        using (FilterReader _FilterReader = new FilterReader(
                            _IFilterSourceMemoryStream,
                            _FileTypeFileInfo.Extension,
                            false,
                            true))
                        {
                            _Text += _FilterReader.ReadToEnd();
                        }
                    else
                        _Text += JsonConvert.SerializeObject(
                            _BaseDoc,
                            Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ContractResolver = ShouldSerializeContractResolver.Instance,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            });

                    _IFilterSourceMemoryStream.Close();
                    _IFilterSourceMemoryStream.Dispose();
                }


                //TODO:Find a more elegant way of making the documents DocKeys search-able. Currently they are simply concatenated to the DocData
                _Document.Add(
                    new Field(Parm.DocData,
                        string.Format(@"{0}\n\r{1}", _Term.Text, _Text),
                        Field.Store.NO,
                        Field.Index.ANALYZED,
                        Field.TermVector.WITH_POSITIONS_OFFSETS));

                // Add individual doc keys
                foreach (KeyValuePair<string, string> _DocKey in _BaseDoc.DocIdKeys)
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
                            p.Name == Parm.DocChecksum || p.Name == Parm.DocStatus
                                ? Field.Store.YES
                                : Field.Store.NO,
                            Field.Index.NOT_ANALYZED));

                return _Document;
            }
        }
    }
}