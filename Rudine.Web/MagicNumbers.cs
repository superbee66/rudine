using System.IO;
using System.Runtime.Serialization;
using System.Linq;

namespace Rudine.Web
{
    [DataContract]
    public class MagicNumbers
    {

        [DataMember]
        public byte[] Bytes { get; set; }

        [DataMember]
        public int Offset { get; set; }

        /// <summary>
        ///     checks first 4 bytes of stream against magicNumbers to see if we are dealing with a solid non-empty archive
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="offset"></param>
        /// <param name="magicBytes"></param>
        /// <returns></returns>
        public bool IsMagic(MemoryStream memoryStream)
        {
            byte[] streamBytes = null;

            if (memoryStream != null)
            {
                long Position = memoryStream.Position;
                streamBytes = new byte[Bytes.Length];
                memoryStream.Read(streamBytes, Offset, Bytes.Length);
                memoryStream.Position = Position;
            }

            return streamBytes != null && IsMagic(streamBytes);
        }

        public bool IsMagic(byte[] docData) =>
            !Bytes.Where((t, i) => docData[i + Offset] != t).Any();
    }
}