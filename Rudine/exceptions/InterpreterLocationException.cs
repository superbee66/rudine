namespace Rudine.Exceptions
{
    internal class InterpreterLocationException : DocDataException
    {
        public override string Message
        {
            get { return "could not locate a DocDataInterpreter to process the data"; }
        }
    }
}