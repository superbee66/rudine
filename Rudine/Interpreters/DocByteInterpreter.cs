using Rudine.Web;

namespace Rudine.Interpreters
{
    public abstract class DocByteInterpreter : DocBaseInterpreter, IDocByteInterpreter
    {
        /// <summary>
        ///     Synonymous with the verbs parse & deserialize
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        public abstract BaseDoc Read(byte[] DocData, bool DocRevStrict = false);

        /// <summary>
        ///     Desterilize only properties associated with this solution's internal DocProcessing.
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>filled DocProcessingInstructions or null if they can't be extract</returns>
        public abstract DocProcessingInstructions ReadDocPI(byte[] DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>actual name or null if it can't be extract</returns>
        public abstract string ReadDocTypeName(byte[] DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns></returns>
        public abstract string ReadDocRev(byte[] DocData);

        public abstract void Validate(byte[] DocData);
        public abstract byte[] WriteByte<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions;

        public abstract byte[] WritePI(byte[] DocData, DocProcessingInstructions pi);
    }
}