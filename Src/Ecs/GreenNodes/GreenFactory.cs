using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;
using ecs;
using S = ecs.CodeSymbols;

namespace Loyc.CompilerCore
{
	/// <summary>Contains static helper methods for creating <see cref="GreenNode"/>s.
	/// Also contains the Cache method, which deduplicates subtrees that have the
	/// same structure.
	/// </summary>
	public class GreenFactory
	{
		public static readonly GreenNode Missing = new GreenSymbol(S.Missing, EmptySourceFile.Unknown, -1);
		public GreenNode _Missing { get { return Missing; } } // allow access through class reference

		// Common literals
		public GreenNode @true        { get { return Literal(true); } }
		public GreenNode @false       { get { return Literal(false); } }
		public GreenNode @null        { get { return Literal((object)null); } }
		public GreenNode @void        { get { return Literal(ecs.@void.Value); } }
		public GreenNode int_0        { get { return Literal(0); } }
		public GreenNode int_1        { get { return Literal(1);  } }
		public GreenNode string_empty { get { return Literal(""); } }

		public GreenNode DefKeyword   { get { return Symbol(S.Def, -1); } }
		public GreenNode EmptyList    { get { return Symbol(S.List, -1); } }
		
		// Standard data types (marked synthetic)
		public GreenNode Void    { get { return Symbol(S.Void, -1);	} }
		public GreenNode String  { get { return Symbol(S.String, -1);	} }
		public GreenNode Char    { get { return Symbol(S.Char, -1);	} }
		public GreenNode Bool    { get { return Symbol(S.Bool, -1);	} }
		public GreenNode Int8    { get { return Symbol(S.Int8, -1);	} }
		public GreenNode Int16   { get { return Symbol(S.Int16, -1);	} }
		public GreenNode Int32   { get { return Symbol(S.Int32, -1);	} }
		public GreenNode Int64   { get { return Symbol(S.Int64, -1);	} }
		public GreenNode UInt8   { get { return Symbol(S.UInt8, -1);	} }
		public GreenNode UInt16  { get { return Symbol(S.UInt16, -1);	} }
		public GreenNode UInt32  { get { return Symbol(S.UInt32, -1);	} }
		public GreenNode UInt64  { get { return Symbol(S.UInt64, -1);	} }
		public GreenNode Single  { get { return Symbol(S.Single, -1);	} }
		public GreenNode Double  { get { return Symbol(S.Double, -1);	} }
		public GreenNode Decimal { get { return Symbol(S.Decimal, -1);} }

		// Standard access modifiers
		public GreenNode Internal    { get { return Symbol(S.Internal, -1);	} }
		public GreenNode Public      { get { return Symbol(S.Public, -1);		} }
		public GreenNode ProtectedIn { get { return Symbol(S.ProtectedIn, -1);} }
		public GreenNode Protected   { get { return Symbol(S.Protected, -1);	} }
		public GreenNode Private     { get { return Symbol(S.Private, -1);    } }

		ISourceFile _file;
		public ISourceFile File { get { return _file; } set { _file = value; } }

		public GreenFactory(ISourceFile file) { _file = file; }

		/// <summary>Gets a structurally equivalent node from the thread-local 
		/// cache, or places the node in the cache if it is not already there.</summary>
		/// <remarks>
		/// If the node is mutable, it will be frozen if it was put in the cache,
		/// or left unfrozen if a different node is being returned from the cache. 
		/// <para/>
		/// The node's SourceWidth and Style are preserved.
		/// </remarks>
		public static GreenNode Cache(GreenNode input)
		{
			if (_cache == null)
				_cache = new SimpleCache<GreenNode>(16384, GreenNode.DeepComparer.WithStyleCompare);
			input = input.AutoOptimize(false, false);
			var r = _cache.Cache(input);
			if (r == input) r.Freeze();
			return r;
		}
		[ThreadStatic]
		static SimpleCache<GreenNode> _cache;

		public static readonly GreenAtOffs[] EmptyGreenArray = new GreenAtOffs[0];

