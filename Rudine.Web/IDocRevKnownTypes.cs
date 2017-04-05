using System;
using System.Collections.Generic;

namespace Rudine.Web
{
    public interface IDocKnownTypes
    {
        /// <summary>
        /// </summary>
        /// <returns>current DocTypeNames known to this system</returns>
        List<string> DocTypeNames();

        /// <summary>
        ///     Types that can be actively served via WCF as "new" documents. There types must have a folder representation in the
        ///     file system. This list is cached internally. Before the list is constructed models and other contents are processed
        ///     & imported to the Docdb database.
        /// </summary>
        /// <returns>current DocTypeNames known to this system</returns>
        List<Type> DocTypeServedItems();
    }
}