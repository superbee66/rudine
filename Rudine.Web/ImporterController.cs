using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Rudine.Util;
using Rudine.Util.Xsds;
using Rudine.Web.Properties;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     creates doc/* directories based on IExternalDoc(s) in memory or any loose files in the doc/* directory that are
    ///     valid.
    ///     APP_CODE/*.cs based on the docSchema XSD are created if they don't exist.
    /// </summary>
    public static class ImporterController
    {
        static ImporterController()
        {
            Reflection.LoadBinDlls();

            new DirectoryInfo(DirectoryPath)
                .mkdir()
                .rAttrib(FileAttributes.NotContentIndexed);
        }

        /// <summary>
        ///     Default is resolved ~/doc directory.
        ///     When not operating under a web context, AppDomain.CurrentDomain.BaseDirectory for ~.
        /// </summary>
        internal static string DirectoryPath =>
            RequestPaths.GetPhysicalApplicationPath(Settings.Default.FileSystemTemplateDirectory);

        /// <summary>
        ///     All known interpreters are queried for the extensions they serve
        /// </summary>
        /// <param name="baseDocController"></param>
        /// <returns></returns>
        private static string[] AllTemplateExtensions(BaseDocController baseDocController) =>
            CacheMan.Cache(() => baseDocController
                    .TemplateSources()
                    .Select(templateSource => "." + templateSource.ContentFileExtension)
                    .Distinct()
                    .ToArray(),
                false,
                nameof(AllTemplateExtensions));

        /// <summary>
        ///     creates xsd including only properties from the given type declared by the user. All properties that begin with
        ///     "Rudine*" are excluded.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string CreateSchemaJson(Type type)
        {
            List<CompositeProperty> _CompositePropertyList = new List<CompositeProperty>();

            foreach (PropertyInfo _PropertyInfo in type.GetProperties())
                if (_PropertyInfo.DeclaringType.Assembly != typeof(ImporterController).Assembly)
                    _CompositePropertyList.Add(new CompositeProperty(_PropertyInfo.Name, _PropertyInfo.PropertyType));

            //TODO:Replace with another serializer that can handle loops
            return JsonConvert.SerializeObject(
                _CompositePropertyList,
                _CompositePropertyList.GetType(),
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    Formatting = Formatting.Indented
                });
        }

        private static DocRev CreateTemplate(BaseDocController baseDocController, FileInfo filepath) =>
            baseDocController.CreateTemplate(
                new List<DocRevEntry>
                {
                    new DocRevEntry
                    {
                        Bytes = File.ReadAllBytes(filepath.FullName),
                        ModDate = filepath.LastWriteTimeUtc,
                        Name = Path.GetFileName(filepath.FullName)
                    }
                });

        internal const string EXTERNET_DOC_PROPERTIES_FILE_NAME = "externalDoc.ext";
        private static DocRev CreateTemplate(BaseDocController baseDocController, DirectoryInfo directoryInfo)
        {
            DocRev docRev = new DocRev
            {
                DocURN = new DocURN
                {
                    DocTypeName = directoryInfo.Name
                },
                DocFiles = new List<DocRevEntry>()
            };

            foreach (FileInfo filepath in directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
                docRev.DocFiles.Add(
                    new DocRevEntry
                    {
                        Bytes = File.ReadAllBytes(filepath.FullName),
                        ModDate = filepath.LastWriteTimeUtc,
                        Name = Path.GetFileName(filepath.FullName)
                    });

            return baseDocController.CreateTemplate(docRev.DocFiles, docRev.DocURN.DocTypeName);
        }

        /// <summary>
        ///     Scans the file system (default is ~/Doc/*) for files with extensions that that possibly have interpreters in the
        ///     app
        /// </summary>
        /// <param name="baseDocController"></param>
        /// <returns></returns>
        private static List<DocRev> CreateTemplatesList(BaseDocController baseDocController)
        {
            List<DocRev> l = new List<DocRev>();

            if (Directory.Exists(DirectoryPath))
            {
                // create a "~/doc/[IExternalDoc Implementing Class Name]/myschema.xsd" on the file system
                foreach (var _DocTypeDirectoryInfo in
                    AppDomain
                        .CurrentDomain
                        .GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .Distinct()
                        .Where(type => type.GetInterfaces().Any(i => i == typeof(IExternalDoc)))
                        .Select(type => new
                        {
                            type,
                            DirectoryInfo = new DirectoryInfo(GetDocDirectoryPath(type.Name)).mkdir(),
                            DefinedPropertiesJson = CreateSchemaJson(type)
                        })
                )
                {
                    // always (over)write the xsd as this will always be generated by and for dCForm.Core regardless of the IDocInterpreter that is handling
                    OutputFile(string.Format(@"{0}{1}", _DocTypeDirectoryInfo.DirectoryInfo.FullName, EXTERNET_DOC_PROPERTIES_FILE_NAME), _DocTypeDirectoryInfo.DefinedPropertiesJson);
                }

                foreach (FileInfo _FileInfo in Directory.EnumerateFiles(DirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Select(fileName => new FileInfo(fileName)))
                    if (AllTemplateExtensions(baseDocController)
                        .Any(extension => extension.Equals(_FileInfo.Extension, StringComparison.InvariantCultureIgnoreCase)))
                        l.Add(_FileInfo.Directory.FullName.Equals(DirectoryPath, StringComparison.InvariantCultureIgnoreCase)
                            ? CreateTemplate(baseDocController, _FileInfo)
                            : CreateTemplate(baseDocController, _FileInfo.Directory));
            }

            return l;
        }

        internal static string GetDocDirectoryPath(string DocTypeName, params string[] subfoldersAndOrFilename) => string.Format(
            @"{0}\{1}\{2}",
            DirectoryPath,
            DocTypeName,
            subfoldersAndOrFilename == null
                ? string.Empty
                : string.Join(@"\", subfoldersAndOrFilename));

        private static void OutputFile(string _Filepath, string _Contents)
        {
            if (!File.Exists(_Filepath) || _Contents != File.ReadAllText(_Filepath))
                File.WriteAllText(_Filepath, _Contents);
        }

        /// <summary>
        ///     converts ~/doc/* to DocRev items then submits them
        /// </summary>
        /// <param name="baseDocController"></param>
        /// <returns></returns>
        public static List<DocRev> SyncTemplates(BaseDocController baseDocController)
        {
            List<DocRev> templatesList = CreateTemplatesList(baseDocController);

            string[] _DistinctDocFilesMd5 = templatesList.Select(docrev => docrev.DocFilesMD5).Distinct().ToArray();

            List<LightDoc> _Existing = baseDocController.List(
                new List<string> { nameof(DocRev) },
                null,
                null,
                string.Join(" ", _DistinctDocFilesMd5));


            if (_Existing.Count != templatesList.Count)
                foreach (DocRev _DocRev in templatesList)
                    baseDocController.SubmitDoc(_DocRev, string.Empty);

            return new List<DocRev>();
        }
    }
}