//
// Decorator types, which wrap an existing IMessageSink and modify its behavior
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc
{
	/// <summary>A decorator (wrapper) for <see cref="IMessageSink"/> that filters
	/// out some messages if their <see cref="Severity"/> is too low, according
	/// to the value of the <see cref="MinSeverity"/> property.</summary>
	public class SeverityMessageFilter<TContext> : IMessageSink<TContext>
	{
		/// <summary>Initializes the filter with a minimum severity.</summary>
		/// <param name="target">Another sink to which all messages will be written that pass this filter.</param>
		/// <param name="minSeverity">Minimum severity for which <see cref="IsEnabled"/> returns true,
		/// possibly modified by <c>includeDetails</c>.</param>
		/// <param name="includeDetails">Causes <c>minSeverity</c> to be reduced
		/// by one if <c>minSeverity</c> is an even number. Often when one uses
		/// a severity like <see cref="Severity.Warning"/>, one actually intends 
		/// to also include any associated <see cref="Severity.WarningDetail"/> 
		/// messages. This parameter ensures that you do not exclude details 
		/// accidentally by changing <c>minSeverity</c> for you. To disable 
		/// this behavior, set this parameter to false.</param>
		public SeverityMessageFilter(IMessageSink<TContext> target, Severity minSeverity, bool includeDetails = true)
		{
			Target = target;
			_minSeverity = (Severity)((int)(minSeverity - 1) & ~(includeDetails ? 1 : 0) + 1);
		}
		Severity _minSeverity;

		public IMessageSink<TContext> Target { get; set; }

		/// <summary>Gets or sets the minimum severity that passes the filter. Note:
		/// usually this property should be set to a detail level such as 
		/// <see cref="Severity.InfoDetail"/> rather than a "normal" level such 
		/// as <see cref="Severity.Info"/>, which is one higher.</summary>
		public Severity MinSeverity
		{
			get { return _minSeverity; }
			set { _minSeverity = value; }
		}

		public void Write(Severity level, TContext context, string format)
		{
 			if (Passes(level)) Target.Write(level, context, format);
		}
		public void Write(Severity level, TContext context, string format, object arg0, object arg1 = null)
		{
 			if (Passes(level)) Target.Write(level, context, format, arg0, arg1);
		}
		public void Write(Severity level, TContext context, string format, params object[] args)
		{
 			if (Passes(level)) Target.Write(level, context, format, args);
		}
		public bool IsEnabled(Severity level)
		{
			return Passes(level) && Target.IsEnabled(level);
		}
		bool Passes(Severity level)
		{
			return level >= _minSeverity;
		}
	}

	/// <summary>Alias for SeverityMessageFilter&lt;object>.</summary>
	public class SeverityMessageFilter : SeverityMessageFilter<object>, IMessageSink
	{
		/// <inheritdoc cref="SeverityMessageFilter{TContext}(IMessageSink{TContext}, Severity, bool)"/>
		public SeverityMessageFilter(IMessageSink<object> target, Severity minSeverity, bool includeDetails = true)
			: base(target, minSeverity, includeDetails) { }
	}

	/// <summary>A decorator that uses a delegate to accept or ignore messages.</summary>
	/// <remarks>The filter can accept or reject messages based on both the message 
	/// type and the actual message (format string). When someone calls 
	/// <see cref="IsEnabled(Severity)"/>, the filter is invoked with only the type;
	/// the message is set to null. Accepted messages are sent to the 
	/// <see cref="Target"/> message sink.</remarks>
	public class MessageFilter : IMessageSink
	{
		public Func<Severity, object, string, bool> Filter { get; set; }
		public Func<Severity, bool> TypeFilter { get; set; }
		public IMessageSink Target { get; set; }

		public MessageFilter(IMessageSink target, Func<Severity, object, string, bool> filter) 
		{
			Filter = filter;
			Target = target;
		}
		public MessageFilter(IMessageSink target, Func<Severity, bool> filter) 
		{
			TypeFilter = filter;
			Target = target;
		}
		bool Passes(Severity level, object context, string format)
		{
			return Filter != null && Filter(level, context, format)
				|| TypeFilter != null && TypeFilter(level);
		}
		public void Write(Severity level, object context, string format)
		{
			if (Passes(level, context, format))
				Target.Write(level, context, format);
		}
		public void Write(Severity level, object context, string format, object arg0, object arg1 = null)
		{
			if (Passes(level, context, format))
				Target.Write(level, context, format, arg0, arg1);
		}
		public void Write(Severity level, object context, string format, params object[] args)
		{
			if (Passes(level, context, format))
				Target.Write(level, context, format, args);
		}
		/// <summary>Returns true if <c>Filter(type, null)</c> and <c>target.IsEnabled(type)</c> are both true.</summary>
		public bool IsEnabled(Severity level)
		{
			return Passes(level, null, null) && Target.IsEnabled(level);
		}
	}

	
	/// <summary>A message sink that sends its messages to a list of other sinks.</summary>
	/// <remarks>Null elements are allowed in the <see cref="List"/> and are ignored.</remarks>
	public class MessageMulticaster<TContext> : IMessageSink<TContext>
	{
		List<IMessageSink<TContext>> _list = new List<IMessageSink<TContext>>();
		public IList<IMessageSink<TContext>> List { get { return _list; } }

		public MessageMulticaster(IEnumerable<IMessageSink<TContext>> targets) { _list = new List<IMessageSink<TContext>>(targets); }
		public MessageMulticaster(params IMessageSink<TContext>[] targets) { _list = new List<IMessageSink<TContext>>(targets); }
		public MessageMulticaster() { _list = new List<IMessageSink<TContext>>(); }
	
		public void  Write(Severity level, TContext context, string format)
		{
 			foreach(var sink in _list)
				if (sink != null)
					sink.Write(level, context, format);
		}
		public void  Write(Severity level, TContext context, string format, object arg0, object arg1 = null)
		{
 			foreach(var sink in _list)
				if (sink != null)
					sink.Write(level, context, format, arg0, arg1);
		}
		public void  Write(Severity level, TContext context, string format, params object[] args)
		{
			foreach (var sink in _list)
				if (sink != null)
					sink.Write(level, context, format, args);
		}
		/// <summary>Returns true if <tt>s.IsEnabled(type)</tt> is true for at least one target message sink 's'.</summary>
		public bool IsEnabled(Severity level)
		{
			foreach (var sink in _list)
				if (sink != null && sink.IsEnabled(level))
					return true;
			return false;
		}
	}

	/// <summary>Alias for MessageSplitter&lt;object>.</summary>
	public class MessageMulticaster : MessageMulticaster<object>, IMessageSink
	{
		public MessageMulticaster(IEnumerable<IMessageSink<object>> targets) : base(targets) { }
		public MessageMulticaster(params IMessageSink<object>[] targets) : base(targets) { }
		public MessageMulticaster() : base() { }
	}

	/// <summary>A message sink wrapper that has a default value for the context 
	/// parameter, which is used when the context provided is null, and an optional 
	/// message prefix which is inserted at the beginning of the format string.</summary>
	public class MessageSinkWithContext<TContext> : IMessageSink<TContext> where TContext: class
	{
		IMessageSink<TContext> _target { get; set; }
		public IMessageSink<TContext> Target
		{
			get { return _target ?? MessageSink.Default; }
			set { _target = value; }
		}
		public TContext DefaultContext { get; set; }
		public string MessagePrefix { get; set; }

		/// <summary>Initializes the wrapper.</summary>
		/// <param name="target">Message sink to which all messages are forwarded. If this parameter 
		/// is null, then messages are written to <see cref="MessageSink.Default"/>
		/// <i>at the time a message is written</i>.</param>
		/// <param name="defaultContext">Default context object, used if Write is called with a context of null.</param>
		/// <param name="messagePrefix">A prefix to prepend at the beginning of all messages written.</param>
		/// <param name="scrubPrefix">Whether to replace "{" with ""{{" and "}" with "}}" in 
		/// <c>messagePrefix</c> to avoid accidental misbehavior when the string is formatted. 
		/// <b>Note:</b> in fact, <c>messagePrefix</c> should not contain braces at all, 
		/// because message formatting is optional. <see cref="Write(Severity, TContext, string)"/>
		/// does not perform formatting while <see cref="Write(Severity, TContext, string, object[])"/>
		/// does. Consequently, when calling the first overload, the scrubbing process will 
		/// cause braces in <c>messagePrefix</c> to be doubled as in "{{" or "}}". We could
		/// fix this by storing two separate MessagePrefix strings in this object, one
		/// for each situation, but that's currently not implemented as it is simpler to
		/// just ask users not to put braces in prefixes.</param>
		public MessageSinkWithContext(IMessageSink<TContext> target, TContext defaultContext, string messagePrefix = null, bool scrubPrefix = true)
		{
			Target = target;
			DefaultContext = defaultContext;
			if (scrubPrefix && messagePrefix != null)
				messagePrefix = messagePrefix.Replace("{", "{{").Replace("}", "}}");
			MessagePrefix = messagePrefix;
		}

		string Prefixed(Severity level, string message)
		{
			if (!string.IsNullOrEmpty(MessagePrefix))
				if (IsEnabled(level))
				return MessagePrefix + message;
			return message;
		}

		public bool IsEnabled(Severity level)
		{
			return Target.IsEnabled(level);
		}

		public void Write(Severity level, TContext context, string format)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format);
		}
		public void Write(Severity level, TContext context, string format, params object[] args)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format, args);
		}
		public void Write(Severity level, TContext context, string format, object arg0, object arg1 = null)
		{
			if (MessagePrefix != null)
			{
				if (!IsEnabled(level))
					return;
				format = MessagePrefix + format;
			}
			Target.Write(level, context ?? DefaultContext, format, arg0, arg1);
		}
	}

	/// <summary>Alias for MessageSinkWithContext&lt;object>.</summary>
	public class MessageSinkWithContext : MessageSinkWithContext<object>, IMessageSink
	{
		public MessageSinkWithContext(IMessageSink target, object defaultContext, string messagePrefix = null, bool scrubPrefix = true)
			: base(target, defaultContext, messagePrefix, scrubPrefix) { }
	}
}