		// Atoms: symbols (including keywords) and literals
		public GreenNode Symbol(string name, int sourceWidth = -1)
		{
			return new GreenSymbol(GSymbol.Get(name), _file, sourceWidth);
		}
		public GreenNode Symbol(Symbol name, int sourceWidth = -1)
		{
			return new GreenSymbol(name, _file, sourceWidth);
		}
		public GreenNode Literal(object value, int sourceWidth = -1)
		{
			return new GreenLiteral(value, _file, sourceWidth);
		}

		// Calls
		public GreenNode Call(GreenAtOffs head, int sourceWidth = -1)
		{
			return new GreenCall0(head, _file, sourceWidth);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs _1, int sourceWidth = -1)
		{
			return new GreenCall1(head, _file, sourceWidth, _1);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs _1, GreenAtOffs _2, int sourceWidth = -1)
		{
			return new GreenCall2(head, _file, sourceWidth, _1, _2);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs _1, GreenAtOffs _2, GreenAtOffs _3, int sourceWidth = -1)
		{
			return Call(head, new[] { _1, _2, _3 }, sourceWidth);
		}
		public GreenNode Call(GreenAtOffs head, params GreenAtOffs[] list)
		{
			return Call(head, list, -1);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs[] list, int sourceWidth = -1)
		{
			return AddArgs(new EditableGreenNode(head, _file, sourceWidth), list);
		}
		public GreenNode Call(Symbol name, int sourceWidth = -1)
		{
			return new GreenSimpleCall0(name, _file, sourceWidth);
		}
		public GreenNode Call(Symbol name, GreenAtOffs _1, int sourceWidth = -1)
		{
			return new GreenSimpleCall1(name, _file, sourceWidth, _1);
		}
		public GreenNode Call(Symbol name, GreenAtOffs _1, GreenAtOffs _2, int sourceWidth = -1)
		{
			return new GreenSimpleCall2(name, _file, sourceWidth, _1, _2);
		}
		public GreenNode Call(Symbol name, GreenAtOffs _1, GreenAtOffs _2, GreenAtOffs _3, int sourceWidth = -1)
		{
			return Call(name, new[] { _1, _2, _3 }, sourceWidth);
		}
		public GreenNode Call(Symbol name, GreenAtOffs[] list, int sourceWidth = -1)
		{
			return AddArgs(new EditableGreenNode(name, _file, sourceWidth), list);
		}
		private static GreenNode AddArgs(EditableGreenNode n, GreenAtOffs[] list)
		{
			n.IsCall = true;
			var a = n.Args;
			for (int i = 0; i < list.Length; i++)
				a.Add(list[i]);
			return n;
		}

