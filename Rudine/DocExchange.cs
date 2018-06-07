using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dCForm.Core.Storage.Sql;
using Rudine.Exceptions;
using Rudine.Interpreters;
using Rudine.Storage.Docdb;
using Rudine.Template;
using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine
{
    public class DocExchange : BaseDocController, IDocKnownTypes
    {
        private SqlController SqlController = new SqlController();
        internal static readonly LuceneController LuceneController = new LuceneController();
        internal static readonly Lazy<DocExchange> _Instance = new Lazy<DocExchange>(() => new DocExchange());

        /// <summary>
        ///     singleton instance safe for multithreading
        /// </summary>
        public static DocExchange Instance => _Instance.Value;

        public override List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null) =>
            LuceneController.Audit(DocTypeName, DocId, RelayUrl);

        public override BaseDoc Create(BaseDoc Doc, Dictionary<string, string> DocKeys, string RelayUrl = null) { return Create(Doc, RelayUrl, true); }

        /// <summary>
        ///     Creates a new instance of the Doc parameter's type passed; merged/automapped/overlayed with another instance of
        ///     that same type constructed straight from the current template.xml located in the ~/form/{DocTypeName}/template.xml
        ///     of this application. Other properties specific not defined in the template.xml's text specific to this solution's
        ///     BaseDoc super-class & the current HttpContext are also inflated.
        /// </summary>
        /// <param name="DocSrc"></param>
        /// <param name="Doc"></param>
        /// <param name="DocKeys"></param>
        /// <param name="RelayUrl"></param>
        /// <param name="ProcessTemplate"></param>
        /// <returns></returns>
        public virtual BaseDoc Create(BaseDoc Doc, string RelayUrl = null, bool ProcessTemplate = true)
        {
            // apply ~/form/{DocTypeName}/template.xml values to document passed into us
            if (ProcessTemplate)
                Doc = PropertyOverlay.Overlay(Doc, DocInterpreter.Instance.Create(Doc.DocTypeName));

            //TODO:need to type-safe all the "object Doc" parameter methods
            Doc.DocChecksum = DocInterpreter.Instance.CalcDocChecksum(Doc);

            Doc.DocSrc = Nav.ToUrl(Doc, RelayUrl);

            return Doc;
        }

        public override DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null) =>
            (DocRev)Create(DocInterpreter.Instance.CreateTemplate(docFiles, docTypeName, docRev, schemaXml, schemaFields), null, false);

        /// <summary>
        ///     string of Types that template (DocRev sources) presents in ~/doc/*
        /// </summary>
        /// <returns>current DocTypeNames known to this system</returns>
        public List<string> DocTypeNames() =>
            DocTypeServedItems()
                .Select(typ => typ.Name)
                .ToList();

        /// <summary>
        ///     Types that can be actively served via WCF as "new" documents. There types must have a folder representation in the
        ///     file system. This list is cached internally. Before the list is constructed models and other contents are processed
        ///     & imported to the docdb database.
        /// </summary>
        /// <returns>current DocTypeNames known to this system</returns>
        public List<Type> DocTypeServedItems()
        {
            ImporterController.SyncTemplates(this);
            Dictionary<string, Type> doctypeserveddic = new Dictionary<string, Type>();

            foreach (LightDoc lightdoc in List(
                    new List<string> { DocRev.MyOnlyDocName })
                .OrderByDescending(lightDoc => new Version(lightDoc.GetTargetDocVer())))
                if (!doctypeserveddic.ContainsKey(lightdoc.GetTargetDocName()))
                    doctypeserveddic[lightdoc.GetTargetDocName()] = Runtime.ActivateBaseDocType(lightdoc.GetTargetDocName(), lightdoc.GetTargetDocVer(), this);

            return doctypeserveddic.Values.ToList();
        }

        public override BaseDoc Get(string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null) =>
            LuceneController.Get(DocTypeName, DocKeys, DocId, RelayUrl);

        public override DocTypeInfo Info(string DocTypeName)
        {
            return new DocTypeInfo
            {
                //   DocTypeName = DocTypeName,
                // DocTypeVer = TemplateController.Instance.TopDocRev(DocTypeName),
                Description = DocInterpreter.Instance.GetDescription(DocTypeName)
                //TODO:source IsSignable from DocDataInterpreter.Instance
                //IsSignable = DocInterpreter.Instance.Create(DocTypeName).IsSignable()
            };
        }

        public override List<ContentInfo> Interpreters() =>
            DocInterpreter.ContentInterpreterInstances.Select(m => m.ContentInfo).ToList();

        public override List<LightDoc> List(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null)
        {
            return List_With_DocSrc(DocTypeNames, DocKeys, DocProperties, KeyWord, PageSize, PageIndex, RelayUrl)
                .ToList();
        }

        private static IEnumerable<LightDoc> List_With_DocSrc(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null)
        {
            foreach (LightDoc _LightDoc in LuceneController.List(DocTypeNames, DocKeys, DocProperties, KeyWord, PageSize, PageIndex, RelayUrl))
            {
                _LightDoc.DocSrc = Nav.ToUrl(_LightDoc.DocTypeName, _LightDoc.DocId, RelayUrl);
                yield return _LightDoc;
            }
        }

        private static byte[] ProcessPI(byte[] DocData, string DocSubmittedByEmail, bool? DocStatus, DateTime? SubmittedDate, Dictionary<string, string> DocKeys, string DocTitle)
        {
            DocProcessingInstructions _DocProcessingInstructions = DocInterpreter.Instance.ReadDocPI(DocData);
            int DocChecksum = DocInterpreter.Instance.CalcDocChecksum(DocData, DocStatus);
            // make sure something has changed since this doc was served up
            if (!_DocProcessingInstructions.IsDocRev() && _DocProcessingInstructions.DocChecksum == DocChecksum)
                throw new NoChangesSinceRenderedException();
            return DocInterpreter.Instance.ModPI(DocData, DocSubmittedByEmail, DocStatus, SubmittedDate, DocKeys, DocTitle, DocChecksum);
        }

        private static string ProcessPI(string DocData, string DocSubmittedByEmail, bool? DocStatus, DateTime? SubmittedDate, Dictionary<string, string> DocKeys, string DocTitle)
        {
            DocProcessingInstructions _DocProcessingInstructions = DocInterpreter.Instance.ReadDocPI(DocData);
            int DocChecksum = DocInterpreter.Instance.CalcDocChecksum(DocData, DocStatus);
            // make sure something has changed since this doc was served up
            if (!_DocProcessingInstructions.IsDocRev() && _DocProcessingInstructions.DocChecksum == DocChecksum)
                throw new NoChangesSinceRenderedException();
            return DocInterpreter.Instance.ModPI(DocData, DocSubmittedByEmail, DocStatus, SubmittedDate, DocKeys, DocTitle, DocChecksum);
        }

        public override BaseDoc ReadBytes(byte[] DocData, string RelayUrl = null)
        {
            BaseDoc _BaseDoc = DocInterpreter.Instance.Read(DocData, true);
            return Create(_BaseDoc, RelayUrl, false);
        }

        public override BaseDoc ReadText(string DocData, string RelayUrl = null)
        {
            BaseDoc _BaseDoc = DocInterpreter.Instance.Read(DocData, true);
            return Create(_BaseDoc, RelayUrl, false);
        }

        public override LightDoc SubmitBytes(byte[] DocData, string SubmittedByEmail, DateTime? SubmittedDate = null, string RelayUrl = null, bool? DocStatus = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)
        {
            // validate the content against it's XSD if it's being "approved" as good captured information for the organization
            // now is a good time to do this as the exception we want the user to see first would have hacazd there chance
            DocInterpreter.Instance.Validate(DocData);
            DocData = ProcessPI(DocData, SubmittedByEmail, DocStatus, SubmittedDate, DocKeys, DocTitle);

            LightDoc _LightDoc = LuceneController.SubmitBytes(DocData);

            string
                TargetDocName = _LightDoc.GetTargetDocName(),
                TargetDocVer = _LightDoc.GetTargetDocVer();

            //#if !FAST
            Version existingVersion;
            // DOCREVs are only submitted via Text, there is no need to worry about them enter the system in another fusion. 
            if (_LightDoc.DocTypeName == DocRev.MyOnlyDocName)
                if (!string.IsNullOrWhiteSpace(TargetDocVer))
                    // if the DocRev submitted supersedes the current or this is no current..
                    if (!Version.TryParse(TemplateController.Instance.TopDocRev(TargetDocName), out existingVersion) || Version.Parse(TargetDocVer) >= existingVersion)
                        // if there is no representation of this DocRev as a directory in the file system (as this trumps the submitted one no matter what
                        // notice the true parameter value to clear the cache as well as assert we have the correct DocRev in the system now
                        if (Directory.Exists(FilesystemTemplateController.GetDocDirectoryPath(TargetDocName))
                            ||
                            TemplateController.Instance.TopDocRev(TargetDocName, true) != TargetDocVer)
                            throw new PocosImportException();

            SqlController.Submit(DocData, SubmittedByEmail, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle);

            return _LightDoc;
        }

        public override LightDoc SubmitDoc(BaseDoc DocData, string SubmittedByEmail, DateTime? SubmittedDate = null, string RelayUrl = null, bool? DocStatus = null, Dictionary<string, string> DocKeys = null, string DocTitle = null) =>
            SubmitStream(DocInterpreter.Instance.WriteStream(DocData, true), SubmittedByEmail, SubmittedDate, RelayUrl, DocStatus, DocKeys, DocTitle);

        public override LightDoc SubmitText(string DocData, string SubmittedByEmail, DateTime? SubmittedDate = null, string RelayUrl = null, bool? DocStatus = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)
        {
            // validate the content against it's XSD if it's being "approved" as good captured information for the organization
            // now is a good time to do this as the exception we want the user to see first would have hacazd there chance
            DocInterpreter.Instance.Validate(DocData);
            DocData = ProcessPI(DocData, SubmittedByEmail, DocStatus, SubmittedDate, DocKeys, DocTitle);
            LightDoc _LightDoc = LuceneController.SubmitText(DocData);

            return _LightDoc;
        }

        public override List<ContentInfo> TemplateSources() =>
            DocInterpreter.Instance.TemplateSources();
    }
}