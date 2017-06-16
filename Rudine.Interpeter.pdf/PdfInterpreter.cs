using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;
using Rudine.Template;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Pdf
{
    /// <summary>
    ///     pdfsharp does not dispose of itself correctly; we choose to dispose it's memorystream each time instead of leaving
    ///     it to the pdfdocument object itself
    /// </summary>
    public class PdfInterpreter : DocByteInterpreter
    {
        private const string TEMPLATE_PDF = "template.pdf";

        public override ContentInfo ContentInfo => new ContentInfo { ContentFileExtension = "pdf", ContentType = "application/pdf" };

        public override BaseDoc Create(string DocTypeName) { throw new NotImplementedException(); }

        /// <summary>
        ///     enumerate each PdfAcroField, convert it's value to clr type, use reflection to set that value to the clr object
        ///     BaseDoc.
        /// </summary>
        /// <param name="docFiles"></param>
        /// <param name="docTypeName">default will be the original pdf's filename without the extension & only alpha numerics</param>
        /// <param name="docRev"></param>
        /// <param name="schemaFields"></param>
        /// <returns></returns>
        public override DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null)
        {
            if (schemaFields == null)
                schemaFields = new List<CompositeProperty>();

            if (schemaFields.Count == 0)
                foreach (var docData in docFiles.Where(docFile => docFile.Name.EndsWith(ContentInfo.ContentFileExtension, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(docFile => docFile.ModDate))
                    if (schemaFields.Count == 0)
                        using (MemoryStream _MemoryStream = new MemoryStream(docData.Bytes))
                        using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.ReadOnly))
                        {
                            PdfAcroForm AcroForm = _PdfDocument.AcroForm;

                            for (int i = 0; i < AcroForm.Fields.Elements.Count; i++)
                                schemaFields.Add(AcroForm.Fields[i].AsCompositeProperty());

                            if (schemaFields.Count > 0)
                            {
                                DocProcessingInstructions pi = ReadDocPI(_PdfDocument);

                                docTypeName = !string.IsNullOrWhiteSpace(docTypeName)
                                                  ? docTypeName
                                                  : !string.IsNullOrWhiteSpace(pi.DocTypeName)
                                                      ? pi.DocTypeName
                                                      : GetFilenameDocTypeName(docData);
                                break;
                            }
                        }

            return schemaFields.Count == 0
                       ? null
                       : base.CreateTemplate(
                           docFiles,
                           docTypeName,
                           docRev,
                           schemaXml,
                           schemaFields);
        }

        public override string GetDescription(string DocTypeName) { throw new NotImplementedException(); }

        private static string GetSetDocPIProperty(PdfDocument _PdfDocument, string propertyName, object propertyValue = null)
        {
            string propertyValueAsString = string.Format("{0}", propertyValue);

            if (!string.IsNullOrWhiteSpace(propertyValueAsString))
                _PdfDocument.Info.Elements.SetString("/" + propertyName, propertyValueAsString);

            return _PdfDocument.Info.Elements.ContainsKey("/" + propertyName)
                       ? string.Format("{0}", _PdfDocument.Info.Elements["/" + propertyName])
                       : null;
        }

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) { throw new NotImplementedException(); }
        public override bool Processable(string DocTypeName, string DocRev) { throw new NotImplementedException(); }

        /// <summary>
        ///     enumerate each PdfAcroField, convert it's value to clr type, use reflection to set that value to the clr object
        ///     BaseDoc
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        public override BaseDoc Read(byte[] DocData, bool DocRevStrict = false)
        {
            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.ReadOnly))
            {
                DocProcessingInstructions _DocProcessingInstructions = ReadDocPI(_PdfDocument);

                BaseDoc _BaseDoc = Runtime.ActivateBaseDoc(
                    _DocProcessingInstructions.DocTypeName,
                    DocRevStrict
                        ? _DocProcessingInstructions.solutionVersion
                        : TemplateController.Instance.TopDocRev(_DocProcessingInstructions.DocTypeName),
                    DocExchange.Instance);

                Type _BaseDocType = _BaseDoc.GetType();

                for (int i = 0; i < _PdfDocument.AcroForm.Fields.Elements.Count; i++)
                {
                    PdfAcroField _Field = _PdfDocument.AcroForm.Fields[i];
                    CompositeProperty _CompositeProperty = _Field.AsCompositeProperty();
                    string _Value = string.Format("{0}", _Field.Value);

                    PropertyInfo _PropertyInfo = _BaseDocType.GetProperty(_CompositeProperty.Name, _CompositeProperty.PropertyType);
                    _PropertyInfo.SetValue(_BaseDoc, Convert.ChangeType(_Value, _PropertyInfo.PropertyType), null);
                }

                return SetPI(_BaseDoc, _DocProcessingInstructions);
            }
        }

        public override DocProcessingInstructions ReadDocPI(byte[] DocData)
        {
            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.InformationOnly))
            {
                return ReadDocPI(_PdfDocument);
            }
        }

        private static DocProcessingInstructions ReadDocPI(PdfDocument _PdfDocument)
        {
            DocProcessingInstructions pi = new DocProcessingInstructions
            {
                DocTitle = _PdfDocument.Info.Title
            };

            pi.DocTypeName = GetSetDocPIProperty(_PdfDocument, nameof(pi.DocTypeName));
            pi.DocStatus = bool.Parse(GetSetDocPIProperty(_PdfDocument, nameof(pi.solutionVersion)) ?? bool.FalseString);
            pi.solutionVersion = GetSetDocPIProperty(_PdfDocument, nameof(pi.solutionVersion));

            // PDF never seen/served by this app will not have a DocId defined to decrypt
            string DocId = GetSetDocPIProperty(_PdfDocument, Parm.DocId);
            if (!string.IsNullOrWhiteSpace(DocId))
                pi.SetDocId(DocId);

            return pi;
        }

        public override string ReadDocRev(byte[] DocData) => ReadDocPI(DocData).solutionVersion;
        public override string ReadDocTypeName(byte[] DocData) => ReadDocPI(DocData).DocTypeName;
        public override List<ContentInfo> TemplateSources() => new List<ContentInfo> { ContentInfo };
        public override void Validate(byte[] DocData) { throw new NotImplementedException(); }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream _MemoryStreamTemplate = TemplateController.Instance.OpenRead(source.DocTypeName, source.solutionVersion, TEMPLATE_PDF))
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStreamTemplate.CopyTo(_MemoryStream);
                _MemoryStream.Position = 0;

                using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.Modify))
                {
                    Type baseDocType = source.GetType();

                    for (int i = 0; i < _PdfDocument.AcroForm.Fields.Elements.Count; i++)
                    {
                        PdfAcroField _Field = _PdfDocument.AcroForm.Fields[i];
                        _Field.Value = new PdfString(string.Format("{0}", baseDocType.GetProperty(_Field.Name).GetValue(source, null)));
                    }

                    if (includeProcessingInformation)
                        WritePI(source, _PdfDocument);

                    _PdfDocument.Save(_MemoryStream, false);

                    return _MemoryStream.ToArray();
                }
            }
        }

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi)
        {
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStream.Write(DocData, 0, DocData.Length);
                _MemoryStream.Position = 0;

                using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.Modify))
                {
                    WritePI(pi, _PdfDocument);
                    _PdfDocument.Save(_MemoryStream, false);
                    _MemoryStream.Position = 0;
                    return _MemoryStream.ToArray();
                }
            }
        }

        private static void WritePI(DocProcessingInstructions pi, PdfDocument _PdfDocument)
        {
            _PdfDocument.Info.ModificationDate = _PdfDocument.Info.ModificationDate == DateTime.MinValue ? DateTime.Now : _PdfDocument.Info.ModificationDate;
            _PdfDocument.Info.Title = pi.DocTitle;

            GetSetDocPIProperty(_PdfDocument, nameof(pi.DocKeys), pi.GetDocId());
            GetSetDocPIProperty(_PdfDocument, nameof(pi.DocStatus), pi.DocStatus);
            GetSetDocPIProperty(_PdfDocument, nameof(pi.DocTypeName), pi.DocTypeName);
            GetSetDocPIProperty(_PdfDocument, nameof(pi.solutionVersion), pi.solutionVersion);
        }
    }
}