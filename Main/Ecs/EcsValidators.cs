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
		static readonly HashSet<Symbol> OperatorIdentifiers = EcsNodePrinter.OperatorIdentifiers;
		static readonly HashSet<Symbol> AssignmentOperators = new HashSet<Symbol> {
			S.Assign, S.MulAssign, S.SubAssign, S.AddAssign, S.DivAssign, S.ModAssign, S.ShrAssign,
			S.ShlAssign, S.XorBitsAssign, S.AndBitsAssign, S.OrBitsAssign, S.NullCoalesceAssign, S.QuickBindAssign,
			S.ExpAssign, S.ConcatAssign
		};

		/// <summary>This is needed by the EC# node printer, but perhaps no one else.</summary>
		public enum Pedantics {
			Strict = 0,
			IgnoreAttributesInOddPlaces = 1, // Without this, attributes in illegal locations force prefix notation
			IgnoreIllegalParentheses = 2,    // Without this, illegal parenthesis (around var decl) force prefix notation
			Lax = IgnoreAttributesInOddPlaces | IgnoreIllegalParentheses
		};

		// These are validators for printing purposes: they check that each node 
		// that shouldn't have attributes, doesn't; if attributes are present in
		// strange places then we print with prefix notation instead to avoid 
		// losing them when round-tripping.

		internal static bool HasPAttrs(LNode node, Pedantics p) // for use in expression context
		{
			return (p & Pedantics.IgnoreAttributesInOddPlaces) == 0 && node.HasPAttrs();
		}
		internal static bool HasSimpleHeadWPA(LNode self, Pedantics p)
		{
			return (p & Pedantics.IgnoreAttributesInOddPlaces) != 0 ? self.HasSimpleHead() : self.HasSimpleHeadWithoutPAttrs();
		}

#if false // Ended up not being used, but might be useful someday
		/// <summary>Returns true if the specified child of the specified node 
		/// can be an implicit child statement, i.e. a child statement that is
		/// not necessarily a braced block, e.g. the second child of a while 
		/// loop.</summary>
		/// <remarks>
		/// This method helps the printer decide when a newline should be added 
		/// before an unbraced child statement when there are no attributes 
		/// dictating whether to add a newline or not.
		/// <para/>
		/// This method only cares about executable parent nodes. It returns 
		/// false for class/space and function/property bodies, which are always 
		/// braced blocks and therefore get a newline before every child statement 
		/// automatically.
		/// </remarks>
		public static bool MayBeImplicitChildStatement(LNode node, int childIndex)
		{
			CheckParam.IsNotNull("node", node);
			if (childIndex < 0) // target or attributes
				return false;
			var n = node.Name;
			if (!LNode.IsSpecialName(n.Name))
				return false;
			if (n == S.Braces)
				return true;
			if (n == S.Try)
				return childIndex == 0;
			switch (node.ArgCount) {
				case 1:
					if (n == S.Finally)
						return true;
					break;
				case 2:
					if (childIndex == 0 ? n == S.DoWhile :
						n == S.If || n == S.While || n == S.UsingStmt || n == S.Lock || n == S.SwitchStmt || n == S.Fixed)
						return true;
					break;
				case 3:
					if (childIndex != 0 && n == S.If)
						return true;
					if (childIndex == 2 && n == S.ForEach)
						return true;
					break;
				case 4:
					if (childIndex == 3 && (n == S.For || n == S.Catch))
						return true;
					break;
			}
			return false;
		}
