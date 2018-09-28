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
        public static SqlDB GetSqlDBInstance(this BaseDoc _BaseDoc) => SqlDB.GetInstance(_BaseDoc);

        /// <summary>
        ///     gathers up types referenced by this BaseDoc via properties that descend from the
        ///     DocKey & BaseAutoIdent super-class designed to work with the generic repository implementation
        /// </summary>
        /// <returns></returns>
        public static List<Type> ListRelatedEntities(this BaseDoc o) => ListRelatedEntities(o.GetType());

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
                    m.IsSubclassOf(typeof(BaseAutoIdent))
                    && m != typeof(BaseDoc)
                    && m != typeof(DocTerm))
                .SelectMany(ListRelatedEntities)
                .Union(new List<Type> {o})
                .Distinct()
                .ToList();
        }

        /// <summary>
        ///     resolves the correct dCFormDBContext for this particular type & graphs itself to the context if required
        ///     before writing itself to the DB this object the dbconte
        /// </summary>
        public static void Save(this BaseDoc _BaseDoc)
        {
            _BaseDoc.Save(_BaseDoc.GetSqlDBInstance());
        }
    }
}