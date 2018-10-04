using System.Collections.Generic;
using System.IO;

namespace Rudine.Template
{
    public interface ITemplateController
    {
        MemoryStream OpenRead(string DocTypeName, string DocRev, string filename);

        string TopDocRev(string DocTypeName);

        Dictionary<string, System.Version> TopDocRevs();
    }
}