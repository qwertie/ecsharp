using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;

namespace Loyc.CompilerCore
{
	public class CompilerMsg
	{
		public static Symbol _Error = GSymbol.Get("Error");
		public static Symbol _Warning = GSymbol.Get("Warning");
		public static Symbol _Note = GSymbol.Get("Note");
		public static Symbol _Detail = GSymbol.Get("Detail");
		public static Symbol _Verbose = GSymbol.Get("Verbose");

		public static CompilerMsg Error(SourceRange range, string message, params object[] args)
			{ return new CompilerMsg(_Error, range, message, args); }
		public static CompilerMsg Error(SourcePos position, string message, params object[] args)
			{ return new CompilerMsg(_Error, position, message, args); }
		public static CompilerMsg Error(SourceRange range, string message)
			{ return new CompilerMsg(_Error, range, message, (object[])null); }
		public static CompilerMsg Error(SourcePos position, string message)
			{ return new CompilerMsg(_Error, position, message, (object[])null); }
		public static CompilerMsg Warning(SourceRange range, string message, params object[] args)
			{ return new CompilerMsg(_Warning, range, message, args); }
		public static CompilerMsg Warning(SourcePos position, string message, params object[] args)
			{ return new CompilerMsg(_Warning, position, message, args); }
		public static CompilerMsg Warning(SourceRange range, string message)
			{ return new CompilerMsg(_Warning, range, message, (object[])null); }
		public static CompilerMsg Warning(SourcePos position, string message)
			{ return new CompilerMsg(_Warning, position, message, (object[])null); }
		public static CompilerMsg Note(SourceRange range, string message, params object[] args)
			{ return new CompilerMsg(_Note, range, message, args); }
		public static CompilerMsg Note(SourcePos position, string message, params object[] args)
			{ return new CompilerMsg(_Note, position, message, args); }
		public static CompilerMsg Note(SourceRange range, string message)
			{ return new CompilerMsg(_Note, range, message, (object[])null); }
		public static CompilerMsg Note(SourcePos position, string message)
			{ return new CompilerMsg(_Note, position, message, (object[])null); }
		public static CompilerMsg Detail(SourceRange range, string message, params object[] args)
			{ return new CompilerMsg(_Detail, range, message, args); }
		public static CompilerMsg Detail(SourcePos position, string message, params object[] args)
			{ return new CompilerMsg(_Detail, position, message, args); }
		public static CompilerMsg Detail(SourceRange range, string message)
			{ return new CompilerMsg(_Detail, range, message, (object[])null); }
		public static CompilerMsg Detail(SourcePos position, string message)
			{ return new CompilerMsg(_Detail, position, message, (object[])null); }
		public static CompilerMsg Verbose(SourceRange range, string message, params object[] args)
			{ return new CompilerMsg(_Verbose, range, message, args); }
		public static CompilerMsg Verbose(SourcePos position, string message, params object[] args)
			{ return new CompilerMsg(_Verbose, position, message, args); }
		public static CompilerMsg Verbose(SourceRange range, string message)
			{ return new CompilerMsg(_Verbose, range, message, (object[])null); }
		public static CompilerMsg Verbose(SourcePos position, string message)
			{ return new CompilerMsg(_Verbose, position, message, (object[])null); }

		public CompilerMsg(Symbol type, SourceRange range, string message, params object[] args)
		{
			_range = range;
			_type = type;
			_message = message;
			_args = args;
		}
		public CompilerMsg(Symbol type, SourcePos position, string message, params object[] args)
		{
			_range = SourceRange.Nowhere;
			_pos = position;
			_type = type;
			_message = message;
			_args = args;
		}

		string _message;
		object[] _args;
		string _formattedMessage, _messageAndPosition;
		Symbol _type;

		SourceRange _range;
		SourcePos _pos;
		public SourceRange Range { get { return _range; } }
		public SourcePos Position
		{ 
			get { 
				if (_pos == null)
					_pos = _range.Begin;
				return _pos;
			}
		}
		public Symbol Type
		{
			get { return _type; }
		}
		public bool IsError
		{
			get { return _type == _Error; }
		}
		public string MessageAlone
		{
			get {
				if (_formattedMessage == null)
					_formattedMessage = Localize.From(_message, _args);
				return _formattedMessage;
			}
		}
		public string MessageAndPosition
		{
			get {
				if (_messageAndPosition == null)
				{
					StringBuilder sb;
					SourcePos p = Position;
					if (p != null && (p.Line > 0 || p.FileName != null)) {
						sb = new StringBuilder(p.ToString());
						sb.Append(": ");
					} else
						sb = new StringBuilder();
					if (_type != _Error) {
						sb.Append(Localize.From(_type.Name));
						sb.Append(": ");
					}
					sb.Append(MessageAlone);
					_messageAndPosition = sb.ToString();
				}
				return _messageAndPosition;
			}
		}
		public string UnformattedMessage
		{
			get { return _message; }
		}
		public object[] MessageArgs
		{
			get { return _args; }
		}
		public CompilerException ToException()
		{
			return new CompilerException(Position, MessageAndPosition);
		}

		public CompilerMsg With(SourcePos sourcePos)
		{
			CompilerMsg msg = new CompilerMsg(_type, sourcePos, _message, _args);
			msg._formattedMessage = _formattedMessage;
			msg._messageAndPosition = _messageAndPosition;
			return msg;
		}
	}

	public class CompilerException : ApplicationException
	{
		public CompilerException(SourcePos position, string message) 
			: base(message) { _pos = position; }
		public CompilerException(SourcePos position, string message, Exception innerException) 
			: base(message, innerException) { _pos = position; }
		
		SourcePos _pos;
		SourcePos Position
		{ 
			get { return _pos; }
		}
	}
}
