﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Util.Zips
{
    public static class BaseDocExtensions
    {
        /// <summary>
        ///     gathers up types referenced by this BaseDoc via properties that descend from the
        ///     DocKey & BaseAutoIdent super-class designed to work with the generic repository implementation
        /// </summary>
        /// <returns></returns>
        public static List<Type> ListDeps(this BaseDoc BaseDoc) =>
            ListDeps(BaseDoc.GetType());

        /// <summary>
        ///     gathers up types referenced by the o via properties that descend from the
        ///     BaseAutoIdent super-class designed to work with this generic repository implementation
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        private static List<Type> ListDeps(Type Type) =>
            Type
                .GetProperties()
                .Select(m => m.PropertyType.GetEnumeratedType() ?? m.PropertyType)
                .Where(m =>
                    m.IsSubclassOf(typeof(BaseAutoIdent))
                    && m != typeof(BaseDoc)
                    && m != typeof(DocTerm))
                .SelectMany(ListDeps)
                .Union(new List<Type>
                {
                    Type
                })
                .Distinct()
                .ToList();
    }
}