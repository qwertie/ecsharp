using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.CompilerCore
{
	using S = CodeSymbols;
	using Loyc.Utilities;
	using Loyc.Essentials;
	using System.Diagnostics;

	public class NodePrinter : S
	{
		INodeReader _n;
		int _indent;
		StringBuilder _sb;

		public NodePrinter(INodeReader node, int indent, StringBuilder sb = null)
		{
			_n = node;
			_indent = (sbyte)indent;
			_sb = sb ?? new StringBuilder();
		}
		public string Result() { return _sb.ToString(); }


		private NodePrinter Indented { get { return new NodePrinter(_n, _indent + 1, _sb); } }
		private NodePrinter Unindented { get { return new NodePrinter(_n, _indent - 1, _sb); } }
		private NodePrinter With(INodeReader node) { return new NodePrinter(node, _indent, _sb); }
		NodePrinter Newline()
		{
			_sb.Append('\n');
			_sb.Append('\t', _indent);
			return this;
		}
		
		static readonly HashSet<Symbol> CsKeywords = new HashSet<Symbol>(new[] {
			"abstract",  "event",     "new",        "struct", 
			"as",        "explicit",  "null",       "switch", 
			"base",      "extern",    "object",     "this", 
			"bool",      "false",     "operator",   "throw", 
			"break",     "finally",   "out",        "true", 
			"byte",      "fixed",     "override",   "try", 
			"case",      "float",     "params",     "typeof", 
			"catch",     "for",       "private",    "uint", 
			"char",      "foreach",   "protected",  "ulong", 
			"checked",   "goto",      "public",     "unchecked", 
			"class",     "if",        "readonly",   "unsafe", 
			"const",     "implicit",  "ref",        "ushort", 
			"continue",  "in",        "return",     "using", 
			"decimal",   "int",       "sbyte",      "virtual", 
			"default",   "interface", "sealed",     "volatile", 
			"delegate",  "internal",  "short",      "void", 
			"do",        "is",        "sizeof",     "while", 
			"double",    "lock",      "stackalloc",
			"else",      "long",      "static",
			"enum",      "namespace", "string",
		}.Select(s => GSymbol.Get(s)));

		private static string ToIdent(Symbol sym, bool keywordPrintingMode)
		{
			Debug.Assert(sym.Name != "");
			if (keywordPrintingMode) {
				if (sym.Name[0] != '#')
					return sym.Name;
				Symbol word = GSymbol.GetIfExists(sym.Name.Substring(1));
				if (CsKeywords.Contains(word ?? GSymbol.Empty))
					return word.Name;
			} else {
				if (CsKeywords.Contains(sym))
					return "@" + sym.Name;
			}
			return sym.Name;
		}
		bool IsOneWordIdent(bool allowKeywords)
		{
			if (_n.Name.Name == "")
				return false;
			if (_n.IsCall)
				return false;
			if (allowKeywords)
				return true;
			else
				return !CsKeywords.Contains(_n.Name) && !_n.IsKeyword;
		}
		bool IsIdentAtThisLevel(bool allowKeywords, bool allowDot, bool allowTypeArgs)
		{
			if (_n.Name.Name == "")
				return false;
			if (_n.Name == S._Dot)
				return allowDot && _n.IsCall;
			if (_n.Name == _TypeArgs)
				return allowTypeArgs && _n.ArgCount >= 1;
			if (!allowKeywords && CsKeywords.Contains(_n.Name))
				return false;
			return !_n.IsKeyword;
		}


		public void PrintPrefixNotation(bool recursive)
		{
			if (_n.IsCall) {
				if (recursive)
					With(_n.Head).PrintPrefixNotation(recursive);
				else
					With(_n.Head).PrintExpr();
				_sb.Append('(');
				for (int i = 0, c = _n.ArgCount; i < c; i++) {
					if (recursive)
						With(_n.Args[i]).PrintPrefixNotation(recursive);
					else
						With(_n.Args[i]).PrintExpr();
					if (i + 1 == c)
						_sb.Append(')');
					else
						_sb.Append(", ");
				}
			} else {
				if (!_n.IsCall)
					With(_n).PrintAtom();
			}
		}

		private void PrintAtom()
		{
			if (_n.IsLiteral) {
				var v = _n.Value;
				if (v == null)
					_sb.Append("null");
				else if (v is string) {
					_sb.Append("\"");
					_sb.Append(G.EscapeCStyle(v as string, EscapeC.Control | EscapeC.DoubleQuotes));
					_sb.Append("\"");
				} else if (v is Symbol) {
					_sb.Append("$");
					_sb.Append(v.ToString()); // TODO
				} else {
					_sb.Append(v);
					if (v is float)
						_sb.Append('f');
				}
			} else {
				_sb.Append(_n.Name);
			}
		}

		public NodePrinter PrintStmts(bool autoBraces, bool initialNewline, bool forceTreatAsList = false)
		{
			if (_n.Name == _List || forceTreatAsList) {
				// Presumably we are starting in-line with some expression
				bool braces = autoBraces && _n.ArgCount != 1;
				if (braces) _sb.Append(" {");
				
				var ind = autoBraces ? Indented : this;
				for (int i = 0; i < _n.ArgCount; i++) {
					if (initialNewline || i != 0 || _n.ArgCount > 1)
						ind.Newline();
					ind.PrintStmt(false);
				}
				if (braces)
					Newline()._sb.Append('}');
			} else {
				PrintStmt(initialNewline);
			}
			return this;
		}

		NodePrinter PrintStmt(bool initialNewline)
		{
			if (initialNewline)
				Newline();

			//if (_n.HasAttrs)
			//	PrintAttr(true);

			if (_n.Name == _Braces) {
				_sb.Append('{');
				Indented.PrintStmts(false, true, true);
				Newline();
				_sb.Append("} ");
				return this;
			}
			// Default
			PrintExpr();
			_sb.Append(';');
			return this;
		}

		public NodePrinter PrintExpr()
		{
			if (_n.Calls(_NamedArg, 2) && _n.Args[0].IsSimpleSymbol)
			{
				string ident = ToIdent(_n.Args[0].Name, false);
				_sb.Append(ident.EndsWith(":") ? " : " : ": ");
				With(_n.Args[1]).PrintExpr();
			}
			else if (_n.CallsMin(_Dot, 2) && _n.Args.All(n => IsIdentAtThisLevel(false, false, true)))
			{
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i > 0) _sb.Append(".");
					With(_n.Args[i]).PrintExpr();
				}
			}
			else if (_n.CallsMin(_TypeArgs, 1) && _n.Args.All(n => IsIdentAtThisLevel(false, true, false)))
			{
				for (int i = 0; i < _n.ArgCount; i++) {
					if (i == 1) _sb.Append('<');
					else _sb.Append(", ");
					With(_n.Args[i]).PrintExpr();
				}
				_sb.Append('>');
			}
			else
				PrintPrefixNotation(false);
			return this;
		}

		//private void PrintAttr(bool stmtMode)
		//{
		//    var attrs = _n.Attrs;
		//    Debug.Assert(attrs.Count > 0);
		//	
		//    _sb.Append('[');
		//    for (int i = 0; i < attrs.Count; i++)
		//    {
		//        With(attrs[i]).PrintExpr();
		//        if (i + 1 != attrs.Count)
		//            _sb.Append(", ");
		//    }
		//    _sb.Append("] ");
		//}
	}
}
