using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Deployment.Compression;

namespace Rudine.Interpreters.Xsn.util.Cabs
{
    internal class ArchiveMemoryStreamContext : ArchiveFileStreamContext, IDisposable
    {
        private Dictionary<string, MemoryStream> _DictionaryStringMemoryStream = new Dictionary<string, MemoryStream>();

        public ArchiveMemoryStreamContext(string archiveFile, string directory, IDictionary<string, string> files)
            : base(archiveFile, directory, files) { }

        public Dictionary<string, MemoryStream> DictionaryStringMemoryStream
        {
            get { return _DictionaryStringMemoryStream; }
            set { _DictionaryStringMemoryStream = value; }
        }

        public void Dispose()
        {
            foreach (MemoryStream _MemoryStream in DictionaryStringMemoryStream.Values)
                _MemoryStream.Dispose();
        }
    }
}