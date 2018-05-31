using System.IO;

namespace Rudine.Util
{
    /// <summary>
    ///     Hosts a routine to convert a simple .Net string to a System.IO.MemoryStream
    /// </summary>
    internal static class StringExtensions
    {
        public static MemoryStream AsMemoryStream(this string String)
        {
            MemoryStream memoryStream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream)
            {
                AutoFlush = true
            };
            streamWriter.Write(String);
            streamWriter.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}