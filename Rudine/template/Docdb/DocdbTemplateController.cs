using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rudine.Web;

namespace Rudine.Template.Docdb
{
    internal class DocdbTemplateController : ITemplateController
    {
        public MemoryStream OpenRead(string DocTypeName, string DocTypeVer, string filename)
        {
            IDocRev o = (IDocRev) DocExchange.LuceneController.Get(
                DocRev.MY_ONLY_DOC_NAME,
                new Dictionary<string, string> { { DocRev.KeyPart2, DocTypeVer }, { DocRev.KeyPart1, DocTypeName } });

            if (o == null)
                o = (IDocRev) DocExchange.LuceneController.Get(
                    DocRev.MY_ONLY_DOC_NAME,
                    new Dictionary<string, string> { { DocRev.KeyPart2, DocTypeVer }, { DocRev.KeyPart1, DocTypeName } });

            byte[] bytes = o.DocFiles == null
                ? null
                : o.DocFiles.FirstOrDefault(f => f.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))?.Bytes;

            return bytes == null
                       ? null
                       : new MemoryStream(bytes);
        }

        public string TopDocRev(string DocTypeName)
        {
            return DocExchange.LuceneController.List(
                                  new List<string> { DocRev.MY_ONLY_DOC_NAME },
                                  new Dictionary<string, List<string>>
                                  {
                                      {
                                          DocRev.KeyPart1, new List<string> { DocTypeName }
                                      }
                                  })
                              .Select(_LightDoc => _LightDoc.GetTargetDocVer())
                              .OrderByDescending(s => new Version(s))
                              .FirstOrDefault();
        }
    }
}