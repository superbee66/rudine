using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Rudine.Web
{
    /// <summary>
    ///     Implemented at every tier of the Rudine architecture.
    ///     [x] ClientBaseDocController -> WCF-Client-Proxy -> DocExchange
    ///     [x] DocExchange -> Memory -> Entity Framework Code First
    ///     [0] Entity Framework Code First-> Database
    /// </summary>
    [ServiceContract]
    public interface IBaseDocController : IRudineController
    {
        [OperationContract]
        BaseDoc Create(BaseDoc Doc, Dictionary<string, string> DocKeys, string RelayUrl = null);
        [OperationContract]
        DocTypeInfo Info(string DocTypeName);
        [OperationContract]
        List<ContentInfo> Interpreters();
        [OperationContract]
        BaseDoc ReadBytes(byte[] DocData, string RelayUrl = null);
        /// <summary>
        ///     not represented as an OperationContract as WCF requires there to be only one Stream parameter in the method. If the
        ///     Rudine.Web lib is reference on the client, it will take care of this limitation by immediately translating the
        ///     Stream parameter into a byte array or string.
        /// </summary>
        /// <param name="DocSrc"></param>
        /// <param name="DocData"></param>
        /// <param name="DocKeys"></param>
        /// <param name="RelayUrl"></param>
        /// <returns></returns>
        BaseDoc ReadStream(Stream DocData, string RelayUrl = null);
        [OperationContract]
        BaseDoc ReadText(string DocData, string RelayUrl = null);
        [OperationContract]
        LightDoc Status(string DocTypeName, Dictionary<string, string> DocKeys, bool DocStatus, string DocSubmittedByEmail, string RelayUrl = null);
        /// <summary>
        ///     not represented as an OperationContract as WCF requires there to be only one Stream parameter in the method. If the
        ///     Rudine.Web lib is reference on the client, it will take care of this limitation by immediately translating the
        ///     Stream parameter into a byte array or string.
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocSubmittedByEmail"></param>
        /// <param name="RelayUrl"></param>
        /// <param name="DocStatus"></param>
        /// <param name="SubmittedDate"></param>
        /// <param name="DocKeys"></param>
        /// <param name="DocTitle"></param>
        /// <returns></returns>
        LightDoc SubmitStream(Stream DocData, string DocSubmittedByEmail, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null);
    }
}