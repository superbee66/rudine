﻿using System;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace Rudine.Web
{
    [XmlType(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class DocRevEntry : BaseAutoIdent
    {
        [XmlElement(DataType = "base64Binary")]
        public byte[] Bytes { get; set; }
        /// <summary>
        /// path of file compatible with ZipEntry
        /// </summary>
        public string Name { get; set; }

        [XmlIgnore]
        [ScriptIgnore]
        public DateTime ModDate { get; set; }
    }
}