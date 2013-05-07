using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Loyc.Collections;
using Loyc.Essentials;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Serializes or deserializes AstNode objects to Loyc AST Interchange Format 
	/// (LAIF).
	/// </summary>
	/// <remarks>
	/// LAIF syntax by example:
	/// 
	/// Ops:\"+"
	/// 
	/// 
	/// </remarks>
	public static class Laif
	{
		public static void Write(AstNode node, TextWriter writer)
		{
			Write(node, writer, true, false);
		}
		public static void Write(RVList<AstNode> nodes, TextWriter writer)
		{
			Write(nodes, writer, true, false);
		}
		public static void Write(AstNode node, TextWriter writer, bool writeTags, bool writeRanges)
		{
			LaifWriter w = new LaifWriter(writer, writeTags, writeRanges);
			w.Write(node);
		}
		public static void Write(RVList<AstNode> nodes, TextWriter writer, bool writeTags, bool writeRanges)
		{
			LaifWriter w = new LaifWriter(writer, writeTags, writeRanges);
			w.Write(nodes);
		}

		public static string ToString(AstNode node)
		{
			StringWriter w = new StringWriter();
			Write(node, w);
			return w.ToString();
		}

		public static RVList<AstNode> Parse(string s)
		{
			return Parse(new StringCharSourceFile(s));
		}
		public static RVList<AstNode> Parse(Stream s, string filename)
		{
			return Parse(new StreamCharSourceFile(s, filename));
		}
		public static RVList<AstNode> Parse(ISourceFile input)
		{
			LaifParser parser = new LaifParser();
			return parser.Parse(input, EmptySourceFile.Default);
		}
	}
}
