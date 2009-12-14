using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities;
using Loyc.Runtime;
using System.IO;
using System.Diagnostics;

namespace Loyc.CompilerCore
{
	public class LaifWriter
	{
		RWList<Pair<string, int>> _indents = new RWList<Pair<string, int>>();
		TextWriter _w;
		string _tab = "  ";
		int _tabSize = 2;
		int _lineLen;
		int _col;
		int _initialLineCapacity;
		bool _writeTags = true;
		bool _writeRange = false;
		string _curFileName; // last filename written to _w

		enum Tip
		{
			None, Open, Close, NoBreakAfter
		}
		List<Pair<string, Tip>> _q = new List<Pair<string, Tip>>();

		public LaifWriter(TextWriter writer) : this(writer, true, false, 80, "") { }
		public LaifWriter(TextWriter writer, bool writeTags, bool writeRange) : this(writer, writeTags, writeRange, 80, "") { }
		public LaifWriter(TextWriter writer, bool writeTags, bool writeRange, int lineLength, string prefixOnEachLine)
		{
			_indents.Push(G.Pair(prefixOnEachLine, 0));
			_w = writer;
			_lineLen = lineLength;
			_col = 0;
			_initialLineCapacity = Math.Max(prefixOnEachLine.Length + _lineLen, 50);
			_writeTags = writeTags;
			_writeRange = writeRange;

			_keywords = new List<string>();
			_keywords.Add("null");
		}

		public string Tab
		{
			get { return _tab; }
			set { _tab = value; }
		}
		public int TabSize
		{
			get { return _tabSize; }
			set { _tabSize = value; }
		}
		public bool WriteRange
		{
			get { return _writeRange; }
			set { _writeRange = value; }
		}
		public bool WriteTags
		{
			get { return _writeTags; }
			set { _writeTags = value; }
		}

		public void Write(AstNode node)
		{
			WriteNode(node);
			Flush();
		}
		public void Write(RVList<AstNode> nodes)
		{
			WriteNodes(nodes);
			Flush();
		}

		private void Flush()
		{
			throw new NotImplementedException();
		}

		private void WriteNodes(RVList<AstNode> nodes)
		{
			WriteNode(nodes[0]);
			for (int i = 1; i < nodes.Count; i++)
			{
				Write(" ");
				WriteNode(nodes[i]);
			}
		}
		private void WriteNode(AstNode node)
		{
			Write(UnparseID(node.NodeType.Name), Tip.NoBreakAfter);
			if (node.Value != null)
			{
				Write("[", Tip.Open);
				WriteValue(node.Value);
				Write("]", Tip.Close);
			}
			if (_writeRange && node.Range != SourceRange.Nowhere)
			{
				Write(node.Range);
			}
			if (_writeTags && node.Tags.Count > 0)
			{
				Write("{", Tip.Open);
				Write(node.Tags);
				Write("}", Tip.Close);
			}
			if (!node.IsLeaf)
			{
				Write(" (", Tip.Open);
				WriteNodes(node.Children);
				Write(")", Tip.Close);
			}
		}

		List<string> _keywords;
		string UnparseID(string id)
		{
			return LoycG.UnparseID(id, _keywords, false);
		}

		private void Write(IDictionary<Symbol, object> dict)
		{
			bool first = true;
			foreach (KeyValuePair<Symbol, object> kvp in dict)
			{
				if (first)
					first = false;
				else
					Write(", ");
				Write(UnparseID(kvp.Key.Name) + ": ");
				WriteValue(kvp.Value);
			}
		}

