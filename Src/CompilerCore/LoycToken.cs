using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// The standard token type shared between the C# and boo styles.
	/// </summary>
	public class LoycToken : ExtraAttributes<object>, IToken
	{
		protected Symbol _nodeType;
		protected ICharSource _source;
		protected int _startIndex;
		protected int _length;
		protected object _content;
		//protected IToken _lexNext;
		//protected IToken _lexPrev;
		protected IList<IToken> _block;
		protected bool _visibleToParser;

		public LoycToken(IToken prototype, Symbol nodeType) : this(prototype.CharSource, prototype.StartIndex, prototype.Length, nodeType, true) { }
		public LoycToken(ICharSource source, int startIndex, int length, Symbol nodeType) : this(source, startIndex, length, nodeType, true) { }
		public LoycToken(ICharSource source, int startIndex, int length, Symbol nodeType, bool visibleToParser)
		{
			_nodeType = nodeType;
			_source = source;
			_startIndex = startIndex;
			_length = length;
			_nodeType = nodeType;
			_visibleToParser = visibleToParser;
		}

		#region IToken & IBaseNode Members

		/*public virtual void LexicallyInsertAfter(IToken other)
		{
			if (_lexPrev == other)
				return;
			if (_lexNext != null || _lexPrev != null)
				LexicallyUnlink();
			_lexPrev = other;
			if (_lexPrev != null) {
				_lexNext = _lexPrev.LexicalNext;
				_lexNext.LexicalPrev = this;
				_lexPrev.LexicalNext = this;
			}
		}
		public virtual void LexicallyUnlink()
		{
			IToken n, p;
			if ((p = _lexPrev) != null) {
				_lexPrev = null;
				p.LexicalNext = _lexNext;
			}
			if ((n = _lexNext) != null) {
				_lexNext = null;
				n.LexicalPrev = p;
			}
		}
		public virtual IToken LexicalNext { 
			get { return _lexNext; }
			set { 
				Debug.Assert((value != null && value.LexicalPrev == this) ||
				             (_lexNext != null && _lexNext.LexicalPrev == null));
				_lexNext = value;
			}
		}
		public virtual IToken LexicalPrev {
			get { return _lexPrev; }
			set { 
				Debug.Assert((value != null && value.LexicalNext == this) ||
				             (_lexPrev != null && _lexPrev.LexicalNext == null));
				_lexPrev = value;
			}
		}*/

		public virtual ICharSource CharSource
		{
			get { return _source; }
		}
		public virtual int StartIndex
		{
			get { return _startIndex; }
		}
		public virtual int Length
		{
			get { return _length; }
		}
		public virtual object Content
		{
			get { 
				if (_content == null)
					_content = Text;
				return _content; 
			}
			set { _content = value; }
		}
		public IList<IToken> Block
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public virtual bool VisibleToParser
		{
			get { return _visibleToParser; }
			set { _visibleToParser = value; }
		}
		public int SpacesAfter
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public IDictionary<Symbol, object> Extra
		{
			get { return this; }
		}

		#endregion

		#region ITokenValueAndPos & ITokenValue Members

		public SourcePos Position
		{
			get { return _source.IndexToLine(_startIndex); }
		}

		public string Text
		{
			get { return _source.Substring(_startIndex, _length); }
		}
		public Symbol NodeType
		{
			get { return _nodeType; }
		}

		#endregion
	}
}
