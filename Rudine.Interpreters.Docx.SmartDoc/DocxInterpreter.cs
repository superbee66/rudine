using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rudine.Interpreters.Xsn;
using Rudine.Template;
using Rudine.Web;

namespace Rudine.Interpreters.Docx.SmartDoc
{
    /// <summary>
    ///     Supports the byte array based Microsoft Word docx document format with bound Content Controls. Essentially a Word
    ///     document wrapping an XML document created with the same underlying writer as InfoPath.
    /// </summary>
    public class DocxInterpreter : DocByteInterpreter
    {
        private const string XML_PROCESSING_INSTRUCTIONS_FORMAT = "DocId=\"{0}\" DocTitle=\"{1}\" DocTypeName=\"{2}\" DocChecksum=\"{3}\" solutionVersion=\"{4}\" href=\"{5}\"";
        private const string TEMPLATE_DOCX = "template.docx";

        /// <summary>
        ///     ms word seems to be good with most of the routines used to produce infopath xml. DocxInterpreter is just about a
        ///     wrapper for XsnXmlInterpreter. xml processing instructions from the old infopath are one of the few things not
        ///     wanted from infopath (mso-* prefixed pi lines)
        /// </summary>
        private static readonly XsnXmlInterpreter UnderlyingXsnXmlInterpreter = new XsnXmlInterpreter
        {
            WriteXmlProcessingInstructions = (pi, writer) =>
            {
                writer.WriteProcessingInstruction(
                    typeof(DocxInterpreter).Name.ToLower(),
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        XML_PROCESSING_INSTRUCTIONS_FORMAT,
                        pi.GetDocId(),
                        pi.DocTitle,
                        pi.DocTypeName,
                        pi.DocChecksum,
                        pi.solutionVersion ?? TemplateController.Instance.TopDocRev(pi.DocTypeName),
                        pi.href));

                return writer;
            }
        };

        private static readonly string parseReadDocTypeName_PATTEN = string.Format(System.Globalization.CultureInfo.InvariantCulture,@"(<?{0} [^>]+ DocTypeName="")(?<DocTypeName>\w+)([^>]+>)", typeof(DocxInterpreter).Name.ToLower());

        public override ContentInfo ContentInfo =>
            new ContentInfo
            {
                ContentFileExtension = "docx",
                ContentType = MsOfficeContentType.FileExtensions["docx"],
                ContentSignature = new MagicNumbers
                {
                    Bytes = new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                    Offset = 0
                }
            };

        public override BaseDoc Create(string DocTypeName) =>
            UnderlyingXsnXmlInterpreter.Create(DocTypeName);

