using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Standard parsing modes used with <see cref="IParsingService"/> and
	/// <see cref="ILNodePrinterOptions"/>.</summary>
	public class ParsingMode : Symbol
	{
		private ParsingMode(Symbol prototype) : base(prototype) { }
		public static new readonly SymbolPool<ParsingMode> Pool 
		                     = new SymbolPool<ParsingMode>(p => new ParsingMode(p));

		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input 
		/// as a single expression or expression list (which, in most languages, 
		/// is comma-separated).</summary>
		public static readonly ParsingMode Expressions = Pool.Get("Exprs");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of statements. If the language makes a distinction between 
		/// executable and declaration contexts, this refers to the executable 
		/// context.</summary>
		public static readonly ParsingMode Statements = Pool.Get("Stmts");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of statements. If the language makes a distinction between 
		/// executable and declaration contexts, this refers to the declaration
		/// context, in which types, methods, and properties are recognized.</summary>
		public static readonly ParsingMode Declarations = Pool.Get("Decls");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a list of types (or a single type, if a list is not supported).</summary>
		public static readonly ParsingMode Types = Pool.Get("Types");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a formal argument list (parameter names with types).</summary>
		public static readonly ParsingMode FormalArguments = Pool.Get("FormalArguments");
		/// <summary>Tells <see cref="IParsingService.Parse"/> to treat the input
		/// as a complete source file (this should be the default, i.e. null will
		/// do the same thing).</summary>
		public static readonly ParsingMode File = Pool.Get("File");
	}
}
