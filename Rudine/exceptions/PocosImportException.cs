namespace Rudine.Exceptions
{
    internal class PocosImportException : ImportException
    {
        public override string Message
        {
            get { return "Poco import has not resulted in a new Head DocRev to match. DocRev string conventions will place the most current DocRev at the top of a list when OrderByDescending is applied. There seems to be a more current DocRev in the data store then what is being imported."; }
        }
    }
}