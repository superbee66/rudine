using System;
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
        private static readonly string[] FormNonFillablePropertyNames =
        {
            "DocChecksum", "DocStatus", "DocId", "DocTitleFormat", "DocSrc", "DocDropbox"
        };

        /// <summary>
        ///     User may want to title there own document?
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public override string DocTitle
        {
            get { return base.DocTitle ?? DocTypeName.ToUpper(); }
            set { base.DocTitle = value; }
        }

        #region GetForm*Properties*

        /// <summary>
        ///     Further filters GetFormObjectNavProperties stripping DocId
        /// </summary>
        /// <param name="filled"></param>
        /// <returns></returns>
        public PropertyInfo[] GetFormFillableProperties(bool filled = false)
        {
            //TODO:Need to detect interface implementation safely
            return GetFormObjectNavProperties(filled).Where(m => !FormNonFillablePropertyNames.Contains(m.Name)).ToArray();
        }

        /// <summary>
        ///     Further filters GetFormObjectMappedProperties stripping IgnoreDataMember,XmlIgnoreAttribute,ScriptIgnoreAttribute &
        ///     NotMapped properties
        /// </summary>
        /// <param name="filled">when true, ensures the properties have been explicitly set</param>
        /// <returns></returns>
        public PropertyInfo[] GetFormObjectNavProperties(bool filled = false)
        {
            PropertyInfo[] p = CacheMan.Cache(() => GetFormObjectMappedProperties(false)
                                                  .Where(m =>
                                                             m.DeclaringType != typeof(BaseDoc) &&
                                                             m.DeclaringType != typeof(BaseAutoIdent)
                                                  )
                                                  .ToArray(),
                false,
                "GetFormObjectNavProperties",
                GetType().FullName,
                "GetFormObjectNavProperties");

            return p.Where(m => !filled || !this.IsDefaultValue(m)).ToArray();
        }

        /// <summary>
        ///     serialize-able, settable properties
        /// </summary>
        /// <param name="filled">when true, ensures the properties have been explicitly set</param>
        /// <returns></returns>
        public PropertyInfo[] GetFormObjectMappedProperties(bool filled = false)
        {
            PropertyInfo[] p = CacheMan.Cache(() =>
                                              {
                                                  return GetType().GetProperties(
                                                      BindingFlags.IgnoreCase
                                                      | BindingFlags.Public
                                                      | BindingFlags.Instance
                                                      | BindingFlags.SetProperty).Where(m =>
                                                                                            m.CanWrite
                                                                                            && m.PropertyType.IsPublic
                                                                                            && m.PropertyType.IsSerializable
                                                                                            && !m.PropertyType.IsArray
                                                                                            && !ExpressionParser.GetNonNullableType(m.PropertyType).IsAbstract
                                                                                            && !ExpressionParser.GetNonNullableType(m.PropertyType).IsGenericType).ToArray();
                                              },
                false,
                GetType().FullName,
                "GetFormObjectMappedProperties");

            return p.Where(m => !filled || !this.IsDefaultValue(m)).ToArray();
        }

        #endregion GetForm*Properties*

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
        ///     gathers up types referenced by this BaseDoc via properties that descend from the
        ///     DocKey & BaseAutoIdent super-class designed to work with the generic repository implementation
        /// </summary>
        /// <returns></returns>
        public List<Type> ListRelatedEntities() { return ListRelatedEntities(GetType()); }

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