using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rudine.Tests;
using Rudine.Tests.Properties;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded.Tests
{
    [Serializable]
    public class TestExternalDoc : ExternalDoc
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [TestFixture]
    public class ExternalDocInterpreterTests : DocExchangeTests
    {
        private static ExternalDocInterpreter _ExternalDocInterpreter = new ExternalDocInterpreter();

        [Test]
        public void ExternalDocSubmitTest()
        {
            LightDoc _LightDoc;

            _LightDoc = DocExchange.Instance.SubmitDoc(
               new TestExternalDoc
               {
                   DocIdKeys = new Dictionary<string, string> { { "ok", "lala" } },
                   DocTypeName = nameof(TestExternalDoc),
                   solutionVersion = ExternalDoc.MyOnlyDocRev,
                   FirstName = "Gary",
                   LastName = "Bruno",
                   RawBytes = Resources.BaseLineOpenOffice4
               }, "test@ok.com");


            Assert.IsTrue(_LightDoc.DocTypeName == nameof(TestExternalDoc));
        }
    }
}