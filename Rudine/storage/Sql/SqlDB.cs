using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.DynamicData;
using Microsoft.AspNet.DynamicData.ModelProviders;
using Rudine.Web;

namespace Rudine.Storage.Sql
{
    /// <summary>
    ///     Singleton model to initiate our MetaModel(s) outside the Global.asax suggested by Microsoft.
    ///     This allows the model to stay loaded in memory with out re-initialization per request
    /// </summary>
    public class SqlDB : MetaModel, IDisposable
    {
        /// <summary>
        ///     If an enterprise level 2008+ server is running we can apply compression
        /// </summary>
        public const bool PAGE_COMPRESSION = true;

        /// <summary>
        ///     Page compression for change tables
        /// </summary>
        public const string PAGE_COMPRESSION_CDC_SQL = "alter table cdc.dbo_{0}_CT rebuild with ( data_compression = page )";

        /// <summary>
        ///     Page compression for all code-first fostered tables
        /// </summary>
        public const string PAGE_COMPRESSION_DBO_SQL = "alter table {0}.{1} rebuild with ( data_compression = page )";

        /// <summary>
        ///     not comment at this time
        /// </summary>
        public const string DROP_CONCRETE_TABLE_CHANGE = @"DROP TABLE {0}.{1}";

        private readonly SqlDbContext _underlyingDbContext;

        private static Regex _ChangeTableObjMatch = new Regex(@"[\n\r]+.*""dbo\.\w+Change"".*;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static Regex _NothingToMigrate = new Regex(@"public override void Up\(\)\s+\{\s+\}", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public SqlDbContext UnderlyingDbContext { get { return _underlyingDbContext; } }

        /// <summary>
        /// Band-aid to for SqlTableAttributeConvention to set table schema name.
        /// </summary>
        static internal string InitializingDocTypeName { get; private set; }
        static private object InitializingDocTypeNameLock = new object();


        /// <summary>
        ///     establishes one DBContext & sql schema per-csharp-namespace
        /// </summary>
        /// <param name="_BaseDoc"></param>
        private SqlDB(BaseDoc _BaseDoc)
            : base(false)
        {
            lock (InitializingDocTypeNameLock)
            {
                InitializingDocTypeName = _BaseDoc.DocTypeName;

                _underlyingDbContext = SqlDbContext.CreateInstance(_BaseDoc);
                if (!_underlyingDbContext.Database.Exists())
                    _underlyingDbContext.Database.Create();

                _underlyingDbContext.Database.CompatibleWithModel(false);

                // register with meta data model so we can use it's features later
                RegisterContext(
                new EFDataModelProvider(() => _underlyingDbContext),
                new ContextConfiguration { ScaffoldAllTables = true }
                );
            }
        }

        private static readonly Dictionary<string, SqlDB> _Dictionary_string_InfoPathDB = new Dictionary<string, SqlDB>();

        public static SqlDB GetInstance(BaseDoc baseDoc)
        {
            string key = baseDoc.GetType().Namespace;

            return _Dictionary_string_InfoPathDB.ContainsKey(key)
                       ? _Dictionary_string_InfoPathDB[key]
                       : _Dictionary_string_InfoPathDB[key] = new SqlDB(baseDoc);
        }

        public void Dispose()
        {
            string key = _Dictionary_string_InfoPathDB.First(m => m.Value == this).Key;
            _Dictionary_string_InfoPathDB.Remove(key);
            UnderlyingDbContext.Dispose();
        }
    }
}