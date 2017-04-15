using System.Runtime.Serialization;

namespace Rudine.Web
{
    [DataContract]
    public class ContentInfo
    {
        /// <summary>
        ///     File extensions that should be used when serving the rendered document to the client.
        ///     PERMANENT!!! This should not change at anytime though out the entire software development life cycle
        /// </summary>
        [DataMember]
        public string ContentFileExtension { get; set; }

        /// <summary>
        ///     PERMANENT!!! This should not change at anytime though out the entire software development life cycle
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public string ContentType { get; set; }
    }
}