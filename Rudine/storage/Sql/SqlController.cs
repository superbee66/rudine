using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Objects;
using System.Linq;
using Rudine.Web.Util;
using System.Reflection;
using System.Text;
using System.Web.DynamicData;
using System.Xml;
using dCForm.Core.Storage.Sql.Merge;
using dCForm.Core.Storage.Sql.Reverser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Rudine;
using Rudine.Interpreters;
using Rudine.Util;
using Rudine.Web;
using Rudine.Web.Util;

namespace dCForm.Core.Storage.Sql
{
    public static class DictionaryExtensions
    {
        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> d)
        {
            NameValueCollection _NameValueCollection = new NameValueCollection();
            if (d != null)
                if (d.Count > 0)
                    foreach (var item in d)
                        _NameValueCollection[item.Key] = item.Value;
            return _NameValueCollection;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, List<string>> d)
        {
            NameValueCollection _NameValueCollection = new NameValueCollection();
            foreach (string key in d.Keys)
                foreach (string val in d[key])
                    _NameValueCollection.Add(key, val);

            return _NameValueCollection;
        }


        public static Type GetEnumeratedType<T>(this IEnumerable<T> _)
        {
            return typeof(T);
        }
    }

    /// <summary>
    ///     Utilizes Entity Code First 6 over SQL to persist BaseDocs. Audit
    ///     operations are not implemented/supported by this implementation
    ///     of the IDocController.
    ///     All undying Entity Framework calls are performed by a slave AppDomain's instance of this SqlController. To support
    ///     runtime Entity Framework Code First Migration behavior (not supported out of the box with EF): DocTypeName, DocRev
    ///     & existing sql database "__MigrationHistory.Model" EDMX models are evaluated at runtime. If the current
    ///     __MigrationHistory.Model does not support the given DocTypeName/DocRev passed the slave AppDomain will be unloaded
    ///     & a new will be activated ready for Entity Framework to refresh it's own models & generate migration logic
    ///     accordingly.
    /// </summary>
    public class SqlController : SelfMarshalByRefObject<SqlController>
    {
        private const bool PRETTY_NAMES = true;

        /// <summary>
        ///     utilizes TextualShouldSerializeContractResolver to serialize keys that follow the PRETTY_NAMES rule
        /// </summary>
        private static readonly JsonSerializer _JsonSerializer = new JsonSerializer
        {
            ContractResolver = TextualShouldSerializeContractResolver.Instance
        };

        /// <summary>
        ///     Sql will only be storing textual data data is not of default value in nature
        /// </summary>
        private class SqlIdsOnlySerializeContractResolver : DefaultContractResolver
        {
            public static readonly SqlIdsOnlySerializeContractResolver Instance = new SqlIdsOnlySerializeContractResolver
            {
                IgnoreSerializableAttribute = true,
                IgnoreSerializableInterface = true,
                SerializeCompilerGeneratedMembers = true
            };

            public static readonly JsonSerializerSettings MyJsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = Instance
            };

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                property.ShouldSerialize = instance =>
                                           property.PropertyType != typeof(string)
                                           && (!property.PropertyType.IsValueType || property.PropertyName == "Id");
                property.Ignored = false;
                if (PRETTY_NAMES)
                    property.PropertyName = StringTransform.PrettyMsSqlIdent(property.PropertyName);

                return property;
            }
        }

        /// <summary>
        ///     Sql will only be storing textual data data is not of default value in nature
        /// </summary>
        private class TextualShouldSerializeContractResolver : DefaultContractResolver
        {
            public static readonly TextualShouldSerializeContractResolver Instance = new TextualShouldSerializeContractResolver
            {
                IgnoreSerializableAttribute = true,
                IgnoreSerializableInterface = true,
                SerializeCompilerGeneratedMembers = true
            };

            public static readonly JsonSerializerSettings MyJsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = Instance
            };

            public static readonly Type[] IgnoredDataTypes =
            {
                typeof (byte[]), typeof (XmlElement[])
            };

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                property.Ignored = IgnoredDataTypes.Any(p => p == property.PropertyType);
                property.ShouldSerialize = instance => !property.Ignored;

                if (PRETTY_NAMES)
                    property.PropertyName = StringTransform.PrettyMsSqlIdent(property.PropertyName);

                return property;
            }
        }

        #region private

        /// <summary>
        ///     Applies the strict naming conventions practiced though out the dCForm solution
        ///     to resolve the MetaTable the baseForm Code First class spawned. The meta table itself
        ///     was materialized by DynamicData
        /// </summary>
        /// <param name="o"></param>
        /// <returns>An in memory MetaTable representing a physical SQL table observed</returns>
        private static MetaTable GetFormTable(BaseDoc o) { return SqlDB.GetInstance(o).GetTable(o.DocTypeName); }

        private static MetaTable GetDocKeyTable(BaseDoc o) { return SqlDB.GetInstance(o).GetTable("DocKey"); }

        private IEnumerable ListInternal(Type filter, NameValueCollection docKeyFilters = null, int PageSize = 150, int PageIndex = 0) { return ListInternal((BaseDoc)Activator.CreateInstance(filter), docKeyFilters, PageSize, PageIndex); }

        private IEnumerable ListInternal(BaseDoc filter, NameValueCollection docKeyFilters = null, int PageSize = 150, int PageIndex = 0)
        {
            docKeyFilters = docKeyFilters ?? new NameValueCollection();
            string propInfoPredicate = string.Empty;

            //TODO:Add logic to filter forms based on the documentKey(s) passed
            Dictionary<string, string> DocKeys = filter.DocKeys;

            if( DocKeys!=null)
            foreach (string key in DocKeys.Keys)
                docKeyFilters.Add(key, DocKeys[key]);

            //filter.DocId = null;

            // predicate formed from PropertyInfo[] of the form business object
            // FormHandlerNavigation filterFormHandlerNavigation = new FormHandlerNavigation(filter);
            // List<object> parms =  filterFormHandlerNavigation.RenderNavigateUrlParameters().ToList<object>();
            List<object> parms = new List<object>();
            StringBuilder predicateStringBuilder = new StringBuilder();
            MetaTable _table = GetFormTable(filter);

            // run through all the properties suitable for SQL/EF-mapping
            foreach (PropertyInfo _PropertyInfo in filter.GetFormObjectNavProperties(true).Where(m => !Attribute.IsDefined(m, typeof(NotMappedAttribute))))
            {
                Type parmType = ExpressionParser.GetNonNullableType(_PropertyInfo.PropertyType) ?? _PropertyInfo.PropertyType;
                bool IsNullable = parmType != _PropertyInfo.PropertyType;
                parms.Add(Convert.ChangeType(_PropertyInfo.GetValue(filter, null), parmType, null));

                bool Null_Value = false;
                if (!Null_Value)
                    Null_Value = IsNullable && parmType.IsPrimitive;
                if (!Null_Value)
                    Null_Value = IsNullable && parmType == typeof(DateTime);

                predicateStringBuilder
                    .Append(_PropertyInfo.Name)
                    .Append(Null_Value ? ".Value" : string.Empty)
                    .Append(".Equals(@")
                    .Append(parms.Count - 1)
                    .Append(") && ");
            }

            propInfoPredicate = predicateStringBuilder.ToString().Trim(' ', '&');

            // Merge the docKeys & NameValueCollection items
            // Remove dictionary items that exist in the namevalue collection first
            MetaTable _docKeyTable = GetDocKeyTable(filter);
            StringBuilder DocKeyMatchSQLPredicate = new StringBuilder();
            foreach (string key in docKeyFilters.Keys)
                DocKeyMatchSQLPredicate
                    .AppendFormat(@" OR  ( N'{0}' = KeyName  AND  ( ", key)
                    .Append(string.Join(" or ", docKeyFilters.GetValues(key).Select(m => "N'" + m + "'=Keyval").Distinct().ToArray()))
                    .Append(") )");

            /* note this reference to CamelCase is also done in the .tt file to make the SQL column names pretty.
                     * technically we should be reading the properties Column attribute value to get the CamelCase
                     * version of the property's name */
            string docKeyMatchSQL = "";

            docKeyMatchSQL = docKeyFilters.Count == 0
                                 ? string.Empty
                                 : string.Format(
                                     @"
        
                    SELECT TOP 1 Id
                    FROM   {1}.{2} /* The docKey table for the given entity */
                    WHERE  {3} /* The predicate */
                    GROUP  BY Id HAVING COUNT(*) = {4}",
                                     "Id",
                                     string.IsNullOrWhiteSpace(propInfoPredicate)
                                         ? filter.DocTypeName
                                         : "(" + ((ObjectQuery)_table.GetQuery().Where(propInfoPredicate, parms.ToArray())).ToTraceString() + ")", /* Notice we use the MetaTable that has access to the underlying ObjectContext to yield our object-SQL */
                                     _docKeyTable.Name,
                                     DocKeyMatchSQLPredicate.ToString().Replace(" OR ", " || ").Trim(' ', '|').Replace(" || ", " OR "),
                                     docKeyFilters.Keys.Count);


            // locate the keys 
            object[] keyValues = !string.IsNullOrWhiteSpace(docKeyMatchSQL)
                                     ? SqlDB.GetInstance(filter).UnderlyingDbContext.Database.SqlQuery<int>(docKeyMatchSQL).Cast<object>().ToArray()
                                     : new object[]
                                     {};

            filter.DocKeys = DocKeys;

            return !string.IsNullOrWhiteSpace(docKeyMatchSQL)
                       ? keyValues.Length == 0
                             ? new List<object>() : new List<object>
                             {
                                 SqlDB.GetInstance(filter).UnderlyingDbContext.Set(_table.EntityType).Find(keyValues)
                             }
                       : !string.IsNullOrWhiteSpace(propInfoPredicate)
                             ? (IEnumerable)_table.GetQuery().Where(propInfoPredicate, parms.ToArray()).Skip(PageIndex).Take(PageSize)
                             : new List<BaseDoc>().AsEnumerable();
        }

        #endregion private

        #region IDocController Methods

        public virtual List<LightDoc> Audit(string DocTypeName, string DocId, string RelayUrl = null) { throw new NotImplementedException(); }

        public virtual object Get(out string DocSrc, out Dictionary<string, string> DocKeysFromDocId, string DocTypeName, Dictionary<string, string> DocKeys = null, string DocId = null, string RelayUrl = null) { throw new NotImplementedException(); }

        public virtual List<LightDoc> List(List<string> DocTypeNames, Dictionary<string, List<string>> DocKeys = null, Dictionary<string, List<string>> DocProperties = null, string KeyWord = null, int PageSize = 150, int PageIndex = 0, string RelayUrl = null) { throw new NotImplementedException("Can't list with SqlController as the DocRev problem has not been solved yet."); }

        private static readonly Dictionary<string, List<Type>> SqlKnownDocTypes = new Dictionary<string, List<Type>>();

        private static readonly JsonMergeSettings _JsonMergeSettings = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Merge
        };

        private void Submit_Internal(byte[] DocData, string DocSubmittedBy, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)
        {

            DocData = DocInterpreter.Instance.ModPI(DocData, DocSubmittedBy, DocStatus, null, DocKeys);

            // let the BaseDoc parse it's string just as normal
            BaseDoc _SubmittedBaseDoc = DocInterpreter.Instance.Read(DocData, true);
            Type _SqlBaseDocType = ReverseEngineerCodeFirst(_SubmittedBaseDoc);

            BaseDoc _SqlBaseDoc = (BaseDoc)Activator.CreateInstance(_SqlBaseDocType);

            IQueryable _SqlList = ListInternal(_SqlBaseDocType, _SubmittedBaseDoc.DocKeys.ToNameValueCollection()).AsQueryable();

            if (_SqlList.Any())
                foreach (var o in _SqlList)
                    _SqlBaseDoc = (BaseDoc)o;

            if (_SqlBaseDoc.DocChecksum != _SubmittedBaseDoc.DocChecksum)
            {
                JObject _SubmittedJObject = JObject.FromObject(_SubmittedBaseDoc, _JsonSerializer);

                if (_SqlBaseDoc.Id > 0)
                {
                    // Serialize the _ExistingBaseDoc Id property values only (the rest of the values will be merged in from the _SubmittedBaseDoc)
                    // keep in mind that EF lazy loading will kick in & load to ensure all the child objects & there Id(s) are serialized here
                    string _SqlIdsAsJson = JsonConvert.SerializeObject(_SqlBaseDoc, SqlIdsOnlySerializeContractResolver.MyJsonSerializerSettings);
                    JObject _SqlJObject = JObject.Parse(_SqlIdsAsJson);

                    // take the SQL Id(s) from the existing data & the new textual content from the new submission & combine them
                    // in order to make a clean up to the SQL database
                    _SubmittedJObject.Merge(_SqlJObject, _JsonMergeSettings);

                    _SqlBaseDoc = (BaseDoc)JsonConvert.DeserializeObject(
                        _SubmittedJObject.ToString(),
                        _SqlBaseDoc.GetType(),
                        TextualShouldSerializeContractResolver.MyJsonSerializerSettings);

                    // Utilize PropertyOverlay.Overlay(topO,bottomO,true), set those combined objects of business data & SQL Ids to the _ExistingBaseDoc
                    // ensure the PropertyOverlay.Overlay sizeToTop is set to true in order to force child List item counts (one-to-many) to agree with
                    // what the user has submitted. This results in Insert/Update/Delete statements shaping the existing dataset (_ExistingBaseDoc)
                    // to the _SubmittedBaseDoc.
                }
                else
                {
                    _SqlBaseDoc = (BaseDoc)JsonConvert.DeserializeObject(
                        _SubmittedJObject.ToString(),
                        _SqlBaseDoc.GetType(),
                        TextualShouldSerializeContractResolver.MyJsonSerializerSettings);

                    MetaTable _docKeyTable = GetDocKeyTable(_SqlBaseDoc);
                    foreach (KeyValuePair<string, string> _Item in _SubmittedBaseDoc.DocKeys)
                    {
                        DocKey _DocKeyEntry = (DocKey)Activator.CreateInstance(_docKeyTable.EntityType, null);
                        _DocKeyEntry.Id = _SqlBaseDoc.Id;
                        _DocKeyEntry.KeyName = _Item.Key;
                        _DocKeyEntry.KeyVal = _Item.Value;
                        SqlDB.GetInstance(_SqlBaseDoc).UnderlyingDbContext.Set(_docKeyTable.EntityType).Add(_DocKeyEntry);
                    }
                }
                _SqlBaseDoc.Save();
            }
        }

        private static string DocModelMergeCountIdent(string DocTypeName)
        {
            return string.Format(
                "DocModelMergeCount_{0}",
                SqlKnownDocTypes.ContainsKey(DocTypeName)
                    ? SqlKnownDocTypes[DocTypeName].Count()
                    : 0);
        }

        private static readonly Dictionary<string, Type> ReverseEngineerCodeFirstDic = new Dictionary<string, Type>();

        /// <summary>
        /// </summary>
        /// <param name="_SubmittedBaseDoc"></param>
        /// <returns>Something that Entity Framework Code First would contract the same SQL objects read to create this type</returns>
        private static Type ReverseEngineerCodeFirst(BaseDoc _SubmittedBaseDoc)
        {
            if (!ReverseEngineerCodeFirstDic.ContainsKey(_SubmittedBaseDoc.DocTypeName))
            {
                string sqlAsCharp = string.Empty;
                try
                {
                    // attempt to observe any tables that would accommodate this DocTypeName in the database now as this will be needed the remaining execution of this appdomain to assess code first auto-migrations
                    // in the database itself, the namespace of a given document has no baring on how the doc it persisted in sql or the structure it may spawn
                    sqlAsCharp = Handler.ReverseEngineerCodeFirst(
                       RuntimeTypeNamer.CalcCSharpFullName(_SubmittedBaseDoc.DocTypeName, "0.0.0.0", "SqlToNonBaseDoc"),
                       _SubmittedBaseDoc.DocTypeName,
                       SqlDbContext.GetConnectionString());
                }
                catch (Exception) { /*TODO:Check for database instead of letting this fail*/ }

                ReverseEngineerCodeFirstDic[_SubmittedBaseDoc.DocTypeName] =
                    ClassMerger.Instance.MergeOnPropertyNames(
                        RuntimeTypeNamer.CalcCSharpFullName(_SubmittedBaseDoc.DocTypeName, "0.0.0.0"),
                        new[]
                        {
                            _SubmittedBaseDoc.GetType(),
                            string.IsNullOrWhiteSpace(sqlAsCharp)
                                ? _SubmittedBaseDoc.GetType()
                                : Runtime.CompileCSharpCode(
                                    ()=>sqlAsCharp,
                                    string.Format("{0}." + "0.0.0.0" + "." + "PostMergeTo" + ".{1}", _SubmittedBaseDoc.DocTypeName, _SubmittedBaseDoc.solutionVersion)
                                    ).GetTypes().First(t => t.Name == _SubmittedBaseDoc.DocTypeName)
                        },
                        _SubmittedBaseDoc.DocTypeName,
                        new[] { "Any", "Xml_Element" }, // ignore XmlElement Any properties
                        true,
                        typeof(BaseDoc));

            }

            return ReverseEngineerCodeFirstDic[_SubmittedBaseDoc.DocTypeName];
        }


        //TODO:Condense the parameters for Submit methods to a single ProcessingInstructions argument
        public LightDoc Submit(byte[] DocData, string DocSubmittedBy, string RelayUrl = null, bool? DocStatus = null, DateTime? SubmittedDate = null, Dictionary<string, string> DocKeys = null, string DocTitle = null)
        {
            // let the BaseDoc parse it's string just as normal
            BaseDoc _SubmittedBaseDoc = DocInterpreter.Instance.Read(DocData, true);

            // has any DocRev of the given DocTypeName been submitted to the database previously? 
            if (!SqlKnownDocTypes.ContainsKey(_SubmittedBaseDoc.DocTypeName))
                SqlKnownDocTypes[_SubmittedBaseDoc.DocTypeName] = new List<Type> { _SubmittedBaseDoc.GetType(), ReverseEngineerCodeFirst(_SubmittedBaseDoc) }.Distinct().ToList();
            else if (!SqlKnownDocTypes[_SubmittedBaseDoc.DocTypeName].Contains(_SubmittedBaseDoc.GetType()))
            {
                // when this new DocType is added, does it bring any new properties that would force a migration?
                int preAdd_CalcClassSignature = ClassMerger.Instance.CalcClassSignature(
                    DocModelMergeCountIdent(_SubmittedBaseDoc.DocTypeName),
                    SqlKnownDocTypes[_SubmittedBaseDoc.DocTypeName].ToArray(),
                    _SubmittedBaseDoc.DocTypeName);

                SqlKnownDocTypes[_SubmittedBaseDoc.DocTypeName].Add(_SubmittedBaseDoc.GetType());

                int postAdd_CalcClassSignature = ClassMerger.Instance.CalcClassSignature(
                    DocModelMergeCountIdent(_SubmittedBaseDoc.DocTypeName),
                        SqlKnownDocTypes[_SubmittedBaseDoc.DocTypeName].ToArray(),
                    _SubmittedBaseDoc.DocTypeName);

                if (preAdd_CalcClassSignature != postAdd_CalcClassSignature)
                    SlaveReload();
            }

            Slave().Submit_Internal(DocData, DocSubmittedBy, RelayUrl, DocStatus, SubmittedDate, DocKeys, DocTitle);
            return _SubmittedBaseDoc.ToLightDoc();
        }

        public virtual LightDoc Status(string DocTypeName, string DocId, bool DocStatus, string DocSubmittedBy, string RelayUrl) { throw new NotImplementedException(); }

        #endregion IDocController Methods
    }
}