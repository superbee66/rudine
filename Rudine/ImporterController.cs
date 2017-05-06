
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Deployment.Compression;
using Microsoft.Deployment.Compression.Cab;
using Newtonsoft.Json;
using Rudine.Exceptions;
using Rudine.Interpreters;
using Rudine.Interpreters.Embeded;

using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Util.Cabs;
using Rudine.Util.Xsds;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine
{
    public static class ImporterController
    {
        public static string DirectoryFullName
        {
            get { return RequestPaths.GetPhysicalApplicationPath("import"); }
        }

        static ImporterController() { Reflection.LoadBinDlls(); }

        private static List<ImporterLightDoc> GetImporterLightDocList(string FullPathOfFile)
        {
            FileInfo _FileInfoLog = new FileInfo(GetLogFilePath(FullPathOfFile));

            return _FileInfoLog.Exists
                       ? JsonConvert.DeserializeObject<List<ImporterLightDoc>>(File.ReadAllText(_FileInfoLog.FullName))
                       : new List<ImporterLightDoc>();
        }

        /// <summary>
        /// </summary>
        /// <param name="TargetImportFile"></param>
        /// <returns></returns>
        private static string GetLogFilePath(string TargetImportFile)
        {
            return string.Format(@"{0}\{1}.import.json", new FileInfo(TargetImportFile).Directory.FullName,
                Environment.GetEnvironmentVariable("computername"));
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

        private static IDictionary<string, string> CreateStringDictionary(IList<string> keys, IList<string> values)
        {
            IDictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            checked
            {
                for (int i = 0; i < keys.Count; i++)
                    dictionary.Add(keys[i], values[i]);
                return dictionary;
            }
        }

        /// <summary>
        ///     Expects the directory to contain an infopath manifest.xsf & template.xml files. The contents are then persisted &
        ///     indexed by DocTypeName & DocTypeRev (aka solutionVersion) for OpenStream & OpenText operations. As of this writing,
        ///     this application must have write access to the parent folder of the given directory for cab compression operations.
        /// </summary>
        /// <param name="importFolderPath"></param>
        /// <param name="workingFolderPath">default is parent of importFolderPath</param>
        public static List<ImporterLightDoc> ImportContentFolder(string sourceFolderPath, string workingFolderPath = null)
        {
            List<ImporterLightDoc> List_ImporterLightDoc = new List<ImporterLightDoc>();

            //DirectoryInfo _DirectoryInfo = new DirectoryInfo(sourceFolderPath);

            //if (workingFolderPath == null)
            //    workingFolderPath = RequestPaths.GetPhysicalApplicationPath("import");

            ////// ensure the import folder actually exists
            //new DirectoryInfo(workingFolderPath)
            //    .mkdir()
            //    .Attributes = FileAttributes.NotContentIndexed | FileAttributes.Hidden;

            //string DocMD5, DocTypeVer;
            //string DocTypeName = FilesystemTemplateController.ScanContentFolder(_DirectoryInfo, out DocTypeVer, out DocMD5);
            //if (!DocExchange.LuceneController.List(new List<string> { DocRev.MY_ONLY_DOC_NAME }, null, null, DocMD5).Any())
            //{
            //    IList<string> relativeFilePathsInDirectoryTree = GetRelativeFilePathsInDirectoryTree(_DirectoryInfo.FullName, true);
            //    IDictionary<string, string> files = CreateStringDictionary(relativeFilePathsInDirectoryTree, relativeFilePathsInDirectoryTree);
            //    IDocRev DocRevBaseDoc = (IDocRev)DocInterpreter.Instance.Create(DocRev.MY_ONLY_DOC_NAME);

            //    DocRevBaseDoc.DocURN.DocTypeName = DocTypeName;
            //    DocRevBaseDoc.DocURN.solutionVersion = DocTypeVer;
            //    DocRevBaseDoc.DocChecksum = int.MinValue;
            //    DocRevBaseDoc.DocStatus = true;
            //    DocRevBaseDoc.DocTitle = String.Format("{0} {1}", DocTypeName, DocTypeVer);
            //    DocRevBaseDoc.DocTypeName = DocRev.MY_ONLY_DOC_NAME;
            //    DocRevBaseDoc.DocKeys = new Dictionary<string, string>
            //        {
            //            { "TargetDocTypeName", DocTypeName },
            //            { "TargetDocTypeVer", DocTypeVer }
            //        };

            //    foreach (KeyValuePair<string, string> file in files)
            //    {

            //        FileInfo AddFileInfo = new FileInfo(_DirectoryInfo.FullName + "\\" + file.Value);
            //        DocRevBaseDoc.FileList.Add(
            //            new DocRevEntry()
            //            {
            //                Bytes = AddFileInfo.OpenRead().AsBytes(),
            //                Name = file.Key
            //            });
            //    }

            //    List_ImporterLightDoc.Add(
            //         new ImporterLightDoc
            //         {
            //             LightDoc = DocExchange.Instance.Import(
            //                 DocInterpreter.Instance.WriteStream((BaseDoc)DocRevBaseDoc))
            //         });
            //}
            return List_ImporterLightDoc;
        }

        /// <summary>
        ///     Scans AppDomain for classes implementing the IDocModel & performs all transformations needed to represent them as
        ///     BaseDoc to be served.
        /// </summary>
        /// <param name="DocTypeName">
        ///     Processes only the given DocTypeName the IDocModel represents. If a IDocModel can not be
        ///     located through out the AppDomain nothing will be processed & no IDocRev will be imported. If no DocTypeName is
        ///     specified all IDocModel located will be processed.
        /// </param>
        public static List<ImporterLightDoc> ReadIDocModelCSharpCode()
        {
            List<ImporterLightDoc> List_ImporterLightDoc = new List<ImporterLightDoc>();

            //TODO:Validate POCO utilizes correct title-case underscore separated labeling practices
            //TODO:add a placeholder file describing what goes in the given DocTypeName's form root directory
            var IDocModelItems = AppDomain
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

            foreach (var docTypeDirectoryInfo in IDocModelItems)
            {
                string filepath = string.Format(@"{0}{1}", docTypeDirectoryInfo.DirectoryInfo.FullName, Runtime.MYSCHEMA_XSD_FILE_NAME);

                // always (over)write the xsd as this will always be generated by and for Rudine.Core regardless of the IDocInterpreter that is handling
                // compare the existing xsd on disk with the one generated here (excluding the "rolling" namespace) to see if anything has changed
                if (
                    !File.Exists(filepath)
                    ||
                    RuntimeTypeNamer.VALID_CSHARP_NAMESPACE_PART_MATCH.Replace(docTypeDirectoryInfo.myschemaXsd, string.Empty) != RuntimeTypeNamer.VALID_CSHARP_NAMESPACE_PART_MATCH.Replace(File.ReadAllText(filepath), string.Empty)
                )
                {
                    File.WriteAllText(string.Format(@"{0}{1}", docTypeDirectoryInfo.DirectoryInfo.FullName, Runtime.MYSCHEMA_XSD_FILE_NAME), docTypeDirectoryInfo.myschemaXsd);
                }

                // create placeholder App_Code\DocTypeName.c_ files for developer to get started with myschema.xsd generation via cSharp file editing & thus auto translating
                string App_Code_Directory_Fullname = RequestPaths.GetPhysicalApplicationPath("App_Code");
                if (Directory.Exists(App_Code_Directory_Fullname))
                    Tasker.StartNewTask(() =>
                                        {
                                            foreach (string DocTypeName in DocExchange.DocTypeDirectories())
                                                if (!IDocModelItems.Any(m => m.DirectoryInfo.Name.Equals(DocTypeName, StringComparison.CurrentCultureIgnoreCase)))
                                                {
                                                    string cSharpCodeFileName = string.Format(@"{0}\{1}.c_", App_Code_Directory_Fullname, DocTypeName);
                                                    string xsdFileName = RequestPaths.GetPhysicalApplicationPath("doc", DocTypeName, Runtime.MYSCHEMA_XSD_FILE_NAME);
                                                    string xsd = File.ReadAllText(xsdFileName);
                                                    string myclasses_cs = new Xsd().ImportSchemasAsClasses(
                                                        new[] { xsd },
                                                        null,
                                                        CodeGenerationOptions.GenerateOrder | CodeGenerationOptions.GenerateProperties,
                                                        new StringCollection());

                                                    if (!File.Exists(cSharpCodeFileName) || File.ReadAllText(cSharpCodeFileName) != myclasses_cs)
                                                    {
                                                        File.WriteAllText(cSharpCodeFileName, myclasses_cs);
                                                        File.SetAttributes(cSharpCodeFileName, FileAttributes.Hidden);
                                                    }
                                                }
                                            return true;
                                        });
            }
            return List_ImporterLightDoc;
        }

        private static readonly ConcurrentStack<string> dirs = new ConcurrentStack<string>();

        /// <summary>
        ///     Intended time of use it that of a "post cache reset" time. If cache has been cleared, all the logic behind this
        ///     method will run.
        /// </summary>
        public static void TryDocRevImporting() =>
            CacheMan.Cache(() =>
                           {
                               ReadIDocModelCSharpCode();

                               dirs.PushRange(Directory
                                   .EnumerateDirectories(FilesystemTemplateController.DirectoryPath)
                                   .Select(dirpath => new DirectoryInfo(dirpath).FullName)
                                   .Where(dirpath => !dirs.Contains(dirpath))
                                   .OrderByDescending(dirpath =>
                                                          new[]
                                                              {
                                                                  Directory.GetLastWriteTime(dirpath).Ticks
                                                              }
                                                              .Union(
                                                                  GetRelativeFilePathsInDirectoryTree(dirpath, true)
                                                                      .Select(filepath => File.GetLastWriteTimeUtc(filepath).Ticks))
                                                              .Max())
                                   .ToArray());

                               // starting with the newest directory, synchronously process each & abend when its found that nothing is imported
                               string dir;
                               while (dirs.TryPop(out dir) && ImportContentFolder(dir).Any())
                               { }

                               // process the remaining directories queued asynchronously (as this is a resource intensive) on the chance that the "GetLastWriteTimeUtc" lied to us
                               if (!dirs.IsEmpty)
                                   Tasker.StartNewTask(() =>
                                                       {
                                                           while (dirs.TryPop(out dir))
                                                               ImportContentFolder(dir);
                                                           return true;
                                                       });

                               return new object();
                           },
                false,
                "ImportDocModelsRunOnce"
            );

        /// <summary>
        ///     Enumerates infopath format files (*.xml) in the /import folder. At this time only Infopath formats are processed.
        ///     Ordered for processing; docrevs first by solutionversion then other doctypenames also by solution version
        /// </summary>
        /// <returns></returns>
        public static List<ImporterLightDoc> ProcessImportDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DirectoryFullName))
                lock (DirectoryFullName)
                {
                    if (!Directory.Exists(DirectoryFullName))
                        return new List<ImporterLightDoc>();

                    //TODO:Use yet to be made "Capabilities()" methods to see what extensions & interprester will be used
                    return Directory
                        .EnumerateFiles(DirectoryFullName, "*.xml")
                        .Select(path => new
                        {
                            path,
                            PI = DocExchange.Instance.ReadStream(File.OpenRead(path))
                        })
                        .Where(file => !string.IsNullOrWhiteSpace(file.PI.DocTypeName))
                        .OrderBy(file => !file.PI.DocTypeName.Equals(DocRev.MY_ONLY_DOC_NAME, StringComparison.CurrentCultureIgnoreCase))
                        .ThenBy(file => file.PI.DocTypeName)
                        .ThenBy(file => Version.Parse(file.PI.solutionVersion))
                        .ToArray()
                        .Select(file =>
                                {
                                    List<ImporterLightDoc> List_LightDoc = GetImporterLightDocList(file.path);
                                    ImporterLightDoc _LightDoc = List_LightDoc.FirstOrDefault(m => m.ImportDocSrc == file.path);

                                    if (_LightDoc == null || (!string.IsNullOrWhiteSpace(_LightDoc.ExceptionMessage) && string.Format("{0}", _LightDoc.ExceptionMessage).IndexOf("skipped") == -1))
                                        try
                                        {
                                            List_LightDoc.Remove(_LightDoc);
                                            using (Stream _Stream = File.OpenRead(file.path))
                                                _LightDoc = new ImporterLightDoc
                                                {
                                                    LightDoc = DocExchange.Instance.Import(_Stream)
                                                };
                                        }
                                        catch (Exception ex)
                                        {
                                            _LightDoc = new ImporterLightDoc
                                            {
                                                LightDoc = new LightDoc
                                                {
                                                    DocSubmitDate = DateTime.Now
                                                },
                                                ExceptionMessage = ex.Message + "\n" + file.path + "\n"
                                            };
                                            if (ex.InnerException != null)
                                                _LightDoc.ExceptionMessage = string.Format("{0} {1} {3}", _LightDoc.ExceptionMessage, ex.InnerException.Message, ex.StackTrace);
                                        }
                                        finally
                                        {
                                            _LightDoc.ImportDocSrc = file.path;

                                            List_LightDoc.Add(_LightDoc);
                                            SaveImporterLightDocList(List_LightDoc);
                                        }

                                    return _LightDoc;
                                })
                        .Where(o => o != null)
                        .ToList();
                }

            return new List<ImporterLightDoc>();
        }

        /// <summary>
        ///     Persists a log file(s) to the directories ImportLightDoc.ImportDocSrc originated from
        /// </summary>
        /// <param name="List_ImporterLightDoc"></param>
        public static void SaveImporterLightDocList(List<ImporterLightDoc> List_ImporterLightDoc)
        {
            foreach (string FilePath in List_ImporterLightDoc.Select(m => GetLogFilePath(m.ImportDocSrc)).Distinct())
                File.WriteAllText(
                    FilePath,
                    JsonConvert.SerializeObject(
                        List_ImporterLightDoc.Where(m => GetLogFilePath(m.ImportDocSrc) == FilePath).ToList(),
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        }));
        }
    }
}