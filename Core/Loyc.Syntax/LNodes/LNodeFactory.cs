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
		
		private LNode _emptyList, _emptySplice, _emptyTuple;
		public LNode Missing { get { return Missing_; } } // allow access through class reference

		ISourceFile _file;
		public ISourceFile File { get { return _file; } set { _file = value; } }

		public LNodeFactory(ISourceFile file) { _file = file; }

		#region Common literals, data types and access modifiers

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

		LNode _newline = null;
		public LNode TriviaNewline { get { return _newline = _newline ?? Id(S.TriviaNewline); } }
		
		/// <summary>Adds a leading newline to the node if the first attribute isn't a newline.</summary>
		/// <remarks>By convention, in Loyc languages, top-level nodes and nodes within 
		/// braces have an implicit newline, such that a leading blank line appears
		/// if you add <see cref="CodeSymbols.TriviaNewline"/>. For all other nodes,
		/// this method just ensures there is a line break.</remarks>
		public LNode OnNewLine(LNode node)
		{
			if (node.Attrs[0, Missing_].IsIdNamed(S.TriviaNewline))
				return node;
			return node.PlusAttrBefore(TriviaNewline);
		}

		#endregion

		#region Id(), Literal() and Triva()

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
			return new StdIdNode(t.Value as Symbol ?? GSymbol.Get((t.Value ?? "").ToString()),
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

		#endregion

		#region Call with LNode target

		public LNode Call(LNode target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, VList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, VList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, LNode _3, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(_1, _2).Add(_3), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, LNode _3, LNode _4, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(_1, _2).Add(_3).Add(_4), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(LNode target, params LNode[] list)
		{
			return new StdComplexCallNode(target, new VList<LNode>(list), new SourceRange(_file));
		}
		public LNode Call(LNode target, LNode[] list, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdComplexCallNode(target, new VList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		#endregion

		#region Call with Symbol target (and optional target range)

		public LNode Call(Symbol target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			return Call(target, new VList<LNode>(args), startIndex, endIndex);
		}
		public LNode Call(Symbol target, VList<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, VList<LNode> args, int startIndex, int endIndex, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex), targetStart, targetEnd, style);
		}
		public LNode Call(Symbol target, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, VList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, LNode _3, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1, _2).Add(_3), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, LNode _3, LNode _4, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1, _2).Add(_3).Add(_4), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Call(Symbol target, int startIndex, int endIndex, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default)
		{
			Debug.Assert(endIndex >= startIndex);
			Debug.Assert(targetEnd >= targetStart && targetStart >= startIndex);
			return new StdSimpleCallNode(target, VList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex), targetStart, targetEnd, style);
		}
		public LNode Call(Symbol target, LNode _1, int startIndex, int endIndex, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default)
		{
			Debug.Assert(endIndex >= startIndex);
			Debug.Assert(targetEnd >= targetStart && targetStart >= startIndex);
			return new StdSimpleCallNode(target, new VList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex), targetStart, targetEnd, style);
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, int startIndex, int endIndex, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default)
		{
			Debug.Assert(endIndex >= startIndex);
			Debug.Assert(targetEnd >= targetStart && targetStart >= startIndex);
			return new StdSimpleCallNode(target, new VList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex), targetStart, targetEnd, style);
		}
		public LNode Call(Symbol target, params LNode[] args)
		{
			return new StdSimpleCallNode(target, new VList<LNode>(args), new SourceRange(_file));
		}
		public LNode Call(Symbol target, LNode[] args, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex));
		}

		#endregion

		#region Call with string target (string is simply converted to a Symbol)

		public LNode Call(string target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1)
		{
			return Call(GSymbol.Get(target), args, startIndex, endIndex);
		}
		public LNode Call(string target, VList<LNode> args, int startIndex = -1, int endIndex = -1)
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

		#endregion

		#region Call with Token target (uses Token.Value as Symbol and Token range as target range)

		public LNode Call(Token target, IEnumerable<LNode> args, int startIndex = -1, int endIndex = -1, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(args), new SourceRange(_file, startIndex, endIndex - startIndex), style);
		}
		public LNode Call(Token target, VList<LNode> args, int startIndex = -1, int endIndex = -1, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, args, new SourceRange(_file, startIndex, endIndex - startIndex), style);
		}
		public LNode Call(Token target, int startIndex = -1, int endIndex = -1, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, VList<LNode>.Empty, new SourceRange(_file, startIndex, endIndex - startIndex), style);
		}
		public LNode Call(Token target, LNode _1, int startIndex = -1, int endIndex = -1, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1), new SourceRange(_file, startIndex, endIndex - startIndex), style);
		}
		public LNode Call(Token target, LNode _1, LNode _2, int startIndex = -1, int endIndex = -1, NodeStyle style = NodeStyle.Default)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(target, new VList<LNode>(_1, _2), new SourceRange(_file, startIndex, endIndex - startIndex), style);
		}

		#endregion

		#region Dot()

		public LNode Dot(Symbol prefix, Symbol symbol)
		{
			return new StdSimpleCallNode(S.Dot, new VList<LNode>(Id(prefix), Id(symbol)), new SourceRange(_file));
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
			int start = parts[0].Range.StartIndex;
			if (parts.Length == 1) {
				start = System.Math.Max(start, 0);
				return Call(S.Dot, parts[0], start - 1, parts[0].Range.EndIndex, start - 1, start);
			}
			var expr = Call(S.Dot, parts[0], parts[1], start, parts[1].Range.EndIndex);
			for (int i = 2; i < parts.Length; i++)
				expr = Call(S.Dot, expr, parts[i], start, parts[i].Range.EndIndex);
			return expr;
		}
		public LNode Dot(LNode prefix, Symbol symbol, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Dot, new VList<LNode>(prefix, Id(symbol)), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Dot(LNode prefix, LNode symbol, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Dot, new VList<LNode>(prefix, symbol), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Dot(LNode prefix, LNode symbol, int startIndex, int endIndex, int dotStart, int dotEnd, NodeStyle style = NodeStyle.Default)
		{
			return new StdSimpleCallNode(S.Dot, new VList<LNode>(prefix, symbol), new SourceRange(_file, startIndex, endIndex - startIndex), dotStart, dotEnd, style);
		}

		#endregion

		#region Of() (for creating generics like List<T>)

		public LNode Of(params Symbol[] list)
		{
			return new StdSimpleCallNode(S.Of, new VList<LNode>(list.SelectArray(sym => Id(sym))), new SourceRange(_file));
		}
		public LNode Of(params LNode[] list)
		{
			return new StdSimpleCallNode(S.Of, new VList<LNode>(list), new SourceRange(_file));
		}
		public LNode Of(LNode stem, LNode T1, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Of, stem, T1, startIndex, endIndex);
		}
		public LNode Of(Symbol stem, LNode T1, int startIndex = -1, int endIndex = -1)
		{
			return Of(Id(stem), T1, startIndex, endIndex);
		}
		public LNode Of(LNode stem, IEnumerable<LNode> typeParams, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Of, stem, startIndex, endIndex).PlusArgs(typeParams);
		}
		public LNode Of(Symbol stem, IEnumerable<LNode> typeParams, int startIndex = -1, int endIndex = -1)
		{
			return Of(Id(stem), typeParams, startIndex, endIndex);
		}

		#endregion

		#region Braces()

		public LNode Braces(params LNode[] contents)
		{
			return Braces(new VList<LNode>(contents));
		}
		public LNode Braces(VList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			if (endIndex > startIndex)
				return new StdSimpleCallNode(S.Braces, contents, 
					new SourceRange(_file, startIndex, endIndex - startIndex), 
					startIndex, startIndex + (endIndex > startIndex + 1 ? 1 : 0));
			else
				return new StdSimpleCallNode(S.Braces, contents, 
					new SourceRange(_file, startIndex, 0));
		}
		public LNode Braces(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			return Braces(new VList<LNode>(contents), startIndex, endIndex);
		}
		public LNode Braces(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			return Braces(new VList<LNode>(contents), startIndex, endIndex);
		}

		#endregion

		#region List() (which creates an S.AltList node), Splice() and Tuple()

		public LNode List()
		{
			if (_emptyList == null) 
				_emptyList = Call(S.AltList);
			return _emptyList;
		}
		public LNode List(params LNode[] contents)
		{
			return Call(S.AltList, contents, -1, -1);
		}
		public LNode List(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			return Call(S.AltList, contents, startIndex, endIndex);
		}
		public LNode List(VList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			return Call(S.AltList, contents, startIndex, endIndex);
		}
		public LNode List(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.AltList, contents, startIndex, endIndex);
		}

		public LNode Splice()
		{
			if (_emptySplice == null) 
				_emptySplice = Call(S.Splice);
			return _emptySplice;
		}
		public LNode Splice(params LNode[] contents)
		{
			return Call(S.Splice, contents, -1, -1);
		}
		public LNode Splice(LNode[] contents, int startIndex = -1, int endIndex = -1)
		{
			return Call(S.Splice, contents, startIndex, endIndex);
		}
		public LNode Splice(VList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			return Call(S.Splice, contents, startIndex, endIndex);
		}
		public LNode Splice(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			return Call(S.Splice, contents, startIndex, endIndex);
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
			return new StdSimpleCallNode(S.Tuple, new VList<LNode>(contents), new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Tuple(VList<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return new StdSimpleCallNode(S.Tuple, contents, new SourceRange(_file, startIndex, endIndex - startIndex));
		}
		public LNode Tuple(IEnumerable<LNode> contents, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Tuple, contents, startIndex, endIndex);
		}

		#endregion

		#region Function, property and variable definitions

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
			return new StdSimpleCallNode(S.Fn, new VList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex), startIndex, startIndex);
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
			return new StdSimpleCallNode(S.Property, new VList<LNode>(list), new SourceRange(_file, startIndex, endIndex - startIndex), startIndex, startIndex);
		}
		
		public LNode Var(LNode type, string name, LNode initValue = null, int startIndex = -1, int endIndex = -1)
		{
			return Var(type, GSymbol.Get(name), initValue, startIndex, endIndex);
		}
		public LNode Var(LNode type, Symbol name, LNode initValue = null, int startIndex = -1, int endIndex = -1)
		{
			return Var(type, Id(name), initValue, startIndex, endIndex);
		}
		public LNode Var(LNode type, LNode name, LNode initValue = null, int startIndex = -1, int endIndex = -1)
		{
			type = type ?? Missing;
			if (initValue != null)
				return Call(S.Var, type, Call(S.Assign, name, initValue), startIndex, endIndex);
			else
				return Call(S.Var, type, name, startIndex, endIndex);
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
			var list = new WList<LNode>() { type };
			list.AddRange(namesWithValues);
			return Call(S.Var, list.ToVList());
		}

		#endregion

		#region Other stuff

		public LNode InParens(LNode inner, int startIndex = -1, int endIndex = -1)
		{
			return LNodeExt.InParens(inner, File, startIndex, endIndex - startIndex);
		}

		public LNode Result(LNode expr)
		{
			return Call(S.Result, expr, expr.Range.StartIndex, expr.Range.EndIndex);
		}

		public LNode Attr(LNode attr, LNode node)
		{
			return node.PlusAttrBefore(attr);
		}
		public LNode Attr(params LNode[] attrsAndNode)
		{
			var node = attrsAndNode[attrsAndNode.Length - 1];
			var newAttrs = node.Attrs.InsertRange(0, attrsAndNode.Slice(0, attrsAndNode.Length-1).AsList());
			return node.WithAttrs(newAttrs);
		}

		public LNode Assign(Symbol lhs, LNode rhs, int startIndex = -1, int endIndex = -1)
		{
			return Assign(Id(lhs), rhs, startIndex, endIndex);
		}
		public LNode Assign(LNode lhs, LNode rhs, int startIndex = -1, int endIndex = -1)
		{
			if (endIndex < startIndex) endIndex = startIndex;
			return Call(S.Assign, new VList<LNode>(lhs, rhs), startIndex, endIndex);
		}

		#endregion
	}
}
