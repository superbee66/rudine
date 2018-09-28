using System;
using System.Linq;
using System.Reflection;

namespace Rudine.Web.Util
{
    internal static class AssemblyExtensions
    {
        /// <summary>
        ///     Calls GetExortedTypes() when assembly is not dynamic. When it is dynamic GetTypes() is called & non-public types
        ///     are filtered out.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Type[] GetExportedTypes2(this Assembly assembly) =>
            assembly.IsDynamic
                ? assembly.GetTypes().Where(type => type.IsPublic).ToArray()
                : assembly.GetExportedTypes();
    }
}