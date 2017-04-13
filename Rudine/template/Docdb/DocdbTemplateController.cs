using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rudine.Template.Embeded;
using Rudine.Util.Cabs;
using Rudine.Web.Util;

namespace Rudine.Template.Docdb
{
    internal class DocdbTemplateController : ITemplateController
    {
        public MemoryStream OpenRead(string DocTypeName, string DocTypeVer, string filename)
        {
            int reaponseFileLength;
            byte[] reaponseFile = null;
            byte[] decodedAttachment = GetCabDecodedAttachment(DocTypeName, DocTypeVer);

            if (decodedAttachment != null)
                if (TemplateController.FOLDER_CONTENTS_VIRTUAL_CAB_FILE == filename)
                    reaponseFile = decodedAttachment;
                else
                    new CabExtract(decodedAttachment).ExtractFile(
                        filename,
                        out reaponseFile,
                        out reaponseFileLength);

            return reaponseFile != null
                       ? new MemoryStream(reaponseFile)
                       : null;
        }

        public string TopDocRev(string DocTypeName) =>
            DocExchange.LuceneController.List(
                           new List<string> { EmbededTemplateController.MY_ONLY_DOC_NAME },
                           new Dictionary<string, List<string>>
                           {
                               {
                                   "TargetDocTypeName", new List<string> { DocTypeName }
                               }
                           })
                       .Select(_LightDoc => _LightDoc.GetTargetDocVer())
                       .OrderByDescending(s => new Version(s))
                       .FirstOrDefault();

        private static byte[] GetCabDecodedAttachment(string DocTypeName, string DocTypeVer) =>
            CacheMan.Cache(() =>
                           {
                               Dictionary<string, string> DocKeys;

                               const int SP1Header_Size = 20;
                               const int FIXED_HEADER = 16;

                               string attachmentName;
                               byte[] reaponseFile = null;

                               string DocSrc;

                               object o = DocExchange.LuceneController.Get(
                                   EmbededTemplateController.MY_ONLY_DOC_NAME,
                                   new Dictionary<string, string> { { "TargetDocTypeVer", DocTypeVer }, { "TargetDocTypeName", DocTypeName } });

                               if (o == null)
                                   o = DocExchange.LuceneController.Get(
                                       EmbededTemplateController.MY_ONLY_DOC_NAME,
                                       new Dictionary<string, string> { { "DocTypeVer", DocTypeVer }, { "DocTypeName", DocTypeName } });

                               byte[] decodedAttachment = null;
                               if (o != null)
                               {
                                   IDocRev _IDocRev = (IDocRev) o;

                                   using (MemoryStream ms = new MemoryStream(_IDocRev.TargetDocTypeFiles))
                                   using (BinaryReader theReader = new BinaryReader(ms))
                                   {
                                       //Position the reader to get the file size.
                                       byte[] headerData = new byte[FIXED_HEADER];
                                       headerData = theReader.ReadBytes(headerData.Length);

                                       int fileSize = (int) theReader.ReadUInt32();
                                       int attachmentNameLength = (int) theReader.ReadUInt32() * 2;

                                       byte[] fileNameBytes = theReader.ReadBytes(attachmentNameLength);
                                       //InfoPath uses UTF8 encoding.
                                       Encoding enc = Encoding.Unicode;

                                       //attachmentName = enc.GetString(fileNameBytes, 0, attachmentNameLength - 2);
                                       decodedAttachment = theReader.ReadBytes(fileSize);
                                   }
                               }
                               return decodedAttachment;
                           }, false, "GetCabDecodedAttachment", DocTypeName, DocTypeVer);
    }
}