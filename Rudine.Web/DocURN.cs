using System;
using System.Runtime.Serialization;

namespace Rudine.Web
{
    [DataContract(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocURN
    {
        [DataMember(EmitDefaultValue = false)]
        public string DocTypeVer { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DocTypeName { get; set; }
    }
}