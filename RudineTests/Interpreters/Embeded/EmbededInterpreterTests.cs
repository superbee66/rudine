using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using NUnit.Framework;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded.Tests
{
    [TestFixture]
    public class EmbededInterpreterTests
    {
        [Test]
        public void CreateTest()
        {
            DocRev basedoc = (DocRev)new EmbededInterpreter().Create("DocRev");

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
            EmbededInterpreter _EmbededInterpreter = new EmbededInterpreter();
            DocRev docRev_A = (DocRev)_EmbededInterpreter.Create("DocRev");

            
            File.WriteAllText(file.FullName,"Hello World!");

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

            Assert.NotNull(docRev_A.DocFilesMD5);

            
            File.WriteAllBytes("test.zip", _EmbededInterpreter.WriteByte(docRev_A));

            FileInfo _FileInfo = new FileInfo("test.zip");

            DocRev docRev_B = (DocRev)_EmbededInterpreter.Read(File.ReadAllBytes("test.zip"));

            File.WriteAllText("a.json", _JavaScriptSerializer.Serialize(docRev_A));
            File.WriteAllText("b.json", _JavaScriptSerializer.Serialize(docRev_B));

        }
    }
}