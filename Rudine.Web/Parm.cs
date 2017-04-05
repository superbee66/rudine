namespace Rudine.Web
{
    /// <summary>
    ///     techniques utilized by Nav to resolve a given BaseDoc's InfoPath XML content (BaseDoc.ToInfoPathXml(string href))
    /// </summary>
    public struct Parm
    {
        public const string DocChecksum = "DocChecksum";

        /// <summary>
        ///     Serialized & reconstructed from the actual Url; never persisting anywhere. This
        ///     is used when a document is requested to be created.
        /// </summary>
        public const string DocBin = "DocBin";

        /// <summary>
        ///     Fallback for DocBin when it becomes to long for browsers like internet explorer to handle (2083+ characters).
        ///     The original DocBin Url is cached & represented by this key's value.
        /// </summary>
        public const string DocCache = "DocCache";

        /// <summary>
        ///     Pull the document straight from the persisted store (database).
        /// </summary>
        public const string DocId = "DocId";

        public const string RelayUrl = "RelayUrl";
        public const string DocTypeName = "DocTypeName";
        public const string DocRev = "DocRev";
        public const string LogSequenceNumber = "LogSequenceNumber";
        public const string DocStatus = "DocStatus";
        public const string DocSrc = "DocSrc";
        public const string DocKeys = "DocKeys";

        /// <summary>
        ///     the BaseDoc POCO
        /// </summary>
        public const string Doc = "Doc";

        /// <summary>
        ///     Microsoft specific & custom Rudine XML processing directions & the actual XML emitted by Microsoft InfoPath
        ///     Form Filler
        /// </summary>
        public const string DocData = "DocData";

        ///// <summary>
        ///// The actual XML emitted by Microsoft InfoPath Form Filler without the first lines of
        ///// Microsoft specific & custom Rudine XML processing directions
        ///// </summary>
        //public const string DocData = "DocData";
        /// <summary>
        ///     Microsoft specific & custom Rudine XML processing directions not concatenated
        ///     with it's principle subject; the actual XML
        /// </summary>
        public const string DocXmlPI = "DocXmlPI";

        public const string LightDoc = "LightDoc";

        /// <summary>
        ///     The most recent & all previous submissions for a given DocId/DocIdKeys set
        /// </summary>
        public const string Submissions = "Submissions";

        /// <summary>
        ///     iDocumentController.List should return n hits
        ///     sorting by this descending (get the most recent documents found)
        /// </summary>
        public const string DocSubmitDate = "DocSubmitDate";
    }
}