using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Runtime;
using System.Diagnostics;

namespace Loyc.CompilerCore
{
	public class AstNode : TagsInWList<object>, ITokenValueAndPos, IListSource<AstNode>, IEnumerable<AstNode>
	{
		protected internal static Symbol _OobList = GSymbol.Get("OobList");
		protected internal static Symbol _SourceText = GSymbol.Get("SourceText");
		
		protected readonly SourceRange _range;
		protected readonly Symbol _type;
		protected readonly RVList<AstNode> _children;
		protected object _value;

		public static AstNode New(SourceRange range, Symbol type, RVList<AstNode> children, object value)
		{	
			// note to self: I'm not exposing the constructors directly because I 
			// may in the future give the language style responsibility for 
			// choosing derived classes to represent nodes.
			return new AstNode(range, type, children, value);
		}
		public static AstNode New(SourceRange range, Symbol type, RVList<AstNode> children, object value, TagsInWList<object> tags)
		{
			return new AstNode(range, type, children, value, tags);
		}
		public static AstNode New(SourceRange range, Symbol type, RVList<AstNode> children)
		{
			return new AstNode(range, type, children);
		}
		public static AstNode New(SourceRange range, Symbol type)
		{
			return new AstNode(range, type, RVList<AstNode>.Empty);
		}
		public static AstNode New(SourceRange range, Symbol type, object value)
		{
			return new AstNode(range, type, RVList<AstNode>.Empty, value);
		}
		public static AstNode NewUnary(SourceRange range, Symbol type, AstNode child)
		{
			return new AstNode(range, type, new RVList<AstNode>(child));
		}
		public static AstNode NewBinary(SourceRange range, Symbol type, AstNode child0, AstNode child1)
		{
			return new AstNode(range, type, new RVList<AstNode>(child0, child1));
		}
		public static AstNode NewTernary(SourceRange range, Symbol type, AstNode child0, AstNode child1, AstNode child2)
		{
			return new AstNode(range, type, new RVList<AstNode>(child0, child1).Add(child2));
		}

		protected AstNode(SourceRange range, Symbol type, RVList<AstNode> children)
		{
			_range = range; _type = type; _children = children;
		}
		protected AstNode(SourceRange range, Symbol type, RVList<AstNode> children, object value)
		{
			_range = range; _type = type; _children = children; _value = value;
		}
		protected AstNode(SourceRange range, Symbol type, RVList<AstNode> children, object value, TagsInWList<object> tags)
			: base(tags)
		{
			_range = range; _type = type; _children = children; _value = value;
		}
		protected AstNode(AstNode @base, Symbol type, RVList<AstNode> children, object value) : base(@base)
		{
			_range = @base._range; _type = type; _children = children; _value = value;
		}
		protected AstNode(AstNode @base, Symbol type) : base(@base)
		{
			_range = @base._range; _type = type; _children = @base.Children; _value = @base.Value;
		}
		protected AstNode(AstNode @base, RVList<AstNode> children) : base(@base)
		{
			_range = @base._range; _type = @base.NodeType; _children = children; _value = @base.Value;
		}
		protected AstNode(AstNode @base, object value) : base(@base)
		{
			_range = @base._range; _type = @base.NodeType; _children = @base.Children; _value = value;
		}
		
		public SourceRange     Range    { get { return _range; } }
		public Symbol          NodeType { get { return _type; } }
		public RVList<AstNode> Children { get { return _children; } }
		public int             ChildCount { get { return _children.Count; } }
		public bool            IsLeaf   { get { return _children.IsEmpty; } }
		public OobList         Oobs     { get { return new OobList(GetTag(_OobList) as AstNode); } }
		public object Value
		{
			get { return _value; }
			set { 
				if (_value != null)
					throw new InvalidOperationException("AstNode.Value can only be set once");
				_value = value;
			}
		}
		
		public AstNode WithRange(SourceRange @new) 
			{ return new AstNode(@new, _type, _children, _value, this); }
		public AstNode WithType(Symbol @new) 
			{ return @new == _type ? this : new AstNode(this, @new); }
		public AstNode WithChildren(RVList<AstNode> @new) 
			{ return @new == _children ? this : new AstNode(this, @new); }
		public AstNode WithChildren(AstNode child0) 
			{ return new AstNode(this, new RVList<AstNode>(child0)); }
		public AstNode WithChildren(AstNode child0, AstNode child1)
			{ return new AstNode(this, new RVList<AstNode>(child0, child1)); }
		public AstNode WithChildren(AstNode child0, AstNode child1, AstNode child2)
			{ return new AstNode(this, new RVList<AstNode>(child0, child1).Add(child2)); }
		public AstNode WithChildren(AstNode child0, AstNode child1, AstNode child2, AstNode child3)
			{ return new AstNode(this, new RVList<AstNode>(child0, child1).Add(child2).Add(child3)); }
		public AstNode WithValue(object @new) 
			{ return @new == _value ? this : new AstNode(this, @new); }
		public AstNode WithoutChildren() 
			{ return _children.IsEmpty ? this : new AstNode(this, RVList<AstNode>.Empty); }
		public AstNode WithAdded(AstNode childToAdd)
			{ return new AstNode(this, Children.Add(childToAdd)); }
		public AstNode WithAdded(AstNode childToAdd1, AstNode childToAdd2)
			{ return new AstNode(this, Children.Add(childToAdd1).Add(childToAdd2)); }
		public AstNode With(Symbol type, RVList<AstNode> children, object value)
			{ return new AstNode(_range, type, children, value, this); }
		public AstNode WithoutTags()
			{ return new AstNode(_range, _type, _children, _value); }
		public AstNode Clone()
			{ return new AstNode(this, _value); }
		public AstNode WithAddedOob(AstNode oobToAdd)
		{
			AstNode n = Clone();
			n.AddOob(oobToAdd);
			return n;
		}
		
