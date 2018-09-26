using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Rudine.Exceptions;
using Rudine.Template;
using Rudine.Util;
using Rudine.Util.Zips;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters
{
    /// <summary>
    ///     the master head of an abstract factory pattern applied to bridge our CLR objects & values to native document
    ///     formats supported
    /// </summary>
    internal sealed class DocInterpreter : IDocTextInterpreter, IDocByteInterpreter
    {
        internal static readonly DocBaseInterpreter[] ContentInterpreterInstances;

        static DocInterpreter()
        {
            ContentInterpreterInstances = Reflection
                .LoadBinDlls()
                .SelectMany(a => a.GetExportedTypes())
                .Where(type => !type.IsAbstract)
                .Where(type => !type.IsInterface)
                .Where(type => type.IsSubclassOf(typeof(DocTextInterpreter)) || type.IsSubclassOf(typeof(DocByteInterpreter)))
                .Select(type => (DocBaseInterpreter)Activator.CreateInstance(type))
                .ToArray();
        }

        private static readonly Lazy<DocInterpreter> _Instance = new Lazy<DocInterpreter>(() => new DocInterpreter());

        /// <summary>
        ///     singleton instance safe for multithreading
        /// </summary>
        public static DocInterpreter Instance => _Instance.Value;


        public string ContentFileExtension =>
            "dat";

        public ContentInfo ContentInfo => null;

        public string ContentType =>
            "application/text";

        /// <summary>
        ///     XmlSerialize object without processing instructions, remove all tags & collapse any white-space to a single space
        /// </summary>
        /// <param name="baseDoc"></param>
        /// <returns></returns>
        public int CalcDocChecksum(string DocData, bool? docStatus) =>
            CalcDocChecksum(Read(DocData, true), docStatus);

        /// <summary>
        ///     XmlSerialize object without processing instructions, remove all tags & collapse any white-space to a single space
        /// </summary>
        /// <param name="baseDoc"></param>
        /// <returns></returns>
        public int CalcDocChecksum(byte[] DocData, bool? docStatus) =>
            CalcDocChecksum(Read(DocData, true), docStatus);

        /// <summary>
        ///     XmlSerialize object without processing instructions, remove all tags & collapse any white-space to a single space,
        ///     convert all apply ToLocalTime to all DateTime properties
        /// </summary>
        /// <param name="baseDoc"></param>
        /// <returns></returns>
        public int CalcDocChecksum(BaseDoc baseDoc, bool? docStatus = null)
        {
            if (docStatus == null && baseDoc.DocStatus != null)
                return CalcDocChecksum(baseDoc, baseDoc.DocStatus);

            // absolutely necessary the object is not altered in any way shape of form
            //TODO:implement NormalizeDateTimePropertyValues recursively & properly
            baseDoc = baseDoc.Clone(); // NormalizeDateTimePropertyValues();

            // strip the processing instruction values so they don't yield a different rendering of the object
            foreach (PropertyInfo p in typeof(DocProcessingInstructions).GetProperties()
                                                                        .Where(p => p.CanWrite && p.PropertyType.IsValueType))
                p.SetValue(baseDoc, Activator.CreateInstance(p.PropertyType), null);

            using (StringWriter _StringWriter = new StringWriter())
            using (XmlTextWriter _XmlTextWriter = new XmlTextWriter(_StringWriter))
            {
                new XmlSerializer(baseDoc.GetType()).Serialize(_XmlTextWriter, baseDoc);
                return _StringWriter.ToString()
                                    .GetHashCode() ^ string.Format("{0}", docStatus).GetHashCode();
            }
        }

        public BaseDoc Create(string DocTypeName) =>
            InstanceLocatorByName<DocBaseInterpreter>(
                    DocTypeName,
                    TemplateController.Instance.TopDocRev(DocTypeName))
                .Create(DocTypeName);

        /// <summary>
        ///     locates a ContentInterpreter based on the claimed TemplateSource ContentFileExtension that matches one of the
        ///     docFiles passed. continues probing and testing until someone can produce a DocRev
        /// </summary>
        /// <param name="docFiles"></param>
        /// <param name="docTypeName"></param>
        /// <param name="docRev"></param>
        /// <param name="schemaXml"></param>
        /// <param name="schemaFields"></param>
        /// <returns></returns>
        public DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null)
        {
            foreach (string Extension in docFiles.Select(_ => Path.GetExtension(_.Name).Trim('.')).Distinct())
                foreach (DocBaseInterpreter _IDocDataInterpreter in ContentInterpreterInstances)
                    if (_IDocDataInterpreter.TemplateSources()
                                            .Any(templateSource =>
                                                templateSource
                                                .ContentFileExtension
                                                .Trim('.')
                                                .Equals(Extension, StringComparison.CurrentCultureIgnoreCase)))
                        return _IDocDataInterpreter.CreateTemplate(docFiles, docTypeName, docRev, schemaXml, schemaFields);

            return null;
        }

        public string GetDescription(string DocTypeName) =>
            InstanceLocatorByName<DocBaseInterpreter>(DocTypeName, Create(DocTypeName)
                    .solutionVersion)
                .GetDescription(DocTypeName);

        public string HrefVirtualFilename(string DocTypeName, string DocRev) =>
            InstanceLocatorByName<DocBaseInterpreter>(DocTypeName, DocRev)
                .HrefVirtualFilename(DocTypeName, DocRev);

        internal static T InstanceLocatorByName<T>(string DocTypeName, string DocRev = null) where T : DocBaseInterpreter =>
            CacheMan.Cache(() =>
            {
                foreach (T _IDocDataInterpreter in ContentInterpreterInstances.OfType<T>())
                    //TODO:Need a better way of discovering what IDocDataInterpreter can process the given document; it needs to consider the DocRev also
                    if (_IDocDataInterpreter.Processable(DocTypeName, DocRev))
                        return _IDocDataInterpreter;
                throw new Exception(String.Format("{0} {1}, {2} could not locate a DocDataInterpreter to process the data passed", DocTypeName, DocRev, typeof(DocInterpreter).Name));
            }, false, "InstanceLocatorByName", DocTypeName, DocRev ?? String.Empty);

        private static DocTextInterpreter LocateInstance(string DocData)
        {
            foreach (DocTextInterpreter _IDocDataInterpreter in ContentInterpreterInstances.OfType<DocTextInterpreter>())
                if (!String.IsNullOrWhiteSpace(_IDocDataInterpreter.ReadDocTypeName(DocData)) && !String.IsNullOrWhiteSpace(_IDocDataInterpreter.ReadDocRev(DocData)))
                    return _IDocDataInterpreter;
            throw new InterpreterLocationException();
        }

        private static DocByteInterpreter LocateInstance(byte[] DocData)
        {
            foreach (DocByteInterpreter _IDocDataInterpreter in ContentInterpreterInstances.OfType<DocByteInterpreter>())
                if (!String.IsNullOrWhiteSpace(_IDocDataInterpreter.ReadDocTypeName(DocData)) && !String.IsNullOrWhiteSpace(_IDocDataInterpreter.ReadDocRev(DocData)))
                    return _IDocDataInterpreter;
            throw new InterpreterLocationException();
        }

        private static DocProcessingInstructions MergePI(bool? DocStatus, Dictionary<string, string> DocKeys, string DocTitle, DocProcessingInstructions _DocProcessingInstructions, int? DocChecksum = null, string href = null)
        {
            _DocProcessingInstructions.DocStatus = DocStatus ?? _DocProcessingInstructions.DocStatus;
            _DocProcessingInstructions.DocTitle = DocTitle ?? _DocProcessingInstructions.DocTitle;
            _DocProcessingInstructions.DocKeys = DocKeys ?? _DocProcessingInstructions.DocKeys;
            _DocProcessingInstructions.DocChecksum = DocChecksum ?? _DocProcessingInstructions.DocChecksum;
            _DocProcessingInstructions.href = href ?? _DocProcessingInstructions.href;

            return _DocProcessingInstructions;
        }

        internal string ModPI(string DocData, string DocSubmittedByEmail = null, bool? DocStatus = default(bool?), DateTime? SubmittedDate = default(DateTime?), Dictionary<string, string> DocKeys = null, string DocTitle = null, int? DocChecksum = null, string href = null) =>
            WritePI(
                DocData,
                MergePI(
                    DocStatus,
                    DocKeys,
                    DocTitle,
                    ReadDocPI(DocData),
                    DocChecksum,
                    href));

        internal byte[] ModPI(byte[] DocData, string DocSubmittedByEmail = null, bool? DocStatus = default(bool?), DateTime? SubmittedDate = default(DateTime?), Dictionary<string, string> DocKeys = null, string DocTitle = null, int? DocChecksum = null, string href = null) =>
            WritePI(
                DocData,
                MergePI(
                    DocStatus,
                    DocKeys,
                    DocTitle,
                    ReadDocPI(DocData),
                    DocChecksum,
                    href));

        public bool Processable(string DocTypeName, string DocRev) =>
            InstanceLocatorByName<DocBaseInterpreter>(DocTypeName, DocRev)
                .Processable(DocTypeName, DocRev);

        public void ProcessRequest(HttpContext context)
        {
            // ensure the latest content has been processed & imported
            ImporterController.SyncTemplates(DocExchange.Instance);

            TemplateFileInfo _TemplateFileInfo = TemplateController.Instance.ParseTemplateFileInfo(context);

            if (!string.IsNullOrWhiteSpace(_TemplateFileInfo.DocTypeName)
                &&
                !string.IsNullOrWhiteSpace(_TemplateFileInfo.solutionVersion)
                &&
                !string.IsNullOrWhiteSpace(_TemplateFileInfo.FileName))
            {
                // serve a supporting template file up (the the target Interpreter may need to alter it in some way, qazi-dynamic)
                InstanceLocatorByName<DocBaseInterpreter>(_TemplateFileInfo.DocTypeName, _TemplateFileInfo.solutionVersion)
                    .ProcessRequest(context);
            }
            else
            {
                // serve a filled & stored document rendered previously
                context.Response.DisableKernelCache();
                context.Response.Clear();
                context.Response.ClearContent();
                context.Response.ClearHeaders();

                object docData;

                if (context.Request.Params.AllKeys.Any(m => m == Parm.DocId.ToString()))
                    docData = DocExchange.LuceneController.GetDocData(
                                             context.Request.Params[Parm.DocTypeName],
                                             context.Request.Params[Parm.DocId],
                                             HttpUtility.UrlDecode(context.Request.Params[Parm.RelayUrl]),
                                             long.Parse(context.Request.Params[Parm.LogSequenceNumber] ?? "0"));
                else
                    docData = ((BaseDoc)(context.Request.Params.AllKeys.Any(m => m == Parm.DocCache)
                                              ? HttpRuntime.Cache[context.Request.Params[Parm.DocCache]]
                                              : Compressor.DecompressFromBase64String(HttpUtility.UrlDecode(context.Request.Params[Parm.DocBin]))
                                                          .FromBytes<BaseDoc>()));

                DocBaseInterpreter _DocBaseInterpreter = null;
                DocProcessingInstructions _DocProcessingInstructions;

                //TODO:need to tidy up this code block as its really hard to follow.. In the end, that docData may be a POCO, raw bytes or text... Need to rewrites it's PI here before it's shoved over the repsonse.stream
                if (docData is BaseDoc)
                {
                    // get the interpreter & convert docData from poco to its raw form
                    _DocBaseInterpreter = InstanceLocatorByName<DocBaseInterpreter>(((BaseDoc)docData).DocTypeName, ((BaseDoc)docData).solutionVersion);
                    if (_DocBaseInterpreter is DocByteInterpreter)
                        docData = ((DocByteInterpreter)_DocBaseInterpreter).WriteByte((BaseDoc)docData);
                    else
                        docData = ((DocTextInterpreter)_DocBaseInterpreter).WriteText((BaseDoc)docData);
                }

                if (docData is byte[])
                {
                    _DocBaseInterpreter = _DocBaseInterpreter ?? LocateInstance((byte[])docData);
                    _DocProcessingInstructions = Instance.ReadDocPI((byte[])docData);
                    docData = Instance.ModPI((byte[])docData, href: DocBaseInterpreter.BuildHref(context, _DocProcessingInstructions.DocTypeName, _DocProcessingInstructions.solutionVersion));
                    context.Response.BinaryWrite((byte[])docData);
                }
                else
                {
                    _DocBaseInterpreter = _DocBaseInterpreter ?? LocateInstance((string)docData);
                    _DocProcessingInstructions = Instance.ReadDocPI((string)docData);
                    docData = Instance.ModPI((string)docData, href: DocBaseInterpreter.BuildHref(context, _DocProcessingInstructions.DocTypeName, _DocProcessingInstructions.solutionVersion));
                    context.Response.Write((string)docData);
                }

                context.Response.ContentType = _DocBaseInterpreter.ContentInfo.ContentType;
                context.Response.AddHeader(
                           "content-disposition",
                           string.Format(
                               "attachment; filename=\"{0}\";",
                               DocBaseInterpreter.GetFilename(_DocProcessingInstructions, context.Request.Params["ContentFileExtension"])));
            }
        }

        public BaseDoc Read(byte[] DocData, bool DocRevStrict = false) =>
            LocateInstance(DocData)
                .Read(DocData, DocRevStrict);

        public BaseDoc Read(string DocData, bool DocRevStrict = false) =>
            LocateInstance(DocData)
                .Read(DocData, DocRevStrict);

        public DocProcessingInstructions ReadDocPI(byte[] DocData) =>
            LocateInstance(DocData)
                .ReadDocPI(DocData);

        public DocProcessingInstructions ReadDocPI(string DocData) =>
            LocateInstance(DocData)
                .ReadDocPI(DocData);

        public string ReadDocRev(byte[] DocData) =>
            LocateInstance(DocData)
                .ReadDocRev(DocData);

        public string ReadDocRev(string DocData) =>
            LocateInstance(DocData)
                .ReadDocRev(DocData);

        public string ReadDocTypeName(byte[] DocData) =>
            LocateInstance(DocData)
                .ReadDocTypeName(DocData);

        public string ReadDocTypeName(string DocData) =>
            LocateInstance(DocData)
                .ReadDocTypeName(DocData);

        public List<ContentInfo> TemplateSources() =>
            ContentInterpreterInstances.SelectMany(ContentInterpreterInstance => ContentInterpreterInstance.TemplateSources())
                                       .Distinct()
                                       .ToList();

        public void Validate(byte[] DocData) =>
            LocateInstance(DocData)
                .Validate(DocData);

        public void Validate(string DocData) =>
            LocateInstance(DocData)
                .Validate(DocData);

        public byte[] WriteByte<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions
        {
            string DocTypeName, DocRev;
            if (!RuntimeTypeNamer.TryParseDocNameAndRev(source.GetType()
                                                              .Namespace, out DocTypeName, out DocRev))
                throw new Exception("Can't determine DocTypeName/DocRev");
            return InstanceLocatorByName<DocByteInterpreter>(DocTypeName, DocRev)
                .WriteByte(source, includeProcessingInformation);
        }

        public byte[] WritePI(byte[] DocData, DocProcessingInstructions pi) =>
            LocateInstance(DocData)
                .WritePI(DocData, pi);

        public string WritePI(string DocData, DocProcessingInstructions pi) =>
            LocateInstance(DocData)
                .WritePI(DocData, pi);

        public MemoryStream WriteStream<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions
        {
            DocBaseInterpreter _DocBaseInterpreter = InstanceLocatorByName<DocBaseInterpreter>(source.DocTypeName, source.solutionVersion);
            MemoryStream _MemoryStream = new MemoryStream();

            if (_DocBaseInterpreter is DocByteInterpreter)
            {
                byte[] docBytes = ((DocByteInterpreter)_DocBaseInterpreter).WriteByte(source, includeProcessingInformation);
                _MemoryStream.Write(docBytes, 0, docBytes.Length);
                _MemoryStream.Position = 0;
                return _MemoryStream;
            }

            return ((DocTextInterpreter)_DocBaseInterpreter).WriteText(source, includeProcessingInformation)
                                                             .AsMemoryStream();
        }

        public string WriteText<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions
        {
            string DocTypeName, DocRev;
            if (!RuntimeTypeNamer.TryParseDocNameAndRev(source.GetType()
                                                              .Namespace, out DocTypeName, out DocRev))
                throw new Exception("Can't determine DocTypeName/DocRev");
            return InstanceLocatorByName<DocTextInterpreter>(DocTypeName, DocRev)
                .WriteText(source, includeProcessingInformation);
        }
    }
}