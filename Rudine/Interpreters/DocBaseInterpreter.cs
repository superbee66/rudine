using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Rudine.Template;
using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Util.Xsds;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters
{
    public abstract class DocBaseInterpreter : IDocBaseInterpreter, IHttpHandler
    {
        public abstract ContentInfo ContentInfo { get; }

        public bool IsReusable => false;

        protected static string AutoFileName(string docTypeName, string fileExtension) { return string.Format("{0}.{1}", GetFilenameDocTypeName(docTypeName), fileExtension); }

        protected static string AutoFileNameApply(string docTypeName, List<DocRevEntry> docFiles, string fileExtension)
        {
            string _MakeFileName = AutoFileName(docTypeName, fileExtension);
            for (int i = 0; i < docFiles.Count; i++)
                if (Path.GetExtension(docFiles[i].Name).Trim('.').Equals(fileExtension.Trim('.'), StringComparison.InvariantCultureIgnoreCase))
                {
                    docFiles[i].Name = _MakeFileName;
                    break;
                }

            return _MakeFileName;
        }

        internal static string BuildHref(string DocTypeName, string solutionVersion)
            => BuildHref(DocTypeName, solutionVersion, new Uri(RequestPaths.ApplicationPath));

        internal static string BuildHref(HttpContext context, string DocTypeName, string solutionVersion)
        {
            string href = BuildHref(DocTypeName, solutionVersion, context.Request.Url);

            return !string.IsNullOrWhiteSpace(context.Request.Params[Parm.RelayUrl])
                       ? // Is this request coming in from a "proxying listener"?
                       string.Format("{0}{1}",
                           context.Request.Params[Parm.RelayUrl],
                           href.Substring(href.IndexOf(context.Request.ApplicationPath, StringComparison.Ordinal) + context.Request.ApplicationPath.Length))
                       : href;
        }

        private static string BuildHref(string DocTypeName, string solutionVersion, Uri _uri)
        {
            string basePath = _uri.Query.Length > 0
                                  ? _uri.AbsoluteUri.Substring(_uri.AbsoluteUri.LastIndexOf('/') + 1).Replace(_uri.Query, "")
                                  : _uri.AbsoluteUri;
            string href = new Uri(string.Format("{0}/{1}/{2}/{3}/{4}",
                _uri.Query.Length > 0
                    ? _uri
                        .AbsoluteUri
                        .Replace(_uri.Query, "")
                        .Replace(basePath, "")
                        .TrimEnd('/')
                    : _uri.AbsoluteUri,
                FilesystemTemplateController.DirectoryName,
                DocTypeName,
                solutionVersion,
                DocInterpreter.Instance.HrefVirtualFilename(DocTypeName, solutionVersion))).ToString();
            return href;
        }

        /// <summary>
        /// Creates poco of the most current DocRev for the given docTypeName 
        /// </summary>
        /// <param name="docTypeName"></param>
        /// <returns></returns>
        public virtual BaseDoc Create(string docTypeName)
        {
            string _TopDocRev = TemplateController.Instance.TopDocRev(docTypeName);

            BaseDoc _BaseDoc = Runtime.ActivateBaseDoc(docTypeName, _TopDocRev, DocExchange.Instance);
            _BaseDoc.DocTypeName = docTypeName;
            _BaseDoc.solutionVersion = _TopDocRev;

            return _BaseDoc;
        }

        /// <summary>
        /// </summary>
        /// <param name="docFiles"></param>
        /// <param name="docTypeName">
        ///     defaults to the newest file's name withou the extension ending with the any of the
        ///     TemplateSources() item's file extension
        /// </param>
        /// <param name="docRev">defaults to docFiles newest file modData AsDocRev</param>
        /// <param name="schemaFields"></param>
        /// <returns></returns>
        public virtual DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null)
        {
            if (string.IsNullOrWhiteSpace(docRev))
                docRev = docFiles.Max(docFile => docFile.ModDate).AsDocRev();

            if (string.IsNullOrWhiteSpace(docTypeName))
                docTypeName = TemplateSources()
                    .SelectMany(templateSource =>
                                    docFiles.Where(docFile => docFile.Name.EndsWith(templateSource.ContentFileExtension, StringComparison.InvariantCultureIgnoreCase)))
                    .OrderByDescending(docFile => docFile.ModDate)
                    .Select(GetFilenameDocTypeName)
                    .FirstOrDefault();

            if (schemaFields != null)
                if (string.IsNullOrWhiteSpace(schemaXml))
                // the "lazy-load" CompositeType requires activation in order for the _template_docx_obj.GetType().Assembly to register as having any types defined
                {
                    Type type = Activator.CreateInstance(
                                             new CompositeType(
                                                 RuntimeTypeNamer.CalcCSharpNamespace(
                                                     docTypeName,
                                                     docRev,
                                                     nameof(IDocBaseInterpreter)),
                                                 docTypeName,
                                                 schemaFields.ToArray())).GetType();
                    schemaXml = XsdExporter.ExportSchemas(
                                               type.Assembly,
                                               new List<string> { type.FullName },
                                               RuntimeTypeNamer.CalcSchemaUri(docTypeName, docRev)).First();
                }

            DocURN _DocURN = new DocURN
            {
                DocTypeName = docTypeName,
                solutionVersion = docRev
            };

            //TODO:Relationship between EmbeddedInterpr.. DocInter.. & BaseDocInter needs to be rethought
            return new DocRev
            {
                DocFiles = docFiles,
                DocSchema = schemaXml,
                DocURN = _DocURN,
                DocIdKeys = DocRev.MakeDocKeys(_DocURN),
                solutionVersion = DocRev.MyOnlyDocVersion,
                DocTypeName = DocRev.MyOnlyDocName
            };
        }

        /// <summary>
        ///     should operate on the data itself while avoiding serialization operations that may alter the DocData
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <returns></returns>
        public abstract string GetDescription(string DocTypeName);

        public static string GetFilename(DocProcessingInstructions _DocProcessingInstructions, string ContentFileExtension = null) =>
            string.Format(
                "{0}.{1}",
                FileSystem.CleanFileName(_DocProcessingInstructions.DocTitle)
                          .Trim(),
                string.IsNullOrWhiteSpace(ContentFileExtension)
                    ? DocInterpreter
                        .InstanceLocatorByName<DocBaseInterpreter>(_DocProcessingInstructions.DocTypeName, _DocProcessingInstructions.solutionVersion)
                        .ContentInfo.ContentFileExtension
                    : ContentFileExtension);

        /// <summary>
        /// </summary>
        /// <param name="docFile"></param>
        /// <returns>filename stripped of non-alpha-numerics uppercase</returns>
        protected static string GetFilenameDocTypeName(DocRevEntry docFile) =>
            GetFilenameDocTypeName(docFile.Name);

        private static string GetFilenameDocTypeName(string name)
        {
            return Regex.Replace(
                StringTransform.SafeIdentifier(Path.GetFileNameWithoutExtension(name)),
                "[^0-9A-Za-z_]",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        ///     Should be backed by a httphandler. For InfoPath there is a manifest.xsf the InfoPath Desktop Application will be
        ///     searching for. For JsonInterpreter a mycontents.cab will be targeted.
        /// </summary>
        public abstract string HrefVirtualFilename(string DocTypeName, string DocRev);

        /// <summary>
        ///     Should this instance of an interpreter actually process the given document if it were passed?
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="DocRev"></param>
        /// <returns></returns>
        public abstract bool Processable(string DocTypeName, string DocRev);

        /// <summary>
        ///     locate versions of what were once physical files utilizing form/DocTypeName/VersionNumber/*.*
        ///     from archives of what was once seen & now compressed as cab in the document database.
        ///     This allows older documents in the field to request resources that deployed a long time ago
        /// </summary>
        /// <param name="context"></param>
        public virtual void ProcessRequest(HttpContext context)
        {
            TemplateFileInfo templatefileinfo;
            using (MemoryStream _MemoryStream = TemplateController.Instance.OpenRead(context, out templatefileinfo))
            {
                context.Response.DisableKernelCache();
                context.Response.Clear();
                context.Response.ClearContent();
                context.Response.ClearHeaders();

                _MemoryStream.CopyTo(context.Response.OutputStream);

                context.Response.ContentType = MimeExtensionHelper.GetMimeType(templatefileinfo.FileName);
                context.Response.AddHeader("content-disposition", "attachment; filename=\"" + templatefileinfo.FileName + "\";");
            }
        }

        /// <summary>
        ///     simple helper method to assign values from a DocProcessingInstruction type to a BaseDoc
        /// </summary>
        /// <param name="dstBaseDoc"></param>
        /// <param name="pi"></param>
        /// <param name="DocTypeName"></param>
        /// <param name="DocRev"></param>
        /// <returns></returns>
        public static BaseDoc SetPI(BaseDoc dstBaseDoc, DocProcessingInstructions pi, string DocTypeName = null, string DocRev = null)
        {
            dstBaseDoc = (BaseDoc)PropertyOverlay.Overlay(pi, dstBaseDoc);
            dstBaseDoc.DocTypeName = DocTypeName ?? pi.DocTypeName;
            dstBaseDoc.solutionVersion = DocRev ?? pi.solutionVersion;
            dstBaseDoc.DocStatus = pi.DocStatus ?? dstBaseDoc.DocStatus;
            return dstBaseDoc;
        }

        public abstract List<ContentInfo> TemplateSources();
    }
}