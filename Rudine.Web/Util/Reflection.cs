using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Rudine.Web.Util;

namespace Rudine.Util
{
    internal static class Reflection
    {
        /// <summary>
        ///     Matches everything but the namespace.typename input. This allows weakening type names cutting them down to just the
        ///     namespace.typename identifier.
        ///     System.Collections.Generic.List`1[[docLCR_1023AFORPD_0715.r2016.r05.r11.r04.r37.r45.ArrayOfRepeaterRepeater,
        ///     docLCR_1023AFORPD_0715_r2016_r05_r11_r04_r37_r45, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] ----->
        ///     now becomes ---->
        ///     System.Collections.Generic.List`1[[docLCR_1023AFORPD_0715.r2016.r05.r11.r04.r37.r45.ArrayOfRepeaterRepeater]]
        /// </summary>
        private static readonly Regex UNQUALIFY_TYPE_NAMES =
            new Regex(@",\s*[\w\.]+,\s*Version=\d+\.\d+\.\d+\.\d+,\s*Culture=\w+,\s*PublicKeyToken=\w+");

        /// <summary>
        ///     Seeks out the type from assemblies currently loaded and caches it for subsequent calls
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="typeNamespace"></param>
        /// <param name="assemblyName"></param>
        /// <param name="noCache"></param>
        /// <returns></returns>
        public static Type GetType(string typeName, string typeNamespace = null, string assemblyName = null,
                                   bool noCache = false)
        {
            if (noCache)
                LoadBinDlls();

            Type t = GetType(
                string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.{1}", typeNamespace, typeName).Trim('.'),
                string.IsNullOrWhiteSpace(assemblyName)
                    ? null
                    : AppDomain.CurrentDomain.GetAssemblies()
                               .Where(m => (m.FullName) == assemblyName)
                               .SelectMany(a => a.GetTypes())
                               .ToArray());

            return t == null && !noCache
                       ? GetType(typeName, typeNamespace, assemblyName, true)
                       : t;
        }

        /// <summary>
        ///     Matches the given TypeFullname string to the fullname(s) of the given types, there child property type's, the
        ///     principle "EnumeratedType" when dealing with something like a generic list  & any other referenced types connected
        /// </summary>
        /// <param name="typeNameOrFullname">simple type's name or fully qualified name</param>
        /// <param name="searchScope">all types currently loaded in the AppDomain by default</param>
        /// <returns>null if nothing can be found</returns>
        public static Type GetType(string typeNameOrFullname, Type[] searchScope = null)
        {
            //TODO:reorganize code as it's not clear what this GetType is actually something needs to make it clear that the weak match is being performed
            string weakName = UNQUALIFY_TYPE_NAMES.Replace(typeNameOrFullname, string.Empty);

            Type t = null;
            foreach (Type type in (searchScope ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetExportedTypes2())))
                if (type != null)
                    if (t == null)
                        t = type.Name == typeNameOrFullname
                            ||
                            type.FullName == typeNameOrFullname
                            ||
                            UNQUALIFY_TYPE_NAMES.Replace(type.Name, string.Empty) == weakName
                            ||
                            UNQUALIFY_TYPE_NAMES.Replace(type.FullName ?? string.Empty, string.Empty) == weakName
                                ? type
                                : type.GetProperties().SelectMany(p =>
                                                                      p.PropertyType.GetEnumeratedType() == null
                                                                          ? new[]
                                                                          {
                                                                              p.PropertyType
                                                                          }
                                                                          : new[]
                                                                          {
                                                                              p.PropertyType, p.PropertyType.GetEnumeratedType()
                                                                          })
                                      .FirstOrDefault(propertyType =>
                                                          propertyType.Name == typeNameOrFullname
                                                          ||
                                                          propertyType.FullName == typeNameOrFullname
                                                          ||
                                                          UNQUALIFY_TYPE_NAMES.Replace(propertyType.Name, string.Empty) == weakName
                                                          ||
                                                          UNQUALIFY_TYPE_NAMES.Replace(propertyType.FullName ?? string.Empty, string.Empty) == weakName
                                      );
            return t;
        }

        /// <summary>
        ///     Checks the AppDomain.CurrentDomain.BaseDirectory & it's bin subdirectory for dlls & loads them into memory
        /// </summary>
        public static List<Assembly> LoadBinDlls()
        {
            List<Assembly> a = new List<Assembly>();
            foreach (FileInfo fileInfo in Directory.EnumerateFiles(
                                                       Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "bin")
                                                           ? AppDomain.CurrentDomain.BaseDirectory + "bin"
                                                           : AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                                                   .Select(m => new FileInfo(m)))
                a.Add(AppDomain.CurrentDomain.Load(fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf('.'))));
            return a;
        }
    }
}