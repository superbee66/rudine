﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Embeded
{
    /// <summary>
    ///     a glorified set of zipping routines that convert to and from a BaseDoc holding bytes that represent supporting
    ///     content for that BaseDoc/DocRev definition
    /// </summary>
    public class EmbededInterpreter : DocByteInterpreter
    {
        private static readonly JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer();

        /// <summary>
        ///     utilizes actual MY_ONLY_DOC_NAME as the file extension
        /// </summary>
        public override ContentInfo ContentInfo =>
            new ContentInfo { ContentFileExtension = DocRev.MY_ONLY_DOC_NAME, ContentType = "application/octet-stream" };

        /// <summary>
        /// </summary>
        /// <param name= DocRev.MY_ONLY_DOCKEY_1>accepts only DcoRev (MY_ONLY_DOC_NAME)</param>
        /// <returns></returns>
        public override BaseDoc Create(string DocTypeName)
        {
            if (!DocTypeName.Equals(DocRev.MY_ONLY_DOC_NAME, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException(string.Empty, nameof(DocTypeName));

            return Create();
        }

        private static DocRev Create()
        {
            return new DocRev
            {
                DocFiles = new List<DocRevEntry>(),
                DocURN = new DocURN(),
                DocTypeName = DocRev.MY_ONLY_DOC_NAME,
                solutionVersion = DocRev.MY_ONLY_DOC_VERSION
            };
        }

        public override string GetDescription(string DocTypeName) => null;

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) => null;

        public override bool Processable(string DocTypeName, string docRev) =>
            DocTypeName.Equals(DocRev.MY_ONLY_DOC_NAME)
            &&
            docRev.Equals(DocRev.MY_ONLY_DOC_VERSION);

        /// <summary>
        ///     extracts contents of the zip file into a DocRev object
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        public override BaseDoc Read(byte[] DocData, bool DocRevStrict = false)
        {
            DocRev _DOCREV = Create();

            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            using (ZipFile _ZipFile = new ZipFile(_MemoryStream))
            {
                foreach (ZipEntry _ZipEntry in _ZipFile)
                    if (_ZipEntry.Name.Equals(DocRev.ManifestFileName, StringComparison.InvariantCultureIgnoreCase))
                        _DOCREV.DocURN = _JavaScriptSerializer.Deserialize<DocTypeInfo>(Encoding.Default.GetString(_ZipFile.GetInputStream(_ZipEntry).AsBytes()));
                    else if (_ZipEntry.Name.Equals(DocRev.SchemaFileName, StringComparison.InvariantCultureIgnoreCase))
                        _DOCREV.DocSchema = Encoding.Default.GetString(_ZipFile.GetInputStream(_ZipEntry).AsBytes());
                    else if (_ZipEntry.IsFile)
                        _DOCREV.DocFiles.Add(
                            new DocRevEntry
                            {
                                Bytes = _ZipFile.GetInputStream(_ZipEntry).AsBytes(),
                                Name = _ZipEntry.Name,
                                ModDate = _ZipEntry.DateTime
                            });
            }

            return _DOCREV;
        }

        public override DocProcessingInstructions ReadDocPI(byte[] DocData) => Read(DocData);

        public override string ReadDocRev(byte[] DocData) => ReadDocPI(DocData).solutionVersion;

        public override string ReadDocTypeName(byte[] DocData) => ReadDocPI(DocData).DocTypeName;

        public override List<ContentInfo> TemplateSources() => new List<ContentInfo> { ContentInfo };

        public override void Validate(byte[] DocData) { }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            //using (FileStream memoryStream = File.Create("test.zip"))
            //using (ZipOutputStream _ZipOutputStream = new ZipOutputStream(memoryStream) { IsStreamOwner = false })
            using (MemoryStream memoryStream = new MemoryStream())
            using (ZipOutputStream _ZipOutputStream = new ZipOutputStream(memoryStream) { IsStreamOwner = false })
            {
                IDocRev _DocRev = (IDocRev) source;
                _ZipOutputStream.SetLevel(9); //0-9, 9 being the highest level of compression

                List<DocRevEntry> _DocInfoDocRevEntry = new List<DocRevEntry>
                {
                    new DocRevEntry
                    {
                        Bytes = Encoding.Default.GetBytes(_JavaScriptSerializer.Serialize(_DocRev.DocURN)),
                        ModDate = _DocRev.DocFiles.Max(DocFile => DocFile.ModDate),
                        Name = DocRev.ManifestFileName
                    },
                    new DocRevEntry
                    {
                        Bytes = Encoding.Default.GetBytes(_DocRev.DocSchema),
                        ModDate = _DocRev.DocFiles.Max(DocFile => DocFile.ModDate),
                        Name = DocRev.ManifestFileName
                    }
                };

                foreach (DocRevEntry docRevEntry in _DocRev.DocFiles.Union(_DocInfoDocRevEntry))
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
                                    DateTime = docRevEntry.ModDate,
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