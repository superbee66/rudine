namespace Rudine.Exceptions
{
    internal class NoChangesSinceRenderedException : SubmitDeniedException
    {
        public override string Message
        {
            get { return "skipped, the document appears to have nothing modified; did you forget to open & fill this doc/form before you submitted?"; }
        }
    }
}