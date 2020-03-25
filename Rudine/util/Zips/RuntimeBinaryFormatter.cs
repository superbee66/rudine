using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Rudine.Web;

namespace Rudine.Util.Zips
{
    /// <summary>
    ///     Binary operations with custom SerializationBinder to look all over the place to find the right dlls if needed. This
    ///     was moved out of the utility section as it meets specific needs to this solution that does many runtime assembly
    ///     loading & changing namespaces over time.
    /// </summary>
    internal static class RuntimeBinaryFormatter
    {
        public static readonly BinaryFormatter
            Formatter = new BinaryFormatter { Binder = new BinaryDeserializationBinder() },
            CloneFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone)) { Binder = new BinaryDeserializationBinder() };

        internal static T Clone<T>(this T o)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                CloneFormatter.Serialize(memoryStream, o);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)CloneFormatter.Deserialize(memoryStream);
            }
        }

        internal static T FromBytes<T>(this byte[] b)
        {
            using (MemoryStream memoryStream = new MemoryStream(b))
                return (T)Formatter.Deserialize(memoryStream);
        }

        internal static byte[] ToBytes(this object o)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                //TODO:have a closer look at this as this is questionable
                Formatter.Serialize(memoryStream, o);
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        private static readonly Dictionary<string, Type> BinaryDeserializationTypeDictionary = new Dictionary<string, Type>();

        /// <summary>
        /// resolves baseDoc types that may need to be loaded into memory at runtime
        /// </summary>
        private class BinaryDeserializationBinder : SerializationBinder
        {

            public override Type BindToType(string assemblyName, string typeFullname)
            {
                if (!BinaryDeserializationTypeDictionary.ContainsKey(typeFullname))
                {

                    Type t = Reflection.GetType(typeFullname, assemblyName: assemblyName)
                             ?? Type.GetType(typeFullname);

                    if (t == null)
                    {
                        string docTypeName, docRev;
                        if (RuntimeTypeNamer.TryParseDocNameAndRev(typeFullname, out docTypeName, out docRev))
                        {
                            // if a DocTypeName & Rev can be parsed then we have narrowed our scope of search as the typeFullname will be in some nested type of the given document
                            // ActivateBaseDoc is also loading all those types into memory if there not present now
                            BaseDoc baseDoc = Runtime.ActivateBaseDoc(docTypeName, docRev, DocExchange.Instance);
                            t = Reflection.GetType(
                                typeFullname,
                                baseDoc.ListDeps().Union(new[]
                                        {
                                        baseDoc.GetType()
                                        })
                                        .ToArray());
                        }
                    }
                    if (t == null)
                        throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0} can't be resolved for binary serialization", typeFullname));

                    BinaryDeserializationTypeDictionary[typeFullname] = t;
                }

                return BinaryDeserializationTypeDictionary[typeFullname];
            }
        }
    }
}