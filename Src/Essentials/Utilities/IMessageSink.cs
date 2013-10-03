using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Threading;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Collections;
using Loyc.Math;

namespace Loyc.Utilities
{
	/// <summary>A general-purpose interface for a class that accepts formatted 
	/// messages with context information.</summary>
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
		/// <summary>Writes a message to the target that this object represents.</summary>
		/// <param name="type">Severity or importance of the message; widely-used
		/// types include Error, Warning, Note, Debug, and Verbose. The special 
		/// type Detail is intended to provide more information about a previous 
		/// message.</param>
		/// <param name="context">An object that represents the location that the
		/// message applies to, a string that indicates what the program was doing 
		/// when the message was generated, or any other relevant context information.
		/// See also <see cref="MessageSink.LocationString"/>().</param>
		/// <param name="format">A message to display. If there are additional 
		/// arguments, placeholders such as {0} and {1} refer to these arguments.</param>
		void Write(Symbol type, object context, string format);
		void Write(Symbol type, object context, string format, object arg0, object arg1 = null);
		void Write(Symbol type, object context, string format, params object[] args);
		
		/// <summary>Returns true if messages of type 'type' will actually be 
		/// printed, or false if Write(type, ...) is a no-op.</summary>
		bool IsEnabled(Symbol type);
	}

	/// <summary>Holds the default message sink for this thread (<see cref="Current"/>),
	/// <see cref="Symbol"/>s for the common message types, such as Warning and 
	/// Error, and default instances of <see cref="ConsoleMessageSink"/>,
	/// <see cref="TraceMessageSink"/> and <see cref="NullMessageSink"/>.</summary>
	/// <seealso cref="IMessageSink"/>
	public static class MessageSink
	{
		public static readonly Symbol Fatal = GSymbol.Get("Fatal");
		public static readonly Symbol Critical = GSymbol.Get("Critical"); // major error
		public static readonly Symbol Error = GSymbol.Get("Error");
		public static readonly Symbol Warning = GSymbol.Get("Warning");
		public static readonly Symbol Note = GSymbol.Get("Note");
		public static readonly Symbol Debug = GSymbol.Get("Debug");
		public static readonly Symbol Verbose = GSymbol.Get("Verbose");
		public static readonly Symbol Detail = GSymbol.Get("Detail");

		#region Severity integer assignments (from 1 for Verbose to 11 for Fatal)

		// levels from log4net that I'm not planning to use. 
		// They are be supported in the severity number table, however.
		internal static readonly Symbol _Emergency = GSymbol.Get("Emergency");
		internal static readonly Symbol _Alert = GSymbol.Get("Alert");
		internal static readonly Symbol _Severe = GSymbol.Get("Severe");
		internal static readonly Symbol _Info = GSymbol.Get("Info");
		internal static readonly Symbol _Trace = GSymbol.Get("Trace");
		internal static readonly Symbol _Finer = GSymbol.Get("Finer");

		static readonly Symbol[] _severityList = new Symbol[] {
			Detail,    // no log4net equivalent
			Verbose,   // log4net: Verbose = Finest = 10000
			_Finer,    // log4net: Finer = 20000
			Debug,     // log4net: Debug = Fine = 30000
			_Info,     // log4net: Info = 40000
			Note,      // log4net: Notice = 50000
			Warning,   // log4net: Warning = 60000
			Error,     // log4net: Error = 70000
			_Severe,   // log4net: Severe = 80000
			Critical,  // log4net: Critical = 90000
			_Alert,    // log4net: Alert = 100000
			Fatal,     // log4net: Fatal = 110000
			_Emergency,// log4net: Emergency = 120000
		};
		static readonly Dictionary<Symbol, int> _severityMap = get_severityMap();
		static Dictionary<Symbol, int> get_severityMap()
		{
			var map = new Dictionary<Symbol, int>();
			for (int i = 0; i < _severityList.Length; i++)
				map.Add(_severityList[i], i);
			return map;
		}

		/// <summary>Gets a fixed numeric value for the specified message type, or 
		/// -1 if the symbol is not recognized.</summary>
		/// <remarks>
		/// The numbers are Fatal=11, Critical=9, Error=7, Warning=6, Note=5, 
		/// Debug=3, Verbose=1 and Detail=0. The severity numbers are based on 
		/// those in log4net, divided by 10000, e.g. Warning=60000 in log4net.
		/// </remarks>
		public static int GetSeverity(Symbol sym) { return _severityMap.TryGetValue(sym, -1); }
		/// <summary>Gets the symbol for the specified severity number. Returns
		/// Emergency for numbers 12 or above and Verbose for numbers below 0.</summary>
		/// <remarks>
		/// See <see cref="GetSeverity"/> for a list of standard integers. In
		/// addition the following values exist: Emergency=12, Alert=10, Severe=8, 
		/// Info=4, and Finer=2. Again, this is based on the severity levels in 
		/// log4net, divided by 10000.
		/// </remarks>
		public static Symbol GetSymbol(int severity) { return _severityList[severity.InRange(0, 12)]; }

		#endregion

		[ThreadStatic]
		static IMessageSink CurrentTLV = null;
		public static IMessageSink Current
		{
			get { return CurrentTLV ?? Null; }
			set { CurrentTLV = value ?? Null; }
		}
		public static PushedCurrent PushCurrent(IMessageSink sink) { return new PushedCurrent(sink); }
		public struct PushedCurrent : IDisposable
		{
			IMessageSink old;
			public PushedCurrent(IMessageSink @new) { old = Current; Current = @new; }
			public void Dispose() { Current = old; }
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

	/// <summary>This interface allows an object to declare its "location". It is
	/// used by <see cref="MessageSink.LocationString"/>, which helps convert
	/// the "context" of a message into a string.</summary>
	/// <remarks>For example, <see cref="Loyc.Syntax.LNode"/> implements this 
	/// interface so that when a compiler error refers to a source code construct,
	/// the error message contains the location of that construct rather than 
	/// the code itself.</remarks>
	public interface ILocationString
	{
		string LocationString { get; }
	}
}
