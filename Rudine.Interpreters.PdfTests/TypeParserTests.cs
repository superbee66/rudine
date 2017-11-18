using System;
using NUnit.Framework;
using Rudine.Interpreters.Pdf.util;

namespace Rudine.Interpreters.PdfTests {
    [TestFixture]
    public class TypeParserTests {
        [Test]
        public void LcdTypeTest() {
            Assert.AreEqual(TypeParser.LcdType(int.MinValue.ToString(), int.MaxValue.ToString()), typeof(Int32));
            Assert.AreEqual(TypeParser.LcdType(int.MinValue.ToString(), int.MaxValue.ToString()), typeof(int));
            Assert.AreEqual(TypeParser.LcdType(int.MinValue.ToString(), long.MaxValue.ToString()), typeof(long));
            Assert.AreEqual(TypeParser.LcdType(long.MinValue.ToString(), int.MaxValue.ToString()), typeof(long));
            Assert.AreEqual(TypeParser.LcdType(uint.MinValue.ToString(), uint.MaxValue.ToString()), typeof(UInt32));
        }
    }
}