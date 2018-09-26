using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace Rudine.Storage.Sql
{
    /// <summary>
    ///     assigns the DocTypeName to the primary table & it's child table's Entity Framework Code First
    ///     __MigrationHistory.ContextKey
    /// </summary>
    public class SqlDbMigrationsConfiguration<TContext> : DbMigrationsConfiguration<TContext> where TContext : DbContext
    {
        public SqlDbMigrationsConfiguration()
        {
            string DocTypeName, DocRev;
            if (RuntimeTypeNamer.TryParseDocNameAndRev(typeof(TContext).Namespace, out DocTypeName, out DocRev))
                ContextKey = DocTypeName;

            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }
    }
}