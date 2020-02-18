using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>This interface allows serializing <see cref="LNode"/> objects into the
	/// syntax of a particular programming language.</summary>
	/// <remarks>The ToString() method of an object that implements this interface should
	/// return the name of the programming language that it is able to print.</remarks>
	public interface ILNodePrinter
	{
		/// <summary>Serializes the specified syntax tree to a StringBuilder in the syntax supported by this object.</summary>
		/// <param name="node">A syntax tree to print.</param>
		/// <param name="target">An output buffer, to which output is appended.</param>
		/// <param name="sink">An object used to print warning and error messages. If 
		/// this is null, messages are sent to <see cref="MessageSink.Default"/>.</param>
		/// <param name="mode">Indicates the context in which the node(s) to be printed 
		/// should be understood (e.g. is it a statement or an expression?).</param>
		/// <param name="options">A set of options to control printer behavior. If null,
		/// an appropriate default set of options should be used. Some languages may
		/// support additional option interfaces beyond <see cref="ILNodePrinterOptions"/>.</param>
		void Print(LNode node, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null);

		/// <summary>Serializes a list of syntax trees to a StringBuilder in the syntax supported by this object.</summary>
		/// <param name="nodes">Syntax trees to print.</param>
		/// <param name="target">An output buffer, to which output is appended.</param>
		/// <param name="sink">An object used to print warning and error messages. If 
		/// this is null, messages are sent to <see cref="MessageSink.Default"/>.</param>
		/// <param name="mode">Indicates the context in which the node(s) to be printed 
		/// should be understood (e.g. is it a statement or an expression?).</param>
		/// <param name="options">A set of options to control printer behavior. If null,
		/// an appropriate default set of options should be used. Some languages may
		/// support additional option interfaces beyond <see cref="ILNodePrinterOptions"/>.</param>
		/// <remarks>Some implementations can simply call <see cref="LNodePrinter.PrintMultiple"/>.</remarks>
		void Print(IEnumerable<LNode> nodes, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null);
	}

	/// <summary>Standard extension methods for <see cref="ILNodePrinter"/>.</summary>
	public static class LNodePrinter
	{
		/// <summary>Serializes the specified syntax tree to a string in the 
		/// syntax supported by the specified <see cref="ILNodePrinter"/>.</summary>
		public static string Print(this ILNodePrinter printer, LNode node, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			StringBuilder target = new StringBuilder();
			printer.Print(node, target, sink, mode, options);
			return target.ToString();
		}

		/// <summary>Serializes a list of syntax trees to a string in the 
		/// syntax supported by the specified <see cref="ILNodePrinter"/>.</summary>
		public static string Print(this ILNodePrinter printer, IEnumerable<LNode> nodes, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			StringBuilder target = new StringBuilder();
			printer.Print(nodes, target, sink, mode, options);
			return target.ToString();
		}

		/// <summary>Converts a sequences of LNodes to strings, adding a line separator between each.</summary>
		/// <param name="printer">Printer to be used for each single LNode.</param>
		/// <remarks>The newline between two nodes is suppressed if the second 
		/// node has a <c>%appendStatement</c> attribute.</remarks>
		public static StringBuilder PrintMultiple(ILNodePrinter printer, IEnumerable<LNode> nodes, StringBuilder sb, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options)
		{
			sb = sb ?? new StringBuilder();
			var lineSeparator = (options != null ? options.NewlineString : null) ?? "\n";
			bool first = true;
			foreach (LNode node in nodes) {
				if (!first)
					sb.Append(node.AttrNamed(CodeSymbols.TriviaAppendStatement) == null ? lineSeparator : " ");
				printer.Print(node, sb, sink, mode, options);
				first = false;
			}
			return sb;
		}
	}

}
