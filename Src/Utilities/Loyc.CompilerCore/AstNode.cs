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
	public class AstNode : ExtraTagsInWList<object>, ITokenValueAndPos, ISimpleSource<AstNode>
	{
		protected internal static Symbol _OobList = Symbol.Get("OobList");
		
		protected readonly SourceRange _range;
		protected readonly Symbol _type;
		protected readonly RVList<AstNode> _children;
		internal  readonly AstNode _oob; // null if there are no associated OOB nodes
		protected object _value; // doubles as ITokenValue.Text

		public static AstNode New(SourceRange range, Symbol type, RVList<AstNode> children, AstNode oobs, object value)
		{	
			// note to self: I'm not exposing the constructors directly because I 
			// may in the future give the language style responsibility for 
			// choosing derived classes to represent nodes.
			return new AstNode(range, type, children, oobs, value);
		}
		public static AstNode New(SourceRange range, Symbol type, RVList<AstNode> children)
		{
			return new AstNode(range, type, children);
		}
		public static AstNode New(SourceRange range, Symbol type)
		{
			return new AstNode(range, type, RVList<AstNode>.Empty);
		}
		public static AstNode NewWithValue(SourceRange range, Symbol type, object value)
		{
			return new AstNode(range, type, RVList<AstNode>.Empty, null, value);
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

		protected AstNode(SourceRange range, Symbol type, RVList<AstNode> children, AstNode oobs, object value)
		{
			_range = range; _type = type; _children = children; _oob = oobs; _value = value;
		}
		protected AstNode(SourceRange range, Symbol type, RVList<AstNode> children)
		{
			_range = range; _type = type; _children = children;
		}
		protected AstNode(ExtraTagsInWList<object> tags, SourceRange range, Symbol type, RVList<AstNode> children, AstNode oobs, object value)
			: base(tags)
		{
			_range = range; _type = type; _children = children; _oob = oobs; _value = value;
		}
		protected AstNode(AstNode @base, Symbol type, RVList<AstNode> children, object value) : base(@base)
		{
			_range = @base._range; _type = type; _children = children; _oob = @base._oob; _value = value;
		}
		protected AstNode(AstNode @base, Symbol type) : base(@base)
		{
			_range = @base._range; _type = type; _children = @base.Children; _oob = @base._oob; _value = @base.Value;
		}
		protected AstNode(AstNode @base, RVList<AstNode> children) : base(@base)
		{
			_range = @base._range; _type = @base.NodeType; _children = children; _oob = @base._oob; _value = @base.Value;
		}
		protected AstNode(AstNode @base, object value) : base(@base)
		{
			_range = @base._range; _type = @base.NodeType; _children = @base.Children; _oob = @base._oob; _value = value;
		}
		protected AstNode(AstNode @base, AstNode oobToAdd) : base(@base)
		{
			_range = @base._range; _type = @base.NodeType; _children = @base.Children; _value = @base._value;
			if (@base._oob == null)
				_oob = oobToAdd;
			else
				_oob = new AstNode(SourceRange.Nowhere, _OobList, new RVList<AstNode>(@base._oob, oobToAdd), null, null);
		}
		
		public SourceRange     Range    { get { return _range; } }
		public Symbol          NodeType { get { return _type; } }
		public RVList<AstNode> Children { get { return _children; } }
		public int             ChildCount { get { return _children.Count; } }
		public OobList         Oobs     { get { return new OobList(_oob); } }
		public object          Value    { get {
			if (_value == null)
				return ((ITokenValue)this).Text; // sets _value
			return _value;
		} }
		
		public AstNode WithRange(SourceRange @new) 
			{ return new AstNode(this, @new, _type, _children, _oob, _value); }
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
		public AstNode WithoutOobs() 
			{ return _oob == null ? this : new AstNode(this, _range, _type, _children, null, _value); }
		public AstNode WithoutChildrenOrOobs() 
			{ return new AstNode(this, _range, _type, RVList<AstNode>.Empty, null, _value); }
		public AstNode WithAdded(AstNode childToAdd)
			{ return new AstNode(this, Children.Add(childToAdd)); }
		public AstNode WithAdded(AstNode childToAdd1, AstNode childToAdd2)
			{ return new AstNode(this, Children.Add(childToAdd1).Add(childToAdd2)); }
		public AstNode WithAddedOob(AstNode oobToAdd)
			{ return new AstNode(this, oobToAdd); }
		public AstNode With(Symbol type, RVList<AstNode> children, object value)
			{ return new AstNode(this, _range, type, children, _oob, value); }
		public AstNode WithoutTags()
			{ return new AstNode(_range, _type, _children, _oob, _value); }
		public AstNode Clone()
			{ return new AstNode(this, _value); }

		public bool IsOob() { return _range.Source.Language.IsOob(_type); }

		public struct OobList : IEnumerableCount<AstNode> {
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
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
				{ return GetEnumerator(); }
			public IEnumerator<AstNode> GetEnumerator()
			{
				if (_oob == null) return EmptyEnumerator<AstNode>.Default;
				if (_oob.NodeType != _OobList) return EnumerateOobItself();
				return _oob.Children.GetEnumerator();
			}
			private IEnumerator<AstNode> EnumerateOobItself() { yield return _oob; }
		};

		SourcePos ITokenValueAndPos.Position
		{
			get { return _range.Begin; }
		}
		public string SourceText
		{
			get {
				string text = _value as string;
				if (text != null)
					return text;
				ISourceFile src = _range.Source;
				if (src == null)
					return null;
				int startI = _range.BeginIndex;
				int endI = _range.EndIndex;
				if (endI <= startI)
					return string.Empty;
				text = src.Substring(startI, endI - startI);
				if (_value == null)
				{
					if (text.Length < 16)
						text = G.Cache(text);
					_value = text;
				}
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

		#region ISimpleSource<AstNode> Members

		AstNode ISimpleSource<AstNode>.this[int index]
		{
			get { return Children[index]; }
		}

		int IEnumerableCount<AstNode>.Count
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

		#endregion
	}

#if false
	public class AstNode : ExtraTagsInWList<object>, IList<AstNode>
	{
		protected static readonly Symbol _ChildNodes = Symbol.Get("_ChildNodes");

	#region ITokenValueAndPos Members

		public SourcePos Position
		{
			get { throw new NotImplementedException(); }
		}

		public Symbol NodeType
		{
			get { throw new NotImplementedException(); }
		}

		public string Text
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

	#region Access to child nodes

		public IList<AstNode> Children { get { return this; } }
		//public IList<IAstNode> IAstNode.Children { get { return this; } }
		public virtual int Count
		{
			get { return (ChildList ?? EmptyList).Count; }
		}
		public AstNode this[int index]
		{
			get { AstNode node; GetAt(index, out node); return node; }
			set { SetAt(index, value); }
		}

		#endregion

	#region IAstNode Members

		public int Index
		{
			get { throw new NotImplementedException(); }
		}

		public SourceRange SourceRange
		{
			get { throw new NotImplementedException(); }
		}

		public SourceIndex SourceIndex
		{
			get { throw new NotImplementedException(); }
		}

		public string Name
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public object Content
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public IList<IAstNode> Oob
		{
			get { throw new NotImplementedException(); }
		}

		public ITypeNode DataType
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public bool IsModified
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

	#region Node list getters, and virtual protected indexer methods

		private static readonly List<AstNode> EmptyList = new List<AstNode>();
		private List<AstNode> ChildList
		{
			get { return GetTag(_ChildNodes) as List<AstNode>; }
		}
		private List<AstNode> CreatedChildList
		{
			get {
				List<AstNode> list = ChildList;
				if (list == null)
					SetTag(_ChildNodes, list = new List<AstNode>());
				return list;
			}
		}
		virtual protected void GetAt(int index, out AstNode node)
		{
			try {
				node = ChildList[index];
			} catch {
				throw new IndexOutOfRangeException(Localize.From("AstNode: invalid index {0}", index));
			}
		}
		virtual protected void SetAt(int index, AstNode node)
		{
			try {
				ChildList[index] = node;
			} catch {
				throw new IndexOutOfRangeException(Localize.From("AstNode: assignment to invalid index {0}", index));
			}
		}

		#endregion

	#region Explicit IList<AstNode> implementation

		int IList<AstNode>.IndexOf(AstNode item)
		{
			return (ChildList ?? EmptyList).IndexOf(item);
		}
		void IList<AstNode>.Insert(int index, AstNode item)
		{
			CreatedChildList.Insert(index, item);
		}
		void IList<AstNode>.RemoveAt(int index)
		{
			(ChildList ?? EmptyList).RemoveAt(index);
		}
		void ICollection<AstNode>.Add(AstNode item)
		{
			CreatedChildList.Add(item);
		}
		void ICollection<AstNode>.Clear()
		{
			(ChildList ?? EmptyList).Clear();
		}
		bool ICollection<AstNode>.Contains(AstNode item)
		{
			return (ChildList ?? EmptyList).Contains(item);
		}
		bool ICollection<AstNode>.Remove(AstNode item)
		{
			return (ChildList ?? EmptyList).Remove(item);
		}
		IEnumerator<AstNode> IEnumerable<AstNode>.GetEnumerator()
		{
			return (ChildList ?? EmptyList).GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		void ICollection<AstNode>.CopyTo(AstNode[] array, int arrayIndex)
		{
			(ChildList ?? EmptyList).CopyTo(array, arrayIndex);
		}
		bool ICollection<AstNode>.IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}


	
	public class BaseNode : ExtraTags<object>, IList<BaseNode>
	{
		protected static readonly Symbol _ChildNodes = Symbol.Get("_ChildNodes");

	#region Access to child nodes
		
		public IList<BaseNode> Children { get { return this; } }
		public virtual int Count
		{
			get { return (ChildList ?? EmptyList).Count; }
		}
		public BaseNode this[int index]
		{
			get { BaseNode node; GetAt(index, out node); return node; }
			set { SetAt(index, value); }
		}

		#endregion

	#region Content, SourcePosition

		//SourceRange

		#endregion

	#region Node list getters, and virtual protected indexer methods

		private static readonly List<BaseNode> EmptyList = new List<BaseNode>();
		private List<BaseNode> ChildList
		{
			get { return GetTag(_ChildNodes) as List<BaseNode>; }
		}
		private List<BaseNode> CreatedChildList
		{
			get {
				List<BaseNode> list = ChildList;
				if (list == null)
					SetTag(_ChildNodes, list = new List<BaseNode>());
				return list;
			}
		}
		virtual protected void GetAt(int index, out BaseNode node)
		{
			try {
				node = ChildList[index];
			} catch {
				throw new IndexOutOfRangeException(Localize.From("BaseNode: invalid index {0}", index));
			}
		}
		virtual protected void SetAt(int index, BaseNode node)
		{
			try {
				ChildList[index] = node;
			} catch {
				throw new IndexOutOfRangeException(Localize.From("BaseNode: assignment to invalid index {0}", index));
			}
		}

		#endregion

	#region Explicit IList<BaseNode> implementation

		int IList<BaseNode>.IndexOf(BaseNode item)
		{
			return (ChildList ?? EmptyList).IndexOf(item);
		}
		void IList<BaseNode>.Insert(int index, BaseNode item)
		{
			CreatedChildList.Insert(index, item);
		}
		void IList<BaseNode>.RemoveAt(int index)
		{
			(ChildList ?? EmptyList).RemoveAt(index);
		}
		void ICollection<BaseNode>.Add(BaseNode item)
		{
			CreatedChildList.Add(item);
		}
		void ICollection<BaseNode>.Clear()
		{
			(ChildList ?? EmptyList).Clear();
		}
		bool ICollection<BaseNode>.Contains(BaseNode item)
		{
			return (ChildList ?? EmptyList).Contains(item);
		}
		bool ICollection<BaseNode>.Remove(BaseNode item)
		{
			return (ChildList ?? EmptyList).Remove(item);
		}
		IEnumerator<BaseNode> IEnumerable<BaseNode>.GetEnumerator()
		{
			return (ChildList ?? EmptyList).GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		void ICollection<BaseNode>.CopyTo(BaseNode[] array, int arrayIndex)
		{
			(ChildList ?? EmptyList).CopyTo(array, arrayIndex);
		}
		bool ICollection<BaseNode>.IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}

	/// <summary>
	/// The base class of all AST nodes in Loyc.
	/// </summary>
	[DebuggerDisplay("{NodeType} \"{Name}\"")]
	public class AstNode : ExtraTags<object>, IAstNode
	{
	#region Variables

		protected Symbol _nodeType;
		//protected AstNode _parent;
		protected ILanguageStyle _language;
		protected SourceRange _range;
		protected bool _isModified;
		protected byte _lineIndentation;
		protected ushort _spacesAfter;

		protected static readonly Symbol _Attrs = Symbol.Get("_Attrs");
		protected static readonly Symbol _Params = Symbol.Get("_Params");
		protected static readonly Symbol _OfParams = Symbol.Get("_OfParams");
		protected static readonly Symbol _Block = Symbol.Get("_Block");
		protected static readonly Symbol _Oob = Symbol.Get("_Oob");
		protected static readonly Symbol _Name = Symbol.Get("_Name");
		protected static readonly Symbol _Content = Symbol.Get("_Content");
		protected static readonly Symbol _Basis = Symbol.Get("_Basis");
		protected static readonly Symbol _DataType = Symbol.Get("_DataType");
		protected static readonly Symbol _DeclaredType = Symbol.Get("_DeclaredType");
		protected static readonly Symbol _Position = Symbol.Get("_Position");
		protected static readonly Symbol _SpacesAfter = Symbol.Get("_SpacesAfter");

		#endregion

	#region Constructors

		public AstNode(Symbol nodeType) : this(nodeType, SourceRange.Empty, null) { }
		public AstNode(Symbol nodeType, SourceRange range) : this(nodeType, range, null) { }
		public AstNode(Symbol nodeType, SourceRange range, string name)
		{
			_nodeType = nodeType;
			_range = range;
			if (name != null)
				SetTag(_Name, name);
		}
		public AstNode(AstNode original) : this(original, original.NodeType) { }
		public AstNode(AstNode original, Symbol nodeType)
			: base(original)
		{
			// FIXME: This is the wrong approach. If original is a derived class of 
			// AstNode, we won't actually capture all its properties this way.
			_nodeType = nodeType;
			_range = original._range;
			_language = original._language;
			_lineIndentation = original._lineIndentation;
			_spacesAfter = original._spacesAfter;
		}

		#endregion

	#region Public properties

		/// <summary>Returns the name of this node, e.g. in the node for "class Foo
		/// {}", the name would be "Foo"; in the node for "2+2", the name would be
		/// "+".</summary>
		public virtual string Name
		{
			get {
				object name = GetTag(_Name);
				if (name == null)
					return null;
				return name.ToString();
			}
			set { SetTag(_Name, value); }
		}

		public virtual object Content
		{
			get { return GetExtra(_Content); }
			set { SetExtra(_Content, value); }
		}

		/// <summary>Returns the list of attributes that the code applies to this
		/// node. If the node cannot have attributes, this may return a read-only, 
		/// empty list.</summary>
		public AstList Attrs { get { return new AstList(this, _Attrs); } }
		IList<IAstNode> IAstNode.Attrs { get { return Attrs; } }
		/// <summary>Returns the list of generic parameters that the code supplies
		/// in this node. If the node cannot take generic parameters, this may
		/// return a read-only, empty list.</summary>
		public AstList OfParams { get { return new AstList(this, _OfParams); } }
		IList<IAstNode> IAstNode.OfParams { get { return OfParams; } }
		/// <summary>Returns the list of parameters that the code supplies in this
		/// node. If the node cannot take parameters, this may return a read-only, 
		/// empty list.</summary>
		public AstList Params { get { return new AstList(this, _Params); } }
		IList<IAstNode> IAstNode.Params { get { return Params; } }
		/// <summary>Returns the list of substatements or other arbitrary-length
		/// children that the code supplies as children of this node. If the node cannot
		/// have substatements, this may return a read-only, empty list.</summary>
		public AstList Block { get { return new AstList(this, _Block); } }
		IList<IAstNode> IAstNode.Block { get { return Block; } }
		/// <summary>Returns the list of substatements or other arbitrary-length
		/// children that the code supplies as children of this node. If the node cannot
		/// have substatements, this may return a read-only, empty list.</summary>
		public AstList Oob { get { return new AstList(this, _Oob); } }
		IList<IAstNode> IAstNode.Oob { get { return Oob; } }
		/// <summary>Returns the specified list of sub-nodes. The list may be empty
        /// and read-only if the specified list type does not apply to this
        /// node.</summary>
        /// <remarks>The default implementation of AstNode allows a list of any
		/// name to be stored in it.
        /// 
        /// The only common non-standard list you would ask for is
        /// :Inherits, which is the list of inherited items applied to this node,
        /// e.g. the list "B", "C" in "class A : B, C {}". For other lists, use the
        /// other properties provided (Attrs, OfParams, Params, Block).</remarks>
		public AstList ListOf(Symbol id) {
			return new AstList(this, id);
		}
		IList<IAstNode> IAstNode.ListOf(Symbol id) { return ListOf(id); }

		/// <summary>Returns an enumerator of all nodes that are immediate children
		/// of this one, regardless of what list they are in.</summary>
		public virtual IEnumerable<AstNode> AllChildren
		{
			get {
				foreach (object extra in Extra) {
					List<AstNode> list = extra as List<AstNode>;
					if (list != null)
						foreach (AstNode node in list)
							yield return node;
				}
			}
		}
		IEnumerable<IAstNode> IAstNode.AllChildren
		{
			get {
				foreach (AstNode child in AllChildren)
					yield return child;
			}
		}

		/// <summary>Gets or sets Params.First. If there is no first parameter, the
		/// getter returns null and the setter throws IndexOutOfRangeException.</summary>
		/// <remarks>Some derived classes may contain exactly one or two parameters,
		/// stored directly in the node. In that case the derived class may provide
		/// more efficient access to the first and second parameters by overriding
		/// these properties.</remarks>
		public virtual AstNode FirstParam
		{
			get { return Params.First; }
			set { AstList p = Params; p.First = value; }
		}
		
		/// <summary>Gets or sets Params.Second. If there is no second parameter, the
		/// getter returns null and the setter throws IndexOutOfRangeException.</summary>
		/// <remarks>Some derived classes may contain exactly one or two parameters,
		/// stored directly in the node. In that case the derived class may provide
		/// more efficient access to the first and second parameters by overriding
		/// these properties.</remarks>
		public virtual AstNode SecondParam
		{
			get { return Params.Second; }
			set { AstList p = Params; p.Second = value; }
		}

		public virtual string DeclaredType
		{
			get {
				object type = GetExtra(_DeclaredType);
				if (type == null)
					return null;
				return type.ToString();
			}
			set { SetExtra(_DeclaredType, value); }
		}
		public virtual ITypeNode DataType
		{
			get { return GetTag(_DataType) as ITypeNode; }
			set { SetTag(_DataType, value); }
		}
		public virtual bool IsSynthetic
		{
			get { return Basis != null; }
		}
		public virtual AstNode Basis
		{
			get { return GetTag(_Basis) as AstNode; }
		}
		IAstNode IAstNode.Basis { get { return Basis; } }

		/// <summary>Gets the language style associated with this node.</summary>
		/// <remarks>This reference is null by default, but when it is inserted as
		/// a child of another node that has a Language, the child automatically
		/// takes on the Language of the parent.</remarks>
		public ILanguageStyle Language { get { return _language; } }
		
		/// <summary>Returns the parent of this node, or null if the node has no
		/// parent or if it might have multiple parents.</summary>
		public AstNode Parent { get { return _parent; } }

		public bool IsModified { get { return _isModified; } set { _isModified = value; } }

		public SourceRange Range { get { return _range; } }

		public virtual int SpacesAfter { 
			get {
				return _spacesAfter;
			}
			set {
				if ((ushort)value == value)
					_spacesAfter = (ushort)value;
				else if (value < 0)
					_spacesAfter = 0;
				else
					_spacesAfter = 0xFFFF;
			}
		}
		public virtual int LineIndentation
		{
			get { return _lineIndentation; }
			set {
				if ((byte)value == value)
					_lineIndentation = (byte)value;
				else if (value < 0)
					_lineIndentation = 0;
				else
					_lineIndentation = 255;
			}
		}

		public bool IsOob { get { return _range.Source.Language.IsOob(NodeType); } }

		#endregion

	#region ITokenValueAndPos Members

		/// <summary>Returns a source position suitable for reporting to the user.</summary>
		public virtual SourcePosition Position { 
			get {
				if (_range.Source == null)
					return SourcePos.Nowhere;
				else
					return _range.Source.IndexToLine(_range.StartIndex);
			}
		}

	#region List manipulators

		/// <summary>This function is called by the default implementation of
		/// this[listId, index] and the protected internal list manipulators.
		/// </summary>
		/// <param name="listId">ID of list, e.g. :_Params</param>
		/// <returns>List of child nodes</returns>
		/// <remarks>
		/// Derived classes may wish to have a specialized list implementation
		/// rather than using List(of AstNode). In order to do that, the derived
		/// class must override this[listId, index] and all the protected internal
		/// list manipulators that call GetList() or GetOrMakeList(), so as to avoid
		/// calling these methods.
		/// </remarks>
		protected virtual List<AstNode> GetList(Symbol listId)
		{
			return GetTag(listId) as List<AstNode>;
		}
		protected virtual List<AstNode> GetOrMakeList(Symbol listId)
		{
			List<AstNode> list = (List<AstNode>)GetTag(listId);
			if (list == null) {
				list = new List<AstNode>(4);
				SetTag(listId, list);
			}
			return list;
		}

		protected internal virtual AstNode this[Symbol listId, int index]
		{
			get { return GetList(listId)[index]; }
			set { GetList(listId)[index] = value; }
		}
		protected internal virtual void InsertRange(Symbol listId, int index, IEnumerable<AstNode> items)
		{
			GetOrMakeList(listId).InsertRange(index, items);
		}
		protected internal virtual void Insert(Symbol listId, int index, AstNode item)
		{
			GetOrMakeList(listId).Insert(index, item);
		}
		protected internal virtual void RemoveAt(Symbol listId, int index)
		{
			GetOrMakeList(listId).RemoveAt(index);
		}
		protected internal virtual void Clear(Symbol listId)
		{
			List<AstNode> list = GetList(listId);
			if (list != null)
				list.Clear();
		}
		protected internal virtual int Count(Symbol listId)
		{
			List<AstNode> list = GetList(listId);
			if (list != null)
				return list.Count;
			else
				return 0;
		}
		protected internal virtual bool IsReadOnly(Symbol listId)
		{
			return false;
		}

		#endregion

		public Symbol NodeType
		{
			get { return _nodeType; }
		}
		public virtual string Text
		{
			get {
				return _range.SourceText;
				/*if (ChildCount == 0)
					return BriefText;
				StringBuilder sb = new StringBuilder(BriefText);
				bool first = true;
				sb.Append('{');
				foreach (IAstNode node in Children)
				{
					if (first)
						first = false;
					else
						sb.Append(", ");
					sb.Append(node.Text);
				}
				sb.Append('}');
				return sb.ToString();*/
			}
		}
		/// <summary>
		/// Returns a string appropriate to identify the node in error and warning
		/// messages.
		/// </summary>
		public virtual string ErrorIdentifier
		{
			get {
				string t = Text;
				if (string.IsNullOrEmpty(t) || t[0] < 32 || t.Length > 32)
					return NodeType.Name;
				else {
					if (Tokens.IsString(NodeType))
						return Text;
					else
						return "'" + Text + "'";
				}
			}
		}

		#endregion

		/// <summary>This is called by the AstList before this node is added to one
		/// of its lists.
		/// </summary>
		/// <param name="parent">The list to which this node will be added.</param>
		/// <remarks>
		/// The default implementation of SetParent() sets _parent to the specified
		/// value, but if a parent was already assigned and _parent is not null, it
		/// throws an InvalidOperationException instead.
		/// 
		/// If this object's Language reference is null, the default implementation
		/// also sets the Language to the parent's Language. Note that ClearParent
		/// clears the language if it is the same as the parent language; thus,
		/// moving a node from one language to another can be accomplished simply
		/// by removing the node from one AST and adding it to another.
		/// 
		/// Please note that the it is possible for a cycle in the AST to form if
		/// the root node (which has no parent) is added to itself or to one of its
		/// child nodes. Therefore, it is recommended that if a node is designed to
		/// be a root node, its SetParent() method should throw an exception and
		/// keep _parent == null.
		/// 
		/// A derived class can allow a node to have multiple parents. If a node has
		/// multiple parents or might have multiple parents, it must set _parent to
		/// null. Note that if care is not taken, a non-leaf node that can have
		/// multiple parents may be allowed to form cycles in the tree, which is bad
		/// because recursive scans of the AST will never complete.
		/// 
		/// The Basis of a node is not considered its child, so SetParent is not
		/// called when a node uses another node as its Basis.
		/// </remarks>
		protected internal virtual void SetParent(AstList parent)
		{
			if (_parent != null && parent.Node != null)
				throw new InvalidOperationException(Localize.From("The AstNode's parent was already assigned."));
			_parent = parent.Node;
			if (_parent != null && _language == null)
				_language = _parent.Language;
		}

		/// <summary>Detaches this node from the specified parent.</summary>
		/// <param name="parent">The parent list to detach.</param>
		/// <remarks>The default implementation checks that the specified parent is
		/// correct, then sets the parent to null and also clears the Language to
		/// null if Language == Parent.Language.
		/// </remarks>
		protected internal virtual void ClearParent(AstList parent)
		{
			if (_parent != parent.Node) {
				if (_parent == null)
					throw new InvalidOperationException(Localize.From("An AstNode's parent cannot be cleared twice."));
				else
					throw new InvalidOperationException(Localize.From("The specified list is not this AstNode's parent."));
			}
			_parent = null;
		}

	}
	
	[TestFixture]
	public abstract class AstNodeTests
	{
		Symbol _Inherits = Symbol.Get("Inherits");
		
		public AstNode NewNode(Symbol nodeType) { return NewNode(nodeType, null); }
		public virtual AstNode NewNode(Symbol nodeType, string name) 
			{ return new AstNode(nodeType, SourceRange.Empty, name); }

		[Test] public void TestEmpty()
		{
			AstNode n = NewNode(Stmts.DefFn);
			Assert.AreEqual(0, n.Attrs.Count + n.OfParams.Count + n.Params.Count + n.Block.Count + n.ListOf(_Inherits).Count);
			n.Attrs.Add(NewNode(MiscNodes.StdAttr, "public"));
		}
		[Test] public void Test()
		{
			AstNode n = NewNode(Stmts.IfThen);
		}
	}
#endif
}
