using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;
using Rudine.Template;
using Rudine.Util.Types;
using Rudine.Web;

namespace Rudine.Interpreters.Pdf
{
    /// <summary>
    ///     pdfsharp does not dispose of itself correctly; we choose to dispose it's memorystream each time instead of leaving
    ///     it to the pdfdocument object itself
    /// </summary>
    public class PdfInterpreter : DocByteInterpreter
    {
        private const string TEMPLATE_PDF = "template.pdf";

        public override ContentInfo ContentInfo =>
            new ContentInfo
            {
                ContentFileExtension = "pdf",
                ContentType = "application/pdf"
            };

        public override BaseDoc Create(string DocTypeName) { throw new NotImplementedException(); }

        public override string GetDescription(string DocTypeName) { throw new NotImplementedException(); }

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) { throw new NotImplementedException(); }
        public override bool Processable(string DocTypeName, string DocRev) { throw new NotImplementedException(); }

        /// <summary>
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="FileData"></param>
        /// <param name="DocRev"></param>
        /// <returns></returns>
        public BaseDoc Publish(string DocTypeName, byte[] FileData)
        {
            using (MemoryStream _MemoryStream = new MemoryStream(FileData))
            using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.ReadOnly))
            {
                PdfAcroForm AcroForm = _PdfDocument.AcroForm;
                List<CompositeProperty> _CompositePropertyList = new List<CompositeProperty>();
                for (int i = 0; i < AcroForm.Fields.Elements.Count; i++)
                    _CompositePropertyList.Add(AcroForm.Fields[i].AsCompositeProperty());

                return DocTempleter.Templify(DocTypeName, _CompositePropertyList);
            }
        }

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
                        : TemplateController.Instance.TopDocRev(_DocProcessingInstructions.DocTypeName));

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
            DocProcessingInstructions _DocProcessingInstructions = new DocProcessingInstructions
            {
                DocTypeName = _PdfDocument.Internals.FirstDocumentID,
                DocStatus = _PdfDocument.Info.Elements.ContainsKey("/" + Parm.DocStatus) && _PdfDocument.Info.Elements.GetBoolean("/" + Parm.DocStatus),
                DocTitle = _PdfDocument.Info.Title,
                solutionVersion = _PdfDocument.Info.Elements["/" + Parm.DocRev].ToString()
            };

            _DocProcessingInstructions.SetDocId(_PdfDocument.Internals.SecondDocumentID);

            return _DocProcessingInstructions;
        }

        public override string ReadDocRev(byte[] DocData) => ReadDocPI(DocData).solutionVersion;

        public override string ReadDocTypeName(byte[] DocData) => ReadDocPI(DocData).DocTypeName;

        public override void Validate(byte[] DocData) { throw new NotImplementedException(); }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream _MemoryStream = TemplateController.Instance.OpenRead(
                source.DocTypeName,
                source.solutionVersion,
                TEMPLATE_PDF))

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

                _PdfDocument.Save(_MemoryStream);

                return _MemoryStream.ToArray();
            }
        }

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi)
        {
            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            using (PdfDocument _PdfDocument = PdfReader.Open(_MemoryStream, PdfDocumentOpenMode.Modify))
            {
                WritePI(pi, _PdfDocument);
                _PdfDocument.Save(_MemoryStream);
                return _MemoryStream.ToArray();
            }
        }

        private static void WritePI(DocProcessingInstructions pi, PdfDocument _PdfDocument)
        {
            //  _PdfDocument.Info.Creator = string.Join(", ", new string[] { _PdfDocument.Info.Creator, typeof(DocByteInterpreter).Assembly.GetName().Name }.Distinct());
            _PdfDocument.Info.ModificationDate = _PdfDocument.Info.ModificationDate == DateTime.MinValue ? DateTime.Now : _PdfDocument.Info.ModificationDate;
            _PdfDocument.Internals.FirstDocumentID = pi.DocTypeName;
            _PdfDocument.Internals.SecondDocumentID = pi.GetDocId();
            _PdfDocument.Info.Title = pi.DocTitle;
            if (pi.DocStatus != null)
                _PdfDocument.Info.Elements.SetBoolean("/" + Parm.DocStatus, pi.DocStatus ?? false);
            _PdfDocument.Info.Elements.SetString("/" + Parm.DocRev, pi.solutionVersion);
        }
    }
}