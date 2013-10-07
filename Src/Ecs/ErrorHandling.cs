using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.CompilerCore
{
	using SourceLocation = String;
	
	//class CompilerMessage : Exception
	//{
	//    public CompilerMessage(SourceLocation location, string msg) : base(Format(location, msg)) { }
	//    public CompilerMessage(SourceLocation location, string msg, Exception innerException) : base(Format(location, msg), innerException) { }
	//    public CompilerMessage(string msg) : base(msg) { }
	//    public CompilerMessage(string msg, Exception innerException) : base(msg, innerException) { }
	//    protected static string Format(SourceLocation location, string msg)
	//    {
	//        return string.Format("{0}: {1}", location ?? Node.UnknownLocation, Localize.From(msg));
	//    }
	//}
	//class CompilerWarning : CompilerMessage
	//{
	//    public CompilerWarning(SourceLocation location, string msg) : base(location, msg) { }
	//    public CompilerWarning(SourceLocation location, string msg, Exception innerException) : base(location, msg, innerException) { }
	//    public CompilerWarning(string msg) : base(msg) { }
	//    public CompilerWarning(string msg, Exception innerException) : base(msg, innerException) { }
	//}
	//class CompilerError : CompilerMessage
	//{
	//    public CompilerError(SourceLocation location, string msg) : base(location, msg) { }
	//    public CompilerError(SourceLocation location, string msg, Exception innerException) : base(location, msg, innerException) { }
	//    public CompilerError(string msg) : base(msg) { }
	//    public CompilerError(string msg, Exception innerException) : base(msg, innerException) { }
	//}
	//class InternalCompilerError : CompilerError
	//{
	//    public InternalCompilerError(SourceLocation location) : base(Format(location, "")) { }
	//    public InternalCompilerError(SourceLocation location, string msg) : base(Format(location, msg)) { }
	//    public InternalCompilerError(SourceLocation location, Exception innerException) : base(Format(location, ""), innerException) { }
	//    public InternalCompilerError(SourceLocation location, string msg, Exception innerException) : base(Format(location, msg), innerException) { }
	//    static new string Format(SourceLocation location, string msg)
	//    {
	//        return Localize.From("{0}: Internal compiler error. {1}", location ?? Node.UnknownLocation, Localize.From(msg));
	//    }
	//}

	class NodeInvalidatedException : InvalidStateException
	{
		public NodeInvalidatedException(string msg) : base(msg) { }
	}
}
