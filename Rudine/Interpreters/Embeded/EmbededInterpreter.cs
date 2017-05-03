using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Rudine.Util.Zips;
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
        internal const string MY_ONLY_DOC_NAME = "DocRev";
        internal static readonly Version MY_ONLY_DOC_VERSION = new Version(1, 0, 0, 0);

        public override BaseDoc Create(string DocTypeName)
        {
            if (!DocTypeName.Equals(MY_ONLY_DOC_NAME, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException(String.Empty, nameof(DocTypeName));

            return Create();
        }

        private static DocRev Create()
        {
            return new DocRev
            {
                DocTypeName = MY_ONLY_DOC_NAME,
                FileList = new List<DocRevEntry>(),
                solutionVersion = MY_ONLY_DOC_VERSION.ToString(),
                Target = new DocURN()
            };
        }

        public override string GetDescription(string DocTypeName) => null;

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) => null;

        public override bool Processable(string DocTypeName, string DocRev) =>
            DocTypeName.Equals(MY_ONLY_DOC_NAME)
            &&
            DocRev.Equals(MY_ONLY_DOC_VERSION.ToString());

        /// <summary>
        /// extracts contents of the zip file into a DocRev object
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
                    if (_ZipEntry.IsFile)
                    {
                        _DOCREV.FileList.Add(
                        new DocRevEntry
                        {
                            Bytes = _ZipFile.GetInputStream(_ZipEntry).AsBytes(),
                            Name = _ZipFile.Name,
                            ModDate = _ZipEntry.DateTime
                        });
                    }
            }

            _DOCREV.DocKeys = new Dictionary<string, string>
            {
                { "TargetDocTypeName", _DOCREV.Target.DocTypeName },
                { "TargetDocTypeVer", _DOCREV.Target.solutionVersion }
            };
            return _DOCREV;
        }

        public override DocProcessingInstructions ReadDocPI(byte[] DocData) => Read(DocData);

        public override string ReadDocTypeName(byte[] DocData) => ReadDocPI(DocData).DocTypeName;

        public override string ReadDocRev(byte[] DocData) => ReadDocPI(DocData).solutionVersion;

        public override void Validate(byte[] DocData) { }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (ZipOutputStream _ZipOutputStream = new ZipOutputStream(memoryStream) { IsStreamOwner = false })
            {
                IDocRev _DocRev = (IDocRev)source;
                _ZipOutputStream.SetLevel(9); //0-9, 9 being the highest level of compression

                // processing instructions
                _ZipOutputStream.PutNextEntry(
                    new ZipEntry(Parm.DocSrc)
                    {
                        IsUnicodeText = true,
                        Comment = BuildHref(_DocRev.Target.DocTypeName, _DocRev.Target.solutionVersion)
                    });

                foreach (DocRevEntry docRevEntry in _DocRev.FileList)
                    if (docRevEntry.Bytes.Length > 0)
                    {
                        // Zip the file in buffered chunks
                        // the "using" will close the stream even if an exception occurs
                        byte[] buffer = new byte[4096];

                        using (MemoryStream streamReader = new MemoryStream(docRevEntry.Bytes))
                        {
                            _ZipOutputStream.PutNextEntry(
                                new ZipEntry(docRevEntry.Name)
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
                return memoryStream.ToBytes();
            }
        }

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi) => WriteByte(SetPI(Read(DocData), pi));

        public override ContentInfo ContentInfo =>
            new ContentInfo
            {
                ContentFileExtension = MY_ONLY_DOC_NAME,
                ContentType = "application/octet-stream"
            };
    }
}