using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rudine.Web
{
    /// <summary>
    ///     interfaces to various probes in client & core to figure out what can be served up
    /// </summary>
    internal static class DocKnownTypesProber
    {
        /// <summary>
        ///     Client & Core have different implementations of this interface. This is why this is done at runtime.
        /// </summary>
        private static readonly IDocKnownTypes[] _IDocRevKnownTypesImpl = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(_Assembly => _Assembly.GetExportedTypes(), (_Assembly, _Type) => new
            {
                _Assembly, _Type
            })
            .Where(t => !t._Type.IsInterface)
            .Where(t => t._Type.GetInterfaces().Any(i => i == typeof(IDocKnownTypes)))
            .Select(t => ((IDocKnownTypes) Activator.CreateInstance(t._Type)))
            .ToArray();

        public static IEnumerable<Type> DocTypes(ICustomAttributeProvider provider) { return _IDocRevKnownTypesImpl.SelectMany(Impl => Impl.DocTypeServedItems()).Distinct(); }
    }
}