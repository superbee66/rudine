//using System;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;

//namespace Rudine.Format.XsnXml
//{
//    /// <summary>
//    ///     InfoPathAttachment encodes file data into the format expected by InfoPath for use in file attachment nodes.
//    /// </summary>
//    internal class InfoPathAttachmentEncoder
//    {
//        private readonly string fullyQualifiedFileName;
//        private string base64EncodedFile = string.Empty;

//        /// <summary>
//        ///     Creates an encoder to create an InfoPath attachment string.
//        /// </summary>
//        /// <param name="fullyQualifiedFileName"></param>
//        public InfoPathAttachmentEncoder(string fullyQualifiedFileName)
//        {
//            if (fullyQualifiedFileName == string.Empty)
//                throw new ArgumentException("Must specify file name", "fullyQualifiedFileName");

//            if (!File.Exists(fullyQualifiedFileName))
//                throw new FileNotFoundException("File does not exist: " + fullyQualifiedFileName, fullyQualifiedFileName);

//            this.fullyQualifiedFileName = fullyQualifiedFileName;
//        }

//        /// <summary>
//        ///     Returns a Base64 encoded string.
//        /// </summary>
//        /// <returns>String</returns>
//        public string ToBase64String()
//        {
//            if (base64EncodedFile != string.Empty)
//                return base64EncodedFile;

//            // This memory stream will hold the InfoPath file attachment buffer before Base64 encoding.
//            MemoryStream ms = new MemoryStream();

//            // Get the file information.
//            using (BinaryReader br = new BinaryReader(File.Open(fullyQualifiedFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
//            {
//                string fileName = Path.GetFileName(fullyQualifiedFileName);

//                uint fileNameLength = (uint) fileName.Length + 1;

//                byte[] fileNameBytes = Encoding.Unicode.GetBytes(fileName);

//                using (BinaryWriter bw = new BinaryWriter(ms))
//                {
//                    // Write the InfoPath attachment signature. 
//                    bw.Write(new byte[]
//                    {
//                        0xC7, 0x49, 0x46, 0x41
//                    });

//                    // Write the default header information.
//                    bw.Write((uint) 0x14); // size
//                    bw.Write((uint) 0x01); // version
//                    bw.Write((uint) 0x00); // reserved

//                    // Write the file size.
//                    bw.Write((uint) br.BaseStream.Length);

//                    // Write the size of the file name.
//                    bw.Write(fileNameLength);

//                    // Write the file name (Unicode encoded).
//                    bw.Write(fileNameBytes);

//                    // Write the file name terminator. This is two nulls in Unicode.
//                    bw.Write(new byte[]
//                    {
//                        0, 0
//                    });

//                    // Iterate through the file reading data and writing it to the outbuffer.
//                    byte[] data = new byte[64*1024];
//                    int bytesRead = 1;

//                    while (bytesRead > 0)
//                    {
//                        bytesRead = br.Read(data, 0, data.Length);
//                        bw.Write(data, 0, bytesRead);
//                    }
//                }
//            }

//            // This memorystream will hold the Base64 encoded InfoPath attachment.
//            MemoryStream msOut = new MemoryStream();

//            using (BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray())))
//            {
//                // Create a Base64 transform to do the encoding.
//                ToBase64Transform tf = new ToBase64Transform();

//                byte[] data = new byte[tf.InputBlockSize];
//                byte[] outData = new byte[tf.OutputBlockSize];

//                int bytesRead = 1;

//                while (bytesRead > 0)
//                {
//                    bytesRead = br.Read(data, 0, data.Length);

//                    if (bytesRead == data.Length)
//                        tf.TransformBlock(data, 0, bytesRead, outData, 0);
//                    else
//                        outData = tf.TransformFinalBlock(data, 0, bytesRead);

//                    msOut.Write(outData, 0, outData.Length);
//                }
//            }

//            msOut.Close();

//            return base64EncodedFile = Encoding.ASCII.GetString(msOut.ToArray());
//        }

//        /// <summary>
//        ///     Returns a Base64 encoded string.
//        /// </summary>
//        /// <returns>String</returns>
//        public byte[] ToBytes()
//        {
//            // This memory stream will hold the InfoPath file attachment buffer before Base64 encoding.
//            MemoryStream ms = new MemoryStream();

//            // Get the file information.
//            using (BinaryReader br = new BinaryReader(File.Open(fullyQualifiedFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
//            {
//                string fileName = Path.GetFileName(fullyQualifiedFileName);

//                uint fileNameLength = (uint) fileName.Length + 1;

//                byte[] fileNameBytes = Encoding.Unicode.GetBytes(fileName);

//                using (BinaryWriter bw = new BinaryWriter(ms))
//                {
//                    // Write the InfoPath attachment signature. 
//                    bw.Write(new byte[]
//                    {
//                        0xC7, 0x49, 0x46, 0x41
//                    });

//                    // Write the default header information.
//                    bw.Write((uint) 0x14); // size
//                    bw.Write((uint) 0x01); // version
//                    bw.Write((uint) 0x00); // reserved

//                    // Write the file size.
//                    bw.Write((uint) br.BaseStream.Length);

//                    // Write the size of the file name.
//                    bw.Write(fileNameLength);

//                    // Write the file name (Unicode encoded).
//                    bw.Write(fileNameBytes);

//                    // Write the file name terminator. This is two nulls in Unicode.
//                    bw.Write(new byte[]
//                    {
//                        0, 0
//                    });

//                    // Iterate through the file reading data and writing it to the outbuffer.
//                    byte[] data = new byte[64*1024];
//                    int bytesRead = 1;

//                    while (bytesRead > 0)
//                    {
//                        bytesRead = br.Read(data, 0, data.Length);
//                        bw.Write(data, 0, bytesRead);
//                    }
//                }
//            }

//            // This memory stream will hold the Base64 encoded InfoPath attachment.
//            MemoryStream msOut = new MemoryStream();

//            using (BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray())))
//            {
//                // Create a Base64 transform to do the encoding.
//                ToBase64Transform tf = new ToBase64Transform();

//                byte[] data = new byte[tf.InputBlockSize];
//                byte[] outData = new byte[tf.OutputBlockSize];

//                int bytesRead = 1;

//                while (bytesRead > 0)
//                {
//                    bytesRead = br.Read(data, 0, data.Length);

//                    if (bytesRead == data.Length)
//                        tf.TransformBlock(data, 0, bytesRead, outData, 0);
//                    else
//                        outData = tf.TransformFinalBlock(data, 0, bytesRead);

//                    msOut.Write(outData, 0, outData.Length);
//                }
//            }

//            msOut.Close();

//            return msOut.ToArray();
//        }
//    }
//}

