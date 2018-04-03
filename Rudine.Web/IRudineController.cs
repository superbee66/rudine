using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Rudine.Web
{
    /// <summary>
    /// The base many other non-poco classes
    /// </summary>
    [ServiceContract]
    public interface IRudineController
    {
        [OperationContract]
        List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null);

        [OperationContract]
        List<LightDoc> List(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null);

        /// <summary>
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="DocKeys">have precedence over DocId when is not null</param>
        /// <param name="DocId"></param>
        /// <param name="RelayUrl"></param>
        /// <returns></returns>
        [OperationContract]
        BaseDoc Get(string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null);

        [OperationContract]
        LightDoc SubmitBytes(byte[] DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);

        [OperationContract]
        LightDoc SubmitText(string DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
    }
}