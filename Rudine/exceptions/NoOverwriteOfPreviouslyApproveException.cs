namespace Rudine.Exceptions
{
    internal class NoOverwriteOfPreviouslyApproveException : SubmitDeniedException
    {
        public override string Message
        {
            get { return "skipped, previously approved, form can not be submitted for updates"; }
        }
    }
}