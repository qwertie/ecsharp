using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecs;
using System.IO;

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

		public static bool SimpleEcsPrintStrategy(INodeReader node, NodeStyle style, TextWriter target, string indentString, string lineSeparator)
		{
			var wr = new SimpleNodePrinterWriter(target, indentString, lineSeparator);
			var np = new EcsNodePrinter(node, wr);
			if ((style & NodeStyle.BaseStyleMask) == NodeStyle.Statement)
				np.PrintStmt();
			else
				np.PrintExpr();
			return true; // TODO: return false if tree contained anything unprintable
		}

		public static StringBuilder Print(INodeReader node, NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			PrintStrategy(node, style, new StringWriter(sb), indentString, lineSeparator);
			return sb;
		}
	}

}
