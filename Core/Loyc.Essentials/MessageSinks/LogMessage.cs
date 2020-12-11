using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;

namespace Loyc
{
	/// <summary>Holds an argument list compatible with 
	/// <see cref="IMessageSink{TContext}.Write(Severity,TContext,string)"/>.
	/// Typically used with <see cref="MessageHolder"/>.</summary>
	public struct LogMessage : IHasLocation
	{
		public LogMessage(Severity type, object context, string format, object arg0, object arg1 = null)
			: this (type, context, format, new object[2] { arg0, arg1 }) {}
		public LogMessage(Severity type, object context, string format)
			: this (type, context, format, EmptyArray<object>.Value) {}
		public LogMessage(Severity type, object context, string format, params object[] args)
		{
			Severity = type;
			Context = context;
			Format = format;
			_args = args;
		}

		public readonly Severity Severity;
		public readonly object Context;
		public readonly string Format;
		readonly object[] _args;
		public object[] Args { get { return _args; } }
		public string Formatted
		{
			get {
				try {
					return Localize.Localized(Format, _args);
				} catch {
					return Format;
				}
			}
		}

		public override string ToString()
		{
			return MessageSink.FormatMessage(Severity, Context, Format, _args);
		}

		public object Location
		{
			get { return MessageSink.LocationOf(Context); }
		}
		public void WriteTo(IMessageSink sink)
		{
			if (_args.Length == 0)
				sink.Write(Severity, Context, Format);
			else
				sink.Write(Severity, Context, Format, _args);
		}
	}
}
