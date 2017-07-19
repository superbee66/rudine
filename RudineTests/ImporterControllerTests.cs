using NUnit.Framework;
using Rudine;
using Rudine.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rudine.Template.Filesystem;
using Rudine.Web.Util;

namespace Rudine.Tests
{
    [TestFixture()]
    public class ImporterControllerTests : DocExchangeTests
    {


        [Test()]
        public void SyncTemplatesTest()
        {
            Assert.AreEqual(0, DocExchange.Instance.List(new List<string> { nameof(DocRev) }).Count());
            ImporterController.SyncTemplates(DocExchange.Instance);

            Assert.AreEqual(0, DocExchange.Instance.List(new List<string> { nameof(DocRev) }).Count());

            new DirectoryInfo(FilesystemTemplateController.DirectoryPath).rmdir();
            var dir = new DirectoryInfo(FilesystemTemplateController.DirectoryPath).mkdir();

            File.WriteAllBytes(dir.FullName + "\\" + nameof(Properties.Resources.BaseLineInfoPath2013) + "." + Properties.Resources.BaseLineInfoPath2013_Extension, Properties.Resources.BaseLineInfoPath2013);

            ImporterController.SyncTemplates(DocExchange.Instance);
            Assert.AreEqual(1, DocExchange.Instance.List(new List<string> { nameof(DocRev) }).Count());
        }
    }
}