		private void Write(SourceRange r)
		{
			if (r.BeginIndex < 0)
				Write("@?");
			else {
				Write("@", Tip.NoBreakAfter);
				if (_curFileName != r.Source.FileName) {
					_curFileName = r.Source.FileName;
					WriteValue(_curFileName);
				}
				SourcePos p = r.Begin;
				Write(string.Format("({0}:{1}):{2}", p.Line, p.PosInLine, r.Length));
			}
		}
		void WriteValue(object obj)
		{
			if (obj == null)
			{
				Write("null");
			}
			if (obj is AstNode)
			{
				Write("node ");
				WriteNode(obj as AstNode);
				return;
			}
			Symbol sym = obj as Symbol;
			if (sym != null)
			{
				WriteValue(sym);
				return;
			}
			string str = obj as string;
			if (str != null)
			{
				WriteValue(str);
				return;
			}
			IDictionary<Symbol, object> sdict = obj as IDictionary<Symbol, object>;
			if (sdict != null)
			{
				WriteValue(sdict);
				return;
			}
			if (obj is int)
			{
				Write(obj.ToString());
				return;
			}
			if (obj is double)
			{
				str = obj.ToString();
				if (!str.Contains("."))
					str += ".0";
				Write(str);
				return;
			}
			if (obj is char)
			{
				char c = (char)obj;
				if ((int)c >= 32 && c != '\'')
					Write("'" + c + "'");
				else
					Write("'" + G.EscapeCStyle(c.ToString(), EscapeC.Control | EscapeC.SingleQuotes) + "'");
				return;
			}

			string fullName = obj.GetType().FullName;
			Write(fullName);
			
			str = obj.ToString();
			for (int i = 0; i < str.Length; i++) {
				char c = str[i];
				if (!(c >= 'A' && c <= 'Z') &&
					!(c >= 'a' && c <= 'z') &&
					!(c >= '0' && c <= '9') && 
					c != '_' && c != '-')
				{
					WriteValue(str);
					return;
				}
			}
			Write(str);
		}
		void WriteValue(Symbol sym)
		{
			Write(":", Tip.NoBreakAfter);
			Write(UnparseID(sym.Name));
		}
		void WriteValue(string sym)
		{
			if (sym.Length > 10 && !sym.Contains("\"\"\"") && !sym.EndsWith("\"")) {
				Write("\"\"\"", Tip.NoBreakAfter);
				Write(sym, Tip.NoBreakAfter);
				Write("\"\"\"");
			} else {
				Write("\"", Tip.NoBreakAfter);
				Write(G.EscapeCStyle(sym, EscapeC.DoubleQuotes | EscapeC.Control), Tip.NoBreakAfter);
				Write("\"");
			}
		}

		void Write(string text)
		{
			Write(text, Tip.None);
		}
		void Write(string text, Tip tip)
		{
			_q.Add(G.Pair(text, tip));
			_col += text.Length;
			if (tip != Tip.NoBreakAfter && _col > _lineLen)
				FlushLine();
		}

		private void FlushLine()
		{
			int indent = _indents.Back.B;
			int lineLen = Math.Max(_lineLen - indent, _lineLen >> 1);
			int L = 0, col = 0, i;
			int delta_L = 0, break_i = -1;

			// Look for openers or closers at which to break the line
			for (i = 0; i < _q.Count; i++)
			{
				if ((col += _q[i].A.Length) > lineLen)
					break;
				Tip t = _q[i].B;
				if (t == Tip.Open)
				{
					if (L == 0 && delta_L >= 0) {
						break_i = i;
						delta_L = 1;
					}
					L++;
				}
				if (t == Tip.Close)
				{
					if (L <= 0) {
						break_i = i;
						delta_L = L - 1;
					}
					L--;
				}
			}

			if (break_i == -1)
			{	// No relevant opener or closer. Break by "word wrapping".
				col = 0;
				for (i = 0; i < _q.Count; i++) {
					if ((col += _q[i].A.Length) > lineLen && break_i != -1)
						break;
					if (_q[i].B != Tip.NoBreakAfter)
						break_i = i;
				}
				if (break_i == -1)
					break_i = _q.Count - 1;
			}
			
			// Flush up to and including _q[break_i].
			FlushCount(break_i + 1);

			// Change indent if delta_L != 0
			if (delta_L > 0)
				_indents.Push(G.Pair(_indents.Back.A + _tab, _indents.Back.B + _tabSize));
			else while (delta_L < 0)
				{
					_indents.Pop();
					Debug.Assert(!_indents.IsEmpty);
					if (_indents.IsEmpty)
						_indents.Push(G.Pair("", 0));
					delta_L++;
				}

		}

		private void FlushCount(int count)
		{
			StringBuilder line = new StringBuilder(_initialLineCapacity);
			line.Append(_indents.Back.A);
			for (int i = 0; i <= count; i++)
				line.Append(_q[i].A);
			
			_w.WriteLine(line.ToString());

			Lists.RemoveAt(_q, 0, count);
		}

		void FlushAll()
		{
			while (_q.Count != 0)
				FlushLine();
		}
	}
}
