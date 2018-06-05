using System;
using System.IO;
using System.Web.Script.Serialization;
using NUnit.Framework;
using Rudine.Interpreters.Embeded;
using Rudine.Web;

namespace Rudine.Tests.Interpreters.Embeded
{
    [TestFixture]
    public class EmbededInterpreterTests
    {
        [Test]
        public void CreateTest()
        {
            DocRev basedoc = (DocRev)new DocRevInterpreter().Create("DocRev");

            Assert.IsTrue(basedoc is BaseDoc);
            Assert.IsTrue(basedoc is DocRev);
        }

        readonly JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer()
        {
            MaxJsonLength = int.MaxValue
        };

        [Test]
        public void WritePITest()
        {
            FileInfo file = new FileInfo("Hi.txt");
            DocRevInterpreter _DocRevInterpreter = new DocRevInterpreter();
            DocRev docRev_A = (DocRev)_DocRevInterpreter.Create("DocRev");


            File.WriteAllText(file.FullName, "Hello World!");

            docRev_A.DocURN.DocTypeName = "Booger";
            docRev_A.DocURN.solutionVersion = new Version(1, 3).ToString();
            docRev_A.DocFiles.Add(
                new DocRevEntry
                {
                    Bytes = File.ReadAllBytes(file.FullName),
                    ModDate = file.LastWriteTime,
                    Name = file.FullName.Replace(Path.GetFullPath("."), string.Empty).TrimStart('/', '\\')
                });

            docRev_A = (DocRev)DocExchange.Instance.Create(docRev_A);

            // round-trip the object to make sure serialization is working everywhere
            DocRev docRev_B = (DocRev)DocExchange.Instance.ReadBytes(_DocRevInterpreter.WriteByte(docRev_A));

            Assert.AreEqual(_JavaScriptSerializer.Serialize(docRev_A), _JavaScriptSerializer.Serialize(docRev_B));

        }
    }
}