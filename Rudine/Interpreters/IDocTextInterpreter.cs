using Rudine.Web;

namespace Rudine.Interpreters
{
    internal interface IDocTextInterpreter : IDocBaseInterpreter
    {
        /// <summary>
        ///     Synonymous with the verbs parse & deserialize
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        BaseDoc Read(string DocData, bool DocRevStrict = false);

        /// <summary>
        ///     Desterilize only properties associated with this solution's internal DocProcessing.
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>filled DocProcessingInstructions or null if they can't be extract</returns>
        DocProcessingInstructions ReadDocPI(string DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns>actual name or null if it can't be extract</returns>
        string ReadDocTypeName(string DocData);

        /// <summary>
        ///     Name of document type that correlates with the folder name in the ~/form/doctypename of this app
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns></returns>
        string ReadDocRev(string DocData);

        void Validate(string DocData);
        string WriteText<T>(T source, bool includeProcessingInformation = true) where T : DocProcessingInstructions;
        string WritePI(string DocData, DocProcessingInstructions pi);
    }
}