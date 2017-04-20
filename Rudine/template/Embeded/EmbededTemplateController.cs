using System;
using System.IO;
using Rudine.Util;

namespace Rudine.Template.Embeded
{
    internal class EmbededTemplateController : ITemplateController
    {
        internal const string MY_ONLY_DOC_NAME = "DOCREV";
        internal static readonly Version MY_ONLY_DOC_VERSION = new Version(1, 0, 0, 0);

        public MemoryStream OpenRead(string DocTypeName, string DocRev, string filename)
        {
            return
                DocTypeName.Equals(MY_ONLY_DOC_NAME)
                &&
                filename.Equals(Runtime.MYSCHEMA_XSD_FILE_NAME)
                &&
                DocRev.Equals(MY_ONLY_DOC_VERSION.ToString())
                    ? DOCREV_SCHEMAS.DOCREV.AsMemoryStream()
                    : null;
        }

        public string TopDocRev(string DocTypeName)
        {
            return DocTypeName == MY_ONLY_DOC_NAME
                       ? "1.0.0.0" //TODO:Read this from URI type is created with
                       : null;
        }
    }
}