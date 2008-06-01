using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
	public class RecognitionException : Exception
	{
		protected IIndexToLine _source;
		protected int _index; // location of exception
		protected string _tokenName;
		protected IAstNode _node;

		public virtual int Index { get { return _index; } }
		public virtual IAstNode CodeNode { get { return _node; } }

		public override string Source
		{
			get {
				return _source.ToString();
			}
		}
		public override string Message
		{
			get {
				if (string.IsNullOrEmpty(base.Message)) {
					return string.Format("{0}: Syntax error at '{1}'",
						SourcePosition.ToString(), TokenName);
				} else
					return base.Message;
			}
		}
		public virtual string TokenName
		{
			get {
				if (_tokenName == null && _node != null)
					return _node.Text;
				else
					return _tokenName;
			}
		}
		public virtual SourcePos SourcePosition
		{
			get {
				if (_source == null)
					return SourcePos.Nowhere;
				return _source.IndexToLine(_index);
			}
		}

		public RecognitionException()
			: this(null, null, null, -1, null, null) { }
		public RecognitionException(string message)
			: this(message, null, null, -1, null, null) { }
		public RecognitionException(string message, Exception innerException)
			: this(message, innerException, null, -1, null, null) { }
		public RecognitionException(string message, RecognitionException innerException)
			: this(message, innerException, innerException._source,
			  innerException.Index, innerException.TokenName, innerException.CodeNode) { }
		public RecognitionException(string message, IIndexToLine source, int index)
			: this(message, null, source, index, null, null) { }
		public RecognitionException(string message, IIndexToLine source, int index, string tokenName)
			: this(message, null, source, index, tokenName, null) { }
		public RecognitionException(string message, IIndexToLine source, int index, IAstNode node)
			: this(message, null, source, index, null, node) { }
		public RecognitionException(IIndexToLine source, int index)
			: this(null, null, source, index, null, null) { }
		public RecognitionException(IIndexToLine source, int index, string tokenName)
			: this(null, null, source, index, tokenName, null) { }
		public RecognitionException(IIndexToLine source, int index, IAstNode node)
			: this(null, null, source, index, null, node) { }
		public RecognitionException(IIndexToLine source, int index, string tokenName, IAstNode node)
			: this(null, null, source, index, tokenName, node) { }
		public RecognitionException(string message, IIndexToLine source, int index, string tokenName, IAstNode node)
			: this(message, null, source, index, tokenName, node) { }
		public RecognitionException(string message, Exception innerException, IIndexToLine source, int index, string tokenName, IAstNode node)
			: base(message, innerException)
		{
			_source = source;
			_index = index;
			_tokenName = tokenName;
			_node = node;
		}
	}
}
