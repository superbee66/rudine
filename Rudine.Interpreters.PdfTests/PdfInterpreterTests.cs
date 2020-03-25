using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rudine.Interpreters.Pdf;
using Rudine.Interpreters.PdfTests.Properties;
using Rudine.Web;

namespace Rudine.Interpreters.PdfTests
{
    [TestFixture]
    public class PdfInterpreterTests
    {


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
                        Bytes = _PdfInterpreter.WritePI( Resources.BaseLineOpenOffice4, pi  ),
                        ModDate = DateTime.Now,
                        Name = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}.pdf", nameof(Resources.BaseLineOpenOffice4))
                    }
                });

            Assert.AreEqual(_DocRev.DocURN.DocTypeName, pi.DocTypeName);
            Assert.NotNull(_DocRev.solutionVersion);

            LightDoc _LightDoc = DocExchange.Instance.SubmitBytes(new Embeded.DocRevInterpreter().WriteByte(_DocRev), "removeThisDocSubmittedByEmail@ok.com");

            Assert.NotNull(_LightDoc);

        }

    }
}