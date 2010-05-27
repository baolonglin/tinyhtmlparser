using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace TinyHTMLParser
{
    /// <summary>
    /// Parser the HTML content.
    /// 
    /// Porting from python HTMLParser library
    /// 
    /// author: robin lin
    /// version: 0.1
    /// </summary>
    public class HTMLParser : ParserBase
    {
        private static Regex INTERESTING_NORMAL = new Regex("[&<]");
        private static Regex STARTTAG_OPEN = new Regex("\\s*<[a-zA-Z]");
        private static Regex CHAR_REF = new Regex("&#(?:[0-9]+|[xX][0-9a-fA-F]+)[^0-9a-fA-F]");
        private static Regex ENTITY_REF = new Regex("&([a-zA-Z][-.a-zA-Z0-9]*)[^a-zA-Z0-9]");
        private static Regex INCOMPLETE = new Regex("&[a-zA-Z#]");
        private static Regex TAGFIND = new Regex("[a-zA-Z][-.a-zA-Z0-9:_]*");
        private static Regex ATTRFIND = new Regex("\\s*([a-zA-Z_][-.:a-zA-Z_0-9]*)(\\s*=\\s*('[^']*\'|\"[^\"]*\"|[-a-zA-Z0-9./,:;+*%?!&$\\(\\)_#=~@]*))?");
        private static Regex ENDTAGFIND = new Regex("</\\s*([a-zA-Z][-.a-zA-Z0-9:_]*)\\s*>");
        private static Regex ENDENDTAG = new Regex(">");
        private static Regex PICLOSE = new Regex(">");
        private static Regex LOCATE_START_TAG_END = new Regex("<[a-zA-Z][-.a-zA-Z0-9:_]*"   // tag name
            + "(?:\\s+"                                                                     // whitespace before attribute name
            + "(?:[a-zA-Z_][-.:a-zA-Z0-9_]*"                                                // attribute name
            + "(?:\\s*=\\s*"                                                                // value indicator
            + "(?:'[^']*'"                                                                  // LITA-enclosed value
            + "|\"[^\"]*\""                                                                 // LIT-enclosed value
            + "|[^'\">\\s]+"                                                                // bare value
            + ")"
            + ")?"
            + ")"
            + ")*"
            + "\\s*");                                                                      // trailing whitespace
        private static Regex INTERESTING_CDATA = new Regex("<(/|\\Z)");

        /// <summary>
        /// Last tag name
        /// </summary>
        private string _lastTag;

        /// <summary>
        /// The regex what to execute.
        /// </summary>
        private Regex _interesting;

        /// <summary>
        /// The start tag text.
        /// </summary>
        private string _startTagText = "";
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HTMLParser()
        {
            reset();
        }

        /// <summary>
        /// Reset all the data member.
        /// </summary>
        public override void reset() {
            base.reset();
            _rawdata = "";
            _lastTag = "???";
            _interesting = INTERESTING_NORMAL;
        }

        /// <summary>
        /// Set the text what to be parsed, and parse the content.
        /// 
        /// This is the only interface user can call.
        /// </summary>
        /// <param name="data">the text to be pared</param>
        public virtual void feed(string data)
        {
            _rawdata += data;
            goAhead(false);
        }

        /// <summary>
        /// handle data as far as reasonable.
        /// </summary>
        /// <param name="end">true: force handling all data as if followed by EOF marker.</param>
        private void goAhead(bool end)
        {
            int i = 0, j;
            int len = _rawdata.Length;
            while (i < len)
            {
                Match m = _interesting.Match(_rawdata, i);
                if (m.Success)
                    j = m.Index;
                else
                    j = len;
                if (i < j)
                    handleData(_rawdata.Substring(i, j-i));
                i = updatepos(i, j);
                if (i == len)
                    break;
                if(i < len && _rawdata[i] == '<') {
                    int k = -1;
                    Match ms = STARTTAG_OPEN.Match(_rawdata, i);
                    if (ms.Success && ms.Index == i) {
                        k = parseStartTag(i);
                    } else if (i + 3 < len && _rawdata.Substring(i, 4) == "<!--") {
                        k = parseComment(i);
                    } else if (i + 1 < len && _rawdata[i + 1] == '!') {
                        k = parseDeclaration(i);
                    } else  if (i + 1 < len && _rawdata[i+1] == '/') {
                        k = parseEndTag(i);
                    }
                    else if (i + 1 < len && _rawdata[i + 1] == '?') {
                        k = parsePi(i);
                    } 
                    else if ((i + 1) < len) {
                        handleData("<");
                        k = i + 1;
                    }
                    else {
                        break;
                    }
                    if (k < 0)
                    {
                        if (end)
                        {
                            error("EOF in middle of construct");
                        }
                        break;
                    }
                    i = updatepos(i, k);
                }
                else if (i+1 < len && _rawdata.Substring(i, 2) == "&#")
                {
                    Match mc = CHAR_REF.Match(_rawdata, i);
                    if (mc.Success)
                    {
                        string name = mc.Groups.ToString().Substring(2);
                        handleCharRef(name);
                        int k = mc.Index + mc.Length;
                        if (_rawdata[k - 1] != ';')
                        {
                            k -= 1;
                        }
                        i = updatepos(i, k);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (i < len && _rawdata[i] == '&')
                {
                    Match me = ENTITY_REF.Match(_rawdata, i);
                    if (me.Success)
                    {
                        string name = me.Groups[0].Value;
                        handleEntityRef(name);
                        int k = me.Index + me.Length;
                        if (_rawdata[k - 1] != ';')
                        {
                            k -= 1;
                        }
                        i = updatepos(i, k);
                        continue;
                    }
                    Match mi = INCOMPLETE.Match(_rawdata, i);
                    if (mi.Success)
                    {
                        if (end && mi.Groups.ToString().Equals(_rawdata.Substring(i)))
                        {
                            error("EOF in middle of entity or char ref");
                        }
                        break;
                    }
                    else if ((i + 1) < len)
                    {
                        handleData("&");
                        i = updatepos(i, i + 1);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    error("INTERESTING.match() lied");
                }

            }
            if (end && i < len)
            {
                handleData(_rawdata.Substring(i));
                i = updatepos(i, len);
            }
            _rawdata = _rawdata.Substring(i);
        }

        /// <summary>
        /// Get the current start tag text.
        /// </summary>
        /// <returns>the current start tag text, contain the attributes</returns>
        protected string getStartTagText()
        {
            return _startTagText;
        }

        /// <summary>
        /// Parse the start tag.
        /// </summary>
        /// <param name="pos">the position to be parsed</param>
        /// <returns>the postion after parsed -1:error</returns>
        protected int parseStartTag(int pos)
        {
            _startTagText = "";
            int endpos = checkForWholeStartTag(pos);
            if (endpos < 0)
            {
                return endpos;
            }
            _startTagText = _rawdata.Substring(pos, endpos - pos);
            List<pair> attrs = new List<pair>();
            Match mt = TAGFIND.Match(_rawdata, pos + 1);
            if (!mt.Success)
            {
                error("unexpected AsyncCallback to parse_starttag");
            }
            int cur = mt.Index + mt.Length;
            _lastTag = _rawdata.Substring(pos + 1, cur - pos - 1).ToLower();
            while (cur < endpos)
            {
                Match ma = ATTRFIND.Match(_rawdata, cur);
                if(!ma.Success || ma.Index != cur) 
                    break;
                string attrname = ma.Groups[1].Value;
                string rest = ma.Groups[2].Value;
                string attrvalue = ma.Groups[3].Value;

                if(rest == null || rest.Equals(String.Empty))
                    attrvalue = "";
                else if(attrvalue[0] == '\'' && '\''== attrvalue[attrvalue.Length -1]
                    || attrvalue[0] == '"' && attrvalue[attrvalue.Length-1] == '"') {
                    attrvalue = attrvalue.Substring(1, attrvalue.Length-2);
                    attrvalue = unescape(attrvalue);
                }
                attrs.Add(new pair(attrname.ToLower(), attrvalue));
                cur = ma.Index + ma.Length;
            }
            string end = _rawdata.Substring(cur, endpos-cur).Trim();
            if(end != ">" && end != "/>") {
                error("junk characters in start tag: " + getErrorContext(cur));
            }
            if(end.EndsWith("/>")) {
                handleStartEndTag(_lastTag, attrs);
            } else {
                handleStartTag(_lastTag, attrs);
                if(_lastTag == "script" || _lastTag == "style") {
                    setCdataMode();
                }
            }
            return endpos;
        }

        /// <summary>
        /// Parser the end tag.
        /// </summary>
        /// <param name="pos">position of the text to be parsed</param>
        /// <returns>postion after parsed -1:error</returns>
        protected int parseEndTag(int pos)
        {
            if (_rawdata.Substring(pos, 2) != "</")       //can't be here
                error("unexpected call to parse_endtag");

            Match me = ENDENDTAG.Match(_rawdata, pos + 1);
            if (!me.Success) {
                return -1;
            }
            int cur = me.Index + me.Length;
            me = ENDTAGFIND.Match(_rawdata, pos);
            if (!me.Success) {
                error("bad end tag: " + _rawdata.Substring(pos, cur - pos));
            }
            string tag = me.Groups[1].Value;
            handleEndTag(tag.ToLower());
            clearCdataMode();
            return cur;
        }

        /// <summary>
        /// Delegate method to be used for replace special character.
        /// </summary>
        /// <param name="m">Regex match result</param>
        /// <returns>the string after process, replacer string</returns>
        private string unescapeEvaluator(Match m)
        {
            String s = m.Groups[0].Value;
            if (s[0] == '#')
            {
                int c = 0;
                s = s.Substring(1);
                if (s[0] == 'x' || s[0] == 'X')
                {
                    c = Int32.Parse(s.Substring(1), System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    c = Int32.Parse(s);
                }
                return Char.ConvertFromUtf32(c);
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Remove the special character quoting.
        /// </summary>
        /// <param name="s">the string to be process</param>
        /// <returns>the string after process</returns>
        private string unescape(string s)
        {
            if (!s.Contains("&"))
                return s;

            Regex re = new Regex("&(#?[xX]?(?:[0-9a-fA-F]+|\\w{1,8}));");
            return re.Replace(s, unescapeEvaluator);
        }

        /// <summary>
        /// Switch the interest following text.
        /// </summary>
        private void clearCdataMode()
        {
            _interesting = INTERESTING_NORMAL;
        }

        /// <summary>
        /// Switch current interest following text.
        /// </summary>
        private void setCdataMode()
        {
            _interesting = INTERESTING_CDATA;
        }

        /// <summary>
        /// Check the start tag.
        /// </summary>
        /// <param name="pos">the begin position of the tag</param>
        /// <returns>the start tag end -1:error</returns>
        protected int checkForWholeStartTag(int pos)
        {
            Match m = LOCATE_START_TAG_END.Match(_rawdata, pos);
            if (m.Success)
            {
                int cur = m.Index + m.Length;
                if (cur >= _rawdata.Length)
                    return -1;
                char next = _rawdata[cur];
                if (next == '>')
                    return cur + 1;
                if (next == '/')
                {
                    if (cur + 1 >= _rawdata.Length)
                    {
                        return -1;
                    }
                    if (_rawdata[cur + 1] == '>')
                        return cur + 2;
                    updatepos(pos, cur + 1);
                    error("malformed empty start tag");
                }
                if (Char.IsLetter(next) || next == '=' || next == '/')
                {
                    return -1;
                }
                updatepos(pos,cur);
                error("malformed start tag");
            }
            error("we should not get here!");
            return -1;
        }

        /// <summary>
        /// Parse the pi section.
        /// </summary>
        /// <param name="pos">the begin position to be parsed</param>
        /// <returns>position after parsed -1:error</returns>
        private int parsePi(int pos) {
            if(_rawdata.Substring(0, 2) == "<?")
                error("unexpected call to parse_pi");
            Match mp = PICLOSE.Match(_rawdata, pos+2);
            if(!mp.Success) 
                return -1;
            int cur = mp.Index;
            handlePi(_rawdata.Substring(pos+2, cur-pos-2));
            cur = mp.Index + mp.Length;
            return cur;
        }

        /// <summary>
        /// Handle the tag contain end tag.
        /// 
        /// e.g. <br />
        /// </summary>
        /// <param name="tag">the tag name</param>
        /// <param name="attrs">the attributes contained by tag</param>
        private void handleStartEndTag(string tag, List<pair> attrs)
        {
            handleStartTag(tag, attrs);
            handleEndTag(tag);
        }

        #region Virtual method to be override
        protected virtual void handlePi(string data)
        {
        }

        protected virtual void handleCharRef(string name)
        {
        }

        protected virtual void handleEntityRef(string name)
        {
        }

        protected virtual void handleStartTag(string tag, List<pair> attrs)
        {
        }

        protected virtual void handleEndTag(string tag)
        {
        }

        protected virtual void handleData(string data)
        {
        }
        #endregion

        #region Internal class
        /// <summary>
        /// Class for storing the attributes.
        /// 
        /// We simply use this than KeyValuePair.
        /// </summary>
        public class pair
        {
            public string name;
            public string value;
            public pair(string n, string v)
            {
                name = n;
                value = v;
            }
        }
        #endregion
    }
}
