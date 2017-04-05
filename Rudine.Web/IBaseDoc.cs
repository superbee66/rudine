using System.Collections.Generic;

namespace Rudine.Web
{
    public interface IBaseDoc : IDocURN
    {
        int DocChecksum { get; set; }
        Dictionary<string, string> DocKeys { get; set; }
        string DocSrc { get; set; }
        bool? DocStatus { get; set; }
        string DocTitle { get; set; }
    }
}