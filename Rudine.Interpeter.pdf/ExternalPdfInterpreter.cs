namespace Rudine.Interpreters.Pdf
{
    public class ExternalPdfInterpreter : PdfInterpreter
    {
        public static readonly string MyOnlyDocTypeName = nameof(ExternalPdfInterpreter).Replace("Interpreter", string.Empty);
        public static readonly string MyOnlyDocTypeVersion = "1.0.0.0";

        public override string ReadDocTypeName(byte[] docData)
        {
            string _DocTypeName = base.ReadDocTypeName(docData);

            return string.IsNullOrWhiteSpace(_DocTypeName)
                ? MyOnlyDocTypeName
                : _DocTypeName;
        }

        public override string ReadDocRev(byte[] docData)
        {
            string _ReadDocRev = base.ReadDocRev(docData);
            return string.IsNullOrWhiteSpace(_ReadDocRev)
                ? MyOnlyDocTypeVersion
                : _ReadDocRev;
        }

        public override bool Processable(string docTypeName, string docRev) => 
            docTypeName == MyOnlyDocTypeName && docRev == MyOnlyDocTypeVersion;
    }
}