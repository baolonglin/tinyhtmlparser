using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TinyHTMLParser
{
    /// <summary>
    /// Base class for XML alise parser.
    /// 
    /// Porting from python ParserBase library
    /// 
    /// author: robin lin
    /// version: 0.1
    /// </summary>
    public abstract class ParserBase
    {
        private static Regex COMMENT_CLOSE = new Regex("--\\s*>");
        private static Regex DECLNAME_MATCH = new Regex("\\s*[a-zA-Z][-_.:a-zA-Z0-9]*\\s*");
        private static Regex MARKED_SECTION_CLOSE = new Regex("]\\s*]\\s*>");
        private static Regex MSMARKED_SECTION_CLOSE = new Regex("]\\s*>");
        private static Regex DECLSTRINGLIT_MATCH = new Regex("('[^']*'|\"[^\"]*\")\\s*");

        /// <summary>
        /// All the data to be parsed
        /// </summary>
        protected string _rawdata;

        /// <summary>
        /// Current line number to be process
        /// </summary>
        protected int _lineno;

        /// <summary>
        /// Offset to the line beginning
        /// </summary>
        protected int _offset;

        private string _decl_otherchars = "";

        /// <summary>
        /// Reset the data member
        /// </summary>
        public virtual void reset()
        {
            _lineno = 1;
            _offset = 0;
        }

        /// <summary>
        /// update line number and offset.
        /// </summary>
        /// <param name="beg">the begin position of <code>_rawdata</code></param>
        /// <param name="end">the end position of <code>_rawdata</code></param>
        /// <returns>the end of the process string</returns>
        protected int updatepos(int beg, int end)
        {
            if (beg >= end)
                return end;
            string rawdata = _rawdata.Substring(beg, end - beg);
            int nlines = countLine(rawdata);
            if (nlines > 0)
            {
                _lineno += nlines;
                int pos = rawdata.LastIndexOf('\n');
                _offset = end - beg - (pos + 1);
            }
            else
            {
                _offset += end - beg;
            }
            return end;
        }

        /// <summary>
        /// Count the '\len' number in the specified text.
        /// </summary>
        /// <param name="text">the text to be parse</param>
        /// <returns>the '\len' number</returns>
        protected int countLine(string text)
        {
            return text.Split('\n').Length -1;
        }

        /// <summary>
        /// Throw the exception.
        /// </summary>
        /// <param name="message">error message user input</param>
        protected void error(string message)
        {
            throw new Exception(message);
        }

        /// <summary>
        /// Parse the comment text
        /// </summary>
        /// <param name="pos">the begin position of <code>_rawdata</code></param>
        /// <param name="report">true: call <code>handleComment</code></param>
        /// <returns>-1: syntax error, else: the end of the comment</returns>
        protected int parseComment(int pos, bool report = true)
        {
            if (_rawdata.Substring(pos, 4) != "<!--")
            {
                error("unexpected call to parse_comments()");
            }
            Match mc = COMMENT_CLOSE.Match(_rawdata, pos+4);
            if(! mc.Success) {
                return -1;
            }
            if(report) {
                handleComment(_rawdata.Substring(pos+4, mc.Index - pos - 4));
            }
            return mc.Index+mc.Length;
        }

        /// <summary>
        /// Parse the declaration.
        /// </summary>
        /// <param name="pos">current position to be parsed</param>
        /// <returns>position after parsed -1:error</returns>
        protected int parseDeclaration(int pos)
        {
            int cur = pos + 2;
            if(_rawdata.Substring(pos, 2) != "<!") {
                error("unexpect call to parse_declaration");
            }
            if (cur >= _rawdata.Length)
                return -1;

            if(_rawdata[cur] == '>')
                return cur+1;
            if(_rawdata[cur] == '-')
                return -1;
            if(cur + 2 < _rawdata.Length && _rawdata.Substring(cur, 2) == "--") {  // TODO: remove it
                return parseComment(pos);
            } else if(_rawdata[cur] == '[') {
                return parseMarkedSection(pos);
            } else {
                string decltype = scanName(ref cur, pos);
                if(cur < 0)
                    return cur;
                if(decltype == "doctype")
                    _decl_otherchars = "";
                while(cur < _rawdata.Length) {
                    char c = _rawdata[cur];
                    if(c == '>') {
                        string data = _rawdata.Substring(pos + 2, cur - pos - 2);
                        if (decltype == "doctype")
                        {
                            handleDecl(data);
                        }
                        else
                        {
                            unknownDecl(data);
                        }
                        return cur + 1;
                    } else if(c == '"' || c == '\'') {
                        Match md = DECLSTRINGLIT_MATCH.Match(_rawdata, cur);
                        if (!md.Success)
                            return -1;
                        cur = md.Index + md.Length;
                    } else if(c >= 'a' && c<= 'z' || c >= 'A' && c <= 'Z') {
                        scanName(ref cur, pos);
                    } else if(_decl_otherchars.Contains(new String(c, 1))) {
                        cur++;
                    } else if(c == '[') {
                        if (decltype == "doctype")
                        {
                            cur = parseDoctypeSubset(cur + 1, pos);
                        }
                        else if (decltype == "attlist" || decltype == "linktype" || decltype == "link" || decltype == "element")
                        {
                            error("unsupported '[' char in " + decltype + " declaration");
                        }
                        else
                        {
                            error("unexpected '[' char in declaration");
                        }
                    } else {
                        error("unexpected " + c + " char in declaration");
                    }
                }
                if(cur < 0)
                    return cur;
            }
            return -1;
        }

        /// <summary>
        /// Parse the doctype content.
        /// 
        /// e.g.
        /// <!DOCTYPE img [
        ///     <!ELEMENT ...>
        ///     ^<------pos
        ///     <!ATTLIST ...>
        ///     <!ENTITY ...>
        ///     <!NOTATION ...>
        ///     % entity_name;
        /// ]>
        ///  ^<-------------------return pos
        /// </summary>
        /// <param name="pos">position to be parsed</param>
        /// <param name="declStartPos">the declaration position</param>
        /// <returns>the end position of the declaration position, see the comment for details, -1: error</returns>
        public int parseDoctypeSubset(int pos, int declStartPos)
        {
            int len = _rawdata.Length;
            int cur = pos;
            while (cur < len)
            {
                char c = _rawdata[cur];
                if (c == '<')
                {
                    if (cur+2 >= len)
                    {
                        return -1;
                    }
                    string s = _rawdata.Substring(cur, 2);
                    if (s != "<!")
                    {
                        updatepos(declStartPos, cur + 1);
                        error("unexpected char in internal subset(in " + s + ")");
                    }

                    if (cur + 4 < len)
                    {
                        if (_rawdata.Substring(cur, 4) == "<!--") {
                            cur = parseComment(cur, false);
                            if (cur < 0) {
                                return cur;
                            }
                            continue;
                        }
                    }
                    
                    cur += 2;
                    string name = scanName(ref cur, declStartPos);
                    if (cur == -1)
                        return -1;
                    if (name == "attlist")
                    {
                        cur = parseDoctypeAttlist(cur, declStartPos);
                    }
                    else if (name == "element")
                    {
                        cur = parseDoctypeElement(cur, declStartPos);
                    }
                    else if (name == "entity")
                    {
                        cur = parseDoctypeEntity(cur, declStartPos);
                    }
                    else if (name == "notation")
                    {
                        cur = parseDoctypeNotation(cur, declStartPos);
                    }
                    else
                    {
                        updatepos(declStartPos, cur + 2);
                        error("unknown declaration " + name + " in internal subset");
                    }
                    if (cur < 0)
                    {
                        return cur;
                    }
                }
                else if (c == '%')
                {
                    if (cur + 1 == len)
                        return -1;
                    cur++;
                    scanName(ref cur, declStartPos);
                    if (cur < 0)
                        return cur;
                    if (_rawdata[cur] == ';')
                        cur++;
                }
                else if (c == ']')
                {
                    cur++;
                    while(cur < len && Char.IsWhiteSpace(_rawdata, cur))
                        cur++;
                    if(cur < len) {
                        if (_rawdata[cur] == '>')
                            return cur;
                        updatepos(declStartPos, cur);
                        error("unexpected char after internal subset");
                    } else {
                        return -1;
                    }
                }
                else if (Char.IsWhiteSpace(c))
                {
                    cur++;
                }
                else
                {
                    updatepos(declStartPos, cur);
                    error("unexpected char " + c + " in internal subset");
                }
            }
            return -1;
        }

        /// <summary>
        /// Get the error context of the current parse string.
        /// </summary>
        /// <param name="pos">the position</param>
        /// <returns>the string after the pos</returns>
        protected string getErrorContext(int pos)
        {
            if (_rawdata.Length > pos + 20)
            {
                return _rawdata.Substring(pos, 20);
            }
            else
            {
                return _rawdata.Substring(pos);
            }
        }

        /// <summary>
        /// Extract the name from the position user input.
        /// </summary>
        /// <param name="pos">the position</param>
        /// <param name="declStartPos">declare start position</param>
        /// <returns>the name of declaration, null if invalid</returns>
        protected string scanName(ref int pos, int declStartPos)
        {
            int len = _rawdata.Length;
            if(pos == len) {
                pos = -1;
                return null;
            }
            Match md = DECLNAME_MATCH.Match(_rawdata, pos);
            if(md.Success) {
                if (md.Index != pos) {      //add here to protected skip the string
                    updatepos(declStartPos, pos);
                    error("expected name token at " + getErrorContext(declStartPos));
                }
                string s = md.Groups[0].Value;
                if(pos + s.Length == len) {
                    pos = -1;
                    return null;
                }
                pos = md.Index + md.Length;
                return s.Trim().ToLower();
            } else {
                updatepos(declStartPos, pos);
                error("expected name token at " + getErrorContext(declStartPos));
            }
            return null;
        }

        /// <summary>
        /// Parse the marked section.
        /// 
        /// e.g.
        /// <![ CDATA [ blah, blah, blah. ]]>
        /// ^<---------pos
        /// 
        /// </summary>
        /// <param name="pos">begin position to be parsed</param>
        /// <param name="report">flag for report the error</param>
        /// <returns>position after parsed -1:error</returns>
        protected int parseMarkedSection(int pos, bool report = true)
        {
            if(_rawdata.Substring(pos, 3) != "<![") 
                error("unexpected call to parse_marked_section()");
            int cur = pos+3;
            string sectName = scanName(ref cur, pos);
            if(cur < 0) 
                return cur;
            Match m = null ;
            if(sectName == "temp" || sectName == "cdata" || sectName == "ignore"
                || sectName == "include" || sectName == "rcdata") {
                m = MARKED_SECTION_CLOSE.Match(_rawdata, pos+3);
            }
            else if (sectName == "if" || sectName == "else" || sectName == "endif")
            {
                m = MSMARKED_SECTION_CLOSE.Match(_rawdata, pos + 3);
            }
            else
            {
                error("unknown status keyword " + _rawdata.Substring(pos + 3, cur - pos - 3) + " in marked section");
            }
            if (!m.Success)
                return -1;
            if (report)
            {
                unknownDecl(_rawdata.Substring(pos + 3, m.Index - pos - 3));
            }
            return (m.Index + m.Length);
        }

        #region Parser doctype
        /// <summary>
        /// Parse ATTLIST declaration.
        /// 
        /// Syntax:
        /// <!ATTLIST element_name attribute_name [attribute_type] [#constraint]>
        ///           ^
        /// e.g. <!ATTLIST image height CDATA #REQUIRED>
        ///                ^
        ///      <!ATTLIST task status (important|normal) #REQUIRED>
        ///                ^
        ///      <!ATTLIST code lang NOTATION (vrml) #REQUIRED>
        ///                ^
        ///      <!ATTLIST task status (important|normal) "normal">
        ///                ^
        ///      <!ATTLIST task status NMTOKEN #FIXED "monthly">
        ///                ^
        ///      <!ATTLIST description xml:lang NMTOKEN #FIXED "en">
        ///                ^
        /// </summary>
        /// <param name="pos">postion to parse</param>
        /// <param name="declStartPos">declaration start postion</param>
        /// <returns>the position after parse, -1:error</returns>
        protected int parseDoctypeAttlist(int pos, int declStartPos)
        {
            int cur = pos;
            scanName(ref cur, declStartPos);      //element name
            if (cur< 0 || cur >= _rawdata.Length)
                return -1;
            char c = _rawdata[cur];
            if (c == '>')
                return cur + 1;
            while (true)
            {
                scanName(ref cur, declStartPos);  //attr name
                if (cur < 0 || cur >= _rawdata.Length)
                    return -1;
                c = _rawdata[cur];
                if (c == '(')                   //type --> enumerated
                {
                    cur = _rawdata.IndexOf(')', cur);
                    if (cur > 0)
                        cur++;
                    else
                        return -1;
                    while (cur < _rawdata.Length && Char.IsWhiteSpace(_rawdata, cur))
                        cur++;
                }
                else {                               //type --> CDATA, NMTOKEN, etc.
                    scanName(ref cur, declStartPos);
                }
                if (cur < 0 || cur >= _rawdata.Length)
                    return -1;
                c = _rawdata[cur];
                if (c == '(')                   //notation special
                {
                    cur = _rawdata.IndexOf(')', cur);
                    if (cur > 0)
                        cur++;
                    else
                        return -1;
                    while (cur < _rawdata.Length && Char.IsWhiteSpace(_rawdata, cur))
                        cur++;
                    if (cur >= _rawdata.Length)
                        return -1;
                    c = _rawdata[cur];
                }
                if (c == '#')               //constraints
                {
                    if (cur == _rawdata.Length - 1) {
                        return -1;
                    }
                    cur++;
                    scanName(ref cur, declStartPos);
                    if (cur < 0 || cur >= _rawdata.Length)
                        return -1;
                    c = _rawdata[cur];
                }
                if (c == '\'' || c == '"')      //default value
                {
                    Match m = DECLSTRINGLIT_MATCH.Match(_rawdata, cur);
                    if (!m.Success)
                    {
                        return -1;
                    }

                    cur = m.Index + m.Length;
                    if (cur >= _rawdata.Length)
                        return -1;
                    c = _rawdata[cur];
                }
                if (c == '>')               //end of attrlist
                {
                    return cur + 1;
                }
            }
        }

        /// <summary>
        /// Parse element declaration.
        /// 
        /// Syntax:
        /// <!ELEMENT name allowable_contents>
        /// 
        /// e.g. <!ELEMENT foo (#PCDATA)>
        ///                ^
        ///      <!ELEMENT img EMPTY>
        ///                ^
        ///      <!ELEMENT student (name, id)>
        ///                ^
        ///      <!ELEMENT student (subject+)>
        ///                ^
        ///      <!ELEMENT student (id|surname)>
        ///                ^
        ///      <!ELEMENT student (#PCDATA|id|surname|dob)*>
        ///                ^
        /// </summary>
        /// <param name="pos">position to be parsed</param>
        /// <param name="declStartPos">declaration start position</param>
        /// <returns>the position after parse, -1:error</returns>
        protected int parseDoctypeElement(int pos, int declStartPos)
        {
            int cur = pos;
            scanName(ref cur, declStartPos);  //name
            if (cur < 0)
                return cur;
            cur = _rawdata.IndexOf('>', cur);
            if (cur > 0) {
                return cur + 1;
            }
            return -1;
        }

        /// <summary>
        /// Parse entity declaration
        /// 
        /// Syntax: 
        /// <!ENTITY name "entity_value">
        /// 
        /// e.g. <!ENTITY js "Jo Smith">
        ///               ^
        ///      <!ENTITY c SYSTEM "http://www.xmlwriter.net/cpyright.xml">
        ///               ^
        ///      <!ENTITY c PUBLIC "-//W3C//TEXT copyright//EN" "http://www.w3.org/xmlspec/copyright.xml">
        ///               ^
        ///      <!ENTITY logo SYSTEM "http://www.xmlwriter.net/logo.gif" NDATA gif>
        ///               ^
        ///      <!ENTITY logo PUBLIC "-//W3C//GIF logo//EN" "http://www.w3.org/logo.gif" NDATA gif>
        ///               ^
        ///      <!ENTITY % p "(#PCDATA)">
        ///               ^
        ///      <!ENTITY % student SYSTEM "http://www.university.com/student.dtd">
        ///               ^
        /// </summary>
        /// <param name="pos">position to be parsed</param>
        /// <param name="declStartPos">the declaration start position</param>
        /// <returns>the end position after parse, -1: error</returns>
        protected int parseDoctypeEntity(int pos, int declStartPos)
        {
            int cur = pos;
            if (_rawdata[pos] == '%')
            {
                cur = pos + 1;
                while (cur < _rawdata.Length && Char.IsWhiteSpace(_rawdata, cur)) {
                    cur++;
                }
                if(cur >= _rawdata.Length) {
                    return -1;
                }
            }
            scanName(ref cur, declStartPos);
            while (true) {
                if (cur < 0 || cur >= _rawdata.Length)
                    return -1;
                char c = _rawdata[cur];
                if (c == '"' || c == '\'')
                {
                    Match m = DECLSTRINGLIT_MATCH.Match(_rawdata, cur);
                    if (!m.Success) {
                        return -1;
                    }
                    cur = m.Index + m.Length;
                }
                else if (c == '>')
                {
                    return cur + 1;
                }
                else
                {
                    scanName(ref cur, declStartPos);
                }
            }
        }

        /// <summary>
        /// Parser notation declaration.
        /// 
        /// Syntax:
        /// <!NOTATION name [SYSTEM "URI"|PUBLIC "public_ID" ["URI"]]>
        /// 
        /// e.g. <!NOTATION gif PUBLIC "gif viewer">
        ///                 ^
        ///      <!NOTATION gif PUBLIC "gif viewer" "http://www.w3.org/gifviewer.xml">
        ///                 ^
        /// </summary>
        /// <param name="pos">position to be parsed</param>
        /// <param name="declStartPos">the declaration start position</param>
        /// <returns>the end position after parse, -1: error</returns>
        protected int parseDoctypeNotation(int pos, int declStartPos)
        {
            int cur = pos;
            scanName(ref cur, declStartPos);
            while (true)
            {
                if (cur < 0 || cur >= _rawdata.Length)
                    return -1;
                char c = _rawdata[cur];
                if (c == '>')
                {
                    return cur + 1;
                }
                else if (c == '"' || c == '\'')
                {
                    Match m = DECLSTRINGLIT_MATCH.Match(_rawdata, cur);
                    if (!m.Success)
                        return -1;
                    cur = m.Index + m.Length;
                }
                else
                {
                    scanName(ref cur, declStartPos);
                }
            }
        }
        #endregion

        #region Virtual method to be override
        protected virtual void unknownDecl(string text)
        {
        }

        protected virtual void handleDecl(string text)
        {
        }

        protected virtual void handleComment(string text)
        {
        }
        #endregion
    }
}
