using System.Runtime.Serialization;

namespace Rudine.Web
{
    [DataContract]
    public class ContentInfo
    {
        /// <summary>
        ///     File extensions that should be used when serving the rendered document to the client.   
        /// </summary>
        [DataMember]
        public string ContentFileExtension { get; set; }

        /// <summary>
        ///     
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string ContentType { get; set; }

        /// <summary>
        /// see https://en.wikipedia.org/wiki/List_of_file_signatures
        /// </summary>
        [DataMember]
        public MagicNumbers ContentSignature { get; set; }
    }
}