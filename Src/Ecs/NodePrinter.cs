using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ecs;

namespace Loyc.CompilerCore
{
	enum IndentType : byte { Tab = 0, Spaces = 1, DotTab = 2, DotSpaces = 3 };

	public static class NodePrinter
	{
		public delegate bool Strategy(INodeReader node, NodeStyle style, TextWriter target, string indentString, string lineSeparator);

		[ThreadStatic]
		static Strategy _printStrategy;
		public static Strategy PrintStrategy
		{
			get { return _printStrategy ?? new Strategy(SimpleEcsPrintStrategy); }
			set { _printStrategy = value; }
		}

		public static StringBuilder Print(INodeReader node, NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			PrintStrategy(node, style, new StringWriter(sb), indentString, lineSeparator);
			return sb;
		}

		public static EcsNodePrinter NewEcsPrinter(this INodeReader node, StringBuilder target, string indentString = "\t", string lineSeparator = "\n")
		{
			return NewEcsPrinter(node, new StringWriter(target), indentString, lineSeparator);
		}
		public static EcsNodePrinter NewEcsPrinter(this INodeReader node, TextWriter target, string indentString = "\t", string lineSeparator = "\n")
		{
			var wr = new SimpleNodePrinterWriter(target, indentString, lineSeparator);
			return new EcsNodePrinter(node, wr);
		}
		public static bool SimpleEcsPrintStrategy(INodeReader node, NodeStyle style, TextWriter target, string indentString, string lineSeparator)
		{
			var wr = new SimpleNodePrinterWriter(target, indentString, lineSeparator);
			var np = new EcsNodePrinter(node, wr);
			switch (style & NodeStyle.BaseStyleMask)
			{
				case NodeStyle.Expression:         np.PrintExpr(); break;
				case NodeStyle.PrefixNotation:     np.PrintPrefixNotation((style & NodeStyle.Recursive)!=0, false);  break;
				case NodeStyle.PurePrefixNotation: np.PrintPrefixNotation((style & NodeStyle.Recursive)!=0, true); break;
				default:                           np.PrintStmt(); break;
			}
			return true; // TODO: return false if tree contained anything that was unprintable
		}
	}

}
