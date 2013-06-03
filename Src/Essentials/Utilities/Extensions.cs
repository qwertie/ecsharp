using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Loyc.Math;
using Loyc.Collections.Impl;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Essentials;

namespace Loyc
{
	public static class TypeExt
	{
		public static string NameWithGenericArgs(this Type type)
		{
			string result = type.Name;
			if (type.IsGenericType)
			{
				// remove generic parameter count (e.g. `1)
				int i = result.LastIndexOf('`');
				if (i > 0)
					result = result.Substring(0, i);

				result = string.Format(
					"{0}<{1}>",
					result,
					StringExt.Join(", ", type.GetGenericArguments()
					                     .Select(t => NameWithGenericArgs(t))));
			}
			return result;
		}
	}

	public static class ExceptionExt
	{
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

		public static string DataList(this Exception ex)
		{
			return DataList(ex, "", " = ", "\n");
		}
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
