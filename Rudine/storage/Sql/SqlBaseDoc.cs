using System.Collections.Generic;
using Rudine.Web;

namespace Rudine.Storage.Sql
{
    /// <summary>
    /// Contains a generic list representing DocKey items as Entity Framework Code First can not map a BaseDoc.DocKeys dictionary to table automaticly
    /// </summary>
    public abstract class SqlBaseDoc : BaseDoc
    {
        public  List<DocKey> DocKey { get; set; }
    }
}