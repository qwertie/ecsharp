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
		static Dictionary<Symbol, Symbol> CodeSymbolTable = null;

		[LexicalMacro("e.g. quote({ foo(); }) ==> F.Id(id);", 
			"Poor-man's code quote mechanism, to be used until something more sophisticated becomes available. "+
			"Assumes the existence of an LNodeFactory F, which is used to build a syntax tree from the specified code. "+
			"If there is a single parameter that is braces, the braces are stripped out. "+
			"If there are multiple parameters or multiple statements in braces, the result is a call to #splice(). "+
			"The output refers unqualified to 'CodeSymbols' and 'LNodeFactory' so you must have 'using Loyc.Syntax' at the top of your file. " +
			"The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output.",
			"quote", "#quote")]
		public static LNode quote(LNode node, IMessageSink sink)
		{
			return quote2(node, sink, true);
		}
		[LexicalMacro(@"e.g. quoteRaw($foo) ==> F.Call(CodeSymbols.Substitute, F.Id(""foo""));",
			"Behaves the same as quote(code) except that the substitution operator $ is not recognized as a request for substitution.",
			"rawQuote", "#rawQuote")]
		public static LNode rawQuote(LNode node, IMessageSink sink)
		{
			return quote2(node, sink, false);
		}
		static LNode quote2(LNode node, IMessageSink sink, bool substitutions)
		{
			if (node.ArgCount != 1)
				return Reject(sink, node, "Expected a single parameter.");
			LNode code = node.Args[0];
			if (code.Calls(S.Braces, 1) && !code.HasPAttrs())
				return QuoteOne(code.Args[0], substitutions);
			else
				return QuoteOne(code, substitutions);
		}
		static LNode QuoteCore(LNode node)
		{
			if (node.ArgCount == 1)
				return QuoteOne(node.Args[0]);
			else {
				var quoteds = node.Args.SmartSelect(n => QuoteOne(n));
				return F.Call(F_Call, quoteds.Insert(0, CodeSymbols_Splice)); 
			}
		}
		static LNodeFactory F_ = new LNodeFactory(new EmptySourceFile("CodeQuoteMacro.cs"));
		static LNode Id_F = F_.Id("F");
		static LNode Id_WithAttrs = F_.Id("WithAttrs");
		static LNode _CodeSymbols = F_.Id("CodeSymbols");
		static LNode CodeSymbols_Splice = F_.Dot(_CodeSymbols, F_.Id("Splice"));
		static LNode F_Literal = F_.Dot(Id_F, F_.Id("Literal"));
		static LNode F_Id       = F_.Dot(Id_F, F_.Id("Id"));
		static LNode F_Call     = F_.Dot(Id_F, F_.Id("Call"));
		static LNode F_Dot      = F_.Dot(Id_F, F_.Id("Dot"));
		static LNode F_Of       = F_.Dot(Id_F, F_.Id("Of"));
		static LNode F_Braces   = F_.Dot(Id_F, F_.Id("Braces"));
		public static LNode QuoteIdHelper(Symbol name)
		{
			if (CodeSymbolTable == null)
				CodeSymbolTable = FindStaticReadOnlies<Symbol>(typeof(CodeSymbols), fInfo => !fInfo.Name.StartsWith("_"));
			Symbol field;
			if (CodeSymbolTable.TryGetValue(name, out field))
				return F.Dot(_CodeSymbols, F.Id(field));
			else
				return F.Literal(name.Name);
		}
		public static LNode QuoteOne(LNode node, bool substitutions = true)
		{
			LNode result;
			switch (node.Kind) {
			case NodeKind.Literal: // => F.Literal(value)
				result = F.Call(F_Literal, node.WithoutAttrs());
				break;
			case NodeKind.Id: // => F.Id(string), F.Id(CodeSymbols.Name)
				result = F.Call(F_Id, QuoteIdHelper(node.Name));
				break;
			default: // NodeKind.Call => F.Dot(...), F.Of(...), F.Call(...), F.Braces(...)
				if (substitutions && node.Calls(S.Substitute, 1))
					result = node.Args[0];
				else if (node.Calls(S.Braces)) // F.Braces(...)
					result = F.Call(F_Braces, node.Args.SmartSelect(arg => QuoteOne(arg)));
				else if (node.Calls(S.Dot) && node.ArgCount.IsInRange(1, 2))
					result = F.Call(F_Dot, node.Args.SmartSelect(arg => QuoteOne(arg)));
				else if (node.Calls(S.Of))
					result = F.Call(F_Of, node.Args.SmartSelect(arg => QuoteOne(arg)));
				else { // General case: F.Call(<Target>, <Args>)
					LNode outTarget;
					if (node.Target.IsId)
						outTarget = QuoteIdHelper(node.Name);
					else
						outTarget = QuoteOne(node.Target);
					RVList<LNode> outArgs = new RVList<LNode>(outTarget);
					foreach (LNode arg in node.Args)
						outArgs.Add(QuoteOne(arg));
					result = F.Call(F_Call, outArgs);
				}
				break;
			}
			// Translate attributes too (if any)
			RWList<LNode> outAttrs = null;
			foreach (LNode attr in node.Attrs)
				if (!attr.IsTrivia) {
					outAttrs = outAttrs ?? new RWList<LNode>();
					outAttrs.Add(QuoteOne(attr));
				}
			if (outAttrs != null)
				result = F.Call(F.Dot(result, Id_WithAttrs), outAttrs.ToRVList());

			return result;
		}

		/// <summary>Helper function that finds the static readonly fields of a given 
		/// type in a given class, and creates a table from the _values_ of those 
		/// fields to the _names_ of those fields.</summary>
		private static Dictionary<T, Symbol> FindStaticReadOnlies<T>(Type type, Predicate<FieldInfo> filter = null)
		{
			var dict = new Dictionary<T, Symbol>();
			var list = type.GetFields(BindingFlags.Static | BindingFlags.Public)
				.Where(field => typeof(T).IsAssignableFrom(field.FieldType) && field.IsInitOnly);
			foreach (var field in list)
				if (filter == null || filter(field))
					dict[(T)field.GetValue(null)] = GSymbol.Get(field.Name);
			return dict;
		}
	}
}
