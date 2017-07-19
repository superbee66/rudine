using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     The only means of communications to the Core. No in memory/direct reference is every made
    ///     by InfoPath.Client to InfoPath.Core. All communication is over WCF. The WCF client proxy should
    ///     be generated & wrapped by this. It's the developer responsibility to do this.
    /// </summary>
    /// <typeparam name="TClientBaseT">Visual studio generated WCF client pointing to the Core's service URL</typeparam>
    public class ClientBaseDocController<TClientBaseT> : BaseDocController where TClientBaseT : ICommunicationObject
    {
        private readonly Type _UnderlyingControllerType;
        private readonly TClientBaseT _UnderlyingWSClient;
        private string _defaultRelayUrl = ReverseProxy.GetRelayUrl();

        public ClientBaseDocController(TClientBaseT ClientBaseT)
        {
            _UnderlyingWSClient = ClientBaseT;
            _UnderlyingControllerType = _UnderlyingWSClient.GetType();
        }

        public string DefaultRelayUrl
        {
            get { return _defaultRelayUrl; }
            set { _defaultRelayUrl = value; }
        }

        public TClientBaseT UnderlyingWSClient =>
            _UnderlyingWSClient;

        public override List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null) =>
            (List<LightDoc>) GetMethodInfo(DocCmd.Audit)
                .Invoke(UnderlyingWSClient,
                    new object[]
                    {
                        DocTypeName,
                        DocId,
                        RelayUrl
                    });

        public override BaseDoc Create(BaseDoc Doc, Dictionary<string, string> DocKeys, string RelayUrl = null) =>
            (BaseDoc) GetMethodInfo(DocCmd.Create)
                .Invoke(UnderlyingWSClient, new Dictionary<string, object>
                {
                    { Parm.Doc, Doc },
                    { Parm.DocKeys, DocKeys },
                    { Parm.RelayUrl, string.IsNullOrWhiteSpace(RelayUrl) ? DefaultRelayUrl : RelayUrl }
                });

        public override DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null) =>
            (DocRev) _UnderlyingControllerType.GetMethod(nameof(CreateTemplate))
                                              .Invoke(UnderlyingWSClient,
                                                  new object[]
                                                  {
                                                      docFiles,
                                                      docTypeName,
                                                      docRev,
                                                      schemaXml,
                                                      schemaFields
                                                  });

        public override BaseDoc Get(string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null) =>
            (BaseDoc) GetMethodInfo(DocCmd.Get).Invoke(UnderlyingWSClient, new Dictionary<string, object>
            {
                { Parm.DocTypeName, DocTypeName },
                { Parm.DocKeys, DocKeys },
                { Parm.DocId, DocId },
                {
                    Parm.RelayUrl, string.IsNullOrWhiteSpace(RelayUrl)
                                       ? DefaultRelayUrl
                                       : RelayUrl
                }
            });

        public byte[] GetDocBytes(string DocSrc, out string filename)
        {
            using (MemoryStream _Stream = GetDocMemoryStream(DocSrc, out filename))
                return _Stream.ToArray();
        }

        /// <summary>
        /// </summary>
        /// <param name="DocSrc"></param>
        /// <returns>memory stream at position 0 as copy of the orignal httpwebresponse</returns>
        public MemoryStream GetDocMemoryStream(string DocSrc, out string filename)
        {
            MemoryStream _MemoryStream = new MemoryStream();
            //TODO:Relocate this to a more appropriate class
            HttpWebRequest _HttpWebRequest = (HttpWebRequest) WebRequest.Create(new Uri(DocSrc));
            using (HttpWebResponse _HttpWebResponse = (HttpWebResponse) _HttpWebRequest.GetResponse())
            {
                filename = Regex.Match(_HttpWebResponse.Headers["content-disposition"], "filename=\"([^\"]+)\"").Groups[1].Value;
                _HttpWebResponse.GetResponseStream().CopyTo(_MemoryStream);
            }
            _MemoryStream.Position = 0;
            return _MemoryStream;
        }

        /// <summary>
        ///     Download the actual InfoPath XML Document the desktop application would open. This
        ///     is performed over an System.Net.HttpWebRequest utilizing the same DocSrc (URL)
        ///     used to redirect browsers to download the InfoPath document.
        /// </summary>
        /// <param name="DocSrc"></param>
        /// <returns>Xml suitable for opening in Office InfoPath</returns>
        public string GetDocText(string DocSrc, out string filename)
        {
            using (StreamReader _StreamReader = new StreamReader(GetDocMemoryStream(DocSrc, out filename)))
                return _StreamReader.ReadToEnd();
        }

        /// <summary>
        ///     Get
        /// </summary>
        /// <param name="MethodPrefix"></param>
        /// <param name= NavKey.DocTypeName></param>
        /// <param name="DocTypeName"></param>
        /// <returns></returns>
        private MethodInfo GetMethodInfo(DocCmd MethodPrefix) =>
            _UnderlyingControllerType.GetMethod(string.Format("{0}", MethodPrefix));

        public override DocTypeInfo Info(string DocTypeName) =>
            CacheMan.Cache(() =>
                               (DocTypeInfo) GetMethodInfo(DocCmd.Info)
                                   .Invoke(UnderlyingWSClient, new object[] { DocTypeName }),
                false,
                "DocTypeInfo",
                DocTypeName,
                DocCmd.Info);

        public override List<ContentInfo> Interpreters() { return null; }

        public override List<LightDoc> List(List<string> DocTypes, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null) =>
            (List<LightDoc>) GetMethodInfo(DocCmd.List).Invoke(UnderlyingWSClient, new object[]
            {
                DocTypes,
                DocKeys ?? new Dictionary<string, List<string>>(),
                DocProperties ?? new Dictionary<string, List<string>>(),
                KeyWord ?? string.Empty,
                PageSize,
                PageIndex,
                string.IsNullOrWhiteSpace(RelayUrl)
                    ? DefaultRelayUrl
                    : RelayUrl
            });

        private BaseDoc Read(object DocData, string RelayUrl) =>
            (BaseDoc) GetMethodInfo(DocData is byte[] ? DocCmd.ReadBytes : DocCmd.ReadText)
                .Invoke(UnderlyingWSClient, new Dictionary<string, object>
                {
                    { Parm.DocData, DocData },
                    {
                        Parm.RelayUrl, string.IsNullOrWhiteSpace(RelayUrl)
                                           ? DefaultRelayUrl
                                           : RelayUrl
                    }
                });

        public override BaseDoc ReadBytes(byte[] DocData, string RelayUrl = null) =>
            Read(DocData, RelayUrl);

        public override BaseDoc ReadText(string DocData, string RelayUrl = null) =>
            Read(DocData, RelayUrl);

        private LightDoc Submit(object DocData, string DocSubmittedByEmail, string RelayUrl, bool? DocStatus, DateTime? SubmittedDate, Dictionary<string, string> DocKeys, string DocTitle) =>
            (LightDoc) GetMethodInfo(DocData is byte[] ? DocCmd.SubmitBytes : DocCmd.SubmitText)
                .Invoke(UnderlyingWSClient,
                    new[]
                    {
                        DocData,
                        DocSubmittedByEmail,
                        string.IsNullOrWhiteSpace(RelayUrl)
                            ? DefaultRelayUrl
                            : RelayUrl,
                        DocStatus,
                        SubmittedDate,
                        DocKeys,
                        DocTitle
                    });

        public override LightDoc SubmitBytes(byte[] DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null) =>
            Submit(DocData, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle);

        public override LightDoc SubmitDoc(BaseDoc DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)  =>
            (LightDoc) GetMethodInfo(DocCmd.SubmitDoc)
                .Invoke(UnderlyingWSClient,
                    new object[]
                    {
                        DocData,
                        DocSubmittedByEmail,
                        string.IsNullOrWhiteSpace(RelayUrl)
                            ? DefaultRelayUrl
                            : RelayUrl,
                        DocStatus,
                        SubmittedDate,
                        DocKeys,
                        DocTitle
                    });

        public override LightDoc SubmitText(string DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null) =>
            Submit(DocData, DocSubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle);

        public override List<ContentInfo> TemplateSources() =>
            (List<ContentInfo>) _UnderlyingControllerType.GetMethod(nameof(TemplateSources))
                                                         .Invoke(UnderlyingWSClient,
                                                             new object[] { });
    }
}