using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Rudine.Storage.Sql
{
    /// <summary>
    /// assigns the DocTypeName to the primary table & it's child table's schema identifier
    /// </summary>
    public class SqlTableAttributeConvention : TableAttributeConvention
    {
     /// <inheritdoc />
        public override void Apply(ConventionTypeConfiguration configuration, TableAttribute attribute)
        {
            string docTypeName, docRev;
            if (RuntimeTypeNamer.TryParseDocNameAndRev(configuration.ClrType.Namespace, out docTypeName, out docRev))
                configuration.ToTable(attribute.Name, docTypeName);
            else
                configuration.ToTable(attribute.Name, SqlDB.InitializingDocTypeName);
        }
    }
}