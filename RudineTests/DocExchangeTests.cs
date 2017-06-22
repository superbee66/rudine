using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rudine.Interpreters.Embeded;
using Rudine.Web;
using RudineTests.Properties;

namespace Rudine.Tests
{
    [TestFixture]
    public class DocExchangeTests
    {
        [Test]
        [Sequential]
        public void CreateTemplateTest(
            [Values(nameof(Resources.BaseLineInfoPath2013), nameof(Resources.BaseLineOpenOffice4))] string docTypeName,
            [Values("xsn", "pdf")] string fileExtension
        )
        {
            byte[] docBytes = (byte[])Resources.ResourceManager.GetObject(docTypeName);

            DocProcessingInstructions pi = new DocProcessingInstructions
            {
                DocTypeName = docTypeName
            };

            DocRev docRev = DocExchange.Instance.CreateTemplate(
                new List<DocRevEntry>
                {
                    new DocRevEntry
                    {
                        Bytes = docBytes,
                        ModDate = DateTime.Now,
                        Name = string.Format("{0}.{1}", docTypeName, fileExtension)
                    }
                });

            Assert.AreEqual(docRev.DocURN.DocTypeName, pi.DocTypeName);
            Assert.NotNull(docRev.solutionVersion);

            LightDoc lightDoc = DocExchange.Instance.SubmitBytes(new EmbededInterpreter().WriteByte(docRev), "removeThisDocSubmittedByEmail@ok.com");

            Assert.NotNull(lightDoc);
        }
    }
}