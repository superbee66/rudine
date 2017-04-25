using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rudine.Interpreters.Embeded;
using Rudine.Web.Util;

namespace Rudine.Template.Docdb
{
    internal class DocdbTemplateController : ITemplateController
    {
        public MemoryStream OpenRead(string DocTypeName, string DocTypeVer, string filename)
        {
            IDocRev o = (IDocRev)DocExchange.LuceneController.Get(
                EmbededInterpreter.MY_ONLY_DOC_NAME,
                new Dictionary<string, string> { { "TargetDocTypeVer", DocTypeVer }, { "TargetDocTypeName", DocTypeName } });

            if (o == null)
                o = (IDocRev)DocExchange.LuceneController.Get(
                    EmbededInterpreter.MY_ONLY_DOC_NAME,
                    new Dictionary<string, string> { { "DocTypeVer", DocTypeVer }, { "DocTypeName", DocTypeName } });

            byte[] bytes = o.FileList?.FirstOrDefault(f => f.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))?.Bytes;

            return bytes == null
                       ? null
                       : new MemoryStream(bytes);
        }

        public string TopDocRev(string DocTypeName)
        {
            return DocExchange.LuceneController.List(
                                  new List<string> { EmbededInterpreter.MY_ONLY_DOC_NAME },
                                  new Dictionary<string, List<string>>
                                  {
                                      {
                                          "TargetDocTypeName", new List<string> { DocTypeName }
                                      }
                                  })
                              .Select(_LightDoc => _LightDoc.GetTargetDocVer())
                              .OrderByDescending(s => new Version(s))
                              .FirstOrDefault();
        }
    }
}