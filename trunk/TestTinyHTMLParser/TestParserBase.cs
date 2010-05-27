using System;
using System.Collections.Generic;
using System.Text;
using TinyHTMLParser;
using NUnit.Framework;

namespace TestTinyHTMLParser
{
    [TestFixture]
    class TestParserBase : ParserBase
    {
        TestParserBase tp;
        public TestParserBase()
        {
            reset();
        }

        [SetUp]
        public void Init()
        {
            tp = new TestParserBase();
        }

        [Test]
        public void TestUpdatePos1()
        {
            Assert.AreEqual(1, tp._lineno);
            Assert.AreEqual(0, tp._offset);

            tp._rawdata = "<html>\n"
                + "<head>\n"
                + "<title>Test HTML piece</title>\n"
                + "</head>\n"
                + "<body>\n"
                + "</body>\n"
                + "</html>\n";
            Assert.AreEqual(tp._rawdata.Length, tp.updatepos(3, tp._rawdata.Length));
            Assert.AreEqual(8, tp._lineno);
            Assert.AreEqual(0, tp._offset);
        }

        [Test]
        public void TestUpdatePos2()
        {
            tp._rawdata = "<html>\n"
                + "<head>\n"
                + "<title>Test HTML piece</title>\n"
                + "</head>\n"
                + "<body>\n"
                + "</body>\n"
                + "</html>\n";
            Assert.AreEqual(5, tp.updatepos(3, 5));
            Assert.AreEqual(1, tp._lineno);
            Assert.AreEqual(2, tp._offset);
        }

        [Test]
        public void TestUpdatePos3()
        {
            tp._rawdata = "<html>\n"
                + "<head>\n"
                + "<title>Test HTML piece</title>\n"
                + "</head>\n"
                + "<body>\n"
                + "</body>\n"
                + "</html>\n";
            Assert.AreEqual(5, tp.updatepos(7, 5));
            Assert.AreEqual(1, tp._lineno);
            Assert.AreEqual(0, tp._offset);
        }

        [Test]
        public void TestCountLine()
        {
            string t1 = "";
            Assert.AreEqual(0, countLine(t1));
            t1 = "abc";
            Assert.AreEqual(0, countLine(t1));
            t1 = "abc\n";
            Assert.AreEqual(1, countLine(t1));
            t1 = "abc\n"
                + "def";
            Assert.AreEqual(1, countLine(t1));
            t1 = "abc\n"
                + "def\n";
            Assert.AreEqual(2, countLine(t1));
            t1 = "<html>\n"
                + "<head>\n"
                + "<title>Test HTML piece</title>\n"
                + "</head>\n"
                + "<body>\n"
                + "</body>\n"
                + "</html>\n";
            Assert.AreEqual(7, countLine(t1));
        }

        [Test]
        public void TestParseComment1()
        {
            const string COMMENT = "Hello this is comment";
            tp._rawdata = "<!--" + COMMENT + "-->";
            Assert.AreEqual(tp._rawdata.Length, tp.parseComment(0));
            Assert.AreEqual(tp._comment, COMMENT);
        }

        [Test]
        public void TestParseComment2()
        {
            tp._rawdata = "<!--Invalid comment->";
            Assert.AreEqual(-1, tp.parseComment(0));
        }

        [Test]
        [ExpectedException(typeof(Exception))]
        public void TestParseComment3()
        {
            tp._rawdata = "<!--Invalid comment->";
            tp.parseComment(1);
        }

        [Test]
        public void TestScanName0()
        {
            tp._rawdata = "Test Name OK";
            int pos = 5;
            string name = tp.scanName(ref pos, 0);
            Assert.AreEqual(10, pos);
            Assert.AreEqual("name", name);
        }

        [Test]
        public void TestScanName1()
        {
            tp._rawdata = "Test Name";
            int pos = 0;
            string name = tp.scanName(ref pos, 0);
            Assert.AreEqual(5, pos);
            Assert.AreEqual("test", name);
        }

        [Test]
        public void TestScanName2()
        {
            int pos = 9;
            tp._rawdata = "Test Name";
            Assert.AreEqual(null, tp.scanName(ref pos, 0));
            Assert.AreEqual(-1, pos);
        }

        [Test]
        public void TestScanName3()
        {
            tp._rawdata = "Test";
            int pos = 0;
            Assert.AreEqual(null, tp.scanName(ref pos, 0));
            Assert.AreEqual(-1, pos);
        }

        [Test]
        [ExpectedException(typeof(Exception))]
        public void TestScanName4()
        {
            tp._rawdata = "0invalid Name";
            int pos = 0;
            tp.scanName(ref pos, 0);
        }

