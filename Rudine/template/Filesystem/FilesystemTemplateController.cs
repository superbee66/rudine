using System;
using System.IO;
using System.Linq;
using Rudine.Interpreters;
using Rudine.Interpreters.Embeded;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Template.Filesystem
{
    internal class FilesystemTemplateController : ITemplateController
    {
        /// <summary>
        ///     Sets up the form directory if not there. New or not, all read-only file attributes are removed from sub-folders &
        ///     files
        /// </summary>
        static FilesystemTemplateController()
        {
            DirectoryPath = RequestPaths.GetPhysicalApplicationPath("doc");
            //Task.Factory.StartNew(() =>
            //                      {
            new DirectoryInfo(DirectoryPath)
                .mkdir()
                .rAttrib(FileAttributes.NotContentIndexed);
            //});
        }

        public static string DirectoryName
        {
            get { return new DirectoryInfo(DirectoryPath).Name.ToLower(); }
        }

        public static string DirectoryPath { get; }

        public MemoryStream OpenRead(string DocTypeName, string DocRev, string filename)
        {
            string filepath = GetDocDirectoryPath(DocTypeName, filename);
            return File.Exists(filepath) && TopDocRev(DocTypeName) == DocRev
                       ? File.OpenRead(filepath).AsMemoryStream()
                       : null;
        }

        public string TopDocRev(string DocTypeName)
        {
            string DocMd5 = null, DocRev = null;
            DirectoryInfo _DirectoryInfo = new DirectoryInfo(GetDocDirectoryPath(DocTypeName));

            // try the file system first
            if (_DirectoryInfo.Exists)
                ScanContentFolder(_DirectoryInfo, out DocRev, out DocMd5);

            return DocRev;
        }

        public static string GetDocDirectoryPath(string DocTypeName, params string[] subfoldersAndOrFilename)
        {
            return string.Format(
                @"{0}\{1}\{2}",
                DirectoryPath,
                DocTypeName,
                subfoldersAndOrFilename == null
                    ? string.Empty
                    : string.Join(@"\", subfoldersAndOrFilename));
        }

        /// <summary>
        /// </summary>
        /// <param name="_DirectoryInfo"></param>
        /// <param name="TargetDocTypeVer"></param>
        /// <param name="TargetDocMD5"></param>
        /// <returns>DocTypeVer only if the DocTypeName can be interpreted from a same file</returns>
        public static string ScanContentFolder(DirectoryInfo _DirectoryInfo, out string TargetDocTypeVer, out string TargetDocMD5)
        {
            TargetDocTypeVer = string.Empty;
            TargetDocMD5 = FileSystem.calcDirMd5(_DirectoryInfo.FullName);
            string TargetDocTypeName = string.Empty;

            // run through all the text files at the root of the target directory and try to resolve there revision number/string
            foreach (FileInfo filepath in _DirectoryInfo.EnumerateFiles().Where(fileinfo => !fileinfo.Name.Equals(DocRev.SchemaFileName, StringComparison.InvariantCultureIgnoreCase)))
            {
                bool isBinary = FileSystem.isBinary(filepath.FullName);

                try
                {
                    TargetDocTypeVer = isBinary
                                           ? DocInterpreter.Instance.ReadDocRev(File.ReadAllBytes(filepath.FullName))
                                           : DocInterpreter.Instance.ReadDocRev(File.ReadAllText(filepath.FullName));

                    if (!string.IsNullOrWhiteSpace(TargetDocTypeVer))
                        TargetDocTypeName = isBinary
                                                ? DocInterpreter.Instance.ReadDocTypeName(File.ReadAllBytes(filepath.FullName))
                                                : DocInterpreter.Instance.ReadDocTypeName(File.ReadAllText(filepath.FullName));
                } catch (Exception) {}

                if (!string.IsNullOrWhiteSpace(TargetDocTypeName) && !string.IsNullOrWhiteSpace(TargetDocTypeVer))
                    break;

                TargetDocTypeName = string.Empty;
                TargetDocTypeVer = string.Empty;
            }
            return TargetDocTypeName;
        }
    }
}