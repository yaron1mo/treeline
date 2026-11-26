using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Treeline.Sim
{
    /// <summary>
    /// Minimal JSON reader so Sim needs no packages and no UnityEngine.
    /// Objects become Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;,
    /// numbers double, plus string, bool and null.
    /// </summary>
    public static class Json
    {
        public static object Parse(string text)
        {
            var p = new Reader(text);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (!p.AtEnd) throw p.Error("Unexpected text after the JSON value");
            return v;
        }

        sealed class Reader
        {
            readonly string s;
            int i;
            public Reader(string text) { s = text ?? ""; }
            public bool AtEnd => i >= s.Length;

            public Exception Error(string msg) => new FormatException($"{msg} at position {i}");

            public void SkipWs()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object ReadValue()
            {
                if (AtEnd) throw Error("Unexpected end");
                char c = s[i];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ReadNumber();
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error("Expected " + word);
                i += word.Length;
            }

            Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                i++; // {
                SkipWs();
                if (i < s.Length && s[i] == '}') { i++; return d; }
                for (;;)
                {
                    SkipWs();
                    if (AtEnd || s[i] != '"') throw Error("Expected a key");
                    string key = ReadString();
                    SkipWs();
                    if (AtEnd || s[i] != ':') throw Error("Expected ':'");
                    i++;
                    SkipWs();
                    d[key] = ReadValue();
                    SkipWs();
                    if (AtEnd) throw Error("Unterminated object");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw Error("Expected ',' or '}'");
                }
            }

            List<object> ReadArray()
            {
                var list = new List<object>();
                i++; // [
                SkipWs();
                if (i < s.Length && s[i] == ']') { i++; return list; }
                for (;;)
                {
                    SkipWs();
                    list.Add(ReadValue());
                    SkipWs();
                    if (AtEnd) throw Error("Unterminated array");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return list; }
                    throw Error("Expected ',' or ']'");
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                i++; // opening quote
                while (i < s.Length)
                {
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) break;
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw Error("Bad \\u escape");
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw Error("Bad escape");
                    }
                }
                throw Error("Unterminated string");
            }

            double ReadNumber()
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (start == i) throw Error("Unexpected character '" + s[i] + "'");
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
