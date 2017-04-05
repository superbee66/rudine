using Rudine.Web;

namespace Rudine.Interpreters
{
    internal interface IDocByteInterpreter : IDocBaseInterpreter
    {
        /// <summary>
        ///     Synonymous with the verbs parse & deserialize
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        BaseDoc Read(byte[] DocData, bool DocRevStrict = false);

        /// <summary>
        ///     Desterilize only properties associated with this solution's internal DocProcessing.
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>filled DocProcessingInstructions or null if they can't be extract</returns>
        DocProcessingInstructions ReadDocPI(byte[] DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>actual name or null if it can't be extract</returns>
        string ReadDocTypeName(byte[] DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns></returns>
        string ReadDocRev(byte[] DocData);

        void Validate(byte[] DocData);
        byte[] WriteByte<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions;
        byte[] WritePI(byte[] DocData, DocProcessingInstructions pi);
    }
}