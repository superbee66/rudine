using Rudine;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rudine.Interpreters.Embeded;
using Rudine.Template;
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
            Assert.IsTrue(docRev.DocChecksum != default(int));

            LightDoc lightDoc = DocExchange.Instance.SubmitBytes(new EmbededInterpreter().WriteByte(docRev), "removeThisDocSubmittedByEmail@ok.com");

            Assert.NotNull(lightDoc);
        }

        [Test()]
        [Sequential]
        public void CreateTest(
            [Values(nameof(Resources.BaseLineInfoPath2013), nameof(Resources.BaseLineOpenOffice4))] string docTypeName,
            [Values("xsn", "pdf")] string fileExtension
        )
        {
            CreateTemplateTest(docTypeName, fileExtension);

            Type DocType = Runtime.ActivateBaseDocType(
                     docTypeName,
                     TemplateController.Instance.TopDocRev(docTypeName),
                     DocExchange.Instance);

            Dictionary<string, string> DocKeys = new Dictionary<string, string> {
                {"RightNow",DateTime.Now.ToString() },
                {"CreateTestDocTypeName", docTypeName }
            };

            BaseDoc baseDoc = (BaseDoc)Activator.CreateInstance(DocType);
            baseDoc.DocKeys = DocKeys;

            BaseDoc createdBaseDoc = DocExchange.Instance.Create(baseDoc);

            Assert.AreEqual(baseDoc.DocKeys, createdBaseDoc.DocKeys);
        }
    }
}