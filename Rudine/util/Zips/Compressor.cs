using System;
using System.IO;
using System.IO.Compression;

namespace Rudine.Util.Zips
{
    internal static class Compressor
    {
        public static byte[] Compress<T>(T obj) where T : class
        {
            if (obj == null)
                return new byte[0];

            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                using (GZipStream _GZipStream = new GZipStream(_MemoryStream, CompressionMode.Compress))
                    RuntimeBinaryFormatter.Formatter.Serialize(_GZipStream, obj);
                return _MemoryStream.ToArray();
            }
        }

        public static string CompressToBase64String(byte[] b, bool urlSafe = true)
        {
            using (MemoryStream _MemoryStreamOut = new MemoryStream())
            {
                using (GZipStream _GZipStream = new GZipStream(_MemoryStreamOut, CompressionMode.Compress))
                using (MemoryStream _MemoryStreamIn = new MemoryStream(b))
                    _MemoryStreamIn.CopyTo(_GZipStream);
                return
                    urlSafe
                        ? Url.EncodeParameter(Convert.ToBase64String(_MemoryStreamOut.ToArray()))
                        : Convert.ToBase64String(_MemoryStreamOut.ToArray());
            }
        }

        public static T Decompress<T>(byte[] b)
        {
            using (MemoryStream s = new MemoryStream(b))
            using (GZipStream gs = new GZipStream(s, CompressionMode.Decompress))
                return (T) RuntimeBinaryFormatter.Formatter.Deserialize(gs);
        }

        public static byte[] DecompressFromBase64String(string s, bool urlSafe = true)
        {
            using (MemoryStream _MemoryStreamIn = new MemoryStream(Convert.FromBase64String(urlSafe ? Url.DecodeParameter(s) : s)))
            using (GZipStream _GZipStream = new GZipStream(_MemoryStreamIn, CompressionMode.Decompress))
            using (MemoryStream _MemoryStreamOut = new MemoryStream())
            {
                _GZipStream.CopyTo(_MemoryStreamOut);
                return _MemoryStreamOut.ToArray();
            }
        }
    }
}