		public void AddOob(AstNode newOob)
		{
			AstNode oobs = (AstNode)GetTag(_OobList);
			if (oobs == null)
				SetTag(_OobList, newOob);
			else if (oobs.NodeType == _OobList)
				SetTag(_OobList, oobs.WithAdded(newOob));
			else
				SetTag(_OobList, new AstNode(SourceRange.Nowhere, _OobList, new RVList<AstNode>(oobs, newOob), null));
		}

		public bool IsOob() { return Tokens.IsOob(_type); }

		public struct OobList : ISource<AstNode>, IEnumerable<AstNode>
		{
			private AstNode _oob;
			public OobList(AstNode oob)
				{ _oob = oob; }
			
			public int Count {
				get {
					if (_oob == null) return 0;
					if (_oob.NodeType != _OobList) return 1;
					return _oob.Children.Count;
				}
			}
			public IEnumerator<AstNode> GetEnumerator()
			{
				if (_oob == null) return EmptyEnumerator<AstNode>.Default;
				if (_oob.NodeType != _OobList) return EnumerateOobItself();
				return _oob.Children.GetEnumerator();
			}
			private IEnumerator<AstNode> EnumerateOobItself() { yield return _oob; }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
				{ return GetEnumerator(); }

			public Iterator<AstNode> GetIterator()
			{
				return GetEnumerator().ToIterator();
			}
			public bool Contains(AstNode item)
			{
				return Collections.Contains(this, item);
			}
		};

		SourcePos ITokenValueAndPos.Position
		{
			get { return _range.Begin; }
		}
		public string SourceText
		{
			get {
				ISourceFile src = _range.Source;
				if (src == null)
					return null;
				int startI = _range.BeginIndex;
				int endI = _range.EndIndex;
				if (endI <= startI)
					return string.Empty;
				string text = src.Substring(startI, endI - startI);
				return text;
			}
		}
		string ITokenValue.Text
		{
			get { return SourceText; }
		}

		// List of token types for which the ShortName should return the text of
		// the token (e.g. '{') rather than the token type ('LBRACE').
		static SymbolSet _useTextForShortName = new SymbolSet();
		
		static AstNode()
		{
			_useTextForShortName.AddRange(Tokens.SetOfBraces);
			_useTextForShortName.AddRange(Tokens.SetOfParens);
			_useTextForShortName.Add(Tokens.LANGLE);
			_useTextForShortName.Add(Tokens.RANGLE);
			_useTextForShortName.AddRange(Tokens.SetOfLiterals);
			_useTextForShortName.Add(Tokens.ID);
		}

		/// <summary>Returns a short string--either the node's type or its text--
		/// suitable for display in an error/warning message.</summary>
		public string ShortName
		{
			get {
				if (_useTextForShortName.Contains(_type))
					return ((ITokenValue)this).Text;
				return _type.Name;
			}
		}
		public override string ToString()
		{
			return ShortName;
		}

		#region ISimpleSource<AstNode> and IEnumerable<AstNode> Members

		AstNode IListSource<AstNode>.this[int index]
		{
			get { return Children[index]; }
		}
		AstNode IListSource<AstNode>.this[int index, AstNode defaultValue]
		{
			get { return Children[index, defaultValue]; }
		}

		int ISource<AstNode>.Count
		{
			get { return Children.Count; }
		}

		IEnumerator<AstNode> IEnumerable<AstNode>.GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		Iterator<AstNode> IIterable<AstNode>.GetIterator()
		{
			return Children.GetEnumerator().ToIterator();
		}

		bool ISource<AstNode>.Contains(AstNode item)
		{
			return Children.Contains(item);
		}
		int IListSource<AstNode>.IndexOf(AstNode item)
		{
			return Children.IndexOf(item);
		}

		#endregion
	}


#if true
	public class IAstNode : ExtraTagsInWList<object>, IList<AstNode>
	{
		// A node has: a NodeType, a Scope, tags, children, and a location in a
		// SourceFile. If it is a literal, the ISourceFile is responsible for
		// decoding it; its value is cached in the :Value tag of the token.
		
		private IAstNodeFactory _factory;

