#region

using System;
using System.Collections.Generic;
using System.IO;
using Rudine.Web.Util;

#endregion

namespace Rudine.Web
{
    public abstract class BaseDocController : IBaseDocController, IBaseDocTemplateBuilder
    {
        public abstract List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null);
        public abstract BaseDoc Create(BaseDoc Doc, Dictionary<string, string> DocKeys, string RelayUrl = null);
        public abstract DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null);
        public abstract BaseDoc Get(string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null);
        public abstract DocTypeInfo Info(string DocTypeName);
        public abstract List<ContentInfo> Interpreters();
        public abstract List<LightDoc> List(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null);
        public abstract BaseDoc ReadBytes(byte[] DocData, string RelayUrl = null);

        public BaseDoc ReadStream(Stream DocData, string RelayUrl = null) =>
            DocData.Spork(
                streamAsValue => ReadBytes(streamAsValue, RelayUrl),
                streamAsValue => ReadText(streamAsValue, RelayUrl));

        public abstract BaseDoc ReadText(string DocData, string RelayUrl = null);
        public abstract LightDoc SubmitBytes(byte[] DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
        public abstract LightDoc SubmitDoc(BaseDoc DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);

        /// <summary>
        /// </summary>
        /// <param name="DocData">detected as bytes or a string</param>
        /// <param name="DocSubmittedByEmail"></param>
        /// <param name="RelayUrl"></param>
        /// <param name="DocStatus"></param>
        /// <param name="SubmittedDate"></param>
        /// <param name="DocKeys"></param>
        /// <param name="DocTitle"></param>
        /// <returns></returns>
        public LightDoc SubmitStream(Stream DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null) =>
            DocData.Spork(
                streamAsValue => SubmitBytes(streamAsValue, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle),
                streamAsValue => SubmitText(streamAsValue, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle));

        public abstract LightDoc SubmitText(string DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
        public abstract List<ContentInfo> TemplateSources();
    }
}