using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestAlgebraicDataTypes : MacroTesterBase
	{
		[Test]
		public void TestAlgebraicDataTypeDecls()
		{
			// Simple example with multiple 'type constructors'(subclasses)
			TestEcs(@"
				public alt class SExpr {
					public alt Atom(object Value);
					public alt List(params object[] Items);
				}", @"
					public class SExpr
					{
						public SExpr() {}
					}
					public class Atom : SExpr
					{
						public Atom(object Value) { this.Value = Value; }
						
						public object Value { get; private set; }
						public Atom WithValue(object newValue) { return new Atom(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object Item1 { get { return Value; } }
					}
					public class List : SExpr
					{
						public List(params object[] Items) { this.Items = Items; }

						public object[] Items { get; private set; }
						public List WithItems(params object[] newValue) { return new List(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object[] Item1 { get { return Items; } }
					}");
			// Try adding a generic parameter.
			TestEcs(@"
				public alt class Opt<T> {
					public alt Have(T Value);
				}", @"
					public class Opt<T> {
						public Opt() { }
					}
					public class Have<T> : Opt<T>
					{
						public Have(T Value) { this.Value = Value; }
						
						public T Value { get; private set; }
						public Have<T> WithValue(T newValue) { return new Have<T>(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public T Item1 { get { return Value; } }
					}
					public static partial class Have
					{
						public static Have<T> New<T>(T Value) { return new Have<T>(Value); }
					}");
			// Check that attributes are preserved, that an ultimate base class is 
			// allowed, and that additional init code and common_stuff is preserved.
			TestEcs(@"
				[A] public abstract alt class BinaryTree<T> : BaseClass {
					[N] alt Node(BinaryTree<T> Left, BinaryTree<T> Right);
					[L] alt Leaf<T>(T Value) { stuff; }
					common_stuff;
				}", @"
				[A] public abstract class BinaryTree<T> : BaseClass
				{ 
					public BinaryTree() { }
					common_stuff;
				}
				[N] class Node<T> : BinaryTree<T>
				{
					public Node(BinaryTree<T> Left, BinaryTree<T> Right) { this.Left = Left; this.Right = Right; }
					
					public BinaryTree<T> Left { get; private set; }
					public BinaryTree<T> Right { get; private set; }
					public Node<T> WithLeft(BinaryTree<T> newValue) { return new Node<T>(newValue, Right); }
					public Node<T> WithRight(BinaryTree<T> newValue) { return new Node<T>(Left, newValue); }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public BinaryTree<T> Item1 { get { return Left; } }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public BinaryTree<T> Item2 { get { return Right; } }
				}
				[N] static partial class Node
				{
					public static Node<T> New<T>(BinaryTree<T> Left, BinaryTree<T> Right)
						{ return new Node<T>(Left, Right); }
				}
				[L] class Leaf<T> : BinaryTree<T>
				{
					public Leaf(T Value) { this.Value = Value; }
					
					public T Value { get; private set; }
					public Leaf<T> WithValue(T newValue) { return new Leaf<T>(newValue); }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public T Item1 { get { return Value; } }
					stuff;
				}
				[L] static partial class Leaf
				{
					public static Leaf<T> New<T>(T Value) 
						{ return new Leaf<T>(Value); }
				}");
		}

		[Test]
		public void TestAlgebraicDataTypesAdvanced()
		{
			// Try including shared data in the base type.
			TestEcs(@"
				public abstract alt class LNode {
					public alt this(LNode[] Attributes);
					public alt LId(Symbol Name);
					public alt LLiteral(object Value);
					public alt LCall(LNode Target, params LNode[] Args);
				}", @"
					public abstract class LNode
					{
						public LNode(LNode[] Attributes) { this.Attributes = Attributes; }

						public LNode[] Attributes { get; private set; }
						public abstract LNode WithAttributes(LNode[] newValue);
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode[] Item1 { get { return Attributes; } }
					}
					public class LId : LNode
					{
						public LId(LNode[] Attributes, Symbol Name) : base(Attributes) { this.Name = Name; }
						
						public Symbol Name { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LId(newValue, Name); }
						// cov_With* functions (workaround for C#'s lack of covariant 
						// return types) are not currently implemented (and comments in test strings are ignored)
						//{ return cov_WithAttributes(newValue); }
						//public virtual LId cov_WithAttributes(LNode[] newValue) { return new LId(newValue, Name); }
						public LId WithName(Symbol newValue) { return new LId(Attributes, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public Symbol Item2 { get { return Name; } }
					}
					public class LLiteral : LNode
					{
						public LLiteral(LNode[] Attributes, object Value) : base(Attributes) { this.Value = Value; }

						public object Value { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LLiteral(newValue, Value); }
						//{ return cov_WithAttributes(newValue); }
						//public virtual LId cov_WithAttributes(LNode[] newValue) { return new LLiteral(newValue, Value); }
						public LLiteral WithValue(object newValue) { return new LLiteral(Attributes, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object Item2 { get { return Value; } }
					}
					public class LCall : LNode
					{
						public LCall(LNode[] Attributes, LNode Target, params LNode [] Args) : base(Attributes) { this.Target = Target; this.Args = Args; }

						public LNode Target { get; private set; }
						public LNode[] Args { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LCall(newValue, Target, Args); }
						//{ return cov_WithAttributes(newValue); }
						//public virtual LCall cov_WithAttributes(LNode[] newValue) { return new LCall(newValue, Target, Args); }
						public LCall WithTarget(LNode newValue) { return new LCall(Attributes, newValue, Args); }
						public LCall WithArgs(params LNode[] newValue) { return new LCall(Attributes, Target, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode Item2 { get { return Target; } }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode[] Item3 { get { return Args; } }
					}");
			// Try adding a generic parameter to the subtypes
			TestEcs(@"
				public alt class MyTuple<T1> {
					public alt this(T1 Item1);
					public alt MyTuple<T1,T2>(T2 Item2) {
						public alt MyTuple<T1,T2,T3>(T3 Item3) { }
					}
				}", @"
					public class MyTuple<T1> {
						public MyTuple(T1 Item1) { this.Item1 = Item1; }
						public T1 Item1 { get; private set; }
						public virtual MyTuple<T1> WithItem1(T1 newValue) { return new MyTuple<T1>(newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1> New<T1>(T1 Item1)
							{ return new MyTuple<T1>(Item1); }
					}
					public class MyTuple<T1, T2> : MyTuple<T1> {
						public MyTuple(T1 Item1, T2 Item2) : base(Item1) { this.Item2 = Item2; }
						public T2 Item2 { get; private set; }
						public override MyTuple<T1> WithItem1(T1 newValue) //{ return cov_WithItem1(newValue); }
						//public override MyTuple<T1, T2> cov_WithItem1(T1 newValue) 
						{ return new MyTuple<T1, T2>(newValue, Item2); }
						public virtual  MyTuple<T1, T2> WithItem2(T2 newValue) { return new MyTuple<T1, T2>(Item1, newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1, T2> New<T1, T2>(T1 Item1, T2 Item2)
							{ return new MyTuple<T1, T2>(Item1, Item2); }
					}
					public class MyTuple<T1, T2, T3> : MyTuple<T1, T2> {
						public MyTuple(T1 Item1, T2 Item2, T3 Item3) : base(Item1, Item2) { this.Item3 = Item3; }
						public T3 Item3 { get; private set; }
						public override MyTuple<T1> WithItem1(T1 newValue) //{ return cov_cov_WithItem1(newValue); }
						//public virtual  MyTuple<T1, T2, T3> cov_cov_WithItem1(T1 newValue) 
						{ return new MyTuple<T1, T2, T3>(newValue, Item2, Item3); }
						public override MyTuple<T1, T2> WithItem2(T2 newValue) //{ return cov_WithItem2(newValue); }
						//public virtual  MyTuple<T1, T2, T3> cov_WithItem2(T2 newValue) 
						{ return new MyTuple<T1, T2, T3>(Item1, newValue, Item3); }
						public MyTuple<T1, T2, T3> WithItem3(T3 newValue) { return new MyTuple<T1, T2, T3>(Item1, Item2, newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1, T2, T3> New<T1, T2, T3>(T1 Item1, T2 Item2, T3 Item3)
							{ return new MyTuple<T1, T2, T3>(Item1, Item2, Item3); }
					}
				");
			// A variation in which T has a `where` clause
			TestEcs(@"
				public alt class Base<T> where T: IComparable<T> {
					public alt this(T Toilet) { base_constructor_code; }
					public alt Derived<T,P>(P Paper) where T: IEquatable<T> 
					                                 where P: IPaper
					{
						public alt this() { constructor_code; }
					}
				}", @"
					public class Base<T> where T: IComparable<T> {
						public Base(T Toilet)
						{
							this.Toilet = Toilet;
							base_constructor_code;
						}

						public T Toilet { get; private set; }
						public virtual Base<T> WithToilet(T newValue) { return new Base<T>(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public T Item1 { get { return Toilet; } }
					}
					public static partial class Base
					{
						public static Base<T> New<T>(T Toilet)
							where T: IComparable<T> 
							{ return new Base<T>(Toilet); }
					}
					public class Derived<T,P> : Base<T> where T: IEquatable<T>, IComparable<T> where P: IPaper
					{
						public Derived(T Toilet, P Paper) : base(Toilet)
						{
							this.Paper = Paper;
							constructor_code;
						}
						public P Paper { get; private set; }
						public override Base<T> WithToilet(T newValue) { return new Derived<T,P>(newValue, Paper); }
						public Derived<T,P> WithPaper(P newValue) { return new Derived<T,P>(Toilet, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
						public P Item2 { get { return Paper; } }
					}
					public static partial class Derived
					{
						public static Derived<T,P> New<T,P>(T Toilet, P Paper) 
							where T: IEquatable<T>, IComparable<T> where P: IPaper 
							{ return new Derived<T,P>(Toilet, Paper); }
					}");
		}
	}
}
