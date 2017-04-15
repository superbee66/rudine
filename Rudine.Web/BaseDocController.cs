using System;
using System.Collections.Generic;
using System.IO;
using Rudine.Web.Util;

namespace Rudine.Web
{
    public abstract class BaseDocController : IBaseDocController
    {
        public abstract List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null);
        public abstract BaseDoc Create(BaseDoc Doc, Dictionary<string, string> DocKeys, string RelayUrl = null);
        public abstract BaseDoc Get(string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null);
        public abstract DocTypeInfo Info(string DocTypeName);
        public abstract List<ContentInfo> Interpreters();
        public abstract List<LightDoc> List(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null);
        public abstract BaseDoc ReadBytes(byte[] DocData, string RelayUrl = null);
        public BaseDoc ReadStream(Stream DocData, string RelayUrl = null)
        {
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStream.Position = 0;
                DocData.Position = 0;
                DocData.CopyTo(_MemoryStream);
                _MemoryStream.Position = 0;
                using (StreamReader _StreamReader = new StreamReader(_MemoryStream))
                {
                    bool isBinary = FileSystem.isBinary(_StreamReader);
                    _MemoryStream.Position = 0;
                    return !isBinary
                               ? ReadText(_StreamReader.ReadToEnd(), RelayUrl)
                               : ReadBytes(_MemoryStream.ToArray(), RelayUrl);
                }
            }
        }
        public abstract BaseDoc ReadText(string DocData, string RelayUrl = null);
        public abstract LightDoc Status(string DocTypeName, Dictionary<string, string> DocKeys, bool DocStatus, string DocSubmittedByEmail, string RelayUrl = null);
        public abstract LightDoc SubmitBytes(byte[] DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
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
        public LightDoc SubmitStream(Stream DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)
        {
            return DocData.Spork(
                streamAsValue => SubmitBytes(streamAsValue, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle),
                streamAsValue => SubmitText(streamAsValue, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle));
        }
        public abstract LightDoc SubmitText(string DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
    }
}