using System;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace Rudine.Web
{
    /// <summary>
    ///     The base of all entities utilizing a single auto increment integer id.
    /// </summary>
    [DataContract(Namespace = DocURN.RudineXmlNamespace)]
    [Serializable]
    public abstract class BaseAutoIdent
    {
        [IgnoreDataMember]
        [XmlIgnore]
        [ScriptIgnore]
        public virtual int Id { get; set; }
    }
}