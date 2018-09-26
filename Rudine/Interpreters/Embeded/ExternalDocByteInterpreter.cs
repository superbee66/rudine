using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Rudine.Template;
using Rudine.Util;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Embeded
{
    /// <summary>
    ///     a glorified set of zipping routine that convert to and from a BaseDoc holding bytes that represent supporting
    ///     content for that BaseDoc/ExternalDoc definition
    /// </summary>
    public class ExternalDocInterpreter : DocByteInterpreter
    {
        private static readonly Lazy<ExternalDocInterpreter> _Instance = new Lazy<ExternalDocInterpreter>(() => new ExternalDocInterpreter());
        private static readonly JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer();


        /// <summary>
        ///     utilizes actual MY_ONLY_DOC_NAME as the file extension, content is zip compression 9
        /// </summary>
        public override ContentInfo ContentInfo => new ContentInfo
        {
            ContentFileExtension = "ext",
            ContentType = "application/octet-stream",
            ContentSignature = new MagicNumbers
            {
                Bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                Offset = 0
            }
        };

        public static ExternalDocInterpreter Instance =>
            _Instance.Value;

        public override BaseDoc Create(string docTypeName) =>
            Create(docTypeName, TemplateController.Instance.TopDocRev(docTypeName));

        private static BaseDoc Create(string DocTypeName, string DocRev)
        {
            Type _BaseDocType = Reflection
                .LoadBinDlls()
                .SelectMany(a => a.GetExportedTypes())
                .Distinct()
                .FirstOrDefault(typ =>
                    !(typ == typeof(DocRev))
                    &&
                    typ.IsSubclassOf(typeof(ExternalDoc))
                    &&
                    typ.Name == DocTypeName);

            BaseDoc _BaseDoc = null;
            if (_BaseDocType != null)
            {
                _BaseDoc = (BaseDoc)Activator.CreateInstance(_BaseDocType);
                //TODO:Complete DocRev creations from ExternalDoc types in memory so there schemas can be persisted
                //BaseDoc _BaseDoc = Runtime.ActivateBaseDoc(DocTypeName, DocRev, DocExchange.Instance);
                _BaseDoc.DocTypeName = DocTypeName;
                _BaseDoc.solutionVersion = ExternalDoc.MyOnlyDocRev;
            }

            return _BaseDoc;
        }

        public override string GetDescription(string DocTypeName) =>
            null;

        public override string HrefVirtualFilename(string DocTypeName, string ExternalDoc) =>
            null;

        public override bool Processable(string docTypeName, string docRev)
        {
            object o = Create(docTypeName, docRev);
            return !(o is DocRev) && o is ExternalDoc;
        }

        /// <summary>
        ///     extracts contents of the zip file into a ExternalDoc object
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="ExternalDocStrict"></param>
        /// <returns></returns>
        public override BaseDoc Read(byte[] DocData, bool ExternalDocStrict = false)
        {
            DocProcessingInstructions _DocProcessingInstructions = new DocProcessingInstructions();

            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            {
                using (ZipFile _ZipFile = new ZipFile(_MemoryStream))
                {
                    foreach (ZipEntry _ZipEntry in _ZipFile)
                        if (_ZipEntry.Name.Equals(ExternalDoc.PIFileName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _DocProcessingInstructions = _JavaScriptSerializer.Deserialize<DocProcessingInstructions>(Encoding.Default.GetString(_ZipFile.GetInputStream(_ZipEntry).AsBytes()));
                            _DocProcessingInstructions.solutionVersion = ExternalDoc.MyOnlyDocRev;
                        }

                    BaseDoc _IExternalDoc = Create(
                        _DocProcessingInstructions.DocTypeName,
                        _DocProcessingInstructions.solutionVersion);

                    foreach (ZipEntry _ZipEntry in _ZipFile)
                        if (_ZipEntry.Name.Equals(ExternalDoc.PropertiesFileName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _IExternalDoc = (BaseDoc)_JavaScriptSerializer.Deserialize(Encoding.Default.GetString(_ZipFile.GetInputStream(_ZipEntry).AsBytes()), _IExternalDoc.GetType());
                            _IExternalDoc.DocTypeName = _DocProcessingInstructions.DocTypeName;
                            _IExternalDoc.solutionVersion = _DocProcessingInstructions.solutionVersion;
                        }

                    return _IExternalDoc;
                }
            }
        }

        public override DocProcessingInstructions ReadDocPI(byte[] DocData) => Read(DocData);

        public override string ReadDocRev(byte[] DocData) => ReadDocPI(DocData)?.solutionVersion;

        public override string ReadDocTypeName(byte[] DocData) => ReadDocPI(DocData)?.DocTypeName;

        public override List<ContentInfo> TemplateSources() => new List<ContentInfo> { ContentInfo };

        public override void Validate(byte[] DocData)
        {
        }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (ZipOutputStream _ZipOutputStream = new ZipOutputStream(memoryStream) { IsStreamOwner = false })
            {
                IExternalDoc _ExternalDoc = (IExternalDoc)source;
                _ZipOutputStream.SetLevel(9); //0-9, 9 being the highest level of compression
                DateTime DefaultModDate = DateTime.Now;

                List<DocRevEntry> _DocInfoDocRevEntry = new List<DocRevEntry>();

                if (includeProcessingInformation)
                    _DocInfoDocRevEntry.Add(
                        new DocRevEntry
                        {
                            //TODO:get T source to downcast properly
                            Bytes = Encoding.Default.GetBytes(
                                _JavaScriptSerializer.Serialize(
                                    new DocProcessingInstructions
                                    {
                                        DocChecksum = source.DocChecksum,
                                        DocKeys = source.DocKeys,
                                        DocSrc = source.DocSrc,
                                        DocStatus = source.DocStatus,
                                        DocTitle = source.DocTitle,
                                        DocTypeName = source.DocTypeName,
                                        href = source.href,
                                        name = source.name,
                                        solutionVersion = source.solutionVersion
                                    })),
                            ModDate = DefaultModDate,
                            Name = ExternalDoc.PIFileName
                        });

                foreach (DocRevEntry docRevEntry in
                    new[]
                    {
                        new DocRevEntry
                        {
                            Bytes = Encoding.Default.GetBytes(_JavaScriptSerializer.Serialize(source)),
                            ModDate = DefaultModDate,
                            Name = ExternalDoc.PropertiesFileName
                        },
                        new DocRevEntry
                        {
                            Bytes = _ExternalDoc.RawBytes,
                            ModDate = DefaultModDate,
                            Name = ExternalDoc.SubmissionFileName
                        }
                    }.Union(_DocInfoDocRevEntry))
                    if (docRevEntry.Bytes.Length > 0)
                    {
                        // Zip the file in buffered chunks
                        // the "using" will close the stream even if an exception occurs
                        byte[] buffer = new byte[4096];

                        using (MemoryStream streamReader = new MemoryStream(docRevEntry.Bytes))
                        {
                            _ZipOutputStream.PutNextEntry(
                                new ZipEntry(docRevEntry.Name.TrimStart('/', '\\'))
                                {
                                    // Note the zip format stores 2 second granularity
                                    DateTime = docRevEntry.ModDate.ToLocalTime(),
                                    // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                                    // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                                    // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                                    // but the zip will be in Zip64 format which not all utilities can understand.
                                    //   zipStream.UseZip64 = UseZip64.Off;
                                    Size = docRevEntry.Bytes.Length
                                });
                            StreamUtils.Copy(streamReader, _ZipOutputStream, buffer);
                            _ZipOutputStream.CloseEntry();
                        }
                    }

                _ZipOutputStream.Close();

                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi) => WriteByte(SetPI(Read(DocData), pi));
    }
}