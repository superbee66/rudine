using NUnit.Framework;
using Rudine.Interpreters.Embeded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded.Tests
{
    [TestFixture()]
    public class EmbededInterpreterTests
    {
        [Test()]
        public void CreateTest()
        {
            var basedoc = new EmbededInterpreter().Create("DOCREV");
            Assert.IsTrue(basedoc is BaseDoc);
            Assert.IsTrue(basedoc is DOCREV);
        }
    }
}