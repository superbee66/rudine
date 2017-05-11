using NUnit.Framework;
using Rudine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rudine.Tests
{
    [TestFixture()]
    public class ImporterControllerTests
    {
        [Test()]
        public void TryDocRevImportingTest()
        {
            ImporterController.CreateTemplateItems(DocExchange.Instance);
        }
    }
}