using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Threading;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Utilities
{
	/// <summary>An interface for a class that accepts formatted messages.</summary>
	/// <remarks>
	/// IMessageSink is used for dependency injection of a target for formatted 
	/// messages; it could be used for log messages, compiler error messages, or
	/// to report the progress of a process, for instance.
	/// <para/>
	/// Since .NET does not allow static members in an interface, the static
	/// members can be found in <see cref="MessageSink"/>.
	/// <para/>
	/// Each message has a "type" <see cref="Symbol"/> which indicates the 
	/// type of message being printed. For message sinks that are used as 
	/// loggers, this should be one of the following logging levels, listed
	/// in order of importance: Fatal, Error, Warning, Note, Debug, Verbose.
	/// For message sinks that are used for compiler messages, the standard
	/// levels are: Fatal, Error, SoftError, Warning, Note, Detail. "SoftError"
	/// is intended to describe code that is technically illegal, but which
	/// does not impede code generation. "Detail" provides more information 
	/// about a previously-printed message, while "Note" is intended for
	/// independent messages that are less severe than warnings.
	/// <para/>
	/// The message sink itself should perform localization, which can be done
	/// with <see cref="Localize.From"/>.
	/// <para/>
	/// Only a single Write() method is truly needed (<see cref="Write(Symbol, string, params object[])"/>),
	/// but efficiency reasons the interface contains two other writers. It 
	/// is expected to be fairly common that a message sink will drop some or
	/// all messages without printing them, e.g. if a message sink is used for 
	/// logging, verbose messages might be "off" by default. It would be 
	/// wasteful to actually localize and format a message if the message will
	/// not actually be printed, and it would also be wasteful to create an array 
	/// of objects to hold the arguments if they are just going to be discarded.
	/// With that in mind, since most formatting requests only need a couple of 
	/// arguments, there is an overload of Write() that accepts up to two
	/// arguments without the need to package them into an array, and there is
	/// an overload that takes no formatting arguments (this indicates that 
	/// parameter substitution is not required and should not be attempted.)
	/// <para/>
	/// In addition, the caller can call <see cref="IsEnabled(Symbol)"/> to avoid 
	/// doing any work required to prepare a message for printing when a certain
	/// category of output is disabled.
	/// </remarks>
	/// <seealso cref="MessageSink"/>
	/// <seealso cref="ConsoleMessageSink"/>
	/// <seealso cref="TraceMessageSink"/>
	/// <seealso cref="NullMessageSink"/>
	/// <seealso cref="MessageFilter"/>
	/// <seealso cref="MessageHolder"/>
	/// <seealso cref="MessageSplitter"/>
	public interface IMessageSink
	{
		void Write(Symbol type, object context, string format);
		void Write(Symbol type, object context, string format, object arg0, object arg1 = null);
		void Write(Symbol type, object context, string format, params object[] args);
		
		/// <summary>Returns true if messages of type 'type' will actually be 
		/// printed, or false if Write(type, ...) is a no-op.</summary>
		bool IsEnabled(Symbol type);
	}

	/// <summary>Holds the <see cref="Current"/> default message sink for this 
	/// thread, <see cref="Symbol"/>s for the common message types, such as 
	/// Warning and Error, and default instances of <see cref="ConsoleMessageSink"/>,
	/// <see cref="TraceMessageSink"/> and <see cref="NullMessageSink"/>.</summary>
	public static class MessageSink
	{
		public static readonly Symbol Fatal = GSymbol.Get("Fatal");
		public static readonly Symbol Error = GSymbol.Get("Error");
		public static readonly Symbol SoftError = GSymbol.Get("SoftError");
		public static readonly Symbol Warning = GSymbol.Get("Warning");
		public static readonly Symbol Note = GSymbol.Get("Info");
		public static readonly Symbol Debug = GSymbol.Get("Debug");
		public static readonly Symbol Verbose = GSymbol.Get("Verbose");
		public static readonly Symbol Detail = GSymbol.Get("Detail");

		public static readonly ThreadLocalVariable<IMessageSink> CurrentTLV = new ThreadLocalVariable<IMessageSink>();
		public static IMessageSink Current
		{
			get { return CurrentTLV.Value ?? Null; }
			set { CurrentTLV.Value = value ?? Null; }
		}

		/// <summary>Returns <see cref="ILocationString.LocationString"/> if 
		/// 'context' implements that interface, null if context is null, and
		/// <see cref="object.ToString()"/> otherwise.</summary>
		public static string LocationString(object context)
		{
			if (context == null) return null;
			var ils = context as ILocationString;
			return ils != null ? ils.LocationString : context.ToString();
		}

		/// <summary>Sends all messages to <see cref="System.Diagnostics.Trace.WriteLine"/>.</summary>
		public static readonly TraceMessageSink Trace = new TraceMessageSink();
		/// <summary>Sends all messages to the <see cref="System.Console.WriteLine"/>.</summary>
		public static readonly ConsoleMessageSink Console = new ConsoleMessageSink();
		/// <summary>Discards all messages.</summary>
		public static readonly NullMessageSink Null = new NullMessageSink();
	}

	/// <summary>An interface 
	/// 
	/// </summary>
	public interface ILocationString
	{
		string LocationString { get; }
	}
}