		public static IAstNode New(Symbol type, SourceRange range, RVList<IAstNode> children, object value, IDictionary<Symbol, object> tags)
		{
			return _factory.New(range, type, children, value, tags);
		}
		public static IAstNode New(Symbol type, SourceRange range, RVList<IAstNode> children, object value, TagsInWList<object> tags)
		{
			return _factory.New(range, type, children, value, tags);
		}
		public static IAstNode New(Symbol type, SourceRange range, RVList<IAstNode> children, object value)
		{
			return _factory.New(range, type, children, value, null);
		}
		public static IAstNode New(Symbol type, SourceRange range, RVList<IAstNode> children)
		{
			return _factory.New(range, type, children, null, null);
		}
		public static IAstNode New(Symbol type, SourceRange range)
		{
			return _factory.New(range, type, null, (TagsInWList<object>)null);
		}
		public static IAstNode New(Symbol type, SourceRange range, object value, TagsInWList<object> tags)
		{
			return _factory.New(range, type, value, tags);
		}
		public static IAstNode New(Symbol type, SourceRange range, object value)
		{
			return _factory.New(range, type, value, null);
		}
		public static IAstNode New(Symbol type, SourceRange range, IAstNode child, object value, TagsInWList<object> tags)
		{
			return _factory.New(range, type, child, value, tags);
		}
		public static IAstNode New(Symbol type, SourceRange range, IAstNode child, object value)
		{
			return _factory.New(range, type, child, value, (TagsInWList<object>)null);
		}
		public static IAstNode New(Symbol type, SourceRange range, IAstNode child0, IAstNode child1)
		{
			return _factory.New(range, type, new RVList<IAstNode>(child0, child1));
		}
		public static IAstNode New(Symbol type, SourceRange range, IAstNode child0, IAstNode child1, IAstNode child2)
		{
			return _factory.New(range, type, new RVList<IAstNode>(child0, child1).Add(child2));
		}
		public IAstNode WithRange(SourceRange @new) 
			{ return _factory.New(@new, _type, _children, _value, this); }
		public IAstNode WithType(Symbol @new)
			{ return @new == _type ? this : _factory.New(this, @new); }
		public IAstNode WithChildren(RVList<IAstNode> @new) 
			{ return @new == _children ? this : _factory.New(this, @new); }
		public IAstNode WithChildren(IAstNode child0) 
			{ return _factory.New(this, new RVList<IAstNode>(child0)); }
		public IAstNode WithChildren(IAstNode child0, IAstNode child1)
			{ return _factory.New(this, new RVList<IAstNode>(child0, child1)); }
		public IAstNode WithChildren(IAstNode child0, IAstNode child1, IAstNode child2)
			{ return _factory.New(this, new RVList<IAstNode>(child0, child1).Add(child2)); }
		public IAstNode WithChildren(IAstNode child0, IAstNode child1, IAstNode child2, IAstNode child3)
			{ return _factory.New(this, new RVList<IAstNode>(child0, child1).Add(child2).Add(child3)); }
		public IAstNode WithValue(object @new) 
			{ return @new == _value ? this : _factory.New(this, @new); }
		public IAstNode WithoutChildren() 
			{ return _children.IsEmpty ? this : _factory.New(this, RVList<IAstNode>.Empty); }
		public IAstNode WithAdded(IAstNode childToAdd)
			{ return _factory.New(this, Children.Add(childToAdd)); }
		public IAstNode WithAdded(IAstNode childToAdd1, IAstNode childToAdd2)
			{ return _factory.New(this, Children.Add(childToAdd1).Add(childToAdd2)); }
		public IAstNode With(Symbol type, RVList<IAstNode> children, object value)
			{ return _factory.New(_range, type, children, value, this); }
		public IAstNode WithoutTags()
			{ return _factory.New(_range, _type, _children, _value); }
		public IAstNode Clone()
			{ return _factory.New(this, _value); }
		public IAstNode WithAddedOob(IAstNode oobToAdd)
		{
			IAstNode n = Clone();
			n.AddOob(oobToAdd);
			return n;
		}
	}

	public class MethodNode : IAstNode
	{
		protected internal static Symbol _Method = GSymbol.Get("Method");
		IAstNode _attrs,

		public Symbol NodeType { get { return _Method; } }
		public IAstNode Attributes
	}

	public static class AstNodeFactory
	{
		Dictionary<Pair<Symbol, int>, IAstNodeFactory>
	}

	public interface IAstNodeFactory
	{
		public static IAstNode New(Symbol type, SourceRange range, RVList<IAstNode> children, object value, TagsInWList<object> tags)

	}

	class BinaryOp
	{

	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// A scope...
	/// 1. Provides a namespace in which stuff can be found
	/// 3. Can represent a type, or a value of a type
	/// 2. Provides references needed during code generation
	
	/// </remarks>
	interface IScope
	{
		#region Stuff for code generation
		// int x = 3;
		// Console.WriteLine("2+3={0}", 2+x)
		//
		// Node
		//
		#endregion
	}
#endif
}