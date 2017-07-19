﻿using System;
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
        public override ContentInfo ContentInfo => new ContentInfo
        {
            ContentFileExtension = "pdf",
            ContentType = "application/pdf",
            ContentSignature = new MagicNumbers
            {
                Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 },
                Offset = 0
            }
        };

        public override BaseDoc Create(string docTypeName) =>
            Create(docTypeName, TemplateController.Instance.TopDocRev(docTypeName));

        private static BaseDoc Create(string DocTypeName, string DocRev)
        {
            BaseDoc _BaseDoc = Runtime.ActivateBaseDoc(DocTypeName, DocRev, DocExchange.Instance);
            _BaseDoc.DocTypeName = DocTypeName;
            _BaseDoc.solutionVersion = DocRev;
            return _BaseDoc;
        }

        /// <summary>
        ///     enumerate each PdfAcroField, convert it's value to clr type, use reflection to set that value to the clr object
        ///     BaseDoc.
        /// </summary>
        /// <param name="docFiles"></param>
        /// <param name="docTypeName">default will be the original pdf's filename without the extension & only alpha numerics</param>
        /// <param name="docRev"></param>
        /// <param name="schemaXml"></param>
        /// <param name="schemaFields"></param>
        /// <returns></returns>
        public override DocRev CreateTemplate(List<DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null)
        {
            if (schemaFields == null)
                schemaFields = new List<CompositeProperty>();

            if (schemaFields.Count == 0)
                foreach (DocRevEntry docData in docFiles
                    .Where(docFile => docFile.Name.EndsWith(ContentInfo.ContentFileExtension, StringComparison.InvariantCultureIgnoreCase))
                    .OrderByDescending(docFile => docFile.ModDate))
                    if (schemaFields.Count == 0)
                        using (PdfDocument pdfDocument = OpenRead(docData.Bytes))
                        {
                            PdfAcroForm acroForm = pdfDocument.AcroForm;

                            for (int i = 0; i < acroForm.Fields.Elements.Count; i++)
                                schemaFields.Add(acroForm.Fields[i].AsCompositeProperty());

                            if (schemaFields.Count > 0)
                            {
                                DocProcessingInstructions pi = ReadDocPI(pdfDocument);

                                docTypeName = !string.IsNullOrWhiteSpace(docTypeName)
                                                  ? docTypeName
                                                  : !string.IsNullOrWhiteSpace(pi.DocTypeName)
                                                      ? pi.DocTypeName
                                                      : GetFilenameDocTypeName(docData);
                                break;
                            }
                        }

            AutoFileNameApply(docTypeName, docFiles, ContentInfo.ContentFileExtension);

            return schemaFields.Count == 0
                       ? null
                       : base.CreateTemplate(
                           docFiles,
                           docTypeName,
                           docRev,
                           schemaXml,
                           schemaFields);
        }

        public override string GetDescription(string docTypeName) { throw new NotImplementedException(); }

        private static string GetSetDocPIProperty(PdfDocument pdfDocument, string propertyName, object propertyValue = null)
        {
            string propertyValueAsString = string.Format("{0}", propertyValue);

            if (!string.IsNullOrWhiteSpace(propertyValueAsString))
                pdfDocument.Info.Elements.SetString("/" + propertyName, propertyValueAsString);

            //TODO:Research if beta version of PDFSharp is suppose to be wrapping it's property values with ()
            return pdfDocument.Info.Elements.ContainsKey("/" + propertyName)
                       ? string.Format("{0}", pdfDocument.Info.Elements["/" + propertyName]).Trim('(', ')')
                       : null;
        }

        public override string HrefVirtualFilename(string docTypeName, string docRev) { throw new NotImplementedException(); }

        /// <summary>
        /// </summary>
        /// <param name="docData"></param>
        /// <param name="openmode"></param>
        /// <returns>null if bytes are null or have no magic numbers </returns>
        private PdfDocument OpenRead(byte[] docData, PdfDocumentOpenMode openmode = PdfDocumentOpenMode.ReadOnly) =>
            ContentInfo.ContentSignature.IsMagic(docData)
                ? PdfReader.Open(new MemoryStream(docData), openmode)
                : null;

        public override bool Processable(string docTypeName, string docRev)
        {
            using (MemoryStream stream = TemplateController.Instance.OpenRead(docTypeName, AutoFileName(docTypeName, ContentInfo.ContentFileExtension)))
            {
                return ContentInfo.ContentSignature.IsMagic(stream);
            }
        }

        /// <summary>
        ///     enumerate each PdfAcroField, convert it's value to clr type, use reflection to set that value to the clr object
        ///     BaseDoc
        /// </summary>
        /// <param name="docData"></param>
        /// <param name="docRevStrict"></param>
        /// <returns></returns>
        public override BaseDoc Read(byte[] docData, bool docRevStrict = false)
        {
            BaseDoc basedoc = null;
            using (PdfDocument pdfDocument = OpenRead(docData))
            {
                if (pdfDocument != null)
                {
                    DocProcessingInstructions docProcessingInstructions = ReadDocPI(pdfDocument);

                    BaseDoc baseDoc = Create(
                        docProcessingInstructions.DocTypeName,
                        docRevStrict
                            ? docProcessingInstructions.solutionVersion
                            : TemplateController.Instance.TopDocRev(docProcessingInstructions.DocTypeName));

                    Type baseDocType = baseDoc.GetType();

                    for (int i = 0; i < pdfDocument.AcroForm.Fields.Elements.Count; i++)
                    {
                        PdfAcroField field = pdfDocument.AcroForm.Fields[i];
                        CompositeProperty compositeProperty = field.AsCompositeProperty();
                        string value = string.Format("{0}", field.Value);

                        PropertyInfo propertyInfo = baseDocType.GetProperty(compositeProperty.Name, compositeProperty.PropertyType);
                        if ((Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType).Equals(typeof(Boolean)))
                        {
                            //TODO:figure out a more generic way of dealing with values that must be parsed that ChangeType pukes on
                            bool b;
                            if (bool.TryParse(
                                    value.ToLower()
                                         .Replace("yes", bool.TrueString)
                                         .Replace("no", bool.FalseString)
                                         .Replace("1", bool.TrueString)
                                         .Replace("0", bool.FalseString)
                                    , out b))
                                propertyInfo.SetValue(baseDoc, b, null);
                        }
                        else
                            propertyInfo.SetValue(baseDoc, Convert.ChangeType(value, propertyInfo.PropertyType), null);

                    }

                    basedoc = SetPI(baseDoc, docProcessingInstructions);
                }
            }
            return basedoc;
        }

        public override DocProcessingInstructions ReadDocPI(byte[] docData)
        {
            using (PdfDocument pdfDocument = OpenRead(docData, PdfDocumentOpenMode.InformationOnly))
            {
                return ReadDocPI(pdfDocument);
            }
        }

        private static DocProcessingInstructions ReadDocPI(PdfDocument pdfDocument)
        {
            DocProcessingInstructions pi = new DocProcessingInstructions();
            if (pdfDocument != null)
            {
                int docChecksum = 0;
                bool docStatus = false;

                pi = new DocProcessingInstructions
                {
                    DocChecksum = int.TryParse(GetSetDocPIProperty(pdfDocument, nameof(pi.DocChecksum)), out docChecksum) ? docChecksum : 0,
                    DocSrc = GetSetDocPIProperty(pdfDocument, nameof(pi.DocSrc)),
                    DocStatus = bool.TryParse(GetSetDocPIProperty(pdfDocument, nameof(pi.DocStatus), pi.DocStatus), out docStatus) ? new bool?(docStatus) : null,
                    DocTitle = pdfDocument.Info.Title,
                    DocTypeName = GetSetDocPIProperty(pdfDocument, nameof(pi.DocTypeName)),
                    href = GetSetDocPIProperty(pdfDocument, nameof(pi.href)),
                    name = GetSetDocPIProperty(pdfDocument, nameof(pi.name)),
                    solutionVersion = GetSetDocPIProperty(pdfDocument, nameof(pi.solutionVersion))
                };

                // PDF never seen/served by this app will not have a DocId defined to decrypt
                string docId = GetSetDocPIProperty(pdfDocument, Parm.DocId);
                if (!string.IsNullOrWhiteSpace(docId))
                    pi.SetDocId(docId);
            }

            return pi;
        }

        public override string ReadDocRev(byte[] docData) => ReadDocPI(docData)
            .solutionVersion;

        public override string ReadDocTypeName(byte[] docData) => ReadDocPI(docData)
            .DocTypeName;

        public override List<ContentInfo> TemplateSources() => new List<ContentInfo> { ContentInfo };

        public override void Validate(byte[] docData)
        {
            //throw new NotImplementedException();
        }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream memoryStreamTemplate = TemplateController.Instance.OpenRead(source.DocTypeName, source.solutionVersion, AutoFileName(source.DocTypeName, ContentInfo.ContentFileExtension)))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStreamTemplate.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using (PdfDocument pdfDocument = PdfReader.Open(memoryStream, PdfDocumentOpenMode.Modify))
                {
                    Type baseDocType = source.GetType();

                    for (int i = 0; i < pdfDocument.AcroForm.Fields.Elements.Count; i++)
                    {
                        PdfAcroField field = pdfDocument.AcroForm.Fields[i];

                        field.Value = new PdfString(string.Format("{0}", baseDocType.GetProperty(field.nameofCSharpProperty())
                                                                                    .GetValue(source, null)));
                    }

                    if (includeProcessingInformation)
                        WritePI(source, pdfDocument);

                    pdfDocument.Save(memoryStream, false);

                    return memoryStream.ToArray();
                }
            }
        }

        public override byte[] WritePI(byte[] docData, DocProcessingInstructions pi)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(docData, 0, docData.Length);
                memoryStream.Position = 0;

                using (PdfDocument pdfDocument = PdfReader.Open(memoryStream, PdfDocumentOpenMode.Modify))
                {
                    WritePI(pi, pdfDocument);
                    pdfDocument.Save(memoryStream, false);
                    memoryStream.Position = 0;
                    return memoryStream.ToArray();
                }
            }
        }

        private static void WritePI(DocProcessingInstructions pi, PdfDocument pdfDocument)
        {
            pdfDocument.Info.ModificationDate = pdfDocument.Info.ModificationDate == DateTime.MinValue ? DateTime.Now : pdfDocument.Info.ModificationDate;
            pdfDocument.Info.Title = pi.DocTitle;

            GetSetDocPIProperty(pdfDocument, Parm.DocId, pi.GetDocId());
            GetSetDocPIProperty(pdfDocument, nameof(pi.DocStatus), pi.DocStatus);
            GetSetDocPIProperty(pdfDocument, nameof(pi.DocTypeName), pi.DocTypeName);
            GetSetDocPIProperty(pdfDocument, nameof(pi.solutionVersion), pi.solutionVersion);
            GetSetDocPIProperty(pdfDocument, nameof(pi.DocChecksum), pi.DocChecksum);

        }
    }
}