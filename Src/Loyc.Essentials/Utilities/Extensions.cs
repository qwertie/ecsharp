using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Loyc.Math;
using Loyc.Collections.Impl;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc
{
	/// <summary>Extension methods for <c>Type</c>.</summary>
	public static class TypeExt
	{
		/// <summary>Returns the type with generic parameters in C# style, e.g.
		/// <c>typeof(List&lt;List&lt;string>>.NameWithGenericArgs()</c> returns
		/// <c>List&lt;List&lt;String>></c>.</summary>
		public static string NameWithGenericArgs(this Type type)
		{
			return MemoizedTypeName.Get(type);
		}
	}

	/// <summary>Extension methods for exceptions.</summary>
	public static class ExceptionExt
	{
		/// <summary>Returns a string of the form "{ex.Message} ({ex.GetType().Name})".</summary>
		public static string ExceptionMessageAndType(this Exception ex) {
			return string.Format("{0} ({1})", ex.Message, ex.GetType().Name);
		}
		/// <summary>Gets the innermost InnerException, or <c>ex</c> itself if there are no inner exceptions.</summary>
		/// <exception cref="NullReferenceException">ex is null.</exception>
		public static Exception InnermostException(this Exception ex)
		{
			while (ex.InnerException != null)
				ex = ex.InnerException;
			return ex;
		}
		/// <inheritdoc cref="Description(Exception, bool, string)"/>
		public static string Description(this Exception ex) { return Description(ex, false); }
		/// <inheritdoc cref="Description(Exception, bool, string)"/>
		/// <remarks>Adds a stack trace.</remarks>
		public static string DescriptionAndStackTrace(this Exception ex) { return Description(ex, true); }
		/// <summary>Gets a description of the exception in the form "{ex.Message} ({ex.GetType().Name})".
		/// If the exception has InnerExceptions, these are printed afterward in 
		/// the form "Inner exception: {ex.Message} ({ex.GetType().Name})" and 
		/// separated from the outer exception by "\n\n" (or a string of your 
		/// choosing).</summary>
		/// <param name="addStackTrace">If true, the stack trace of the outermost
		/// exception is added to the end of the message (not the innermost 
		/// exception, because the inner stack trace gets truncated. TODO: 
		/// investigate whether the full stack trace can be reconstructed).</param>
		/// <param name="lineSeparator">Separator between different exceptions and 
		/// before the stack trace.</param>
		public static string Description(this Exception ex, bool addStackTrace, string lineSeparator = "\n\n")
		{
			Exception inner = ex;
			StringBuilder msg = new StringBuilder();
			do {
				if (inner != ex) {
					msg.Append(lineSeparator);
					msg.Append(Localize.From("Inner exception: "));
				}
				msg.AppendFormat("{0} ({1})", ex.Message, ex.GetType().Name);
				if (inner.InnerException == null)
					break;
				inner = inner.InnerException;
			} while (true);
			msg.Append(lineSeparator);
			if (addStackTrace)
				msg.Append(ex.StackTrace);
			return msg.ToString();
		}

		/// <summary>Returns a string containing the exception type, message, 
		/// Data pairs (if any) and stack strace, followed by the type, message and 
		/// stack strace of inner exceptions, if any.</summary>
		/// <remarks>If <c>maxInnerExceptions</c> is not given, the default is 3.</remarks>
		public static string ToDetailedString(this Exception ex) { return ToDetailedString(ex, 3); }
		public static string ToDetailedString(this Exception ex, int maxInnerExceptions)
		{
			StringBuilder sb = new StringBuilder();
			try {
				for (;;)
				{
					sb.AppendFormat("{0}: {1}\n", ex.GetType().Name, ex.Message);
					AppendDataList(ex.Data, sb, "  ", " = ", "\n");
					sb.Append(ex.StackTrace);
					if ((ex = ex.InnerException) == null)
						break;
					sb.Append("\n\n");
					sb.Append(Localize.From("Inner exception:"));
					sb.Append(' ');
				}
			} catch { }
			return sb.ToString();
		}

		/// <summary>Converts <c>Exception.Data</c> to a string, separating each key-value
		/// pair by a newline.</summary>
		public static string DataList(this Exception ex)
		{
			return DataList(ex, "", " = ", "\n");
		}
		/// <summary>Converts <c>Exception.Data</c> to a string, separating each key
		/// from each value with <c>keyValueSeparator</c>, prepending each line by
		/// <c>linePrefix</c>, and separating each pair with <c>newLine</c>, which
		/// may or may not be "\n", your choice.</summary>
		public static string DataList(this Exception ex, string linePrefix, string keyValueSeparator, string newLine)
		{
			return AppendDataList(ex.Data, null, linePrefix, keyValueSeparator, newLine).ToString();
		}

		public static StringBuilder AppendDataList(IDictionary dict, StringBuilder sb, string linePrefix, string keyValueSeparator, string newLine)
		{
			sb = sb ?? new StringBuilder();
			foreach (DictionaryEntry kvp in dict)
			{
				sb.Append(linePrefix);
				sb.Append(kvp.Key);
				sb.Append(keyValueSeparator);
				sb.Append(kvp.Value);
				sb.Append(newLine);
			}
			return sb;
		}
	}
}
