using System;
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

        static ImporterController()
        {
            Reflection.LoadBinDlls();
        }

        private static string[] AllTemplateExtensions(BaseDocController baseDocController) =>
            CacheMan.Cache(() => baseDocController
                               .TemplateSources()
                               .Select(templateSource => "." + templateSource.ContentFileExtension)
                               .Distinct()
                               .ToArray(),
                false,
                nameof(AllTemplateExtensions));

        private static DocRev CreateTemplate(BaseDocController baseDocController, FileInfo fileinfo) =>
          baseDocController.CreateTemplate(
               new List<DocRevEntry> {
                    new DocRevEntry {
                        Bytes = File.ReadAllBytes(fileinfo.FullName),
                        ModDate = fileinfo.LastWriteTimeUtc,
                        Name = fileinfo.FullName.Substring(fileinfo.FullName.Length + 1)
                    }
                });

        private static DocRev CreateTemplate(BaseDocController baseDocController, DirectoryInfo directoryinfo)
        {
            DocRev docRev = new DocRev
            {
                DocURN = new DocURN
                {
                    DocTypeName = directoryinfo.Name
                },
                DocFiles = new List<DocRevEntry>()
            };

            foreach (FileInfo filepath in directoryinfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
                docRev.DocFiles.Add(
                          new DocRevEntry
                          {
                              Bytes = File.ReadAllBytes(filepath.FullName),
                              ModDate = filepath.LastWriteTimeUtc,
                              Name = filepath.FullName.Substring(directoryinfo.FullName.Length + 1)
                          });

            return baseDocController.CreateTemplate(docRev.DocFiles, docRev.DocURN.DocTypeName);
        }

        /// <summary>
        /// converts ~/doc/* to DocRev items then submits them
        /// </summary>
        /// <param name="baseDocController"></param>
        /// <returns></returns>
        public static List<DocRev> SyncTemplates(BaseDocController baseDocController)
        {
            //List<DocRev> templatesList = CreateTemplatesList(baseDocController);
            //string[] DisintctDocFilesMD5 = templatesList.Select(docrev => docrev.DocFilesMD5).Distinct().ToArray();
            //var Existing = baseDocController.List(
            //                             new List<string> { nameof(DocRev) },
            //                             null,
            //                             null,
            //                      string.Join(" ", DisintctDocFilesMD5));

            return new List<Web.DocRev>();

        }

        internal static List<DocRev> CreateTemplatesList(BaseDocController baseDocController)
        {
            List<DocRev> l = new List<DocRev>();
            if (Directory.Exists(FilesystemTemplateController.DirectoryPath))
                foreach (FileInfo fileinfo in Directory.EnumerateFiles(FilesystemTemplateController.DirectoryPath, "*.*", SearchOption.AllDirectories)
                                                       .Select(filepath => new FileInfo(filepath)))
                    if (AllTemplateExtensions(baseDocController)
                        .Any(extension => extension.Equals(fileinfo.Extension, StringComparison.InvariantCultureIgnoreCase)))
                        l.Add(fileinfo.Directory.FullName.Equals(FilesystemTemplateController.DirectoryPath, StringComparison.InvariantCultureIgnoreCase)
                                  ? CreateTemplate(baseDocController, fileinfo)
                                  : CreateTemplate(baseDocController, fileinfo.Directory));
            return l;
        }

        /// <summary>
        ///     converts the DocSchema XSD to a csharp POCO and writes it out to the APP_CODE directory
        /// </summary>
        /// <param name="docTypeName"></param>
        private static void WriteCSharpAPP_CODE(string docTypeName)
        {
            string cSharpCodeFileName = String.Format(@"{0}\{1}.c_", AppCode, docTypeName);

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