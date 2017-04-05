using Rudine.Web;

namespace Rudine
{
    public interface IDocRev : IBaseDoc
    {
        string TargetDocMD5 { get; set; }
        byte[] TargetDocTypeFiles { get; set; }
        string TargetDocTypeName { get; set; }
        string TargetDocTypeVer { get; set; }
    }
}