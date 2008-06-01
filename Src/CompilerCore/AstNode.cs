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
	// Universal expression syntax:
	// <BinaryExpr> [...attributes...] Class Foo<TypeParam T> 
	// (...params...) :Inherits(...) :MyList(...content...) #1234 {
	//     ...block...
	// }

	/// <summary>
	/// The base class of all AST nodes in Loyc.
	/// </summary>
	public abstract class AstNode : ExtraAttributes<object>, IAstNode
	{
		#region Variables

		protected Symbol _nodeType;
		protected AstNode _parent;
		protected ITokenValueAndPos _positionToken;
		protected ILanguageStyle _language;

		protected static Symbol _Attrs = Symbol.Get("_Attrs");
		protected static Symbol _Params = Symbol.Get("_Params");
		protected static Symbol _OfParams = Symbol.Get("_OfParams");
		protected static Symbol _Block = Symbol.Get("_Block");
		protected static Symbol _Name = Symbol.Get("_Name");
		protected static Symbol _Basis = Symbol.Get("_Basis");
		protected static Symbol _DataType = Symbol.Get("_DataType");
		protected static Symbol _Position = Symbol.Get("_Position");

		#endregion

		public AstNode(Symbol nodeType, ITokenValueAndPos positionToken) 
			{ _nodeType = nodeType; _positionToken = positionToken; }
		public AstNode(Symbol nodeType, SourcePos position) { 
			_nodeType = nodeType; 
			if (position != null) 
				SetExtra(_Position, position);
		}

		#region Public properties

		/// <summary>Returns a dictionary that can be used to store additional state
		/// beyond the content of the token or node. See documentation of this
		/// property in IBaseNode.
		/// </summary>
		public IDictionary<Symbol, object> Extra { get { return this; } }

		/// <summary>Returns the name of this node, e.g. in the node for "class Foo
		/// {}", the name would be "Foo"; in the node for "2+2", the name would be
		/// "+".</summary>
		public virtual string Name { 
			get {
				object name = GetExtra(_Name);
				if (name == null)
					return null;
				return name.ToString();
			}
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

		public virtual ITypeNode DataType
		{
			get { return GetExtra(_DataType) as ITypeNode; }
			set { SetExtra(_DataType, value); }
		}
		public virtual bool IsSynthetic
		{
			get { return Basis != null; }
		}
		public virtual AstNode Basis
		{
			get { return GetExtra(_Basis) as AstNode; }
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
			return GetExtra(listId) as List<AstNode>;
		}
		protected virtual List<AstNode> GetOrMakeList(Symbol listId)
		{ 
			List<AstNode> list = (List<AstNode>)GetExtra(listId);
			if (list == null) {
				list = new List<AstNode>(4);
				SetExtra(listId, list);
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

		#region ITokenValueAndPos Members

		/// <summary>Returns a source position suitable for reporting to the user.</summary>
		public virtual SourcePos Position { 
			get {
				if (_positionToken != null)
					return _positionToken.Position;
				else {
					SourcePos p = GetExtra(_Position) as SourcePos;
					if (p != null)
						return p;
					else
						return SourcePos.Nowhere;
				}
			}
		}

		public Symbol NodeType
		{
			get { return _nodeType; }
		}
		public virtual string Text
		{
			get {
				throw new NotImplementedException();
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

		#endregion
	}
}