#endif

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
			if (SpaceDefinitionStmts.Contains(type) && HasSimpleHeadWPA(n, p) && n.ArgCount.IsInRange(2, 3)) {
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

		/// <summary>Alias for <see cref="MethodDefinitionKind(LNode,bool,Pedantics)"/> that returns true if 
		/// MethodDefinitionKind returns #fn.</summary>
		public static bool IsNormalMethod(LNode n, Pedantics p = Pedantics.Lax)
		{
			return MethodDefinitionKind(n, false, p) == S.Fn;
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
		/// <returns>The definition kind (#fn, #cons, or #delegate), or null if it's no kind of method.</returns>
		/// <remarks>
		/// Method declarations (no body) also count.
		/// <para/>
		/// A destructor counts as a #fn with a method name that calls the ~ operator.
		/// </remarks>
		public static Symbol MethodDefinitionKind(LNode n, out LNode retType, out LNode name, out LNode args, out LNode body, bool allowDelegate = true, Pedantics p = Pedantics.Lax)
		{
			retType = name = args = body = null;
			var kind = n.Name;
			if ((kind != S.Fn && kind != S.Delegate && kind != S.Constructor) || !HasSimpleHeadWPA(n, p))
				return null;
			if (!n.ArgCount.IsInRange(3, kind == S.Delegate ? 3 : 4))
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
				((body != null && !CallsWPAIH(body, S.Braces, p) && !CallsWPAIH(body, S.Forward, 1, p))
				|| !retType.IsIdNamed(S.Missing)))
				return null;
			if (IsComplexIdentifier(name, ICI.Default | ICI.NameDefinition, p)) {
				return IsComplexIdentifier(retType, ICI.Default | ICI.AllowAttrs, p) ? kind : null;
			} else {
				// Check for a destructor (name has the form ~Foo)
				if (retType.IsIdNamed(S.Missing)
					&& CallsWPAIH(name, S._Destruct, 1, p)
					&& IsSimpleIdentifier(name.Args[0], p)) return kind;
				if (name.Value is bool) // operator true/false
					return kind;
				return null;
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
		/// <remarks>The body may be anything. If it calls CodeSymbols.Braces, it's a normal 
		/// body, otherwise it's a getter-only body (e.g. int Foo => 42). Indexer 
		/// properties can have an argument list, e.g. <c>T Foo[int x] { get; }</c>
		/// would have a syntax tree like <c>#property(T, Foo, #(#var(#int32, x)), { get; })</c>.</remarks>
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

		public static bool IsEventDefinition(LNode n, Pedantics p)
		{
			LNode type, name, body;
			return IsEventDefinition(n, out type, out name, out body, p);
		}

		internal static bool IsEventDefinition(LNode node, out LNode type, out LNode name, out LNode body, Pedantics p)
		{
			// Syntax should either be
			//   #event(EventHandler, Click, {...}), or
			//   #event(EventHandler, #(Click, DoubleClick)),
			// but we can also parse
			//   #event(EventHandler, #(Click, DoubleClick), {...})
			type = name = body = null;
			int argCount = node.ArgCount;
			if (!CallsMinWPAIH(node, S.Event, 2, p) || argCount > 3)
				return false;

			type = node.Args[0];
			name = node.Args[1];
			if (!IsComplexIdentifier(type, ICI.Default, p))
				return false;
			if (!IsComplexIdentifier(name, ICI.Default, p) && 
				!(name.CallsMin(S.AltList, 1) && name.Args.All(a => IsComplexIdentifier(a, ICI.Default, p))))
				return false;

			if (argCount == 3) {
				body = node.Args[2];
				return CallsWPAIH(body, S.Braces, p) || CallsWPAIH(body, S.Forward, p);
			} else
				return argCount == 2;
		}

		/// <summary>Verifies that a declaration of a single variable is valid, and gets its parts.</summary>
		/// <param name="expr">Potential variable or field declaration</param>
		/// <param name="type">Variable type (empty identifier if `var`)</param>
		/// <param name="name">Variable name (identifier or $substutution expr)</param>
		/// <param name="initialValue">Initial value that is assigned in <c>expr</c>, or null if unassigned.</param>
		/// <returns>True if <c>expr</c> is declaration of a single variable.</returns>
		public static bool IsVariableDeclExpr(LNode expr, out LNode type, out LNode name, out LNode initialValue, Pedantics p = Pedantics.Lax)
		{
			type = name = initialValue = null;
			if (expr.Calls(S.Var, 2)) {
				type = expr.Args[0];
				name = expr.Args[1];
				// don't need to call HasPAttrs(type, p) because IsComplexIdentifier will check for printable attrs
				if (HasPAttrs(expr, p) || HasPAttrs(name, p))
					return false;
				if (name.Calls(S.Assign, 2)) {
					initialValue = name.Args[1];
					name = name.Args[0];
					if (HasPAttrs(name, p) || HasPAttrs(initialValue, p))
						return false;
				}
				return IsComplexIdentifier(type, ICI.AllowAnyExprInOf, p) && (name.IsId || name.Calls(S.Substitute, 1));
			}
			return false;
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
				if (a.IsIdNamed(S.TriviaInParens) || (p & Pedantics.IgnoreAttributesInOddPlaces) == 0 && !a.IsTrivia)
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
			// 3. An 'of' expression a<b,...>, where 'a' is (1) or (2) and each arg 'b' 
			//    is a complex identifier (if printing in C# style)
			// 4. A dotted expression (a.b), where 'a' is a complex identifier and 'b' 
			//    is (1), (2) or (3); structures like @`'.`(a, b, c) and @`'.`(a, b.c) 
			//    do not count as complex identifiers. Note that a.b<c> is 
			//    structured @`'.`(a, @'of(b, c)), not #of(@`'.`(a, b), c). A dotted
			//    expression that starts with a dot, such as .a.b, is structured
			//    (.a).b rather than .(a.b), as unary . has precedence as high as $.
			// 5. A scope-resolution expression (a::b), where 'a' is (1) or (2) and
			//    'b' does not contain another scope-resolution operator.
			// 
			// Type names have the same structure, with the following patterns for
			// arrays, pointers, nullables and typeof<>:
			// 
			// Foo*      <=> @'of(@*, Foo)
			// Foo[]     <=> @'of(@`[]`, Foo)
			// Foo[,]    <=> @'of(#`[,]`, Foo)
			// Foo?      <=> @'of(@?, Foo)
			//
			// Note that we can't just use @'of(Nullable, Foo) for Foo? because it
			// doesn't work if System is not imported. It's reasonable to allow '? 
			// instead of global::System.Nullable, since we have special symbols 
			// for types like #int32 anyway.
			// 
			// (a.b<c>.d<e>.f is structured a.(b<c>).(d<e>).f or @`'.`(@`'.`(@`'.`(a, @'of(b, c)), #of(d, e)), f).
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

			var args = n.Args;
			if (CallsMinWPAIH(n, S.Of, 1, p)) {
				var baseName = args[0];
				if (!IsSimpleIdentifier(baseName, p))
					return false;
				if ((f & ICI.AllowAnyExprInOf) != 0)
					return true;
				ICI childFlags = ICI.InOf;
				if ((f & ICI.NameDefinition) != 0)
				{
					childFlags = (childFlags | ICI.NameDefinition | ICI.DisallowDotted);
				}
				for (int i = 1; i < n.ArgCount; i++)
				{
					var childArg = n.Args[i];
					if (!IsComplexIdentifier(childArg, childFlags, p))
					{
						if (baseName.IsIdNamed(S.Tuple) && (childFlags & ICI.NameDefinition) == 0)
						{   // If part of a tuple type isn't a valid complex Id, 
							// it must be an unassigned variable declaration
							if (childArg.Calls(S.Var, 2) &&
								IsComplexIdentifier(childArg.Args[0], childFlags, p) &&
								IsSimpleIdentifier(childArg.Args[1], p))
								continue;
						}
						return false;
					}
				}
				return true;
			}
			if (CallsWPAIH(n, S.Dot, 2, p) && (f & ICI.DisallowDotted) == 0) {
				// right-hand argument must be simple or be an "of<expression>"
				if (!IsComplexIdentifier(args.Last, ICI.DisallowDotted|ICI.DisallowColonColon, p))
					return false;
				return IsComplexIdentifier(args[0], ICI.Default, p);
			}
			if (CallsWPAIH(n, S.ColonColon, 2, p) && (f & ICI.DisallowColonColon) == 0) {
				// left-hand argument must be simple
				if (!IsSimpleIdentifier(args[0], p))
					return false;
				return IsComplexIdentifier(args.Last, ICI.DisallowColonColon, p);
			}
			return false;
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
					} else if ((p & Pedantics.IgnoreAttributesInOddPlaces) == 0)
						return false;
				} else {
					if ((p & Pedantics.IgnoreAttributesInOddPlaces) == 0 && name != S.In && name != S.Out)
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
			// S.SwitchStmt:             #switch(expr, @`{}`(...))
			// S.While (S.Using, etc.):  #while(expr, stmt), #using(expr, stmt), #lock(expr, stmt), #fixed(expr, stmt)
			var argCount = _n.ArgCount;
			if (argCount != 2)
				return null;
			var name = _n.Name;
			if (name == S.SwitchStmt)
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
				return argCount == 4 && CallsWPAIH(_n.Args[0], S.AltList, p)
				                     && CallsWPAIH(_n.Args[2], S.AltList, p) ? name : null;
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
				(argC == 1 ||
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

		/// <summary>Checks whether an expression is a valid "is" test (pattern-
		/// matching expression) such as "x is Foo", "x is Foo y" or "x is Foo y(z)".</summary>
		/// <remarks>The format of a valid "is" test is <c>@'is(subject, type_or_vardecl, extraArgsList)</c>.
		/// For example <c>a is Foo</c> would be <c>@'is(a, Foo)</c> and
		/// <c>a is Foo b(c, d)</c> would be <c>@'is(a, #var(Foo, b), #(c, d))</c>.
		/// Unary "is" expressions like <c>is Foo</c> are stored as binary expressions 
		/// with an empty identifier as the left-hand side: <c>@'is(@``, Foo)</c>.
		/// </remarks>
		public static bool IsIsTest(LNode n, out LNode subject, out LNode targetType, out LNode targetVarName, out LNode extraArgs, Pedantics p = Pedantics.Lax)
		{
			// `x is Foo<T> y (e1, e2)` parses to @'is(x, #var(Foo<T>, y), e1, e2)
			subject = targetType = targetVarName = extraArgs = null;
			if (!n.CallsMin(S.Is, 2))
				return false;

			int c = n.ArgCount;
			if (c > 3 || c == 3 && !(extraArgs = n.Args[2]).Calls(S.AltList))
				return false;

			subject = n.Args[0];
			LNode target = n.Args[1], initialValue;
			if (IsVariableDeclExpr(target, out targetType, out targetVarName, out initialValue, p))
				return initialValue == null;
			else
				return IsComplexIdentifier(targetType = target, ICI.AllowAnyExprInOf, p);
		}

		#region Linq expression validation

		public static bool IsLinqExpression(LNode n, Pedantics p = Pedantics.Lax)
		{
			var parts = n.Args;
			if (n.CallsMin(S.Linq, 2) && parts[0].Calls(S.From)) {
				if (IsValidIntoClause(parts.Last, p)) {
					parts = parts.WithoutLast(1);
				}
				if (DetectSelectOrGroupBy(parts.Last) == null)
					return false;
				parts = parts.WithoutLast(1);
				return AreValidLinqClauses(parts, 1, p);
			}
			return false;
		}

		private static bool AreValidLinqClauses(LNodeList parts, int i, Pedantics p)
		{
			for (; i < parts.Count; i++)
				if (LinqClauseKind(parts[i], p) == null)
					return false;
			return true;
		}

		private static Symbol LinqClauseKind(LNode n, Pedantics p)
		{
			Symbol name = n.Name;
			var args = n.Args;
			if (name == S.From) {
				if (!IsInExpr(args[0], p))
					return null;
			} else if (name == S.Let) {
				if (args.Count != 1)
					return null;
			} else if (n.Calls(S.Where)) {
				if (args.Count != 1)
					return null;
			} else if (n.Calls(S.Join)) {
				if (args.Count < 2 || args.Count > 3)
					return null;
				if (!IsInExpr(args[0], p))
					return null;
				if (!args[1].Calls("#equals", 2))
					return null;
				if (args.Count >= 3) {
					LNode into = args[2], id;
					if (!(into.Calls(S.Into, 1) && !HasPAttrsOrParens(id = into[0], p) && (id.IsId || id.Calls(S.Substitute, 1))))
						return null;
				}
			} else if (n.Calls(S.OrderBy)) {
				// All argument lists are acceptable
			} else
				return null;
			return name;
		}

		private static bool IsInExpr(LNode expr, Pedantics p)
		{
			LNode lhs;
			return expr.Calls(S.In, 2) && ((lhs = expr.Args[0]).IsId || lhs.Calls(S.Var, 2)) && !HasPAttrsOrParens(lhs, p);
		}

		private static Symbol DetectSelectOrGroupBy(LNode n)
		{
			Symbol name = n.Name;
			var args = n.Args;
			if (name == S.Select) {
				if (args.Count != 1)
					return null;
			} else if (name == S.GroupBy) {
				if (args.Count != 2)
					return null;
			} else
				return null;
			return name;
		}

		private static bool IsValidIntoClause(LNode n, Pedantics p)
		{
			if (n.Name != S.Into || n.ArgCount < 2)
				return false;
			var parts = n.Args;
			if (!(parts[0].IsId || parts[0].Calls(S.Substitute, 1)) || HasPAttrsOrParens(parts[0], p))
				return false;

			if (IsValidIntoClause(parts.Last, p) && n.ArgCount >= 3)
				parts = parts.WithoutLast(1);
			if (DetectSelectOrGroupBy(parts.Last) == null)
				return false;
			parts = parts.WithoutLast(1);
			return AreValidLinqClauses(parts, 1, p);
		}

		#endregion

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
			// global::Foo<int>.Bar<T> is structured (global::(Foo<int>)).(Bar<T>)
			// so if `'.` or `'::` get second arg, then if @'of, get first arg.
			if (name.CallsMin(S.Dot, 1) || name.Calls(S.ColonColon, 2))
				name = name.Args.Last;
			if (name.CallsMin(S.Of, 1))
				name = name.Args[0];
			if (name.IsCall)
				return KeyNameComponentOf(name.Target);
			return name.Name;
		}

		public static bool IsPlainCsIdentStartChar(char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || (c > 128 && char.IsLetter(c));
		}
		public static bool IsPlainCsIdentContChar(char c)
		{
			return IsPlainCsIdentStartChar(c) || (c >= '0' && c <= '9');
		}
		public static bool IsIdentStartChar(char c)
		{
			return IsPlainCsIdentStartChar(c) || c == '#';
		}
		public static bool IsIdentContChar(char c)
		{
			return IsIdentStartChar(c) || (c >= '0' && c <= '9') || c == '\'';
		}
		public static bool IsPlainCsIdentifier(string text)
		{
			if (text == "")
				return false;
			if (!IsPlainCsIdentStartChar(text[0]))
				return false;
			for (int i = 1; i < text.Length; i++)
				if (!IsPlainCsIdentContChar(text[i]))
					return false;
			return true;
		}

		public static bool IsAssignmentOperator(Symbol opName)
		{
			return opName != null && AssignmentOperators.Contains(opName);
		}
		public static bool IsOperator(Symbol opName)
		{
			return opName != null && OperatorIdentifiers.Contains(opName);
		}

		/// <summary>Eliminates punctuation and special characters from a string so
		/// that the string can be used as a plain C# identifier, e.g. 
		/// "I'd" => "I_aposd", "123" => "_123", "+5" => "_plus5".</summary>
		/// <remarks>The empty string "" becomes "__empty__", ASCII punctuation becomes 
		/// "_xyz" where xyz is an HTML entity name, e.g. '!' becomes "_excl",
		/// and all other characters become "Xxx" where xx is the hexadecimal 
		/// representation of the code point. Designed for the Unicode BMP only.</remarks>
		public static string SanitizeIdentifier(string id)
		{
			if (id == "")
				return "__empty__";
			int i = 0;
			if (IsPlainCsIdentStartChar(id[0])) {
				for (i = 1; i < id.Length; i++)
					if (!IsPlainCsIdentStartChar(id[i]) && !char.IsDigit(id[i]))
						break;
			}
			if (i >= id.Length)
				return id; // it's a normal identifier, do not change
			
			var sb = new StringBuilder(id.Left(i));
			for (; i < id.Length; i++) {
				char c = id[i];
				if (IsPlainCsIdentStartChar(c))
					sb.Append(c);
				else if (c >= '0' && c <= '9') {
					if (i == 0) sb.Append('_');
					sb.Append(c);
				} else {
					char prefix = '_';
					string ent = G.BareHtmlEntityNameForAscii(c);
					if (ent == null || (c < 256 && ent.Length > 5)) {
						prefix = 'x';
						ent = ((int)c).ToString("X2");
					}
					sb.Append(prefix);
					sb.Append(ent);
				}
			}
			return sb.ToString();
		}
	}
}
