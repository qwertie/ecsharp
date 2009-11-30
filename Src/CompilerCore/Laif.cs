using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Loyc.Utilities;
using System.Diagnostics;
using Loyc.Runtime;
using System.Threading;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Serializes or deserializes AstNode objects to Loyc AST Interchange Format 
	/// (LAIF).
	/// </summary>
	/// <remarks>
	/// </remarks>
	public static class Laif
	{
		public static void Write(AstNode node, TextWriter writer)
		{
			Write(node, writer, "\t", 4, "", 80);
		}
		
		public static void Write(AstNode node, TextWriter writer, string tab, int tabSize, string initialIndent, int preferredLineLengthAfterInitialIndent)
		{
			LaifWriter w = new LaifWriter(writer, tab, tabSize, initialIndent, preferredLineLengthAfterInitialIndent);
			w.Write(node);
		}

		public static string ToString(AstNode node)
		{
			StringWriter w = new StringWriter();
			Write(node, w);
			return w.ToString();
		}

		public static AstNode Parse(string s)
		{
			// TODO
			throw new NotImplementedException();
		}

		public static AstNode Parse(TextReader s)
		{
			throw new NotImplementedException();
		}

		static ThreadLocalVariable<Dictionary<Type,Func<object,string>>> _objectWriters = 
		   new ThreadLocalVariable<Dictionary<Type,Func<object,string>>>(
		                       new Dictionary<Type,Func<object,string>>());
		public static Dictionary<Type, Func<object, string>> ObjectWriters
		{
			get { return _objectWriters.Value; }
		}
		static ThreadLocalVariable<Dictionary<Type,Func<string,object>>> _objectReaders = 
		   new ThreadLocalVariable<Dictionary<Type,Func<string,object>>>(
		                       new Dictionary<Type,Func<string,object>>());
		public static Dictionary<Type, Func<string,object>> ObjectReaders
		{
			get { return _objectReaders.Value; }
		}
	}

	class LaifWriter
	{
		RWList<Pair<string, int>> _indents = new RWList<Pair<string, int>>();
		TextWriter _w;
		string _tab;
		int _lineLen, _tabSize, _col, _initialLineCapacity;
		StringBuilder _line = new StringBuilder();

		public LaifWriter(TextWriter writer, string tab, int tabSize, string initialIndent, int preferredLineLengthAfterInitialIndent)
		{
			if (tabSize <= 0)
				tabSize = tab.Length;

			_indents.Push(G.Pair(initialIndent, 0));
			_w = writer;
			_tab = tab;
			_tabSize = tabSize;
			_lineLen = preferredLineLengthAfterInitialIndent;
			_col = 0;
			_initialLineCapacity = Math.Max(initialIndent.Length + _lineLen, 50);
		}
		
		public void Write(AstNode node)
		{
			Write(LoycG.UnparseID(node.NodeType.Name, EmptyCollection<string>.Default, false));
			if (node.Value != null)
			{
				WriteValue(node.Value);
			}
		}
		void WriteValue(object obj)
		{
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
		}
		void WriteValue(Symbol sym)
		{
			Write(":");
			Write(LoycG.UnparseID(sym.Name, EmptyCollection<string>.Default, false));
		}
		void WriteValue(string sym)
		{
			Write("\"");
			Write(G.EscapeCStyle(sym, EscapeC.DoubleQuotes | EscapeC.Control));
			Write("\"");
		}

		private void StartLine()
		{
			Pair<string, int> indent = _indents.Back;
			_line = new StringBuilder(indent.First, _initialLineCapacity);
			_col = indent.Second;
		}
		public void EndLine()
		{
			if (_line != null)
			{
				_w.WriteLine(_line.ToString());
				_col = 0;
				_line = null;
			}
		}
		public void Write(string text)
		{
			_line.Append(text);
			_col += text.Length;
		}
	}

	#if false
	parser LaifReader
	{

		// Assume we start by going through the C#-style lexer and EssentialTreeParser

		rule Start() {
		}
			

	}
	#endif
}
