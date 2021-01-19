using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using S = Loyc.Syntax.CodeSymbols;
using System.Reflection;
using Loyc.Math;

namespace LeMP
{
	public partial class StandardMacros
	{
		[LexicalMacro("quote(code); quote { code(); } /* EC# syntax */",
			"Macro-based code quote mechanism, to be used as long as a more complete compiler is not availabe. " +
			"If there is a single parameter that is braces, the braces are stripped out. " +
			"If there are multiple parameters, or multiple statements in braces, the result is a call to #splice(). " +
			"The output refers unqualified to `CodeSymbols` and `LNode` so you must have 'using Loyc.Syntax' at the top of your file. " +
			"The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output.",
			"quote", "#quote")]
		public static LNode quote(LNode node, IMessageSink sink) => Quote(node, true, true);

		[LexicalMacro(@"e.g. rawQuote($foo) ==> F.Call(CodeSymbols.Substitute, F.Id(""foo""));",
			"Behaves the same as quote(code) except that the substitution operator $ is not recognized as a request for substitution.",
			"rawQuote", "#rawQuote")]
		public static LNode rawQuote(LNode node, IMessageSink sink) => Quote(node, false, true);

		[LexicalMacro(@"e.g. quoteWithTrivia(/* cool! */ $foo) ==> foo.PlusAttrs(LNode.List(LNode.Trivia(CodeSymbols.TriviaMLComment, "" cool! "")))",
			"Behaves the same as quote(code) except that trivia is included in the output.")]
		public static LNode quoteWithTrivia(LNode node, IMessageSink sink) => Quote(node, true, false);

		[LexicalMacro(@"e.g. rawQuoteWithTrivia(/* cool! */ $foo) ==> LNode.Call(LNode.List(LNode.Trivia(CodeSymbols.TriviaMLComment, "" cool! "")), CodeSymbols.Substitute, LNode.List(LNode.Id((Symbol) ""foo"")))",
			"Behaves the same as rawQuote(code) except that trivia is included in the output.")]
		public static LNode rawQuoteWithTrivia(LNode node, IMessageSink sink) => Quote(node, false, false);

		/// <summary>This implements `quote` and related macros. It converts a 
		/// Loyc tree (e.g. `Foo(this)`) into a piece of .NET code that can 
		/// construct the essence of the same tree (e.g.
		/// `LNode.Call((Symbol) "Foo", LNode.List(LNode.Id(CodeSymbols.This)));`),
		/// although this code does not preserve the Range properties.
		/// <param name="allowSubstitutions">If this flag is true, when a calls to 
		///   <c>@`'$`</c> is encountered, it is treated as a substitution request,
		///   e.g. <c>$node</c> causes <c>node</c> to be included unchanged in the 
		///   output, and by example, <c>Foo($x, $(..list))</c> produces this tree:
		///   <c>LNode.Call((Symbol) "Foo", LNode.List().Add(x).AddRange(list))</c>.
		///   </param>
		/// <param name="ignoreTrivia">If this flag is true, the output expression
		///   does not construct trivia attributes.</param>
		/// <returns>The quoted form.</returns>
		public static LNode Quote(LNode node, bool allowSubstitutions, bool ignoreTrivia)
		{
			LNode code = node, arg;
			if (code.ArgCount == 1 && (arg = code.Args[0]).Calls(S.Braces) && !arg.HasPAttrs()) {
				// Braces are needed to allow statement syntax in EC#; they are 
				// not necessarily desired in the output, so ignore them. The user 
				// can still write quote {{...}} to include braces in the output.
				code = arg;
			}
			return new CodeQuoter(allowSubstitutions, ignoreTrivia).Quote(code.Args.AsLNode(S.Splice));
		}

		static Dictionary<Symbol, Symbol> CodeSymbolTable = null;
		static LNodeFactory F_ = new LNodeFactory(new EmptySourceFile("CodeQuoteMacro.cs"));
		static LNode _CodeSymbols = F_.Id("CodeSymbols");

		public class CodeQuoter
		{
			public bool _doSubstitutions;
			public bool _ignoreTrivia;
			public CodeQuoter(
				bool doSubstitutions, 
				bool ignoreTrivia = true)
			{
				_doSubstitutions = doSubstitutions;
				_ignoreTrivia = ignoreTrivia;
			}

			static LNode Id_LNode = F_.Id("LNode");
			static LNode Id_PlusAttrs = F_.Id("PlusAttrs");
			static LNode CodeSymbols_Splice = F_.Dot(_CodeSymbols, F_.Id("Splice"));
			static LNode LNode_List     = F_.Dot(Id_LNode, F_.Id("List"));
			static LNode LNode_Literal  = F_.Dot(Id_LNode, F_.Id("Literal"));
			static LNode LNode_Id       = F_.Dot(Id_LNode, F_.Id("Id"));
			static LNode LNode_Call     = F_.Dot(Id_LNode, F_.Id("Call"));
			static LNode LNode_Trivia   = F_.Dot(Id_LNode, F_.Id("Trivia"));
			static LNode LNode_Dot      = F_.Dot(Id_LNode, F_.Id("Dot"));
			static LNode LNode_Of       = F_.Dot(Id_LNode, F_.Id("Of"));
			static LNode LNode_Braces   = F_.Dot(Id_LNode, F_.Id("Braces"));
			static LNode LNode_InParens = F_.Dot(Id_LNode, F_.Id("InParens"));
			static LNode LNode_InParensTrivia = F_.Dot(Id_LNode, F_.Id("InParensTrivia"));
			static LNode LNode_Missing = F_.Dot(Id_LNode, F_.Id("Missing"));

