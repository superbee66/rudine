using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using Rudine.Web;

namespace Rudine.Storage.Sql
{
    [DbConfigurationType(typeof(ModelConfiguration))]
    public abstract class SqlDbContext : DbContext
    {
        public const string CONTEXT_NAME = "SqlDbContext";

        public SqlDbContext()
            : base(CONTEXT_NAME)
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = true;
            Configuration.ValidateOnSaveEnabled = true;
        }

        public static SqlDbContext CreateInstance(BaseDoc _BaseDoc)
        {
            Type _BaseDocType = _BaseDoc.GetType();
            string _DbContextTypeName = dCFormDBContextTypeName(_BaseDoc);

            string cSharpCode = string.Format(@"
            namespace {0} {{
                public class {1} : {2}.{3} {{
                    public {1}() : base( ) {{
                        System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<{0}.{1}, {2}.SqlDbMigrationsConfiguration<{0}.{1}>>());
                    }}
                }}
            }}",
                _BaseDocType.Namespace,
                _DbContextTypeName,
                typeof(SqlDbContext).Namespace,
                nameof(SqlDbContext));

            // add using statements to beginning to top of the document
            cSharpCode = string.Join("", Runtime.USING_NAMESPACES.Keys.OrderBy(ns => ns).Select(ns => string.Format("using {0};\n", ns))) + cSharpCode;

            //TODO:Separate the dCFormDBContextTypeName from the BaseDoc assembly/code-gen. Runtime-compiled BaseDocs & SqlDbContext code placement organization needs to be rethought thru better. 
            return (SqlDbContext) Activator
                .CreateInstance(Runtime
                    .CompileCSharpCode(() => cSharpCode, string.Format("{0}.{1}.{2}", typeof(SqlDbContext).Namespace, _BaseDoc.DocTypeName, _BaseDoc.solutionVersion))
                    .GetExportedTypes()
                    .First(m => m.Name == _DbContextTypeName));
        }

        private static string dCFormDBContextTypeName(BaseDoc baseDoc) => string.Format("{0}_Db", baseDoc.DocTypeName);

        public static string GetConnectionString() => ConfigurationManager.ConnectionStrings[CONTEXT_NAME].ConnectionString;


        /// <summary>
        ///     A good place to ignore/exclude fields going to the database
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            string myNamespace = GetType().Namespace;
            Type _BaseDocType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.Namespace == myNamespace && t.BaseType == typeof(BaseDoc))).FirstOrDefault();

            if (_BaseDocType == null)
                throw new Exception(string.Format("{0} can't resolve it's own {1}.[*:BaseDoc]", GetType().FullName, myNamespace));

            BaseDoc _BaseDoc = (BaseDoc) Activator.CreateInstance(_BaseDocType);

            ToTable(modelBuilder, typeof(DocKey), _BaseDocType.Name);

            foreach (var t in _BaseDoc
                    .ListRelatedEntities()
                    .Select(m => new
                    {
                        TablePocoType = m,
                        TableName = m.Name.ToLower()
                    }).OrderBy(m => m.TableName)
                    .ToArray())
                // ensure the type has some other property to translate to database columns other then the "Id" property
                //if (t.TablePocoType.GetProperties().Where(p => !p.GetCustomAttributes(true).Any(a => a.GetType() == typeof(NotMapped)) && !p.PropertyType.IsClass).Count() > 1)
                ToTable(modelBuilder, t.TablePocoType, _BaseDocType.Name);

            //The .Net DateTime datatype may have values outside the SQL DateTime data type, SQL datetime2 encompasses all .Net DateTime data types
            modelBuilder
                .Properties<DateTime>()
                .Configure(c => c.HasColumnType("datetime2"));

            // auto identities for Id properties
            //modelBuilder
            //.Properties()
            //.Where(p => p.Name == "Id")
            //.Configure(c => c.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity));

            // reflection is used heavily throughout the solution, names must remain the same between models, poco & sql statements
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
            modelBuilder.Conventions.Remove<TableAttributeConvention>();
            modelBuilder.Conventions.Add<SqlTableAttributeConvention>();

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        ///     Setting the table name & schema up front (not waiting to set in conventions) seem to be the only wau to get get EF
        ///     CF to consistently apply our schemas. When this was performed at the conventions method it did not allways seem to
        ///     take; our tables would then get the default "dbo" schema even when they were told not too.
        /// </summary>
        /// <param name="dbModelBuilder"></param>
        /// <param name="entityType"></param>
        /// <param name="schemaName"></param>
        private static void ToTable(DbModelBuilder dbModelBuilder, Type entityType, string schemaName)
        {
            // add each entity that has a TableName attribute connected to the BaseDoc to the model
            MethodInfo entityMethod = ((object) dbModelBuilder).GetType().GetMethod("Entity");
            object o = entityMethod.MakeGenericMethod(entityType).Invoke(dbModelBuilder, new object[] { });

            // call the ToTable method via reflection as EntityFramework designed it with an accessor level private
            o.GetType().GetMethods().First(m => m.Name == "ToTable" && m.GetParameters().Count() == 2).Invoke(o, new object[] {entityType.Name, schemaName});
        }
    }
}