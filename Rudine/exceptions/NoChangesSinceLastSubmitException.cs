namespace Rudine.Exceptions
{
    internal class NoChangesSinceLastSubmitException : SubmitDeniedException
    {
        public override string Message
        {
            get { return "skipped, the prior submission contained identical information as this"; }
        }
    }
}