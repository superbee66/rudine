using System;

namespace Rudine.Web
{
    public class SubmissionReceivedEventArgs : EventArgs
    {
        public SubmissionReceivedEventArgs(LightDoc baseDoc)
        {
            LightDoc = baseDoc;
        }
        /// <summary>
        /// Result of the document submission
        /// </summary>
        public LightDoc LightDoc { get; private set; }
    }
}