			public LNode Quote(LNode node)
			{
				// TODO: When quoting, ignore injected trivia (trivia with the TriviaInjected flag)
				if (node.Equals(LNode.InParensTrivia))
					return LNode_InParensTrivia;
				if (node.Equals(LNode.Missing))
					return LNode_Missing;

				LNodeList creationArgs = new LNodeList();

				// Translate attributes (if any)
				var attrList = MaybeQuoteList(node.Attrs, isAttributes: true);
				if (attrList != null)
					creationArgs.Add(attrList);

				LNode result;
				switch (node.Kind) {
				case LNodeKind.Literal: // => F.Literal(value)
					creationArgs.Add(node.WithoutAttrs());
					result = F.Call(LNode_Literal, creationArgs);
					break;
				case LNodeKind.Id: // => F.Id(string), F.Id(CodeSymbols.Name)
					creationArgs.Add(QuoteSymbol(node.Name));
					result = F.Call(LNode_Id, creationArgs);
					break;
				default: // NodeKind.Call => F.Dot(...), F.Of(...), F.Call(...), F.Braces(...)
					bool preserveStyle = true;
					if (_doSubstitutions && node.Calls(S.Substitute, 1)) {
						preserveStyle = false;
						result = node.Args[0];
						if (attrList != null) {
							if (result.IsCall)
								result = result.InParens();
							result = F.Call(F.Dot(result, Id_PlusAttrs), attrList);
						}
					} else if (!_ignoreTrivia && node.ArgCount == 1 && node.TriviaValue != NoValue.Value && node.Target.IsId) {
						// LNode.Trivia(Symbol, object)
						result = F.Call(LNode_Trivia, QuoteSymbol(node.Name), F.Literal(node.TriviaValue));
					}
					/*else if (node.Calls(S.Braces)) // F.Braces(...)
						result = F.Call(LNode_Braces, node.Args.SmartSelect(arg => QuoteOne(arg, substitutions)));
					else if (node.Calls(S.Dot) && node.ArgCount.IsInRange(1, 2))
						result = F.Call(LNode_Dot, node.Args.SmartSelect(arg => QuoteOne(arg, substitutions)));
					else if (node.Calls(S.Of))
						result = F.Call(LNode_Of, node.Args.SmartSelect(arg => QuoteOne(arg, substitutions)));*/
					else {
						// General case: F.Call(<Target>, <Args>)
						if (node.Target.IsId)
							creationArgs.Add(QuoteSymbol(node.Name));
						else
							creationArgs.Add(Quote(node.Target));

						var argList = MaybeQuoteList(node.Args);
						if (argList != null)
							creationArgs.Add(argList);
						result = F.Call(LNode_Call, creationArgs);
					}
					// Note: don't preserve prefix notation because if $op is +, 
					// we want $op(x, y) to generate code for x + y (there is no 
					// way to express this with infix notation.)
					if (preserveStyle && node.BaseStyle != NodeStyle.Default && node.BaseStyle != NodeStyle.PrefixNotation)
						result = F.Call(F.Dot(result, F.Id("SetStyle")), F.Dot(F.Id("NodeStyle"), F.Id(node.BaseStyle.ToString())));
					break;
				}
				return result;
			}

			LNode MaybeQuoteList(LNodeList list, bool isAttributes = false)
			{
				if (isAttributes && _ignoreTrivia)
					list = list.SmartWhere(n => !n.IsTrivia || n.IsIdNamed(S.TriviaInParens));
				if (list.IsEmpty)
					return null;
				else if (_doSubstitutions && list.Any(a => VarArgExpr(a) != null))
				{
					if (list.Count == 1)
						return F.Call(LNode_List, VarArgExpr(list[0]));
					// If you write something like quote(Foo($x, $(...y), $z)), a special
					// output style is used to accommodate the variable argument list.
					LNode argList = F.Call(LNode_List);
					foreach (LNode arg in list)
					{
						var vae = VarArgExpr(arg);
						if (vae != null)
							argList = F.Call(F.Dot(argList, F.Id("AddRange")), vae);
						else
							argList = F.Call(F.Dot(argList, F.Id("Add")), Quote(arg));
					}
					return argList;
				}
				else
					return F.Call(LNode_List, list.SmartSelect(item => Quote(item)));
			}

			private static LNode VarArgExpr(LNode arg)
			{
				LNode subj;
				if (arg.Calls(S.Substitute, 1) && ((subj = arg.Args[0]).Calls(S.DotDot, 1) || subj.Calls(S.DotDotDot, 1)))
					return subj.Args[0];
				return null;
			}
		}

		internal static LNode QuoteSymbol(Symbol name)
		{
			if (CodeSymbolTable == null)
				CodeSymbolTable = FindStaticReadOnlies<Symbol>(typeof(CodeSymbols), fInfo => !fInfo.Name.StartsWith("_"));
			Symbol field;
			if (CodeSymbolTable.TryGetValue(name, out field))
				return F.Dot(_CodeSymbols, F.Id(field));
			else
				return F.Call(S.Cast, F.Literal(name.Name), F.Id("Symbol"));
		}

		/// <summary>Helper function that finds the static readonly fields of a given 
		/// type in a given class, and creates a table from the _values_ of those 
		/// fields to the _names_ of those fields.</summary>
		private static Dictionary<T, Symbol> FindStaticReadOnlies<T>(Type type, Predicate<FieldInfo> filter = null)
		{
			var dict = new Dictionary<T, Symbol>();
			var list = type.GetFields(BindingFlags.Static | BindingFlags.Public)
				.Where(field => typeof(T).IsAssignableFrom(field.FieldType) && field.IsInitOnly 
					&& field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0);
			foreach (var field in list)
				if (filter == null || filter(field))
					dict[(T)field.GetValue(null)] = GSymbol.Get(field.Name);
			return dict;
		}
	}
}
