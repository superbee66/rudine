using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Rudine.Web.Util
{
    internal static class FileSystem
    {
        public static string calcDirMd5(string srcPath)
        {
            string[] filePaths = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            using (MD5 md5 = MD5.Create())
            {
                foreach (string filePath in filePaths)
                {
                    // hash path
                    byte[] pathBytes = Encoding.UTF8.GetBytes(filePath);
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes(filePath);

                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                //Handles empty filePaths case
                md5.TransformFinalBlock(new byte[0], 0, 0);

                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        ///     Attempt to get a list of security permissions from the folder. This will raise an exception if the path is read
        ///     only or do not have access to view the permissions.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="exceptionSuppressed">
        ///     default behavior throws an (UnauthorizedAccess)Exception messaging the directory's
        ///     fullpathname and the current WindowsIdentity
        /// </param>
        /// <returns></returns>
        public static bool canwrite(this DirectoryInfo directory, bool exceptionSuppressed = false)
        {
            try { Directory.GetAccessControl(directory.FullName); } catch (Exception ex)
            {
                if (exceptionSuppressed)
                    return false;

                string msg = string.Format("{0} not writable by {2}, verify directory attributes & permissions", directory.FullName, WindowsIdentity.GetCurrent().Name);

                throw ex is UnauthorizedAccessException
                          ? new UnauthorizedAccessException(msg, ex)
                          : new Exception(msg, ex);
            }

            return true;
        }

        public static DirectoryInfo mkdir(this DirectoryInfo directory, bool ensureWrittable = true)
        {
            if (directory.Parent != null)
                directory.Parent.mkdir(false);

            if (!directory.Exists)
                if (directory.Parent.canwrite())
                    directory.Create();

            if (ensureWrittable)
                directory.Parent.canwrite();

            return directory;
        }

        public static void rAttrib(this DirectoryInfo directory, FileAttributes fileattributes)
        {
            if (directory.Exists)
            {
                foreach (DirectoryInfo _DirectoryInfo in directory.EnumerateDirectories())
                    _DirectoryInfo.rAttrib(fileattributes);

                foreach (FileInfo _FileInfo in directory.EnumerateFiles())
                    if (_FileInfo.Exists)
                        _FileInfo.Attributes = fileattributes;

                directory.Attributes = fileattributes;
            }
        }

        /// <summary>
        ///     Removes (deletes) a directory. Removes all directories and files in the specified directory in addition to the
        ///     directory itself.  Used to remove a directory tree.
        /// </summary>
        /// <param name="directory"></param>
        public static void rmdir(this DirectoryInfo directory)
        {
            if (directory.Exists)
            {
                foreach (DirectoryInfo _DirectoryInfo in directory.EnumerateDirectories())
                    _DirectoryInfo.rmdir();

                foreach (FileInfo _FileInfo in directory.EnumerateFiles())
                    if (_FileInfo.Exists)
                        _FileInfo.Delete();

                directory.Delete(true);
            }
        }

        /// <summary>
        ///     Removes (deletes) a directory. Removes all directories and files in the specified directory in addition to the
        ///     directory itself.  Used to remove a directory tree.
        /// </summary>
        /// <param name="directory"></param>
        public static void cleardir(this DirectoryInfo directory)
        {
            if (directory.Exists)
            {
                foreach (DirectoryInfo _DirectoryInfo in directory.EnumerateDirectories())
                    _DirectoryInfo.rmdir();

                foreach (FileInfo _FileInfo in directory.EnumerateFiles())
                    if (_FileInfo.Exists)
                        _FileInfo.Delete();
            }
        }

        public static string CleanFileName(string FileName)
        {
            return Regex.Replace(
                FileName.Replace("/", "-")
                        .Replace("\\", "-"),
                @"[^\w_\-.{}()~=,]+",
                " ").Trim();
        }

        public static bool isBinary(string path)
        {
            int ch;
            using (StreamReader stream = new StreamReader(path))
                if (isBinary(stream))
                    return true;
            return false;
        }

        public static bool isBinary(StreamReader stream)
        {
            int ch;
            while ((ch = stream.Read()) != -1)
                if (isControlChar(ch))
                    return true;
            return false;
        }

        private static bool isControlChar(int ch)
        {
            return (ch > Chars.NUL && ch < Chars.BS)
                   || (ch > Chars.CR && ch < Chars.SUB);
        }

        public static string ParseFileName(string path)
        {
            Uri oUri = new Uri(path);
            return oUri.Segments[oUri.Segments.Length - 1];
        }

        private static class Chars
        {
            public static readonly char NUL = (char) 0; // Null char
            public static readonly char BS = (char) 8; // Back Space
            public static readonly char CR = (char) 13; // Carriage Return
            public static readonly char SUB = (char) 26; // Substitute
        }

        public static MemoryStream AsMemoryStream(this FileStream _FileStream)
        {
            MemoryStream _MemoryStream = new MemoryStream();
            _FileStream.Position = 0;
            _FileStream.CopyTo(_MemoryStream);
            return _MemoryStream;
        }
    }
}