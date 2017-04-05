using System;
using System.IO;

namespace Rudine.Web.Util
{
    internal static class StreamExtensions
    {
        public static byte[] AsBytes(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    _MemoryStream.Write(buffer, 0, read);
                return _MemoryStream.ToArray();
            }
        }

        public static string AsString(this Stream _MemoryStream)
        {
            string s = null;

            if (_MemoryStream != null)
            {
                _MemoryStream.Position = 0;
                using (StreamReader _StreamReader = new StreamReader(_MemoryStream, true))
                    s = _StreamReader.ReadToEnd();
            }

            return s;
        }

        public static bool isBinary(this Stream stream)
        {
            int ch;
            using (StreamReader _StreamReaderream = new StreamReader(stream))
                if (FileSystem.isBinary(_StreamReaderream))
                    return true;
            return false;
        }

        /// <summary>
        ///     stream fork = spork = taco bell table wear. Given a stream with unknown content, we try to detect that content and
        ///     delegate to the appropriate method for processing. A memory stream rewound to position zero will be passed to the
        ///     given delegates for processing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamData"></param>
        /// <param name="bytesProcessor"></param>
        /// <param name="stringProcessor"></param>
        /// <returns></returns>
        public static T Spork<T>(this Stream streamData, Func<byte[], T> bytesProcessor, Func<string, T> stringProcessor)
        {
            //TODO:rethink the spork so we can use out parameters
            using (MemoryStream _MemoryStream = new MemoryStream())
            {
                _MemoryStream.Position = 0;
                streamData.Position = 0;
                streamData.CopyTo(_MemoryStream);
                _MemoryStream.Position = 0;
                using (StreamReader _StreamReader = new StreamReader(_MemoryStream))
                {
                    bool isBinary = FileSystem.isBinary(_StreamReader);
                    _MemoryStream.Position = 0;
                    return !isBinary
                               ? stringProcessor.Invoke(_StreamReader.ReadToEnd())
                               : bytesProcessor.Invoke(_MemoryStream.ToArray());
                }
            }
        }
    }
}