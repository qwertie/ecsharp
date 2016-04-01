using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs
{
	/// <summary>
	/// A class filled with methods for checking whether a node has the correct 
	/// <see cref="LNode.Name"/> and structure. For example, <c>IsPropertyDefinition(node)</c>
	/// checks whether <c>node</c> meets the requirements for being a property 
	/// definition, such as having a Name equal to #property, and having name 
	/// and return value that are complex identifiers.
	/// </summary>
	/// <remarks>This class also has useful helper functions, such as <see cref="KeyNameComponentOf(LNode)"/>.
	/// </remarks>
	public static class EcsValidators
	{
		static readonly HashSet<Symbol> SimpleStmts = EcsNodePrinter.SimpleStmts;
		static readonly HashSet<Symbol> SpaceDefinitionStmts = EcsNodePrinter.SpaceDefinitionStmts;

		/// <summary>This is needed by the EC# node printer, but perhaps no one else.</summary>
		public enum Pedantics {
			IgnoreWeirdAttributes = 1, IgnoreIllegalParentheses = 2,
			Lax = IgnoreWeirdAttributes | IgnoreIllegalParentheses
		}; 

		// These are validators for printing purposes: they check that each node 
		// that shouldn't have attributes, doesn't; if attributes are present in
		// strange places then we print with prefix notation instead to avoid 
		// losing them when round-tripping.

		internal static bool HasPAttrs(LNode node, Pedantics p) // for use in expression context
		{
			return (p & Pedantics.IgnoreWeirdAttributes) == 0 && node.HasPAttrs();
		}
		internal static bool HasSimpleHeadWPA(LNode self, Pedantics p)
		{
			return (p & Pedantics.IgnoreWeirdAttributes) != 0 ? self.HasSimpleHead() : self.HasSimpleHeadWithoutPAttrs();
		}
		internal static bool CallsWPAIH(LNode self, Symbol name, Pedantics p)
		{
			return self.Calls(name) && HasSimpleHeadWPA(self, p);
		}
		internal static bool CallsMinWPAIH(LNode self, Symbol name, int argCount, Pedantics p)
		{
			return self.CallsMin(name, argCount) && HasSimpleHeadWPA(self, p);
		}
		internal static bool CallsWPAIH(LNode self, Symbol name, int argCount, Pedantics p)
		{
			return self.Calls(name, argCount) && HasSimpleHeadWPA(self, p);
		}
		internal static bool IsSimpleSymbolWPA(LNode self, Pedantics p)
		{
			return self.IsId && !HasPAttrs(self, p);
		}
		internal static bool IsSimpleSymbolWPA(LNode self, Symbol name, Pedantics p)
		{
			return self.Name == name && IsSimpleSymbolWPA(self, p);
		}

		/// <summary>Returns the space kind, which is one of the names #struct, 
		/// #class, #enum, #interface, #namespace, #alias, #trait, or null if the 
		/// node Name or structure is not valid for a space statement.</summary>
		public static Symbol SpaceDefinitionKind(LNode n, Pedantics p = Pedantics.Lax)
		{
			LNode name, bases, body;
			return SpaceDefinitionKind(n, out name, out bases, out body, p);
		}
		
		/// <summary>Returns the space kind, which is one of the names #struct, 
		/// #class, #enum, #interface, #namespace, #alias, #trait, or null if the 
		/// node Name or structure is not valid for a space statement.</summary>
		/// <param name="n">The node to examine.</param>
		/// <param name="name">Name of the space.</param>
		/// <param name="bases">bases.Args will be the list of base types.</param>
		/// <param name="body">A braced block of statements holding the contents of the space.</param>
		public static Symbol SpaceDefinitionKind(LNode n, out LNode name, out LNode bases, out LNode body, Pedantics p = Pedantics.Lax)
		{
			// All space declarations and space definitions have the form
			// #spacetype(Name, #(BaseList), { ... }) and the syntax
			// spacetype Name : BaseList { ... }, with optional "where" and "if" clauses
			// e.g. enum Foo : ushort { A, B, C }
			// The "if" clause is attached as an attribute on the statement;
			// "where" clauses are attached as attributes of the generic parameters.
			// For printing purposes,
			// - A declaration always has 2 args; a definition always has 3 args
			// - Name must be a complex (definition) identifier without attributes for
			//   normal spaces, or a #= expression for aliases.
			// - #(BaseList) can be missing (@``); the bases can be any expressions
			// - the arguments do not have attributes
			var type = n.Name;
			if (SpaceDefinitionStmts.Contains(type) && HasSimpleHeadWPA(n, p) && Range.IsInRange(n.ArgCount, 2, 3)) {
				name = n.Args[0];
				bases = n.Args[1];
				body = n.Args[2, null];
				if (type == S.Alias) {
					if (!CallsWPAIH(name, S.Assign, 2, p))
						return null;
					if (!IsComplexIdentifier(name.Args[0], ICI.Default | ICI.NameDefinition, p) ||
						!IsComplexIdentifier(name.Args[1], ICI.Default, p))
						return null;
				} else {
					if (!IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition, p))
						return null;
				}
				if (bases == null) return type;
				if (HasPAttrs(bases, p)) return null;
				if (IsSimpleSymbolWPA(bases, S.Missing, p) || bases.Calls(S.AltList))
				{
					if (body == null) return type;
					if (HasPAttrs(body, p)) return null;
					if (CallsWPAIH(body, S.Braces, p))
						return type;
				}
			} else {
				name = bases = body = null;
			}
			return null;
		}

		/// <summary>If the given node has a valid syntax tree for a method definition,
		/// a constructor, or (when orDelegate is true) a delegate definition, gets
		/// the definition kind (#fn, #cons, or #delegate).</summary>
		public static Symbol MethodDefinitionKind(LNode n, bool allowDelegate, Pedantics p = Pedantics.Lax)
		{
			LNode retType, methodName, argList, body;
			return MethodDefinitionKind(n, out retType, out methodName, out argList, out body, allowDelegate, p);
		}
		
		/// <summary>If the given node has a valid syntax tree for a method definition,
		/// a constructor, or (when orDelegate is true) a delegate definition, gets
		/// the definition kind (#fn, #cons, or #delegate).</summary>
		/// <param name="retType">Return type of the method (if it's a constructor, this will be the empty identifier).</param>
		/// <param name="name">Name of the method.</param>
		/// <param name="args">args.Args is the argument list of the method.</param>
		/// <param name="body">The method body, or null if there is no method body. 
		/// The method body calls <see cref="CodeSymbols.Braces"/> if the method is a 
		/// non-lambda-style method.</param>
		/// <remarks>
		/// Method declarations (no body) also count.
		/// <para/>
		/// A destructor counts as a #fn with a method name that calls the ~ operator.
		/// </remarks>
		public static Symbol MethodDefinitionKind(LNode n, out LNode retType, out LNode name, out LNode args, out LNode body, bool allowDelegate, Pedantics p = Pedantics.Lax)
		{
			retType = name = args = body = null;
			var kind = n.Name;
			if ((kind != S.Fn && kind != S.Delegate && kind != S.Constructor) || !HasSimpleHeadWPA(n, p))
				return null;
			if (!Range.IsInRange(n.ArgCount, 3, kind == S.Delegate ? 3 : 4))
				return null;

			retType = n.Args[0];
			name = n.Args[1];
			args = n.Args[2];
			body = n.Args[3, null];
			if (kind == S.Constructor && !retType.IsIdNamed(S.Missing))
				return null;
			// Note: the parser doesn't require that the argument list have a 
			// particular format, so the printer doesn't either.
			if (!CallsWPAIH(args, S.AltList, p))
				return null;
			if (kind == S.Constructor && 
				( (body != null && !CallsWPAIH(body, S.Braces, p) && !CallsWPAIH(body, S.Forward, 1, p))
				|| !retType.IsIdNamed(S.Missing)))
				return null;
			if (IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition, p)) {
				return IsComplexIdentifier(retType, ICI.Default | ICI.AllowAttrs, p) ? kind : null;
			} else {
				// Check for a destructor
				return retType.IsIdNamed(S.Missing)
					&& CallsWPAIH(name, S._Destruct, 1, p) 
					&& IsSimpleIdentifier(name.Args[0], p) ? kind : null;
			}
		}

		/// <summary>Returns true iff the given node has a valid syntax tree for a property definition.</summary>
		public static bool IsPropertyDefinition(LNode n, Pedantics p = Pedantics.Lax)
		{
			LNode retType, name, args, body, initialValue;
			return IsPropertyDefinition(n, out retType, out name, out args, out body, out initialValue, p);
		}
		
		/// <summary>Returns true iff the given node has a valid syntax tree for 
		/// a property definition, and gets the component parts of the definition.</summary>
		/// <remarks>The body may be anything. If it calls CodeSymbols.Braces, it's a normal body.</remarks>
		public static bool IsPropertyDefinition(LNode n, out LNode retType, out LNode name, out LNode args, out LNode body, out LNode initialValue, Pedantics p = Pedantics.Lax)
		{
			var argCount = n.ArgCount;
			if (!CallsMinWPAIH(n, S.Property, 4, p) || n.ArgCount > 5) {
				retType = name = args = body = initialValue = null;
				return false;
			}

			retType = n.Args[0];
			name = n.Args[1];
			args = n.Args[2];
			body = n.Args[3];
			initialValue = n.Args[4, null];
			return IsComplexIdentifier(retType, ICI.Default, p) &&
			       IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition, p) &&
			       (args.IsIdNamed(S.Missing) || args.Calls(S.AltList));
		}

		public static bool IsEventDefinition(LNode n, Pedantics p) { return EventDefinitionType(n, p) != EventDef.Invalid; }

		public enum EventDef { Invalid, WithBody, List };
		internal static EventDef EventDefinitionType(LNode _n, Pedantics p)
		{
			// EventDef.WithBody: #event(EventHandler, Click, { ... })
			// EventDef.List:     #event(EventHandler, Click, DoubleClick, RightClick)
			if (!CallsMinWPAIH(_n, S.Event, 2, p))
				return EventDef.Invalid;

			LNode type = _n.Args[0], name = _n.Args[1];
			if (!IsComplexIdentifier(type, ICI.Default, p) ||
				!IsSimpleIdentifier(name, p))
				return EventDef.Invalid;

			int argCount = _n.ArgCount;
			if (argCount == 3) {
				var body = _n.Args[2];
				if (CallsWPAIH(body, S.Braces, p) || CallsWPAIH(body, S.Forward, p))
					return EventDef.WithBody;
			}

			for (int i = 2; i < argCount; i++)
				if (!IsSimpleIdentifier(_n.Args[i], p))
					return EventDef.Invalid;
			return EventDef.List;
		}

		public static bool IsVariableDecl(LNode _n, bool allowMultiple, bool allowNoAssignment, Pedantics p) // for printing purposes
		{
			// e.g. #var(#int32, x = 0) <=> int x = 0
			// For printing purposes in EC#,
			// - The expression is not in parenthesis
			// - Head and args do not have attributes
			// - First argument must have the syntax of a type name
			// - Other args must have the form foo or foo = expr, where expr does not have attributes
			// - Must define a single variable unless allowMultiple
			// - Must immediately assign the variable unless allowNoAssignment
			if (CallsMinWPAIH(_n, S.Var, 2, p))
			{
				var a = _n.Args;
				if (!IsComplexIdentifier(a[0], ICI.Default, p))
					return false;
				if (a.Count > 2 && !allowMultiple)
					return false;
				for (int i = 1; i < a.Count; i++)
				{
					var var = a[i];
					if (HasPAttrs(var, p))
						return false;
					if ((p & Pedantics.IgnoreIllegalParentheses) == 0 && var.IsParenthesizedExpr())
						return false;
					if (var.IsId) {
						if (!allowNoAssignment)
							return false;
					} else if (!CallsWPAIH(var, S.Substitute, 1, p)) {
						if (!CallsWPAIH(var, S.Assign, 2, p))
							return false;
						LNode name = var.Args[0], init = var.Args[1];
						if (!IsSimpleIdentifier(name, p) || HasPAttrs(init, p))
							return false;
					}
				}
				return true;
			}
			return false;
		}

		internal static bool HasPAttrsOrParens(LNode node, Pedantics p)
		{
			var attrs = node.Attrs;
			for (int i = 0, c = attrs.Count; i < c; i++) {
				var a = attrs[i];
				if (a.IsIdNamed(S.TriviaInParens) || (p & Pedantics.IgnoreWeirdAttributes) == 0 && !a.IsTrivia)
					return true;
			}
			return false;
		}
		public static bool IsSimpleIdentifier(LNode n, Pedantics p)
		{
			if (HasPAttrsOrParens(n, p)) // Callers of this method don't want attributes
				return false;
			if (n.IsId)
				return true;
			if (CallsWPAIH(n, S.Substitute, 1, p))
				return true;
			return false;
		}
		public static bool IsComplexIdentifier(LNode n, ICI f = ICI.Default, Pedantics p = Pedantics.Lax)
		{
			// Returns true if 'n' is printable as a complex identifier.
			//
			// To be printable, a complex identifier in EC# must not contain 
			// attributes ((p & Pedantics.DropNonDeclAttrs) != 0 to override) and must be
			// 1. A simple symbol
			// 2. A substitution expression
			// 3. A dotted expr (a.b), where 'a' is a complex identifier and 'b' 
			//    is (1) or (2); structures like #.(a, b, c) and #.(a, b<c>) do 
			//    not count as complex identifiers. Note that a.b<c> is 
			//    structured #of(#.(a, b), c), not #.(a, #of(b, c)). A dotted
			//    expression that starts with a dot, such as .a.b, is structured
			//    (.a).b rather than .(a.b); unary . has high precedence.
			// 4. An #of expr a<b,...>, where 
			//    - 'a' is a complex identifier and not itself an #of expr
			//    - each arg 'b' is a complex identifier (if printing in C# style)
			// 
			// Type names have the same structure, with the following patterns for
			// arrays, pointers, nullables and typeof<>:
			// 
			// Foo*      <=> #of(@*, Foo)
			// Foo[]     <=> #of(@`[]`, Foo)
			// Foo[,]    <=> #of(#`[,]`, Foo)
			// Foo?      <=> #of(@?, Foo)
			// typeof<X> <=> #of(#typeof, X)
			//
			// Note that we can't just use #of(Nullable, Foo) for Foo? because it
			// doesn't work if System is not imported. It's reasonable to allow #? 
			// as a synonym for global::System.Nullable, since we have special 
			// symbols for types like #int32 anyway.
			// 
			// (a.b<c>.d<e>.f is structured ((((a.b)<c>).d)<e>).f or #.(#of(#.(#of(#.(a,b), c), d), e), f)
			if ((f & ICI.AllowAttrs) == 0 && ((f & ICI.AllowParensAround) != 0 ? HasPAttrs(n, p) : HasPAttrsOrParens(n, p)))
			{
				// Attribute(s) are illegal, except 'in', 'out' and 'where' when 
				// TypeParamDefinition inside <...>
				return (f & (ICI.NameDefinition | ICI.InOf)) == (ICI.NameDefinition | ICI.InOf) && IsPrintableTypeParam(n);
			}

			if (n.IsId)
				return true;
			if (CallsWPAIH(n, S.Substitute, 1, p))
				return true;

			if (CallsMinWPAIH(n, S.Of, 1, p) && (f & ICI.DisallowOf) == 0) {
				var baseName = n.Args[0];
				if (!IsComplexIdentifier(baseName, (f & (ICI.DisallowDotted)) | ICI.DisallowOf, p))
					return false;
				if ((f & ICI.AllowAnyExprInOf) != 0)
					return true;
				return OfHasNormalArgs(n, (f & ICI.NameDefinition) != 0, p);
			}
			if (CallsWPAIH(n, S.Dot, p) && (f & ICI.DisallowDotted) == 0 && Range.IsInRange(n.ArgCount, 1, 2)) {
				var args = n.Args;
				LNode lhs = args[0], rhs = args.Last;
				// right-hand argument must be simple
				var rhsFlags = (f & ICI.ExprMode) | ICI.DisallowOf | ICI.DisallowDotted;
				if ((f & ICI.ExprMode) != 0)
					rhsFlags |= ICI.AllowParensAround;
				if (!IsComplexIdentifier(args.Last, rhsFlags, p))
					return false;
				if ((f & ICI.ExprMode) != 0 && lhs.IsParenthesizedExpr() || (lhs.IsCall && !lhs.Calls(S.Dot) && !lhs.Calls(S.Of)))
					return true;
				return IsComplexIdentifier(args[0], (f & ICI.ExprMode), p);
			}
			return false;
		}
		internal static bool OfHasNormalArgs(LNode n, bool nameDefinition, Pedantics p)
		{
			if (!CallsMinWPAIH(n, S.Of, 1, p))
				return false;
			
			ICI childFlags = ICI.InOf;
			if (nameDefinition)
				childFlags = (childFlags | ICI.NameDefinition | ICI.DisallowDotted | ICI.DisallowOf);
			for (int i = 1; i < n.ArgCount; i++)
				if (!IsComplexIdentifier(n.Args[i], childFlags, p))
					return false;
			return true;
		}

		/// <summary>Checks if 'n' is a legal type parameter definition.</summary>
		/// <remarks>A type parameter definition must be a simple symbol with at 
		/// most one #in or #out attribute, and at most one #where attribute with
		/// an argument list consisting of complex identifiers.</remarks>
		public static bool IsPrintableTypeParam(LNode n, Pedantics p = Pedantics.Lax)
		{
			foreach (var attr in n.Attrs)
			{
				var name = attr.Name;
				if (attr.IsCall) {
					if (name == S.Where) {
						if (HasPAttrs(attr, p))
							return false;
						foreach (var arg in attr.Args)
							if (!IsComplexIdentifier(arg, ICI.Default, p) && !arg.Calls(S.New, 0))
								return false;
					} else if ((p & Pedantics.IgnoreWeirdAttributes) == 0)
						return false;
				} else {
					if ((p & Pedantics.IgnoreWeirdAttributes) == 0 && name != S.In && name != S.Out)
						return false;
					if (HasPAttrs(attr, p))
						return false;
				}
			}
			return true;
		}

		public static bool IsExecutableBlockStmt(LNode _n, Pedantics p = Pedantics.Lax) { return ExecutableBlockStmtType(_n, p) != null; }
		public static Symbol ExecutableBlockStmtType(LNode _n, Pedantics p = Pedantics.Lax)
		{
			return TwoArgBlockStmtType(_n, p) ?? OtherBlockStmtType(_n, p);
		}
		internal static Symbol TwoArgBlockStmtType(LNode _n, Pedantics p)
		{
			// S.Do:                     #doWhile(stmt, expr)
			// S.Switch:                 #switch(expr, @`{}`(...))
			// S.While (S.Using, etc.):  #while(expr, stmt), #using(expr, stmt), #lock(expr, stmt), #fixed(expr, stmt)
			var argCount = _n.ArgCount;
			if (argCount != 2)
				return null;
			var name = _n.Name;
			if (name == S.Switch)
				return CallsWPAIH(_n.Args[1], S.Braces, p) ? name : null;
			else if (name == S.DoWhile)
				return name;
			else if (name == S.While || name == S.UsingStmt || name == S.Lock || name == S.Fixed)
				return S.While; // all four can be printed in the same style as while()
			return null;
		}
		internal static Symbol OtherBlockStmtType(LNode _n, Pedantics p)
		{
			// S.If:                     #if(expr, stmt [, stmt])
			// S.For:                    #for(expr1, expr2, expr3, stmt)
			// S.ForEach:                #foreach(decl, list, stmt)
			// S.Try:                    #try(stmt, #catch(expr | @``, stmt) | #finally(stmt), ...)
			// S.Checked (S.Unchecked):  #checked(@`{}`(...))       // if no braces, it's a checked(expr)
			var argCount = _n.ArgCount;
			if (!HasSimpleHeadWPA(_n, p) || argCount < 1)
				return null;

			var name = _n.Name;
			if (name == S.If)
				return argCount == 2 || argCount == 3 ? name : null;
			else if (name == S.For)
				return argCount == 4 ? name : null;
			else if (name == S.ForEach)
				return argCount == 3 ? name : null;
			else if (name == S.Checked || name == S.Unchecked)
				return argCount == 1 && CallsWPAIH(_n.Args[0], S.Braces, p) ? S.Checked : null;
			else if (name == S.Try)
			{
				if (argCount < 2) return null;
				for (int i = 1; i < argCount; i++)
				{
					var clause = _n.Args[i];
					if (!clause.HasSimpleHeadWithoutPAttrs())
						return null;
					var n = clause.Name;
					int c = clause.ArgCount;
					if (n == S.Finally) {
						if (c != 1 || i + 1 != argCount)
							return null;
					} else if (n != S.Catch || c != 3)
						return null;
				}
				return name;
			}
			return null;
		}

		public static bool IsBracedBlock(LNode n)
		{
			return n.Calls(S.Braces);
		}

		internal static bool IsSimpleExecutableKeywordStmt(LNode _n, Pedantics p)
		{
			var name = _n.Name;
			int argC = _n.ArgCount;
			return _n.IsCall && SimpleStmts.Contains(_n.Name) && HasSimpleHeadWPA(_n, p) && 
				(argC == 1 || (argC > 1 && name == S.Import) || 
				(argC == 0 && (name == S.Break || name == S.Continue || name == S.Return || name == S.Throw)));
		}

		public static bool IsLabelStmt(LNode _n, Pedantics p = Pedantics.Lax)
		{
			if (_n.Name == S.Label)
				return _n.ArgCount == 1 && IsSimpleSymbolWPA(_n.Args[0], p);
			return CallsWPAIH(_n, S.Case, p);
		}

		public static bool IsNamedArgument(LNode _n, Pedantics p = Pedantics.Lax)
		{
 			return CallsWPAIH(_n, S.NamedArg, 2, p) && IsSimpleSymbolWPA(_n.Args[0], p);
		}
		
		public static bool IsResultExpr(LNode n, Pedantics p = Pedantics.Lax)
		{
			return CallsWPAIH(n, S.Result, 1, p) && !HasPAttrs(n, p);
		}

		public static bool IsForwardedProperty(LNode _n, Pedantics p = Pedantics.Lax)
		{
			// A forwarded property with the syntax  name ==> expr;
			//                  has the syntax tree  name(@==>(expr));
			//      in contrast to the block syntax  name({ code });
			return _n.ArgCount == 1 && HasSimpleHeadWPA(_n, p) && CallsWPAIH(_n.Args[0], S.Forward, 1, p);
		}

		/// <summary>Given a complex name such as <c>global::Foo&lt;int>.Bar&lt;T></c>,
		/// this method identifies the base name component, which in this example 
		/// is Bar. This is used, for example, to identify the expected name for
		/// a constructor based on the class name, e.g. <c>Foo&lt;T></c> => Foo.</summary>
		/// <remarks>It is not verified that name is a complex identifier. There
		/// is no error detection but in some cases an empty name may be returned, 
		/// e.g. for input like <c>Foo."Hello"</c>.</remarks>
		public static Symbol KeyNameComponentOf(LNode name)
		{
			if (name == null)
				return null;
			// global::Foo<int>.Bar<T> is structured (((global::Foo)<int>).Bar)<T>
			// So if #of, get first arg (which cannot itself be #of), then if #dot, 
			// get second arg.
			if (name.CallsMin(S.Of, 1))
				name = name.Args[0];
			if (name.CallsMin(S.Dot, 1))
				name = name.Args.Last;
			if (name.IsCall)
				return KeyNameComponentOf(name.Target);
			return name.Name;
		}
	}
}
