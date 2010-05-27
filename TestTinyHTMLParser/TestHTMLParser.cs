using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TinyHTMLParser;
using System.IO;

namespace TestTinyHTMLParser
{
    [TestFixture]
    class TestHTMLParser : HTMLParser
    {
        TestHTMLParser thp;
        public TestHTMLParser()
        {
            _content = new Dictionary<string, List<pair>>();
        }

        [SetUp]
        public void Init()
        {
            thp = new TestHTMLParser();
            thp._content.Clear();
        }

        [Test]
        public void TestConstructor()
        {
            Assert.AreEqual(1, thp._lineno);
            Assert.AreEqual(0, thp._offset);
            Assert.AreEqual("", thp._rawdata);
        }

        [Test]
        public void TestParseStartTag1()
        {
            thp._rawdata = "<body>";
            Assert.AreEqual(thp._rawdata.Length, thp.parseStartTag(0));
            Assert.AreEqual(true, thp._content.ContainsKey("body"));
            Assert.AreEqual(0, thp._content["body"].Count);
        }

        [Test]
        public void TestParseStartTag2()
        {
            thp._rawdata = "<body background=\"green\">";
            Assert.AreEqual(thp._rawdata.Length, thp.parseStartTag(0));
            Assert.AreEqual(true, thp._content.ContainsKey("body"));
            Assert.AreEqual(1, thp._content["body"].Count);
            Assert.AreEqual("background", thp._content["body"][0].name);
            Assert.AreEqual("green", thp._content["body"][0].value);
        }

        [Test]
        public void TestParseStartTag3()
        {
            thp._rawdata = "<body background=\"green\" font='Seril Arial' >";
            Assert.AreEqual(thp._rawdata.Length, thp.parseStartTag(0));
            Assert.AreEqual(true, thp._content.ContainsKey("body"));
            Assert.AreEqual(2, thp._content["body"].Count);
            Assert.AreEqual("background", thp._content["body"][0].name);
            Assert.AreEqual("green", thp._content["body"][0].value);
            Assert.AreEqual("font", thp._content["body"][1].name);
            Assert.AreEqual("Seril Arial", thp._content["body"][1].value);
        }

        [Test]
        public void TestParseEndTag1()
        {
            thp._rawdata = "</body>";
            Assert.AreEqual(thp._rawdata.Length, thp.parseEndTag(0));
            Assert.AreEqual(true, thp._content.ContainsKey("body"));
            Assert.AreEqual(null, thp._content["body"]);
        }

        [Test]
        public void TestCheckForWholeStartTag1()
        {
            thp._rawdata = "<body>";
            Assert.AreEqual(thp._rawdata.Length, thp.checkForWholeStartTag(0));
        }

        [Test]
        public void TestCheckForWholeStartTag2()
        {
            thp._rawdata = "<br />";
            Assert.AreEqual(thp._rawdata.Length, thp.checkForWholeStartTag(0));
        }

        [Test]
        public void TestCheckForWholeStartTag3()
        {
            thp._rawdata = "<body background=\"green\">";
            Assert.AreEqual(thp._rawdata.Length, thp.checkForWholeStartTag(0));
        }

        [Test]
        public void TestCheckForWholeStartTag4()
        {
            thp._rawdata = "<body background=\"green\" font=\"Seril Arial\">";
            Assert.AreEqual(thp._rawdata.Length, thp.checkForWholeStartTag(0));
        }

        [Test]
        public void TestFeed1()
        {
            string content = File.ReadAllText("../../testfeed1.html");
            thp.feed(content);
        }

        protected override void handleStartTag(string tag, List<HTMLParser.pair> attrs)
        {
            if (!_content.ContainsKey(tag)) {
                _content.Add(tag, attrs);
            }
            System.Console.WriteLine("handleStartTag: " + tag);
            System.Console.Write("\t attrs: ");
            foreach(HTMLParser.pair attr in attrs) {
                System.Console.Write("(" + attr.name + "=" + attr.value + ")");
            }
            System.Console.Write("\n");
        }

        protected override void handleEndTag(string tag)
        {
            if (!_content.ContainsKey(tag)) {
                _content.Add(tag, null);
            }
            System.Console.WriteLine("handleEndTag: " + tag);
            System.Console.WriteLine(this.getStartTagText());
        }

        private Dictionary<string, List<pair>> _content;
    }
}
