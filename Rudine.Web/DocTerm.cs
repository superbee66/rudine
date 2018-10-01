using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     Writes a Lucene document store database string field value that acts as a unique among
    ///     all other documents (of any DocType) persisted to the primary repository of this solution
    /// </summary>
    [DataContract(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocTerm : BaseAutoIdent
    {
        private string _docTypeName;

        [XmlIgnore]
        [DataMember]
        public virtual Dictionary<string, string> DocIdKeys { get; set; }

        /// <summary>
        ///     The reflected GetType().Name of this object
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public virtual string DocTypeName
        {
            //TODO:This should be calculated
            get { return _docTypeName; }
            set { _docTypeName = value; }
        }

        public string AsTermTxt()
        {
            return Serialize.Json.Serialize(new
            {
                //TODO:sort dictionary before serializing
                DocTypeName,
                DocIdKeys
            });
        }
    }
}