		public GreenNode Dot(params GreenAtOffs[] list)
		{
			return Call(S.Dot, list, -1);
		}
		public GreenNode Dot(params Symbol[] list)
		{
			GreenAtOffs[] array = list.Select(s => (GreenAtOffs)Symbol(s, -1)).ToArray();
			return Call(S.Dot, array);
		}
		public GreenNode Of(params GreenAtOffs[] list)
		{
			return Call(S.Of, list, -1);
		}
		public GreenNode Of(params Symbol[] list)
		{
			GreenAtOffs[] array = list.Select(s => (GreenAtOffs)Symbol(s, -1)).ToArray();
			return Call(S.Of, array);
		}
		public GreenNode Name(Symbol name, int sourceWidth = -1)
		{
			return Symbol(name, sourceWidth);
		}
		public GreenNode Braces(params GreenAtOffs[] contents)
		{
			return Braces(contents, -1);
		}
		public GreenNode Braces(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.Braces, contents, sourceWidth);
		}
		public GreenNode List(params GreenAtOffs[] contents)
		{
			return List(contents, -1);
		}
		public GreenNode List(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.List, contents, sourceWidth);
		}
		public GreenNode Tuple(params GreenAtOffs[] contents)
		{
			return Tuple(contents, -1);
		}
		public GreenNode Tuple(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.Tuple, contents, sourceWidth);
		}
		public GreenNode Def(GreenNode retType, Symbol name, GreenNode argList, GreenNode body = null, int sourceWidth = -1)
		{
			return Def(retType, Name(name), argList, body, sourceWidth);
		}
		public GreenNode Def(GreenNode retType, GreenNode name, GreenNode argList, GreenNode body = null, int sourceWidth = -1)
		{
			G.Require(argList.Name == S.List || argList.Name == S.Missing);
			GreenNode def;
			if (body == null) def = Call(S.Def, new GreenAtOffs[] { retType, name, argList, }, sourceWidth);
			else              def = Call(S.Def, new GreenAtOffs[] { retType, name, argList, body }, sourceWidth);
			return def;
		}
		public GreenNode Def(GreenAtOffs retType, GreenAtOffs name, GreenAtOffs argList, GreenAtOffs body = default(GreenAtOffs), int sourceWidth = -1, ISourceFile file = null)
		{
			G.Require(argList.Node.Name == S.List || argList.Node.Name == S.Missing);
			GreenNode def;
			if (body.Node == null) def = Call(S.Def, new[] { retType, name, argList, }, sourceWidth);
			else                   def = Call(S.Def, new[] { retType, name, argList, body }, sourceWidth);
			return def;
		}
		public GreenNode Property(GreenNode type, GreenNode name, GreenNode body = null, int sourceWidth = -1)
		{
			G.Require(body.IsCall && (body.Name == S.Braces || (body.Name == S.Forward && body.ArgCount == 1)));
			return Call(S.Property, new GreenAtOffs[] { type, name, body }, sourceWidth);
		}
		public GreenNode ArgList(params GreenAtOffs[] vars)
		{
			return ArgList(vars, -1);
		}
		public GreenNode ArgList(GreenAtOffs[] vars, int sourceWidth)
		{
			foreach (var var in vars)
				G.RequireArg(var.Node.Name == S.Var && var.Node.ArgCount >= 2, "vars", var);
			return Call(S.List, vars, sourceWidth);
		}
		public GreenNode Var(GreenAtOffs type, Symbol name, GreenAtOffs initValue = default(GreenAtOffs))
		{
			if (initValue.Node != null)
				return Call(S.Var, type, Call(name, initValue));
			else
				return Call(S.Var, type, Symbol(name));
		}
		public GreenNode Var(GreenAtOffs type, params Symbol[] names)
		{
			var list = new List<GreenAtOffs>(names.Length+1) { type };
			list.AddRange(names.Select(n => new GreenAtOffs(Symbol(n))));
			return Call(S.Var, list.ToArray());
		}
		public GreenNode Var(GreenAtOffs type, params GreenAtOffs[] namesWithValues)
		{
			var list = new List<GreenAtOffs>(namesWithValues.Length+1) { type };
			list.AddRange(namesWithValues);
			return Call(S.Var, list.ToArray());
		}

		internal GreenNode InParens(GreenNode inner, int sourceWidth = -1)
		{
			if (inner.Head == null && !inner.IsCall)
				// Because one level of nesting doesn't currently count as being in 
				// parenthesis for a non-call; need two. I might want to rethink this.
				inner = new GreenInParens(inner, inner.SourceFile, inner.SourceWidth);
			return new GreenInParens(inner, inner.SourceFile, sourceWidth <= -1 ? inner.SourceWidth : -1);
		}

		public GreenAtOffs Result(GreenAtOffs expr)
		{
			return new GreenAtOffs(Call(S.Result, new GreenAtOffs(expr.Node, 0), expr.Node.SourceWidth), expr.Offset);
		}

		public GreenNode Attr(GreenNode attr, GreenNode node)
		{
			node = node.Unfrozen();
			node.Attrs.Insert(0, attr);
			return node;
		}
		public GreenNode Attr(params GreenNode[] attrsAndNode)
		{
			var node = attrsAndNode[attrsAndNode.Length - 1].Unfrozen();
			for (int i = 0; i < attrsAndNode.Length - 1; i++)
				node.Attrs.Insert(i, attrsAndNode[i]);
			return node;
		}

	}
}
