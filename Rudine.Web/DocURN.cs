using System;
using System.Runtime.Serialization;

namespace Rudine.Web
{
    [DataContract(Namespace = Rudine.Web.DocURN.RudineXmlNamespace)]
    [Serializable]
    public class DocURN
    {
        /// <summary>
        /// Root namespace for all XML documents originating from the Rudine assembly
        /// </summary>
        public const string RudineXmlNamespace = "urn:rudine.progablab.com";

        [DataMember(EmitDefaultValue = false)]
        public string DocTypeVer { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DocTypeName { get; set; }
    }
}