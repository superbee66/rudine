﻿using System;
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
    [DataContract(Namespace = DocURN.RudineXmlNamespace)]
    [Serializable]
    public class DocTerm : BaseAutoIdent
    {
        private string _docTypeName;

        [XmlIgnore]
        [DataMember]
        public virtual Dictionary<string, string> DocKeys { get; set; }

        /// <summary>
        ///     The reflected GetType().Name of this object
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public virtual string DocTypeName
        {
            get { return _docTypeName ?? (_docTypeName = GetType().Name); }
            set { _docTypeName = value; }
        }

        public string AsTermTxt() => Serialize.Json.Serialize(new
        {
            //TODO:sort dictionary before serializing
            DocTypeName,
            DocKeys
        });
    }
}