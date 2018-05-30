using Rudine.Web;

namespace Rudine.Interpreters
{
    public interface IDocBaseInterpreter : IBaseDocTemplateBuilder
    {
        /// <summary>
        /// file/mime information usually accociated with the document read & written
        /// </summary>
        ContentInfo ContentInfo { get; }


        BaseDoc Create(string DocTypeName);

        /// <summary>
        ///     should operate on the data itself while avoiding serialization operations that may alter the DocData
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="dstBaseDoc"></param>
        /// <returns></returns>
        string GetDescription(string DocTypeName);

        /// <summary>
        ///     Should be backed by a httphandler. For InfoPath there is a manifest.xsf the InfoPath Desktop Application will be
        ///     searching for. For JsonInterpreter a mycontents.cab will be targeted.
        /// </summary>
        string HrefVirtualFilename(string DocTypeName, string DocRev);

        /// <summary>
        ///     Should this instance of an interpreter actually process the given document if it were passed?
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="DocRev"></param>
        /// <returns></returns>
        bool Processable(string DocTypeName, string DocRev);
    }
}