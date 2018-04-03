using Rudine.Web;

namespace Rudine.Storage
{
    /// <summary>
    ///     All methods define in IRudine in addition to various Get methods suited to retrieval operations from persisted
    ///     mediums
    /// </summary>
    public interface IStorageController : IRudineController
    {
        object GetDocData(string DocTypeName, string DocId = null, string RelayUrl = null, long LogSequenceNumber = 0);
        string GetDocDataText(string DocTypeName, string DocId = null, string RelayUrl = null, long LogSequenceNumber = 0);
        byte[] GetDocDataBytes(string DocTypeName, string DocId = null, string RelayUrl = null, long LogSequenceNumber = 0);
    }
}