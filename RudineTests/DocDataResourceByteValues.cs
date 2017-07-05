using RudineTests.Properties;

namespace Rudine.Tests
{
    class DocTypeNameValues : NUnit.Framework.ValuesAttribute
    {
        public DocTypeNameValues() : base(nameof(Resources.BaseLineInfoPath2013), nameof(Resources.BaseLineOpenOffice4)) { }
    }

    class FileExtensionValues : NUnit.Framework.ValuesAttribute
    {
        public FileExtensionValues() : base("xsn", "pdf") { }
    }

}