        private static byte[] DocXmlAttach(byte[] Docx, string DocXml, DocProcessingInstructions pi = null)
        {
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStream.Write(Docx, 0, Docx.Length);
                _MemoryStream.Position = 0;

                using (WordprocessingDocument _WordprocessingDocument = WordprocessingDocument.Open(_MemoryStream, true))
                {
                    _WordprocessingDocument.CoreFilePropertiesPart.OpenXmlPackage.PackageProperties.ContentStatus = pi.DocStatus == null ? string.Empty : pi.DocStatus.ToString();
                    _WordprocessingDocument.CoreFilePropertiesPart.OpenXmlPackage.PackageProperties.Title = pi.DocTitle;
                    _WordprocessingDocument.CoreFilePropertiesPart.OpenXmlPackage.PackageProperties.Identifier = pi.GetDocId();

                    MainDocumentPart _MainDocumentPart = _WordprocessingDocument.MainDocumentPart;
                    _MainDocumentPart.DeleteParts(_MainDocumentPart.CustomXmlParts);

                    if (pi != null)
                    {
                        DocXml = UnderlyingXsnXmlInterpreter.WritePI(DocXml, pi);
                        string TargetNamespace = RuntimeTypeNamer.CalcSchemaUri(pi.DocTypeName, pi.solutionVersion);

                        // refresh the 
                        _MainDocumentPart.DocumentSettingsPart.Settings.DocumentSettingsPart.Settings.RemoveAllChildren<AttachedSchema>();
                        _MainDocumentPart.DocumentSettingsPart.Settings.DocumentSettingsPart.Settings.AppendChild(
                            new AttachedSchema
                            {
                                Val = TargetNamespace
                            });

                        _MainDocumentPart.DocumentSettingsPart.Settings.DocumentSettingsPart.Settings.RemoveAllChildren<SchemaLibrary>();
                        _MainDocumentPart.DocumentSettingsPart.Settings.DocumentSettingsPart.Settings.AppendChild(
                            new SchemaLibrary(new Schema
                            {
                                SchemaLocation = string.IsNullOrWhiteSpace(pi.href) ? XsnXmlInterpreter.schemaFileName : pi.href,
                                Uri = TargetNamespace
                            }));
                    }

                    using (MemoryStream _DocXmlMemoryStream = new MemoryStream())
                    using (StreamWriter _StreamWriter = new StreamWriter(_DocXmlMemoryStream, Encoding.Unicode))
                    {
                        _StreamWriter.Write(DocXml);
                        _StreamWriter.Flush();
                        _DocXmlMemoryStream.Position = 0;
                        _MainDocumentPart.AddCustomXmlPart(CustomXmlPartType.CustomXml)
                                         .FeedData(_DocXmlMemoryStream);
                    }
                }

                _MemoryStream.Position = 0;
                return _MemoryStream.ToArray();
            }
        }

        /// <summary>
        ///     Scans the CustomXMLParts within the document until one is identified with a validate DocTypeName & DocRev
        /// </summary>
        /// <param name="Docx"></param>
        /// <returns>an xml document</returns>
        private static WordprocessingDocumentInfo DocXmlDetach(byte[] Docx)
        {
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStream.Write(Docx, 0, Docx.Length);
                _MemoryStream.Position = 0;

                using (WordprocessingDocument _WordprocessingDocument = WordprocessingDocument.Open(_MemoryStream, false))
                    foreach (CustomXmlPart _CustomXmlPart in _WordprocessingDocument.MainDocumentPart.GetPartsOfType<CustomXmlPart>())
                        using (StreamReader _StreamReader = new StreamReader(_CustomXmlPart.GetStream()))
                        {
                            WordprocessingDocumentInfo _WordprocessingDocumentInfo = new WordprocessingDocumentInfo
                            {
                                DocXml = _StreamReader.ReadToEnd()
                            };

                            _WordprocessingDocumentInfo.Info = new DocTypeInfo
                            {
                                // notice the DocTypeName uses it's own pattern matcher & not the underlying xsn controller's to get the name; DocTypeName are parsed in a careful manner as to not get document's mixed up between controllers
                                DocTypeName = Regex.Match(_WordprocessingDocumentInfo.DocXml, parseReadDocTypeName_PATTEN, RegexOptions.IgnoreCase)
                                                   .Groups["DocTypeName"]
                                                   .Value,
                                solutionVersion = UnderlyingXsnXmlInterpreter.ReadDocRev(_WordprocessingDocumentInfo.DocXml),
                                Description = _WordprocessingDocument.PackageProperties.Description,
                                IsSignable = false
                            };

                            if (!string.IsNullOrWhiteSpace(_WordprocessingDocumentInfo.Info.DocTypeName) && !string.IsNullOrWhiteSpace(_WordprocessingDocumentInfo.Info.solutionVersion))
                                return _WordprocessingDocumentInfo;
                        }
            }
            throw new Exception("word document has no valid custom xml part");
        }

        public override string GetDescription(string DocTypeName) =>
            DocXmlDetach(WriteByte(Create(DocTypeName))).Info.Description;

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) =>
            "myschema.xsd";

        public override bool Processable(string DocTypeName, string DocRev)
        {
            using (MemoryStream _MemoryStream = TemplateController.Instance.OpenRead(DocTypeName, DocRev, TEMPLATE_DOCX))
            {
                if (_MemoryStream != null)
                {
                    WordprocessingDocumentInfo _WordprocessingDocumentInfo = DocXmlDetach(_MemoryStream.ToArray());
                    if (_WordprocessingDocumentInfo.Info.DocTypeName == DocTypeName)
                        if (_WordprocessingDocumentInfo.Info.solutionVersion == DocRev)
                            return true;
                }
                return false;
            }
        }

        public override BaseDoc Read(byte[] DocData, bool DocRevStrict = false) => 
            UnderlyingXsnXmlInterpreter.Read(DocXmlDetach(DocData).DocXml, DocRevStrict);

        public override DocProcessingInstructions ReadDocPI(byte[] DocData) =>
            UnderlyingXsnXmlInterpreter.ReadDocPI(DocXmlDetach(DocData).DocXml);

        public override string ReadDocRev(byte[] DocData) => 
            DocXmlDetach(DocData).Info.solutionVersion;

        public override string ReadDocTypeName(byte[] DocData) =>
            DocXmlDetach(DocData).Info.DocTypeName;

        public override List<ContentInfo> TemplateSources()
        {
            throw new NotImplementedException();
        }

        public override void Validate(byte[] DocData) =>
            UnderlyingXsnXmlInterpreter.Validate(DocXmlDetach(DocData).DocXml);

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream _MemoryStream = TemplateController.Instance.OpenRead(
                source.DocTypeName,
                source.solutionVersion,
                TEMPLATE_DOCX))
            {
                string DocXml = UnderlyingXsnXmlInterpreter.WriteText(source, includeProcessingInformation);

                return DocXmlAttach(
                    _MemoryStream.ToArray(),
                    DocXml,
                    includeProcessingInformation
                        ? UnderlyingXsnXmlInterpreter.ReadDocPI(DocXml)
                        : null);
            }
        }

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi) =>
            DocXmlAttach(DocData, DocXmlDetach(DocData).DocXml, pi);

        private struct WordprocessingDocumentInfo
        {
            public string DocXml;
            public DocTypeInfo Info;
        }
    }
}