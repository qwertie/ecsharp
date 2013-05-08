using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ecs;
using S = ecs.CodeSymbols;
using Loyc.Utilities;
using Loyc.CompilerCore;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>Contains static helper methods for creating <see cref="LNode"/>s.
	/// Also contains the Cache method, which deduplicates subtrees that have the
	/// same structure.
	/// </summary>
	public class GreenFactory
	{
		public static readonly LNode Missing = new StdSymbolNode(S.Missing, new SourceRange(null));
		public LNode _Missing { get { return Missing; } } // allow access through class reference

		// Common literals
		public LNode @true { get { return Literal(true); } }
		public LNode @false { get { return Literal(false); } }
		public LNode @null { get { return Literal(null); } }
		public LNode @void { get { return Literal(ecs.@void.Value); } }
		public LNode int_0 { get { return Literal(0); } }
		public LNode int_1 { get { return Literal(1); } }
		public LNode string_empty { get { return Literal(""); } }

		public LNode DefKeyword { get { return Symbol(S.Def, -1); } }
		public LNode EmptyList { get { return Symbol(S.List, -1); } }

		// Standard data types (marked synthetic)
		public LNode Void { get { return Symbol(S.Void, -1); } }
		public LNode String { get { return Symbol(S.String, -1); } }
		public LNode Char { get { return Symbol(S.Char, -1); } }
		public LNode Bool { get { return Symbol(S.Bool, -1); } }
		public LNode Int8 { get { return Symbol(S.Int8, -1); } }
		public LNode Int16 { get { return Symbol(S.Int16, -1); } }
		public LNode Int32 { get { return Symbol(S.Int32, -1); } }
		public LNode Int64 { get { return Symbol(S.Int64, -1); } }
		public LNode UInt8 { get { return Symbol(S.UInt8, -1); } }
		public LNode UInt16 { get { return Symbol(S.UInt16, -1); } }
		public LNode UInt32 { get { return Symbol(S.UInt32, -1); } }
		public LNode UInt64 { get { return Symbol(S.UInt64, -1); } }
		public LNode Single { get { return Symbol(S.Single, -1); } }
		public LNode Double { get { return Symbol(S.Double, -1); } }
		public LNode Decimal { get { return Symbol(S.Decimal, -1); } }

		// Standard access modifiers
		public LNode Internal { get { return Symbol(S.Internal, -1); } }
		public LNode Public { get { return Symbol(S.Public, -1); } }
		public LNode ProtectedIn { get { return Symbol(S.ProtectedIn, -1); } }
		public LNode Protected { get { return Symbol(S.Protected, -1); } }
		public LNode Private { get { return Symbol(S.Private, -1); } }

		ISourceFile _file;
		public ISourceFile File { get { return _file; } set { _file = value; } }

		public GreenFactory(ISourceFile file) { _file = file; }


		// Atoms: symbols (including keywords) and literals
		public LNode Symbol(string name, int position = -1, int sourceWidth = -1)
		{
			return new StdSymbolNode(GSymbol.Get(name), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Symbol(Symbol name, int position = -1, int sourceWidth = -1)
		{
			return new StdSymbolNode(name, new SourceRange(_file, position, sourceWidth));
		}
		public LNode Literal(object value, int position = -1, int sourceWidth = -1)
		{
			return new StdLiteralNode(value, new SourceRange(_file, position, sourceWidth));
		}
		/// <summary>Creates a node named <c>"#trivia_" + suffix</c> with the 
		/// specified Value attached.</summary>
		/// <remarks>This method adds the prefix <c>#trivia_</c> if it is not 
		/// already present in the 'suffix' argument. See <see cref="GreenValueHolder"/> 
		/// for more information.</remarks>
		public LNode Trivia(string suffix, object value)
		{
			string name = suffix.StartsWith("#trivia_") ? suffix : "#trivia_" + suffix;
			return new StdTriviaNode(GSymbol.Get(name), value, new SourceRange(_file));
		}
		/// <summary>Creates a trivia node with the specified Value attached.</summary>
		/// <remarks>This method asserts that the 'name' argument already starts 
		/// with the prefix '<c>#trivia_</c>'. See <see cref="GreenValueHolder"/> 
		/// for more information.</remarks>
		public LNode Trivia(Symbol name, object value)
		{
			Debug.Assert(name.Name.StartsWith("#trivia_"));
			return new StdTriviaNode(name, value, new SourceRange(_file));
		}

		// Calls
		public LNode Call(LNode target, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(target, RVList<LNode>.Empty, new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(LNode target, LNode _1, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(_1), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(_1, _2), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(LNode target, LNode _1, LNode _2, LNode _3, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(_1, _2).Add(_3), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(LNode target, params LNode[] list)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(list), new SourceRange(_file));
		}
		public LNode Call(LNode target, LNode[] list, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(target, new RVList<LNode>(list), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(Symbol target, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(target, RVList<LNode>.Empty, new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(Symbol target, LNode _1, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(_1), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(Symbol target, LNode _1, LNode _2, LNode _3, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(_1, _2).Add(_3), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Call(Symbol target, params LNode[] list)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(list), new SourceRange(_file));
		}
		public LNode Call(Symbol target, LNode[] list, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(target, new RVList<LNode>(list), new SourceRange(_file, position, sourceWidth));
		}

		public LNode Dot(Symbol prefix, Symbol symbol)
		{
			return new StdSimpleCallNode(S.Dot, new RVList<LNode>(Symbol(prefix), Symbol(symbol)), new SourceRange(_file));
		}
		public LNode Dot(LNode prefix, Symbol symbol, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(S.Dot, new RVList<LNode>(prefix, Symbol(symbol)), new SourceRange(_file));
		}
		public LNode Of(params Symbol[] list)
		{
			return new StdSimpleCallNode(S.Of, new RVList<LNode>(list.SelectArray(sym => Symbol(sym))), new SourceRange(_file));
		}
		public LNode Of(params LNode[] list)
		{
			return new StdSimpleCallNode(S.Of, new RVList<LNode>(list), new SourceRange(_file));
		}
		public LNode Braces(params LNode[] contents)
		{
			return new StdSimpleCallNode(S.Braces, new RVList<LNode>(contents), new SourceRange(_file));
		}
		public LNode Braces(LNode[] contents, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(S.Braces, new RVList<LNode>(contents), new SourceRange(_file, position, sourceWidth));
		}
		public LNode List(params LNode[] contents)
		{
			return new StdSimpleCallNode(S.List, new RVList<LNode>(contents), new SourceRange(_file));
		}
		public LNode List(LNode[] contents, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(S.List, new RVList<LNode>(contents), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Tuple(params LNode[] contents)
		{
			return new StdSimpleCallNode(S.Tuple, new RVList<LNode>(contents), new SourceRange(_file));
		}
		public LNode Tuple(LNode[] contents, int position = -1, int sourceWidth = -1)
		{
			return new StdSimpleCallNode(S.Tuple, new RVList<LNode>(contents), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Def(LNode retType, Symbol name, LNode argList, LNode body = null, int position = -1, int sourceWidth = -1)
		{
			return Def(retType, Symbol(name), argList, body, sourceWidth);
		}
		public LNode Def(LNode retType, LNode name, LNode argList, LNode body = null, int position = -1, int sourceWidth = -1)
		{
			G.Require(argList.Name == S.List || argList.Name == S.Missing);
			LNode[] list = body == null 
				? new[] { retType, name, argList }
				: new[] { retType, name, argList, body };
			return new StdSimpleCallNode(S.Def, new RVList<LNode>(list), new SourceRange(_file, position, sourceWidth));
		}
		public LNode Property(LNode type, LNode name, LNode body = null, int position = -1, int sourceWidth = -1)
		{
			G.Require(body.IsCall && (body.Name == S.Braces || (body.Name == S.Forward && body.Args.Count == 1)));
			LNode[] list = body == null
				? new[] { type, name, }
				: new[] { type, name, body };
			return new StdSimpleCallNode(S.Property, new RVList<LNode>(list), new SourceRange(_file, position, sourceWidth));
		}
		public LNode ArgList(params LNode[] vars)
		{
			foreach (var var in vars)
				G.RequireArg(var.Name == S.Var && var.ArgCount >= 2, "vars", var);
			return List(vars);
		}
		public LNode Var(LNode type, Symbol name, LNode initValue = null)
		{
			if (initValue != null)
				return Call(S.Var, type, Call(name, initValue));
			else
				return Call(S.Var, type, Symbol(name));
		}
		public LNode Var(LNode type, params Symbol[] names)
		{
			var list = new List<LNode>(names.Length + 1) { type };
			list.AddRange(names.Select(n => Symbol(n)));
			return Call(S.Var, list.ToArray());
		}
		public LNode Var(LNode type, params LNode[] namesWithValues)
		{
			var list = new List<LNode>(namesWithValues.Length + 1) { type };
			list.AddRange(namesWithValues);
			return Call(S.Var, list.ToArray());
		}

		internal LNode InParens(LNode inner, int position = -1, int sourceWidth = -1)
		{
			return new StdComplexCallNode(null, new RVList<LNode>(inner), new SourceRange(_file, position, sourceWidth));
		}

		public LNode Result(LNode expr)
		{
			return Call(S.Result, expr, expr.Range.BeginIndex, expr.Range.Length);
		}

		[Obsolete]
		public LNode Attr(LNode attr, LNode node)
		{
			return node.AddAttr(attr);
		}
		[Obsolete]
		public LNode Attr(params LNode[] attrsAndNode)
		{
			var node = attrsAndNode[attrsAndNode.Length - 1];
			var attrs = node.Attrs;
			for (int i = 0; i < attrsAndNode.Length - 1; i++)
				attrs.Add(attrs[i]);
			return node.WithAttrs(attrs);
		}

	}
}
