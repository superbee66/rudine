using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using NUnit.Framework;
using Rudine.Template;
using Rudine.Tests.help;
using Rudine.Tests.Properties;
using Rudine.Util.Zips;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Tests
{
    [TestFixture]
    public class DocExchangeTests
    {
        [SetUp]
        public void TestFixtureSetup()
        {
            string[] DirectoriesToDelete = Directory.EnumerateDirectories(RequestPaths.GetPhysicalApplicationPath(""))
                                                    .ToArray();

            // remove directories that can be created at runtime
            foreach (string path in DirectoriesToDelete)
                for (int i = 0; i < 100 && Directory.Exists(path); i++)
                    try
                    {
                        new DirectoryInfo(path).rmdir();
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(i * 100);
                    }

            foreach (string path in DirectoriesToDelete)
                if (Directory.Exists(path))
                    throw new Exception(string.Format("{0} can't be deleted, TestInitialize ResetPersistedItems method can't finished", path));

            //ensure everything is cleared from memory based cache
            foreach (string cacheKey in MemoryCache.Default.Select(kvp => kvp.Key)
                                                   .ToList())
                MemoryCache.Default.Remove(cacheKey);
        }

        private static string Extension(string docTypeName) => (string)Resources.ResourceManager.GetObject(docTypeName + "_Extension");
        private static byte[] Bytes(string docTypeName) => (byte[])Resources.ResourceManager.GetObject(docTypeName);

        public static LightDoc CreateTemplate(string docTypeName)
        {
            DocProcessingInstructions pi = new DocProcessingInstructions
            {
                DocTypeName = docTypeName
            };

            DocRev docRev = DocExchange.Instance.CreateTemplate(
                new List<DocRevEntry>
                {
                    new DocRevEntry
                    {
                        Bytes = Bytes(docTypeName),
                        ModDate = DateTime.Now,
                        Name = string.Format("{0}.{1}", docTypeName, Extension(docTypeName))
                    }
                });

            Assert.IsTrue(docRev.DocURN.DocTypeName.Equals(pi.DocTypeName, StringComparison.CurrentCultureIgnoreCase));
            Assert.NotNull(docRev.solutionVersion);
            Assert.IsTrue(docRev.DocChecksum != default(int));

            LightDoc lightDoc = DocExchange.Instance.SubmitDoc(docRev, docSubmittedByEmail);

            Assert.NotNull(lightDoc);
            return lightDoc;
        }

        const string docSubmittedByEmail = "removeThisDocSubmittedByEmail@ok.com";

        public static BaseDoc Create(string docTypeName)
        {
            CreateTemplate(docTypeName);

            Dictionary<string, string> DocKeys = new Dictionary<string, string>
            {
                { "RightNow", DateTime.Now.ToString() },
                { "CreateTestDocTypeName", docTypeName }
            };

            BaseDoc baseDoc = Runtime.ActivateBaseDoc(
                docTypeName,
                TemplateController.Instance.TopDocRev(docTypeName),
                DocExchange.Instance);
            baseDoc.DocIdKeys = DocKeys;
            baseDoc.DocTypeName = docTypeName;

            BaseDoc createdBaseDoc = DocExchange.Instance.Create(baseDoc);

            Assert.AreEqual(baseDoc.DocIdKeys, createdBaseDoc.DocIdKeys);

            return createdBaseDoc;
        }

        public static BaseDoc CreateRandomBaseDoc(string docTypeName, bool DocStatus)
        {
            BaseDoc basedoc = Create(docTypeName);

            BaseDoc randdoc = new Rand().obj(basedoc.Clone());

            randdoc.DocChecksum = basedoc.DocChecksum;
            randdoc.DocIdKeys = basedoc.DocIdKeys;
            randdoc.DocStatus = DocStatus;
            randdoc.DocTitle = basedoc.DocTitle;
            randdoc.DocTypeName = basedoc.DocTypeName;
            randdoc.solutionVersion = basedoc.solutionVersion;
            randdoc.name = basedoc.name;

            randdoc.SetDocId(basedoc.GetDocId());
            return randdoc;
        }

        [Test]
        public void AuditTest([DocDataSampleValues] string docTypeName)
        {
            BaseDoc randDoc = CreateRandomBaseDoc(docTypeName, false);

            randDoc.DocStatus = false;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail);
            Assert.AreEqual(DocExchange.Instance.Audit(randDoc.DocTypeName, randDoc.GetDocId()).Count, 1);

            randDoc.DocStatus = true;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail);
            Assert.AreEqual(DocExchange.Instance.Audit(randDoc.DocTypeName, randDoc.GetDocId()).Count, 2);
        }

        [Test]
        [Sequential]
        public void CreateTemplateTest(
            [DocDataSampleValues] string docTypeName
        ) => CreateTemplate(docTypeName);

        [Test]
        [Sequential]
        public void CreateTest(
            [DocDataSampleValues] string docTypeName
        ) => Create(docTypeName);

        [Test]
        [Sequential]
        public void DocTypeNamesTest([DocDataSampleValues] string docTypeName)
        {
            // there should be no listing until the docrev has presence in the ~/doc/*
            Assert.IsFalse(DocExchange.Instance.DocTypeNames()
                                      .Contains(docTypeName));

            CreateTemplate(docTypeName);

            DocRev docrev = (DocRev)DocExchange.Instance.Get(DocRev.MyOnlyDocName, new Dictionary<string, string> { { DocRev.KeyPart1, docTypeName } });

            Assert.IsNotNull(docrev);

            Assert.IsTrue(DocExchange.Instance.DocTypeNames()
                                     .Contains(docTypeName));
        }

        [Test]
        public void InterpretersTest()
        {
            Assert.IsTrue(DocExchange.Instance.Interpreters().Count() == 4);
        }

        [Test, Combinatorial]
        public void SubmitDocParameterizedTest([DocDataSampleValues] string docTypeName, [Values(false, true)] bool DocStatus)
        {
            BaseDoc randDoc = CreateRandomBaseDoc(docTypeName, false);

            randDoc.DocStatus = false;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail, null, null, randDoc.DocStatus, null);

            randDoc.DocStatus = true;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail, null, null, randDoc.DocStatus, null);
        }

        [Test, Combinatorial]
        public void SubmitDocTestNonParameterized([DocDataSampleValues] string docTypeName, [Values(false, true)] bool DocStatus)
        {
            BaseDoc randDoc = CreateRandomBaseDoc(docTypeName, false);

            randDoc.DocStatus = false;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail);

            randDoc.DocStatus = true;
            DocExchange.Instance.SubmitDoc(randDoc, docSubmittedByEmail);

        }

        [Test]
        [Sequential]
        public void TemplateSourcesTest()
        {
            Assert.IsTrue(DocExchange.Instance.TemplateSources().Count() == 4);
        }
    }
}