using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Rudine.Util;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Storage.Sql
{
    public static class BaseDocExtensions
    {
        /// <summary>
        ///     resolves the correct dCFormDBContext for this particular type & graphs itself to the context if required
        ///     before writing itself to the DB this object the dbconte
        /// </summary>
        public static void Save(this BaseDoc _BaseDoc) { _BaseDoc.Save(_BaseDoc.GetSqlDBInstance()); }

        public static SqlDB GetSqlDBInstance(this BaseDoc _BaseDoc) { return SqlDB.GetInstance(_BaseDoc); }


        /// <summary>
        ///     gathers up types referenced by this BaseDoc via properties that descend from the
        ///     DocKey & BaseAutoIdent super-class designed to work with the generic repository implementation
        /// </summary>
        /// <returns></returns>
        public static List<Type> ListRelatedEntities(this BaseDoc o) { return ListRelatedEntities(o.GetType()); }

        /// <summary>
        ///     gathers up types referenced by the o via properties that descend from the
        ///     BaseAutoIdent super-class designed to work with this generic repository implementation
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static List<Type> ListRelatedEntities(Type o)
        {
            return o
                .GetProperties()
                .Select(m => m.PropertyType.GetEnumeratedType() ?? m.PropertyType)
                .Where(m =>
                       (
                           m.IsSubclassOf(typeof (BaseAutoIdent))
                           || m.IsSubclassOf(typeof (DocKey))
                       )
                       && m != typeof (BaseDoc)
                       && m != typeof (DocTerm))
                .SelectMany(ListRelatedEntities)
                .Union(new List<Type> {o})
                .Distinct()
                .ToList();
        }

        /// <summary>
        ///     Calculates a checksum based the BaseDoc's property ColumnAttribute case-insensitive names & there datatypes
        ///     recursively (the property may be another user type we reference) disregarding there order. That value is then
        ///     converted to Base36 & prefixed with the current DocTypeName passed. This string is suitable for cSharp namespaces &
        ///     SQL schemas
        /// </summary>
        /// <param name="_BaseDoc"></param>
        /// <returns></returns>
        [Obsolete("SQL classes are now unionized via the ClassFactory", true)]
        public static string CalcSqlSchemaName(this BaseDoc _BaseDoc)
        {
            return CacheMan.Cache(() =>
                                  {
                                      int checksum = 0;
                                      foreach (var t in _BaseDoc.ListRelatedEntities()
                                                                .Select(m => new
                                                                {
                                                                    TablePocoType = m,
                                                                    TableName = m.Name.ToLower()
                                                                })
                                                                .OrderBy(m => m.TableName))
                                      {
                                          checksum ^= t.TableName.GetHashCode();

                                          foreach (var p in t
                                              .TablePocoType
                                              .GetProperties()
                                              .Select(p =>
                                                      new
                                                      {
                                                          ColumnAttribute = p.GetCustomAttributes(true).OfType<ColumnAttribute>().FirstOrDefault(),
                                                          DataTypeName = p.PropertyType.Name
                                                      }).Where(m =>
                                                               m.ColumnAttribute != null
                                                               && !string.IsNullOrWhiteSpace(m.ColumnAttribute.Name))
                                              .OrderBy(m => m.ColumnAttribute.Name.ToLower()))
                                              checksum ^= p.ColumnAttribute.Name.ToLower().GetHashCode() ^ p.DataTypeName.GetHashCode();
                                      }

                                      return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                          "{0}_{1}",
                                          _BaseDoc.DocTypeName,
                                          Base36.Encode(Math.Abs(checksum)));
                                  }, false, _BaseDoc.GetType().FullName);
        }
    }
}