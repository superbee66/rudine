using NUnit.Framework;
using Rudine.Interpreters.Embeded;
using System;
using System.Collections.Generic;
using System.IO;
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
            DocRev basedoc = (DocRev)new EmbededInterpreter().Create("DocRev");

            Assert.IsTrue(basedoc is BaseDoc);
            Assert.IsTrue(basedoc is DocRev);


        }

        [Test()]
        public void WritePITest()
        {
            FileInfo file = new FileInfo(System.IO.Directory.EnumerateFiles(".").First());
            EmbededInterpreter _EmbededInterpreter = new EmbededInterpreter();
            DocRev docRev = (DocRev)_EmbededInterpreter.Create("DocRev");
            docRev.Target.DocTypeName = "Booger";
            docRev.Target.solutionVersion = new Version(1, 3).ToString();
            docRev.FileList.Add(
                new DocRevEntry
                {
                    Bytes = System.IO.File.ReadAllBytes(file.FullName),
                    ModDate = file.LastWriteTime,
                    Name = file.FullName.Replace(System.IO.Path.GetFullPath("."), string.Empty)

                });

            docRev = (DocRev)DocExchange.Instance.Create(docRev);
            System.IO.File.WriteAllBytes("test.zip", _EmbededInterpreter.WriteByte(docRev));
            throw new Exception(docRev.href);
        }
    }
}