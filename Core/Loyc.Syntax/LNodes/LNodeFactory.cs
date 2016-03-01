using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using System.Diagnostics;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	/// <summary>Contains helper methods for creating <see cref="LNode"/>s.
	/// An LNodeFactory holds a reference to the current source file (<see cref="File"/>) 
	/// so that it does not need to be repeated every time you create a node.
	/// </summary>
	public class LNodeFactory
	{
		public static readonly LNode Missing_ = new StdIdNode(S.Missing, new SourceRange(null));
		
		private LNode _emptyList, _emptyTuple, _inParens;
		public LNode Missing { get { return Missing_; } } // allow access through class reference

		// Common literals
		public LNode @true { get { return Literal(true); } }
		public LNode @false { get { return Literal(false); } }
		public LNode @null { get { return Literal(null); } }
		public LNode @void { get { return Literal(@void.Value); } }
		public LNode int_0 { get { return Literal(0); } }
		public LNode int_1 { get { return Literal(1); } }
		public LNode string_empty { get { return Literal(""); } }

		public LNode @this { get { return Id(S.This); } }
		public LNode @base { get { return Id(S.Base); } }

		public LNode DefKeyword { get { return Id(S.Fn, -1); } }

		// Standard data types (marked synthetic)
		public LNode Void { get { return Id(S.Void, -1); } }
		public LNode String { get { return Id(S.String, -1); } }
		public LNode Object { get { return Id(S.Object, -1); } }
		public LNode Char { get { return Id(S.Char, -1); } }
		public LNode Bool { get { return Id(S.Bool, -1); } }
		public LNode Int8 { get { return Id(S.Int8, -1); } }
		public LNode Int16 { get { return Id(S.Int16, -1); } }
		public LNode Int32 { get { return Id(S.Int32, -1); } }
		public LNode Int64 { get { return Id(S.Int64, -1); } }
		public LNode UInt8 { get { return Id(S.UInt8, -1); } }
		public LNode UInt16 { get { return Id(S.UInt16, -1); } }
		public LNode UInt32 { get { return Id(S.UInt32, -1); } }
		public LNode UInt64 { get { return Id(S.UInt64, -1); } }
		public LNode Single { get { return Id(S.Single, -1); } }
		public LNode Double { get { return Id(S.Double, -1); } }
		public LNode Decimal { get { return Id(S.Decimal, -1); } }

		// Standard access modifiers
		public LNode Internal { get { return Id(S.Internal, -1); } }
		public LNode Public { get { return Id(S.Public, -1); } }
		public LNode ProtectedIn { get { return Id(S.ProtectedIn, -1); } }
		public LNode Protected { get { return Id(S.Protected, -1); } }
		public LNode Private { get { return Id(S.Private, -1); } }

		public LNode True { get { return Literal(true); } }
		public LNode False { get { return Literal(false); } }
		public LNode Null { get { return Literal(null); } }

		ISourceFile _file;
		public ISourceFile File { get { return _file; } set { _file = value; } }

		public LNodeFactory(ISourceFile file) { _file = file; }


		// Atoms: identifier symbols (including keywords) and literals
		public LNode Id(string name, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdIdNode(GSymbol.Get(name), new SourceRange(_file, startIndex, endIndex-startIndex));
		}
		public LNode Id(Symbol name, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdIdNode(name, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Id(Token t)
		{
			return new StdIdNode(t.Value as Symbol ?? GSymbol.Get(t.Value.ToString()),
				new SourceRange(_file, t.StartIndex, t.Length), t.Style);
		}
		public LNode Literal(object value, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdLiteralNode(value, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Literal(Token t)
		{
			return new StdLiteralNode(t.Value, new SourceRange(_file, t.StartIndex, t.Length), t.Style);
		}
		/// <summary>Creates a trivia node named <c>"#trivia_" + suffix</c> with the 
		/// specified Value attached.</summary>
		/// <remarks>This method only adds the prefix <c>#trivia_</c> if it is not 
		/// already present in the 'suffix' argument.</remarks>
		public LNode Trivia(string suffix, object value)
		{
			string name = suffix.StartsWith("#trivia_") ? suffix : "#trivia_" + suffix;
			return LNode.Trivia(GSymbol.Get(name), value, new SourceRange(_file));
		}
		/// <summary>Creates a trivia node with the specified Value attached.</summary>
		/// <seealso cref="LNode.Trivia(Symbol, object, LNode)"/>
		public LNode Trivia(Symbol name, object value, int startIndex = -1, int endIndex = -1)
		{
			return LNode.Trivia(name, value, new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		// Calls
		public LNode Call(LNode target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, RVList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, RVList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, LNode _3, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(_1, _2).Add(_3), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, LNode _3, LNode _4, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(_1, _2).Add(_3).Add(_4), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, params LNode[] list)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(list), new SourceRange(_file));
		}
		public LNode Call(LNode target, LNode[] list, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new RVList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		public LNode Call(Symbol target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, RVList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, RVList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, LNode _3, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2).Add(_3), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, LNode _3, LNode _4, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2).Add(_3).Add(_4), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, params LNode[] args)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(args), new SourceRange(_file));
		}
		public LNode Call(Symbol target, LNode[] args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		public LNode Call(Token target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Token target, RVList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Token target, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, RVList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Token target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Token target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex));
		}


		public LNode Call(string target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), args, startIndex, endIndex);
		}
		public LNode Call(string target, RVList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), args, startIndex, endIndex);
		}
		public LNode Call(string target, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), startIndex, endIndex);
		}
		public LNode Call(string target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), _1, startIndex, endIndex);
		}
		public LNode Call(string target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), _1, _2, startIndex, endIndex);
		}
		public LNode Call(string target, LNode _1, LNode _2, LNode _3, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), _1, _2, _3, startIndex, endIndex);
		}
		public LNode Call(string target, LNode _1, LNode _2, LNode _3, LNode _4, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), _1, _2, _3, _4, startIndex, endIndex);
		}
		public LNode Call(string target, params LNode[] args)
		{
			return Call(GSymbol.Get(target), args);
		}
		public LNode Call(string target, LNode[] args, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), args, startIndex, endIndex);
		}


		public LNode Dot(Symbol prefix, Symbol symbol)
		{
			return new StdSimpleCallNode(S.Dot, new RVList<LNode>(Id(prefix), Id(symbol)), new SourceRange(_file));
		}
		public LNode Dot(params string[] symbols)
		{
			return Dot(symbols.SelectArray(s => Id(GSymbol.Get(s))));
		}
		public LNode Dot(params Symbol[] symbols)
		{
			return Dot(symbols.SelectArray(s => Id(s)));
		}
		public LNode Dot(params LNode[] parts)
		{
			if (parts.Length == 1)
				return Call(S.Dot, parts[0]);
			var expr = Call(S.Dot, parts[0], parts[1]);
			for (int i = 2; i < parts.Length; i++)
				expr = Call(S.Dot, expr, parts[i]);
			return expr;
		}
		public LNode Dot(LNode prefix, Symbol symbol, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Dot, new RVList<LNode>(prefix, Id(symbol)), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Dot(LNode prefix, LNode symbol, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Dot, new RVList<LNode>(prefix, symbol), new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		public LNode Of(params Symbol[] list)
		{
			return new StdSimpleCallNode(S.Of, new RVList<LNode>(list.SelectArray(sym => Id(sym))), new SourceRange(_file));
		}
		public LNode Of(params LNode[] list)
		{
			return new StdSimpleCallNode(S.Of, new RVList<LNode>(list), new SourceRange(_file));
		}
		public LNode Of(LNode stem, LNode T1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Of, stem, T1, startIndex, endIndex);
		}
		public LNode Of(LNode stem, IEnumerable<LNode> typeParams, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Of, stem, startIndex, endIndex).PlusArgs(typeParams);
		}
		public LNode Of(Symbol stem, IEnumerable<LNode> typeParams, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Of, Id(stem), startIndex, endIndex).PlusArgs(typeParams);
		}

		public LNode Braces(params LNode[] contents)
		{
			return Braces(contents, -1);
		}
		public LNode Braces(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Braces, new RVList<LNode>(contents), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Braces(RVList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Braces, contents, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Braces(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Braces, contents, startIndex, endIndex);
		}

		public LNode Assign(LNode lhs, LNode rhs, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Assign, new RVList<LNode>(lhs, rhs), startIndex, endIndex);
		}

		public LNode List()
		{
			if (_emptyList == null) 
				_emptyList = Call(S.AltList);
			return _emptyList;
		}
		public LNode List(params LNode[] contents)
		{
			return List(contents, -1);
		}
		public LNode List(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.AltList, new RVList<LNode>(contents), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode List(RVList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.AltList, contents, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode List(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.AltList, contents, startIndex, endIndex);
		}

		public LNode Tuple()
		{
			if (_emptyTuple == null) 
				_emptyTuple = Call(S.Tuple);
			return _emptyTuple;
		}
		public LNode Tuple(params LNode[] contents)
		{
			return Tuple(contents, -1);
		}
		public LNode Tuple(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Tuple, new RVList<LNode>(contents), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Tuple(RVList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Tuple, contents, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Tuple(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Tuple, contents, startIndex, endIndex);
		}

		public LNode Fn(LNode retType, Symbol name, LNode argList, LNode body = null, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Fn(retType, Id(name), argList, body, startIndex, endIndex);
		}
		public LNode Fn(LNode retType, LNode name, LNode argList, LNode body = null, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			CheckParam.Arg("argList", argList.Name == S.AltList || argList.Name == S.Missing);
			LNode[] list = body == null 
				? new[] { retType, name, argList }
				: new[] { retType, name, argList, body };
			return new StdSimpleCallNode(S.Fn, new RVList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Property(LNode type, LNode name, LNode body = null, int startIndex = -1, int endIndex = -1)
		{
			return Property(type, name, Missing_, body, null, startIndex, endIndex);
		}
		public LNode Property(LNode type, LNode name, LNode argList, LNode body, LNode initializer = null, int startIndex = -1, int endIndex = -1)
		{
			argList = argList ?? Missing_;
			CheckParam.Arg("body with initializer", initializer == null || (body != null && body.Calls(S.Braces)));
			if (endIndex < startIndex) endIndex = startIndex;
			LNode[] list = body == null
				? new[] { type, name, argList, }
				: initializer == null
				? new[] { type, name, argList, body }
				: new[] { type, name, argList, body, initializer };
			return new StdSimpleCallNode(S.Property, new RVList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		
		public LNode Var(LNode type, string name, LNode initValue = null)
		{
			return Var(type, GSymbol.Get(name), initValue);
		}
		public LNode Var(LNode type, Symbol name, LNode initValue = null)
		{
			return Var(type, Id(name), initValue);
		}
		public LNode Var(LNode type, LNode name, LNode initValue = null)
		{
			type = type ?? Missing;
			if (initValue != null)
				return Call(S.Var, type, Call(S.Assign, name, initValue));
			else
				return Call(S.Var, type, name);
		}
		public LNode Var(LNode type, LNode name)
		{
			return Call(S.Var, type ?? Missing, name);
		}
		public LNode Vars(LNode type, params Symbol[] names)
		{
			type = type ?? Missing;
			var list = new List<LNode>(names.Length + 1) { type };
			list.AddRange(names.Select(n => Id(n)));
			return Call(S.Var, list.ToArray());
		}
		public LNode Vars(LNode type, params LNode[] namesWithValues)
		{
			type = type ?? Missing;
			var list = new RWList<LNode>() { type };
			list.AddRange(namesWithValues);
			return Call(S.Var, list.ToRVList());
		}

		public LNode InParens(LNode inner, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			_inParens = _inParens ?? Id(S.TriviaInParens);
			inner = inner.PlusAttr(_inParens);
			if (startIndex != -1 && endIndex != -1)
				return inner.WithRange(startIndex, endIndex);
			else
				return inner;
		}

		public LNode Result(LNode expr)
		{
			return Call(S.Result, expr, expr.Range.StartIndex, expr.Range.EndIndex);
		}

		public LNode Attr(LNode attr, LNode node)
		{
			return node.PlusAttr(attr);
		}
		public LNode Attr(params LNode[] attrsAndNode)
		{
			var node = attrsAndNode[attrsAndNode.Length - 1];
			var attrs = node.Attrs;
			for (int i = 0; i < attrsAndNode.Length - 1; i++)
				attrs.Add(attrsAndNode[i]);
			return node.WithAttrs(attrs);
		}
	}
}