        [Test]
        public void TestParseDoctypeAttlist1()
        {
            tp._rawdata = "<!ATTLIST image height CDATA #REQUIRED>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist2()
        {
            tp._rawdata = "<!ATTLIST task status (important|normal) #REQUIRED>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist3()
        {
            tp._rawdata = "<!ATTLIST code lang NOTATION (vrml) #REQUIRED>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist4()
        {
            tp._rawdata = "<!ATTLIST task status (important|normal) \"normal\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist5()
        {
            tp._rawdata = "<!ATTLIST task status NMTOKEN #FIXED \"monthly\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist6()
        {
            tp._rawdata = "<!ATTLIST description xml:lang NMTOKEN #FIXED \"en\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist7()
        {
            tp._rawdata = "<!ATTLIST description   xml:lang   NMTOKEN #FIXED   \t \"en\" \t>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeAttlist8()
        {
            tp._rawdata = "<!ATTLIST description   xml:lang   NMTOKEN";
            Assert.AreEqual(-1, tp.parseDoctypeAttlist(10, 0));
        }

        [Test]
        public void TestParseDoctypeElement1()
        {
            tp._rawdata = "<!ELEMENT student (#PCDATA|id|surname|dob)*>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeElement(10, 0));
        }

        [Test]
        public void TestParseDoctypeElement2()
        {
            tp._rawdata = "<!ELEMENT student (#PCDATA|id|surnam";
            Assert.AreEqual(-1, tp.parseDoctypeElement(10, 0));
        }

        [Test]
        public void TestParseDoctypeEntity1()
        {
            tp._rawdata = "<!ENTITY js \"Jo Smith\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeEntity(9, 0));
        }

        [Test]
        public void TestParseDoctypeEntity2()
        {
            tp._rawdata = "<!ENTITY c SYSTEM \"http://www.xmlwriter.net/cpyright.xml\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeEntity(9, 0));
        }

        [Test]
        public void TestParseDoctypeEntity3()
        {
            tp._rawdata = "<!ENTITY c PUBLIC \" -//W3C//TEXT copyright//EN\" \"http://www.w3.org/xmlspec/copyright.xml\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeEntity(9, 0));
        }

        [Test]
        public void TestParseDoctypeEntity4()
        {
            tp._rawdata = "<!ENTITY % p \"(#PCDATA)\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeEntity(9, 0));
        }

        [Test]
        public void TestParseDoctypeEntity5()
        {
            tp._rawdata = "<!ENTITY % student SYSTEM \"http://www.university.com/student.dtd\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeEntity(9, 0));
        }

        [Test]
        public void TestParseDoctypeNotation1()
        {
            tp._rawdata = "<!NOTATION gif PUBLIC \"gif viewer\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeNotation(11, 0));
        }

        [Test]
        public void TestParseDoctypeNotation2()
        {
            tp._rawdata = "<!NOTATION gif PUBLIC \"gif viewer\" \"http://www.w3.org/gifviewer.xml\">";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDoctypeNotation(11, 0));
        }

        [Test]
        public void TestParseDoctypeSubset1()
        {
            tp._rawdata = "<!DOCTYPE student [\n"
                + "<!ENTITY % student SYSTEM \"http://www.university.com/student.dtd\">\n"
                + "%student;\n"
                + "]>";
            Assert.AreEqual(tp._rawdata.Length-1, tp.parseDoctypeSubset(20, 0));

        }

        [Test]
        public void TestParseDoctypeSubset2()
        {
            tp._rawdata = "<!DOCTYPE img [\n"
                + "<!ELEMENT img EMPTY>\n"
                + "<!ATTLIST img src ENTITY #REQUIRED>\n"
                + "<!ENTITY logo SYSTEM\n"
                + "\"http://www.xmlwriter.net/logo.gif\" NDATA gif>\n"
                + "<!NOTATION gif PUBLIC \"gif viewer\">"
                + "]>";
            Assert.AreEqual(tp._rawdata.Length - 1, tp.parseDoctypeSubset(16, 0));
        }

        [Test]
        public void TestParseMarkedSection1()
        {
            tp._rawdata = "<![ CDATA [ blah, blah, blah. ]]>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseMarkedSection(0));
        }

        [Test]
        public void TestParseDeclaration1()
        {
            tp._rawdata = "<!DOCTYPE img [\n"
                + "<!ELEMENT img EMPTY>\n"
                + "<!ATTLIST img src ENTITY #REQUIRED>\n"
                + "<!ENTITY logo SYSTEM\n"
                + "\"http://www.xmlwriter.net/logo.gif\" NDATA gif>\n"
                + "<!NOTATION gif PUBLIC \"gif viewer\">"
                + "]>";
            Assert.AreEqual(tp._rawdata.Length, tp.parseDeclaration(0));
        }

        protected override void handleComment(string text)
        {
            base.handleComment(text);
            _comment = text;
        }

        private string _comment;
    }
}
