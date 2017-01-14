// Generated from MessageSink.ecs by LeMP custom tool. LeMP version: 2.5.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using Loyc.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>Keeps track of the default message sink (<see cref="Default"/>); 
	/// contains a series of helper methods; and contains extension methods 
	/// modeled after log4net: Fatal, Error, Warn, Info, Debug.</summary>
	/// <seealso cref="IMessageSink"/>
	public static class MessageSink
	{
		static ThreadLocalVariable<IMessageSink> _default = new ThreadLocalVariable<IMessageSink>(null, autoFallback: true); public static IMessageSink Default { get {
				return _default.Value ?? Null;
			} } [Obsolete("This property is now called Default")] public static IMessageSink Current { get {
				return Default;
			} set {
				_default.Value = value ?? Null;
			} }	/// <summary>Used to change the <see cref="MessageSink.Default"/> property temporarily.</summary>
		/// <example><code>
		/// using (MessageSink.SetDefault(ConsoleMessageSink.Value))
		///     MessageSink.Default.Write(Severity.Warning, null, "This prints on the console.")
		/// </code></example>
		/// <remarks>This method sets a thread-local value, but it also sets the
		/// global default used by threads on which this method was never called.
		/// <para/>
		/// This property follows the Ambient Service Pattern:
		/// http://core.loyc.net/essentials/ambient-service-pattern.html
		/// </remarks>
		public static SavedValue<IMessageSink> SetDefault(IMessageSink sink)
		{
			return new SavedValue<IMessageSink>(_default, sink);
		}
	
		[Obsolete("This method is now called SetDefault()")] 
		public static PushedCurrent PushCurrent(IMessageSink sink) { return new PushedCurrent(sink); }
	
		/// <summary>Returned by <see cref="PushCurrent(IMessageSink)"/>.</summary>
		public struct PushedCurrent : IDisposable
		{
			public readonly IMessageSink OldValue;
			public PushedCurrent(IMessageSink @new) { OldValue = Default; _default.Value = @new ?? NullMessageSink.Value; }
			public void Dispose() { _default.Value = OldValue; }
		}
	
		/// <summary>Returns context.Location if context implements 
		/// <see cref="IHasLocation"/>; otherwise, returns context itself.</summary>
		public static object LocationOf(object context)
		{
			var loc = context as IHasLocation;
			if (loc == null)
				return context;
			return loc.Location;
		}
	
		[Obsolete("Name changed to ContextToString")] 
		public static string LocationString(object context)
		{
			return ContextToString(context);
		}
	
		/// <summary>Gets the location information from the specified object, or
		/// converts the object to a string. This is the default method returned
		/// from <see cref="ContextToString"/>.</summary>
		/// <param name="context">A value whose string representation you want to get.</param>
		/// <returns>
		/// If <c>context</c> implements <see cref="IHasLocation"/>,
		/// this converts <see cref="IHasLocation.Location"/> to a string; 
		/// if <c>context</c> is null, this method returns <c>null</c>; otherwise 
		/// it returns <c>context.ToString()</c>.
		/// </returns>
		public static string GetLocationString(object context)
		{
			if (context == null)
				return null;
			var ils = context as IHasLocation;
			return (ils != null ? ils.Location ?? context : context).ToString();
		}
	
		static readonly Func<object, string> _getLocationString = GetLocationString;
		static ThreadLocalVariable<Func<object, string>> _contextToString = 
		new ThreadLocalVariable<Func<object, string>>(_getLocationString, autoFallback: true);
	
		/// <summary>Gets the strategy that message sinks should use to convert 
		/// a context object to a string.</summary>
		/// <remarks>
		/// Message sinks are commonly used to display error and warning 
		/// messages, and when you write a message with <c>IMessageSink.Write()</c>, 
		/// the second parameter is a "context" argument which specifies the object
		/// to which the message is related (for example, when writing compiler 
		/// output, the context might be a node in a syntax tree). Most message 
		/// sinks display the message in text form (in a log file or terminal), and 
		/// in that case the best option is to display the location information 
		/// associated with the context object (e.g. Foo.cpp:45), rather than a 
		/// string representation of the object itself.
		/// <para/>
		/// Therefore, message sinks that display a message in text form will call
		/// this delegate to convert the context object to a string.
		/// <para/>
		/// See <see cref="GetLocationString"/> to learn how the 
		/// default strategy works, but message sinks should call this delegate 
		/// rather than GetLocationString().
		/// </remarks>
		public static Func<object, string> ContextToString
		{
			get { return _contextToString.Value; }
		}
	
		/// <summary>Sets the strategy that message sinks should use to convert 
		/// a context object to a string.</summary>
		/// <remarks><see cref="ContextToString"/> is a thread-local value, but since
		/// .NET does not support inheritance of thread-local values, this method
		/// also sets the global default used by threads on which this method was 
		/// never called.
		/// <para/>
		/// This property follows the Ambient Service Pattern:
		/// http://core.loyc.net/essentials/ambient-service-pattern.html
		/// </remarks>
		public static SavedValue<Func<object, string>> SetContextToString(Func<object, string> contextToString)
		{
			return new SavedValue<Func<object, string>>(_contextToString, contextToString ?? _getLocationString);
		}
	
		public static bool IsFatalEnabled<C>(this IMessageSink<C> sink)
		{
			return sink.IsEnabled(Severity.Fatal);
		}
		public static void Fatal<C>(this IMessageSink<C> sink, C context, string format)
		{
			sink.Write(Severity.Fatal, context, format);
		}
		public static void Fatal<C>(this IMessageSink<C> sink, C context, string format, params object[] args)
		{
			sink.Write(Severity.Fatal, context, format, args);
		}
		public static void Fatal<C>(this IMessageSink<C> sink, C context, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Fatal, context, format, arg0, arg1);
		}
		public static void Fatal(this IMessageSink<object> sink, string format)
		{
			sink.Write(Severity.Fatal, null, format);
		}
		public static void FatalFormat(this IMessageSink<object> sink, string format, params object[] args)
		{
			sink.Write(Severity.Fatal, null, format, args);
		}
		public static void FatalFormat(this IMessageSink<object> sink, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Fatal, null, format, arg0, arg1);
		}
		public static bool IsErrorEnabled<C>(this IMessageSink<C> sink)
		{
			return sink.IsEnabled(Severity.Error);
		}
		public static void Error<C>(this IMessageSink<C> sink, C context, string format)
		{
			sink.Write(Severity.Error, context, format);
		}
		public static void Error<C>(this IMessageSink<C> sink, C context, string format, params object[] args)
		{
			sink.Write(Severity.Error, context, format, args);
		}
		public static void Error<C>(this IMessageSink<C> sink, C context, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Error, context, format, arg0, arg1);
		}
		public static void Error(this IMessageSink<object> sink, string format)
		{
			sink.Write(Severity.Error, null, format);
		}
		public static void ErrorFormat(this IMessageSink<object> sink, string format, params object[] args)
		{
			sink.Write(Severity.Error, null, format, args);
		}
		public static void ErrorFormat(this IMessageSink<object> sink, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Error, null, format, arg0, arg1);
		}
		public static bool IsWarnEnabled<C>(this IMessageSink<C> sink)
		{
			return sink.IsEnabled(Severity.Warning);
		}
		public static void Warning<C>(this IMessageSink<C> sink, C context, string format)
		{
			sink.Write(Severity.Warning, context, format);
		}
		public static void Warning<C>(this IMessageSink<C> sink, C context, string format, params object[] args)
		{
			sink.Write(Severity.Warning, context, format, args);
		}
		public static void Warning<C>(this IMessageSink<C> sink, C context, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Warning, context, format, arg0, arg1);
		}
		public static void Warn(this IMessageSink<object> sink, string format)
		{
			sink.Write(Severity.Warning, null, format);
		}
		public static void WarnFormat(this IMessageSink<object> sink, string format, params object[] args)
		{
			sink.Write(Severity.Warning, null, format, args);
		}
		public static void WarnFormat(this IMessageSink<object> sink, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Warning, null, format, arg0, arg1);
		}
		public static bool IsInfoEnabled<C>(this IMessageSink<C> sink)
		{
			return sink.IsEnabled(Severity.Info);
		}
		public static void Info<C>(this IMessageSink<C> sink, C context, string format)
		{
			sink.Write(Severity.Info, context, format);
		}
		public static void Info<C>(this IMessageSink<C> sink, C context, string format, params object[] args)
		{
			sink.Write(Severity.Info, context, format, args);
		}
		public static void Info<C>(this IMessageSink<C> sink, C context, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Info, context, format, arg0, arg1);
		}
		public static void Info(this IMessageSink<object> sink, string format)
		{
			sink.Write(Severity.Info, null, format);
		}
		public static void InfoFormat(this IMessageSink<object> sink, string format, params object[] args)
		{
			sink.Write(Severity.Info, null, format, args);
		}
		public static void InfoFormat(this IMessageSink<object> sink, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Info, null, format, arg0, arg1);
		}
		public static bool IsDebugEnabled<C>(this IMessageSink<C> sink)
		{
			return sink.IsEnabled(Severity.Debug);
		}
		public static void Debug<C>(this IMessageSink<C> sink, C context, string format)
		{
			sink.Write(Severity.Debug, context, format);
		}
		public static void Debug<C>(this IMessageSink<C> sink, C context, string format, params object[] args)
		{
			sink.Write(Severity.Debug, context, format, args);
		}
		public static void Debug<C>(this IMessageSink<C> sink, C context, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Debug, context, format, arg0, arg1);
		}
		public static void Debug(this IMessageSink<object> sink, string format)
		{
			sink.Write(Severity.Debug, null, format);
		}
		public static void DebugFormat(this IMessageSink<object> sink, string format, params object[] args)
		{
			sink.Write(Severity.Debug, null, format, args);
		}
		public static void DebugFormat(this IMessageSink<object> sink, string format, object arg0, object arg1 = null)
		{
			sink.Write(Severity.Debug, null, format, arg0, arg1);
		}
		/// <summary>Converts a quadruplet (type, context, format, args) to a single 
		/// string containing all that information. The format string and the Severity
		/// are localized with <see cref="Localize.Localized(string, object[])"/>.</summary>
		/// <remarks>For example, <c>FormatMessage(Severity.Error, "context", "Something happened!")</c>
		/// comes out as "Error: context: Something happened!".</remarks>
		public static string FormatMessage(Severity type, object context, string format, params object[] args)
		{
			string loc = ContextToString(context);
			string formatted = Localize.Localized(format, args);
			if (string.IsNullOrEmpty(loc))
				return type.ToString().Localized() + ": " + formatted;
			else
				return loc + ": " + 
				type.ToString().Localized() + ": " + formatted;
		}
	
		/// <summary>Sends all messages to <see cref="System.Diagnostics.Trace.WriteLine(string)"/>.</summary>
		[Obsolete] public static readonly TraceMessageSink Trace = TraceMessageSink.Value;
		/// <summary>Sends all messages to the <see cref="System.Console.WriteLine(string)"/>.</summary>
		[Obsolete] public static readonly ConsoleMessageSink Console = ConsoleMessageSink.Value;
		/// <summary>The message sink that discards all messages.</summary>
		public static readonly NullMessageSink Null = NullMessageSink.Value;
	
		/// <summary>Sends all messages to a user-defined method.</summary>
		public static MessageSinkFromDelegate FromDelegate(WriteMessageFn writer, Func<Severity, bool> isEnabled = null)
		{
			return new MessageSinkFromDelegate(writer, isEnabled);
		}
	
		/// <summary>Creates a message sink that writes to <see cref="MessageSink.Default"> with a default context to be used
		/// when <c>Write</c> is called with <c>context: null</c>, so that you 
		/// can use extension methods like <c>Error(string)</c> that do not 
		/// have any context parameter.</summary>
		public static MessageSinkWithContext WithContext(object context, string messagePrefix = null)
		{
			return new MessageSinkWithContext(null, context, messagePrefix);
		}
	
		/// <summary>Creates a message sink with a default context to be used
		/// when <c>Write</c> is called with <c>context: null</c>, so that you 
		/// can use extension methods like <c>Error(string)</c> that do not 
		/// have any context parameter.</summary>
		public static MessageSinkWithContext<TContext> WithContext<TContext>(IMessageSink<TContext> target, TContext context, string messagePrefix = null) where TContext: class
		{
			return new MessageSinkWithContext<TContext>(target, context, messagePrefix);
		}
	
	}
}