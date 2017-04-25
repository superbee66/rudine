using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Rudine.Util.Zips;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Embeded
{
    public class EmbededInterpreter : DocByteInterpreter
    {
        internal const string MY_ONLY_DOC_NAME = "DOCREV";
        internal static readonly Version MY_ONLY_DOC_VERSION = new Version(1, 0, 0, 0);

        public override BaseDoc Create(string DocTypeName)
        {
            if (!DocTypeName.Equals(MY_ONLY_DOC_NAME, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException(String.Empty, nameof(DocTypeName));

            return new DOCREV()
            {
                DocTypeName = MY_ONLY_DOC_NAME,
                FileList = new List<DocRevEntry>(),
                solutionVersion = MY_ONLY_DOC_VERSION.ToString(),
                Target = new DocURN()
                {
                    DocTypeName = DocTypeName
                },

            };
        }

        public override string GetDescription(string DocTypeName) => null;

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) => null;

        public override bool Processable(string DocTypeName, string DocRev) =>
            DocTypeName.Equals(MY_ONLY_DOC_NAME)
            &&
            DocRev.Equals(MY_ONLY_DOC_VERSION.ToString());

        public override BaseDoc Read(byte[] DocData, bool DocRevStrict = false)
        {
            DOCREV _DOCREV = new DOCREV
            {
                FileList = new List<DocRevEntry>(),
                Target = new DocURN(),
                DocTypeName = MY_ONLY_DOC_NAME,
                solutionVersion = MY_ONLY_DOC_VERSION.ToString()
            };

            using (MemoryStream _MemoryStream = new MemoryStream(DocData))
            using (ZipInputStream _ZipInputStream = new ZipInputStream(_MemoryStream) { IsStreamOwner = false })
            {
                ZipEntry _NextEntry;

                while ((_NextEntry = _ZipInputStream.GetNextEntry()) != null)
                {
                    _DOCREV.FileList.Add(
                        new DocRevEntry
                        {
                            Bytes = _ZipInputStream.AsBytes(),
                            Name = string.Join("\\", _NextEntry.Name.Split('\\').Skip(2).ToArray())
                        });
                    _DOCREV.Target.DocTypeName = _NextEntry.Name.Split('\\')[0];
                    _DOCREV.Target.solutionVersion = _NextEntry.Name.Split('\\')[1];
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
            using (MemoryStream fsOut = new MemoryStream())
            using (ZipOutputStream _ZipOutputStream = new ZipOutputStream(fsOut) { IsStreamOwner = false })
            {
                IDocRev _DocRev = (IDocRev)source;
                _ZipOutputStream.SetLevel(9); //0-9, 9 being the highest level of compression

                foreach (DocRevEntry file in _DocRev.FileList)
                    if (file.Bytes.Length > 0)
                    {
                        // Zip the file in buffered chunks
                        // the "using" will close the stream even if an exception occurs
                        byte[] buffer = new byte[4096];

                        using (MemoryStream streamReader = new MemoryStream(file.Bytes))
                        {
                            _ZipOutputStream.PutNextEntry(
                                new ZipEntry(string.Format("{0}\\{1}\\{2}", _DocRev.Target.DocTypeName, _DocRev.Target.solutionVersion, file.Name))
                                {
                                    // Note the zip format stores 2 second granularity
                                    DateTime = DateTime.Now,
                                    // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                                    // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                                    // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                                    // but the zip will be in Zip64 format which not all utilities can understand.
                                    //   zipStream.UseZip64 = UseZip64.Off;
                                    Size = file.Bytes.Length
                                });
                            StreamUtils.Copy(streamReader, _ZipOutputStream, buffer);
                            _ZipOutputStream.CloseEntry();
                        }
                        _ZipOutputStream.Close();
                    }

                return fsOut.ToBytes();
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