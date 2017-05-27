using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rudine.Interpreters.PdfTests.Properties;
using Rudine.Web;

namespace Rudine.Interpreters.Pdf.Tests
{
    [TestFixture]
    public class PdfInterpreterTests
    {
        [Test]
        public void CreateTest() { Assert.Fail(); }

        [Test]
        public void GetDescriptionTest() { Assert.Fail(); }

        [Test]
        public void HrefVirtualFilenameTest() { Assert.Fail(); }

        [Test]
        public void ProcessableTest() { Assert.Fail(); }

        [Test]
        public void CreateTemplateTest() { Assert.Fail(); }

        [Test]
        public void ReadTest() { Assert.Fail(); }

        [Test]
        public void ReadDocPITest() { Assert.Fail(); }

        [Test]
        public void ReadDocRevTest() { Assert.Fail(); }

        [Test]
        public void ReadDocTypeNameTest() { Assert.Fail(); }

        [Test]
        public void TemplateSourcesTest()
        {
            PdfInterpreter _PdfInterpreter = new PdfInterpreter();

            DocProcessingInstructions pi = new DocProcessingInstructions
            {
                DocTypeName = "HERE_IS_MY_PDF_DOCTYPENAME"
            };

            DocRev _DocRev = _PdfInterpreter.CreateTemplate(
                new List<DocRevEntry>
                {
                    new DocRevEntry
                    {
                        Bytes = _PdfInterpreter.WritePI( Resources.OpenOffice4, pi  ),
                        ModDate = DateTime.Now,
                        Name = string.Format("{0}.pdf", nameof(Resources.OpenOffice4))
                    }
                });

            Assert.AreEqual(_DocRev.DocURN.DocTypeName, pi.DocTypeName);
            Assert.NotNull(_DocRev.solutionVersion);

            LightDoc _LightDoc = DocExchange.Instance.SubmitBytes(new Embeded.EmbededInterpreter().WriteByte(_DocRev), "removeThisDocSubmittedByEmail@ok.com");

            Assert.NotNull(_LightDoc);

        }

        [Test]
        public void ValidateTest() { Assert.Fail(); }

        [Test]
        public void WriteByteTest() { Assert.Fail(); }

        [Test]
        public void WritePITest() { Assert.Fail(); }
    }
}