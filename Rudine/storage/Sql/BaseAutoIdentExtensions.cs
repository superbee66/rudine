using System;
using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using Rudine.Web;

namespace dCForm.Core.Storage.Sql
{
    public static class BaseAutoIdentExtensions
    {
        private static DbSet set(this BaseAutoIdent o, SqlDB db)
        {
            return db.UnderlyingDbContext.Set(o.entityType());
        }

        private static DbEntityEntry entry(this BaseAutoIdent o,SqlDB db)
        {
            return db.UnderlyingDbContext.Entry(o);
        }

        private static Type entityType(this BaseAutoIdent o)
            =>  ObjectContext.GetObjectType(o.GetType());

        private static string pkStr(this BaseAutoIdent o,SqlDB db)=> o.Id.ToString();

        private static BaseAutoIdent attachedEntity(this BaseAutoIdent o,SqlDB db)
        {
            return o.set(db)
                .Local
                .AsQueryable()
                .Cast<BaseAutoIdent>()
                .FirstOrDefault(m => m.pkStr(db) == o.pkStr(db));
        }

        private static void Update(this BaseAutoIdent o,SqlDB db)
        {
            if (o.entry(db).State == EntityState.Detached)
                if (o.attachedEntity(db) != null)
                    db.UnderlyingDbContext.Entry(o.attachedEntity(db)).CurrentValues.SetValues(o);
                else
                    o.entry(db).State = EntityState.Modified; // This should attach entity
        }

        private static void Add(this BaseAutoIdent o,SqlDB db)
        {
            o.set(db).Add(o);
        }

        /// <summary>
        ///     attaches the objects graph & it's child objects via navigation properties back the the DBContext & executes
        ///     Adds/Updates. This object graph attacher works specifically with BaseAutoIdent & generic lists of them
        /// </summary>
        /// <param name="db"></param>
        /// <param name="AutoSaveChanges"></param>
        public static void Save(this BaseAutoIdent o,SqlDB db, bool AutoSaveChanges = true)
        {
            if (o.Id == 0)
                o.Add(db);
            else
                o.Update(db);

            foreach (PropertyInfo _PropertyInfo in o.GetType().GetProperties())
                if (_PropertyInfo.GetValue(o, null) != null)
                    if (_PropertyInfo.PropertyType.IsSubclassOf(typeof (BaseAutoIdent)))
                        ((BaseAutoIdent) _PropertyInfo.GetValue(o, null)).Save(db, false);
                    else if (_PropertyInfo.PropertyType.GetInterface("IList") != null)
                        foreach (BaseAutoIdent _BaseAutoIdent in ((IList) _PropertyInfo.GetValue(o, null)).OfType<BaseAutoIdent>())
                            _BaseAutoIdent.Save(db, false);


            if (AutoSaveChanges)
                db.UnderlyingDbContext.SaveChanges();
        }
    }
}