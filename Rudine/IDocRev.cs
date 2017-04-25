using System.Collections.Generic;
using Rudine.Interpreters.Embeded;
using Rudine.Web;
using DocURN = Rudine.Interpreters.Embeded.DocURN;

namespace Rudine
{
    public interface IDocRev : IBaseDoc
    {
        string MD5 { get; set; }
        DocURN Target { get; set; }
        List<DocRevEntry> FileList { get; set; }
    }
}