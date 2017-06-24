﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     Summary description for baseInfoPathXmlRequestBody. It is serializable for JavaScriptSerialization.
    ///     The chain of inherited classes (DocProcessingInstructions, DocTerm, BaseAutoIdent) each support
    ///     serialization to individual (different) mediums.
    /// </summary>
    // [ServiceKnownType("DocTypes", typeof(DocExchange))]
    [DataContract(Namespace = "urn:rudine.progablab.com")]
    [Serializable]
    public class BaseDoc : DocProcessingInstructions, IBaseDoc
    {

        /// <summary>
        ///     User may want to title there own document?
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public override string DocTitle
        {
            get { return base.DocTitle ?? DocTypeName; }
            set { base.DocTitle = value; }
        }

        [XmlIgnore]
        [DataMember]
        public sealed override bool? DocStatus
        {
            get { return base.DocStatus; }
            set { base.DocStatus = value; }
        }

        [XmlIgnore]
        [DataMember]
        public sealed override int DocChecksum
        {
            get { return base.DocChecksum; }
            set { base.DocChecksum = value; }
        }


        /// <summary>
        ///     gathers up types referenced by the o via properties that descend from the
        ///     BaseAutoIdent super-class designed to work with this generic repository implementation
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private List<Type> ListRelatedEntities(Type o)
        {
            return o
                .GetProperties()
                .Select(m => m.PropertyType.GetEnumeratedType() ?? m.PropertyType)
                .Where(m => m.IsSubclassOf(typeof(BaseAutoIdent))
                            && m != typeof(BaseDoc)
                            && m != typeof(DocTerm))
                .SelectMany(ListRelatedEntities)
                .Union(new List<Type> { o })
                .Distinct()
                .ToList();
        }
    }
}