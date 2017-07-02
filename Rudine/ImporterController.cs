using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Util.Xsds;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine
{
    /// <summary>
    ///     creates doc/* directories based on IDocModel(s) in memory or any loose files in the doc/* directory that are valid.
    ///     APP_CODE/*.cs based on the docSchema XSD are created if they don't exist.
    /// </summary>
    public static class ImporterController
    {
        static readonly string AppCode = RequestPaths.GetPhysicalApplicationPath("App_Code");
        static readonly DirectoryInfo APP_DOCS = new DirectoryInfo(RequestPaths.GetPhysicalApplicationPath("doc"));
        private static readonly ConcurrentStack<DirectoryInfo> DOCS = new ConcurrentStack<DirectoryInfo>();

        static ImporterController() { Reflection.LoadBinDlls(); }

        /// <summary>
        ///     Intended time of use it that of a "post cache reset" time. If cache has been cleared, all the logic behind this
        ///     method will run.
        /// </summary>
        public static void CreateTemplateItems(BaseDocController baseDocController)
        {
            DocsFromIDocModels();

            DocsFromFiles(baseDocController);

            // scan directories to get ones that have potential files in them to import
            DirectoryInfo[] range = Directory
                .EnumerateDirectories(FilesystemTemplateController.DirectoryPath)
                .Select(dirpath => new DirectoryInfo(dirpath))
                .Where(dirpath =>
                           !DOCS.Contains(dirpath)
                           &&
                           dirpath
                               .EnumerateFiles()
                               .Any(fileinfo =>
                                        AllTemplateExtensions(baseDocController)
                                            .Any(extension =>
                                                     extension.Equals(fileinfo.Extension.Trim('.'), StringComparison.InvariantCultureIgnoreCase))))
                .OrderByDescending(dirpath =>
                                       new[] { dirpath.LastWriteTime.Ticks }
                                           .Union(
                                               GetRelativeFilePathsInDirectoryTree(dirpath.FullName, true)
                                                   .Select(filepath => File.GetLastWriteTimeUtc(filepath).Ticks))
                                           .Max())
                .ToArray();

            if (range.Length > 0)
                DOCS.PushRange(range);

            // starting with the newest directory, synchronously process each & abend when its found that nothing is imported
            DirectoryInfo dir;
            while (DOCS.TryPop(out dir) && ImportContentFolder(baseDocController, dir) != null)
            { }

            // process the remaining directories queued asynchronously (as this is a resource intensive) on the chance that the "GetLastWriteTimeUtc" lied to us
            if (!DOCS.IsEmpty)
                Tasker.StartNewTask(() =>
                                    {
                                        while (DOCS.TryPop(out dir))
                                            ImportContentFolder(baseDocController, dir);
                                        return true;
                                    });
        }

        /// <summary>
        ///     move any loose files taht qualify as TemplateSources items under the doc/* folder, creating there own folder in the
        ///     process
        /// </summary>
        /// <param name="baseDocController"></param>
        private static void DocsFromFiles(BaseDocController baseDocController)
        {
            foreach (FileInfo fileinfo in Directory.EnumerateFiles(APP_DOCS.FullName, "*", SearchOption.TopDirectoryOnly).Select(filepath => new FileInfo(filepath)))
                if (AllTemplateExtensions(baseDocController).Any(extension => extension.Equals(fileinfo.Extension, StringComparison.InvariantCultureIgnoreCase)))
                    ImportContentFiles(
                        baseDocController,
                        fileinfo.Directory,
                        StringTransform.PrettyCSharpIdent(Path.GetFileNameWithoutExtension(fileinfo.FullName)),
                        new List<FileInfo> { fileinfo });
        }

        private static string[] AllTemplateExtensions(BaseDocController baseDocController) =>
            CacheMan.Cache(() => baseDocController
                               .TemplateSources()
                               .Select(templateSource => "." + templateSource.ContentFileExtension)
                               .Distinct()
                               .ToArray(),
                false,
                nameof(AllTemplateExtensions));

        /// <summary>
        ///     Scans AppDomain for classes implementing the IDocModel & performs all transformations needed to represent them as
        ///     BaseDoc to be served.
        /// </summary>
        private static void DocsFromIDocModels()
        {
            //TODO:Validate POCO utilizes correct title-case underscore separated labeling practices
            //TODO:add a placeholder file describing what goes in the given DocTypeName's form root directory
            var docModelItems = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Distinct()
                .Where(typ => (typ.GetInterfaces().Any(i => i == typeof(IDocModel))))
                .Select(type => new
                {
                    type,
                    DirectoryInfo = new DirectoryInfo(FilesystemTemplateController.GetDocDirectoryPath(type.Name)).mkdir(),
                    myschemaXsd = XsdExporter.ExportSchemas(
                        type.Assembly,
                        new List<string> { type.Name },
                        RuntimeTypeNamer.CalcSchemaUri(type.Name)).First()
                });

            foreach (var docTypeDirectoryInfo in docModelItems)
            {
                string filepath = string.Format(@"{0}{1}", docTypeDirectoryInfo.DirectoryInfo.FullName, DocRev.SchemaFileName);

                // always (over)write the xsd as this will always be generated by and for Rudine.Core regardless of the IDocInterpreter that is handling
                // compare the existing xsd on disk with the one generated here (excluding the "rolling" namespace) to see if anything has changed
                if (
                    !File.Exists(filepath)
                    ||
                    RuntimeTypeNamer.VALID_CSHARP_NAMESPACE_PART_MATCH.Replace(docTypeDirectoryInfo.myschemaXsd, string.Empty) != RuntimeTypeNamer.VALID_CSHARP_NAMESPACE_PART_MATCH.Replace(File.ReadAllText(filepath), string.Empty)
                )
                {
                    File.WriteAllText(string.Format(@"{0}{1}", docTypeDirectoryInfo.DirectoryInfo.FullName, DocRev.SchemaFileName), docTypeDirectoryInfo.myschemaXsd);
                }

                // create placeholder App_Code\DocTypeName.c_ files for developer to get started with myschema.xsd generation via cSharp file editing & thus auto translating
                Tasker.StartNewTask(() =>
                                    {
                                        foreach (string docTypeName in DocExchange.DocTypeDirectories())
                                            if (!docModelItems.Any(m => m.DirectoryInfo.Name.Equals(docTypeName, StringComparison.CurrentCultureIgnoreCase)))
                                                WriteCSharpAPP_CODE(docTypeName);
                                        return true;
                                    });
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="includeSubdirectories">when true, in dos/cmd it would like look dir/b/s/a-d</param>
        /// <returns></returns>
        private static IList<string> GetRelativeFilePathsInDirectoryTree(string dir, bool includeSubdirectories)
        {
            IList<string> list = new List<string>();
            RecursiveGetRelativeFilePathsInDirectoryTree(dir, string.Empty, includeSubdirectories, list);
            return list;
        }

        /// <summary>
        ///     Expects the directory to contain an infopath manifest.xsf & template.xml files. The contents are then persisted &
        ///     indexed by DocTypeName & DocTypeRev (aka solutionVersion) for OpenStream & OpenText operations. As of this writing,
        ///     this application must have write access to the parent folder of the given directory for cab compression operations.
        /// </summary>
        /// <param name="importFolderPath"></param>
        /// <param name="workingFolderPath">default is parent of importFolderPath</param>
        public static DocRev ImportContentFolder(BaseDocController baseDocController, DirectoryInfo sourceFolderPath) =>
            ImportContentFiles(
                baseDocController,
                sourceFolderPath,
                Path.GetFileNameWithoutExtension(sourceFolderPath.Name),
                sourceFolderPath.EnumerateFiles("*.*", SearchOption.AllDirectories).ToList());


        public static DocRev ImportContentFiles(BaseDocController baseDocController, DirectoryInfo sourceFolderPath, string DocTypeName, List<FileInfo> sourceFiles)
        {

            DocRev docRev = new DocRev
            {
                DocURN = new DocURN
                {
                    DocTypeName = DocTypeName
                },
                DocFiles = new List<DocRevEntry>()
            };

            foreach (FileInfo filepath in sourceFiles)
                docRev.DocFiles.Add(
                    new DocRevEntry
                    {
                        Bytes = File.ReadAllBytes(filepath.FullName),
                        ModDate = filepath.LastWriteTimeUtc,
                        Name = filepath.FullName.Substring(sourceFolderPath.FullName.Length + 1)
                    });

            docRev = baseDocController.List(
                         new List<string> { nameof(DocRev) },
                         null,
                         null,
                         docRev.DocFilesMD5).Count == 0
                         ? baseDocController.CreateTemplate(docRev.DocFiles, docRev.DocTypeName)
                         : null;

            return docRev;
        }



        private static void RecursiveGetRelativeFilePathsInDirectoryTree(string dir, string relativeDir, bool includeSubdirectories, IList<string> fileList)
        {
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string fileName = Path.GetFileName(path);
                fileList.Add(Path.Combine(relativeDir, fileName));
            }
            if (includeSubdirectories)
            {
                string[] directories = Directory.GetDirectories(dir);
                for (int j = 0; j < directories.Length; j++)
                {
                    string path2 = directories[j];
                    string fileName2 = Path.GetFileName(path2);
                    RecursiveGetRelativeFilePathsInDirectoryTree(Path.Combine(dir, fileName2), Path.Combine(relativeDir, fileName2), includeSubdirectories, fileList);
                }
            }
        }

        /// <summary>
        ///     converts the DocSchema XSD to a csharp POCO and writes it out to the APP_CODE directory
        /// </summary>
        /// <param name="docTypeName"></param>
        private static void WriteCSharpAPP_CODE(string docTypeName)
        {
            string cSharpCodeFileName = string.Format(@"{0}\{1}.c_", AppCode, docTypeName);

            string xsd = File.ReadAllText(
                RequestPaths.GetPhysicalApplicationPath(
                    "doc",
                    docTypeName, DocRev.SchemaFileName));

            string myclassesCs = new Xsd().ImportSchemasAsClasses(
                new[] { xsd },
                null,
                CodeGenerationOptions.GenerateOrder | CodeGenerationOptions.GenerateProperties,
                new StringCollection());

            if (!File.Exists(cSharpCodeFileName) || File.ReadAllText(cSharpCodeFileName) != myclassesCs)
            {
                File.WriteAllText(cSharpCodeFileName, myclassesCs);
                File.SetAttributes(cSharpCodeFileName, FileAttributes.Hidden);
            }
        }
    }
}