using System;
using System.Collections.Generic;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;
using System.Text;

namespace Loyc.CompilerCore
{
	/// <summary>This is an object to which errors, warnings and notices should be 
	/// sent by <see cref="Loyc.CompilerCore.ExprParsing.IOneOperator{Token}"/>().</summary>
	/// <remarks>
	/// </remarks>
	public interface ISimpleMessageSink
	{
		/// <summary>
		/// Writes a message to the screen or an internal queue for later output. 
		/// The IOneParser will add the filename and position to the message if it is 
		/// output.
		/// </summary>
		/// <param name="category">Type of message. Standard categories are :Error, 
		/// :Warning, :Note, :Detail and :Verbose.</param>
		/// <param name="msg">Untranslated format string to display</param>
		/// <param name="args">Arguments to string.Format()</param>
		/// <remarks>When the message is displayed, lang, msg and args[] are 
		/// passed to <see cref="Localize.From"/>().
		/// 
		/// The string may never be put on the screen. When an ambiguity is detected,
		/// IOneParser calls <see cref="Loyc.CompilerCore.ExprParsing.IOneOperator{T}.IsAcceptable"/>() for all possible
		/// interpretations. If a generator fails, it should output an error message via
		/// this method describing why it failed (although it is not required to). The
		/// message is stored in a queue that is discarded if the ambiguity is resolved,
		/// and displayed otherwise.
		/// 
		/// You pass the components of the error message to Write() rather than the
		/// complete message for three reasons: (1) the string may not be output, so
		/// there is no reason to waste CPU cycles on translating and formatting it. 
		/// (2) If the 'msg' values from different sources are the same, IOneParser may 
		/// want to merge them somehow (but this is not currently done).
		/// (3) Potentially, the variable parts of the message can be highlighted by
		/// the output code.
		/// </remarks>
		void Write(Symbol category, string msg, params object[] args);

		/// <summary>
		/// Writes a message to the screen or an internal queue for later output.
		/// See the other overload for more information.
		/// </summary>
		/// <param name="category">Type of message. Standard categories are :Error, 
		/// :Warning, :Note, :Detail and :Verbose.</param>
		/// <param name="resource">Name to give to the first argument of
		/// <see cref="Localize.From"/>()</param>
		/// <param name="args">Arguments to string.Format()</param>
		void Write(Symbol category, Symbol resource, params object[] args);
	}
}
