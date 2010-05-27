using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace TestTinyHTMLParser
{
    [TestFixture]
    class TestRegex
    {
        [Test]
        public void TestRegex1()
        {
            Regex r1 = new Regex("\\s*[a-zA-Z][-_.:a-zA-Z0-9]*\\s*");
            string text = "Test string for regex";
            Match mc = r1.Match(text, 4);
            Assert.AreEqual(mc.Success, true);
            Assert.AreEqual(mc.Index, 4);
            Assert.AreEqual(mc.Groups[0].ToString(), " string ");
        }
    }
}
