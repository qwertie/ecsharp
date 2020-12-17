using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Ecs
{
	/// <summary>
	/// A list of symbols that are very specific to C# or Enhanced C#.
	/// Note: many symbols <see cref="Loyc.Syntax.CodeSymbols"/> should be in 
	/// this class instead, but this class was created very belatedly.
	/// </summary>
	public class EcsCodeSymbols : Loyc.Syntax.CodeSymbols
	{
		public static new readonly Symbol PtrArrow = GSymbol.Get("'->");       //!< "'->":    a->b   <=> @`'->`(a, b)

		// New Symbols for C# 5 and 6 (NullDot `?.` is defined elsewhere, since EC# already supported it)
		public static new readonly Symbol Async = GSymbol.Get("#async"); //!< [#async] Task Foo(); <=> async Task Foo();
																	 // async is a normal contextual attribute so it needs no special parser support.
		public static new readonly Symbol Await = GSymbol.Get("await"); //!< await(x); <=> await x; (TENTATIVE: should this be changed to #await?)
		public static new readonly Symbol DictionaryInitAssign = GSymbol.Get("'[]="); //!< @`'[]=`(0, 1, x) <=> [0,1]=x (only supported in 'new' initializer blocks)

		// Proposed: https://github.com/dotnet/csharplang/issues/74
		public static readonly Symbol ForwardPipeArrow = GSymbol.Get("'|>"); //!< @`'|>`(a, b) <=> a |> b
		public static readonly Symbol NullForwardPipeArrow = GSymbol.Get("'?|>"); //!< @`'?|>`(a, b) <=> a ?|> b

		public static new readonly Symbol TriviaCsRawText = GSymbol.Get("%C#RawText");             //!< "%C#RawText" - `%C#RawText`("stuff") - Raw text that is only printed by the C# printer (not printers for other languages)
		public static new readonly Symbol TriviaCsPPRawText = GSymbol.Get("%C#PPRawText");         //!< "%C#PPRawText" - `%C#PPRawText`("#stuff") - Raw text that is guaranteed to be preceded by a newline and is only printed by the C# printer
		public static new readonly Symbol CsRawText = GSymbol.Get("#C#RawText");
		public static new readonly Symbol CsPPRawText = GSymbol.Get("#C#PPRawText"); //!< "#C#PPRawText" - Preprocessor raw text: always printed on separate line

		public static readonly Symbol PPNullable = GSymbol.Get("##nullable"); //<! C# 8: ##nullable(" enable") <=> #nullable enable
		public static readonly Symbol CsiReference = GSymbol.Get("##reference"); //<! C# interactive: ##reference(" \"C:\\Path\"") <=> #r "C:\Path"
		public static readonly Symbol CsiLoad = GSymbol.Get("##load");
		public static readonly Symbol CsiCls = GSymbol.Get("##cls");
		public static readonly Symbol CsiClear = GSymbol.Get("##clear");
		public static readonly Symbol CsiHelp = GSymbol.Get("##help");
		public static readonly Symbol CsiReset = GSymbol.Get("##reset");
	}
}
