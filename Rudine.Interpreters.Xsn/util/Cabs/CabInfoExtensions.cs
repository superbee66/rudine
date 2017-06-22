using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Deployment.Compression;
using Microsoft.Deployment.Compression.Cab;

namespace Rudine.Interpreters.Xsn.util.Cabs
{
    internal static class CabInfoExtensions
    {
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

        private static IList<string> GetRelativeFilePathsInDirectoryTree(string dir, bool includeSubdirectories)
        {
            IList<string> list = new List<string>();
            RecursiveGetRelativeFilePathsInDirectoryTree(dir, string.Empty, includeSubdirectories, list);
            return list;
        }

        public static void Pack(this ArchiveInfo o, string sourceDirectory, IPackStreamContext ipackstreamcontext) =>
            o.Pack(sourceDirectory, false, CompressionLevel.Max, null);

        public static void Pack(this ArchiveInfo o, string sourceDirectory, bool includeSubdirectories, CompressionLevel compLevel, EventHandler<ArchiveProgressEventArgs> progressHandler, IPackStreamContext ipackstreamcontext)
        {
            IList<string> relativeFilePathsInDirectoryTree = GetRelativeFilePathsInDirectoryTree(sourceDirectory, includeSubdirectories);
            o.PackFiles(sourceDirectory, relativeFilePathsInDirectoryTree, relativeFilePathsInDirectoryTree, compLevel, progressHandler);
        }

        public static void Pack(this ArchiveInfo o, string sourceDirectory, IList<string> sourceFileNames, IList<string> fileNames, IPackStreamContext ipackstreamcontext) { o.PackFiles(sourceDirectory, sourceFileNames, fileNames, CompressionLevel.Max, null); }

        public static void Pack(this ArchiveInfo o, string sourceDirectory, IList<string> sourceFileNames, IList<string> fileNames, CompressionLevel compLevel, EventHandler<ArchiveProgressEventArgs> progressHandler, IPackStreamContext ipackstreamcontext)
        {
            if (sourceFileNames == null)
                throw new ArgumentNullException("sourceFileNames");
            checked
            {
                if (fileNames == null)
                {
                    string[] array = new string[sourceFileNames.Count];
                    for (int i = 0; i < sourceFileNames.Count; i++)
                        array[i] = Path.GetFileName(sourceFileNames[i]);
                    fileNames = array;
                } else if (fileNames.Count != sourceFileNames.Count)
                    throw new ArgumentOutOfRangeException("fileNames");
                using (CompressionEngine compressionEngine = new CabEngine())
                {
                    compressionEngine.Progress += progressHandler;
                    IDictionary<string, string> files = CreateStringDictionary(fileNames, sourceFileNames);
                    ArchiveFileStreamContext archiveFileStreamContext = new ArchiveFileStreamContext(o.FullName, sourceDirectory, files);
                    archiveFileStreamContext.EnableOffsetOpen = true;
                    compressionEngine.CompressionLevel = compLevel;
                    compressionEngine.Pack(archiveFileStreamContext, fileNames);
                }
            }
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
    }
}