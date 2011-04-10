using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Essentials;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// This interface represents the essence of a token: its type and text.
	/// </summary>
	public interface ITokenValue
	{
		/// <summary>A Symbol representing the token type.</summary>
		/// <remarks>The Tokens class contains a list of token types that are shared
		/// among multiple languages.
		/// 
		/// Note that in general, you should not assume that a given symbol
		/// corresponds to a particular token or node class. You can expect that 
		/// occasionally, unrelated classes in different extensions will pick the same 
		/// symbol. However, a standard type symbol should only be used by a node 
		/// class that implements the proper interface (or base class) and semantics for 
		/// that symbol.</remarks>
		Symbol NodeType { get; }
	
		/// <summary>Returns the text of the token or node in the syntax of the source language.</summary>
		/// <remarks>
		/// If this is implemented via ICodeNode and the node contains mutable data 
		/// (such as a list of child nodes), the text should be generated from the current 
		/// state of the node. Text should not return the text from the original source file if
		/// changes have been made to the node after the lexing process.</remarks>
		string Text { get; }
	}

	/// <summary>A very simple implementation of ITokenValue.</summary>
	public class TokenValue : ITokenValue
	{
		public TokenValue(Symbol type) {
			_type = type;
		}
		public TokenValue(Symbol type, string text) {
			_type = type;
			_text = text;
		}
		protected Symbol _type;
		protected string _text;
		public Symbol NodeType { 
			get { return _type; } 
			set { _type = value; }
		}
		public string Text {
			get { return _text; }
			set { _text = value; }
		}

		/// <summary>A factory for use with IOperatorDivider(of TokenValue).</summary>
		public static TokenValueAndPos SubTokenFactory(TokenValueAndPos t, int offset, string substring)
		{
			return new TokenValueAndPos(t.NodeType, substring,
				new SourcePos(t.Position.FileName, t.Position.Line, t.Position.PosInLine + offset));
		}
	}

	/// <summary>A simplified token interface that offers the NodeType, Text, 
	/// and Position properties.
	/// </summary>
	public interface ITokenValueAndPos : ITokenValue
	{
		/// <summary>Returns the source file and position therein the best 
		/// represents this node. Typically it is the position of the beginning
		/// of the text from which this node was created.</summary>
		/// <remarks>If the position cannot be described by a line and position 
		/// (e.g. because it's a synthetic token and not from a real file) then 
		/// the return value can be SourcePos.Nowhere. However, if a node is 
		/// synthetic, the position of an existing token should usually be used, 
		/// so that if an error occurs regarding this node, a relevant position 
		/// can be reported to the user.</remarks>
		SourcePos Position { get; }
	}

	public class TokenValueAndPos : TokenValue, ITokenValueAndPos
	{
		public TokenValueAndPos(Symbol type, string text, SourcePos pos) 
			: base(type, text) { _pos = pos; }
		public TokenValueAndPos(Symbol type, string text)
			: base(type, text) { _pos = SourcePos.Nowhere; }
		public TokenValueAndPos(Symbol type)
			: base(type) { _pos = SourcePos.Nowhere; }

		protected SourcePos _pos;
		public SourcePos Position
		{
			get { return _pos; }
			set { _pos = value; }
		}
	}
}
