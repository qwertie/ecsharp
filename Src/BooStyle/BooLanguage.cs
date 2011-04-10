using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Essentials;

namespace Loyc.BooStyle
{
	// TODO
	public class BooLanguage : ILanguageStyle
	{
		#region ILanguageStyle Members

		public string LanguageName
		{
			get { return "Boo"; }
		}

		public string LanguageVersion
		{
			get { return "L0.1"; }
		}

		private static Dictionary<string, Symbol> _keywords;
		public static IDictionary<string, Symbol> StandardKeywords
		{
			get {
				if (_keywords == null) {
					string[] kw = new string[] {
						"and", "as", "ast", "break", "callable", "cast", "char", "class", 
						"constructor", "continue", "def", "destructor", "do", "elif", 
						"else", "ensure", "enum", "event", "except", "failure", "final", 
						"from", "for", "false", "get", "given", "goto", "if", "import", 
						"in", "interface", "internal", "is", "isa", "not", "null", "of", 
						"or", "otherwise", "override", "namespace", "partial", "pass", 
						"public", "protected", "private", "raise", "ref", "retry", 
						"return", "self", "set", "super", "static", "struct", "success", 
						"transient", "true", "try", "typeof", "unless", "virtual", "when", 
						"while", "yield",

						"bool", "byte", "int", "long", "short", 
						"string", "sbyte", "uint", "ulong", "ushort",
						"single", "double", "regex"
					};
					_keywords = new Dictionary<string,Symbol>();
					foreach(string s in kw)
						_keywords.Add(s, GSymbol.Get("_" + s));
				}
				return _keywords;
			}
		}
		IDictionary<string, Symbol> ILanguageStyle.StandardKeywords { get { return StandardKeywords; } }

		public ILexingProvider NewLexingProvider()
		{
			return new BooLexingProvider();
		}

		protected static BooLexingProvider _defaultLexingProvider = new BooLexingProvider();
		public ILexingProvider DefaultLexingProvider
		{
			get { return _defaultLexingProvider; }
		}

		public IParsingProvider NewParsingProvider()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		//public void AstListChanged(AstList list, int firstIndex, Loyc.Essentials.Symbol changeType) {}

		public bool IsOob(Symbol nodeType)
		{
			return nodeType == Tokens.WS
				|| nodeType == Tokens.SL_COMMENT
				|| nodeType == Tokens.ML_COMMENT
				|| nodeType == Tokens.NEWLINE
				|| nodeType == Tokens.LINE_CONTINUATION
				|| nodeType == Tokens.EXTRA_COMMENT_1
				|| nodeType == Tokens.EXTRA_COMMENT_2;
		}

		#endregion
	}
}
