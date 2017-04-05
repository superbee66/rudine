using System;
using System.Runtime.Serialization;

namespace Rudine.Web
{
    [DataContract(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocTypeInfo : DocURN
    {
        /// <summary>
        ///     a per-DocTypeName basis description that is supplied by the specific DocDataInterpreter
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        ///     If there are signature lines in the form
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsSignable { get; set; }
    }
}