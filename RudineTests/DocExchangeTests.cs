using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using NUnit.Framework;
using Rudine.Template;
using Rudine.Web;
using Rudine.Web.Util;
using Rudine.Tests.Properties;

namespace Rudine.Tests
{
    [TestFixture]
    public class DocExchangeTests
    {
        private static string Extension(string docTypeName) => (string)Resources.ResourceManager.GetObject(docTypeName + "_Extension");
        private static byte[] Bytes(string docTypeName) => (byte[])Resources.ResourceManager.GetObject(docTypeName);

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

            LightDoc lightDoc = DocExchange.Instance.SubmitDoc(docRev, "removeThisDocSubmittedByEmail@ok.com");

            Assert.NotNull(lightDoc);
            return lightDoc;
        }

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
            baseDoc.DocKeys = DocKeys;

            BaseDoc createdBaseDoc = DocExchange.Instance.Create(baseDoc);

            Assert.AreEqual(baseDoc.DocKeys, createdBaseDoc.DocKeys);

            return createdBaseDoc;
        }

        [Test]
        [Sequential]
        public void CreateTemplateTest(
            [DocTypeNameValues] string docTypeName
        ) => CreateTemplate(docTypeName);

        [Test]
        [Sequential]
        public void CreateTest(
            [DocTypeNameValues] string docTypeName
        ) => Create(docTypeName);

        [Test]
        [Sequential]
        public void DocTypeNamesTest(
            [DocTypeNameValues] string docTypeName,
            [FileExtensionValues] string fileExtension)
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
            Assert.IsTrue(DocExchange.Instance.Interpreters().Count() == 3);
        }

        [Test]
        [Sequential]
        public void TemplateSourcesTest()
        {
            var TemplateSources = DocExchange.Instance.TemplateSources();
            Assert.IsTrue(TemplateSources.Count() == 3);
        }
    }
}