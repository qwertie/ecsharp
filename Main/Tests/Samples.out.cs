// Generated from Samples.ecs by LeMP custom tool. LeMP version: 1.5.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.MiniTest;
namespace Samples
{
	using ADT;
	[TestFixture] class Samples
	{
		public static void Run()
		{
			RunTests.Run(new Samples());
		}
		[Test] public void ContainsTest()
		{
			var tree = Node.New(5, Node.New(1, null, Leaf.New(3)), Node.New(9, Leaf.New(7), null));
			for (int i = 0; i <= 12; i++)
				if (tree.Contains(i))
					Console.Write(" {0}", i);
			Console.WriteLine(" were found");
		}
		static void FavoriteNumberGame()
		{
			Console.Write("What's your favorite number? ");
			do {
				var tmp_0 = int.Parse(Console.ReadLine());
				if (7.Equals(tmp_0)) {
					Console.WriteLine("You lucky bastard!");
					break;
				}
				if (777.Equals(tmp_0)) {
					Console.WriteLine("You lucky bastard!");
					break;
				}
				if (5.Equals(tmp_0)) {
					Console.WriteLine("I have that many fingers too!");
					break;
				}
				if (10.Equals(tmp_0)) {
					Console.WriteLine("I have that many fingers too!");
					break;
				}
				if (0.Equals(tmp_0)) {
					Console.WriteLine("What? Nobody picks that!");
					break;
				}
				if (1.Equals(tmp_0)) {
					Console.WriteLine("What? Nobody picks that!");
					break;
				}
				if (2.Equals(tmp_0)) {
					Console.WriteLine("Yeah, I guess you deal with those a lot.");
					break;
				}
				if (3.Equals(tmp_0)) {
					Console.WriteLine("Yeah, I guess you deal with those a lot.");
					break;
				}
				if (12.Equals(tmp_0)) {
					Console.WriteLine("I prefer a baker's dozen.");
					break;
				}
				if (666.Equals(tmp_0)) {
					Console.WriteLine("Isn't that bad luck though?");
					break;
				}
				if (13.Equals(tmp_0)) {
					Console.WriteLine("Isn't that bad luck though?");
					break;
				}
				if (tmp_0 >= 1 && tmp_0 < 10) {
					Console.WriteLine("Kind of boring, don't you think?");
					break;
				}
				if (11.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (13.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (17.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (19.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (23.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (29.Equals(tmp_0)) {
					Console.WriteLine("A prime choice.");
					break;
				}
				if (tmp_0 >= 10 && tmp_0 <= 99) {
					Console.WriteLine("Well... it's got two digits, I'll give you that much.");
					break;
				}
				if (tmp_0 <= -1) {
					Console.WriteLine("Oh, don't be so negative.");
					break;
				}
				{
					Console.WriteLine("What are you, high? Like that number?");
				}
			} while (false);
		}
		static void NumberGuessingGame()
		{
			int num = new Random().Next(1, 101);
			do {
				Console.Write("Guess a number between 1 and 100: ");
				try {
					int guess = int.Parse(Console.ReadLine());
					do {
					} while (false);
				} catch (Exception FormatException) {
					Console.WriteLine("No, I want an integer.");
				}
			} while (true);
		}
	}
}
namespace ADT
{
	public class BinaryTree< T> where T: IComparable<T>
	{
		public BinaryTree(T Value)
		{
			this.Value = Value;
		}
		public T Value
		{
			get;
			set;
		}
		public virtual BinaryTree<T> WithValue(T newValue)
		{
			return new BinaryTree<T>(newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public T Item1
		{
			get {
				return Value;
			}
		}
		public virtual bool Contains(T item)
		{
			return Compare(Value, item) == 0;
		}
		internal static int Compare(T a, T b)
		{
			if (a != null)
				return a.CompareTo(b);
			else if (b != null)
				return -a.CompareTo(a);
			else
				return 0;
		}
	}
	public static partial class BinaryTree
	{
		public static BinaryTree<T> New< T>(T Value) where T: IComparable<T>
		{
			return new BinaryTree<T>(Value);
		}
	}
	class Node< T> : BinaryTree<T> where T: IComparable<T>
	{
		public Node(T Value, BinaryTree<T> Left, BinaryTree<T> Right) : base(Value)
		{
			this.Left = Left;
			this.Right = Right;
			if (Left == null && Right == null)
				throw new ArgumentNullException("Both children");
		}
		public BinaryTree<T> Left
		{
			get;
			set;
		}
		public BinaryTree<T> Right
		{
			get;
			set;
		}
		public override BinaryTree<T> WithValue(T newValue)
		{
			return new Node<T>(newValue, Left, Right);
		}
		public Node<T> WithLeft(BinaryTree<T> newValue)
		{
			return new Node<T>(Value, newValue, Right);
		}
		public Node<T> WithRight(BinaryTree<T> newValue)
		{
			return new Node<T>(Value, Left, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public BinaryTree<T> Item2
		{
			get {
				return Left;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public BinaryTree<T> Item3
		{
			get {
				return Right;
			}
		}
		public override bool Contains(T item)
		{
			int cmp = Compare(item, Value);
			if (cmp < 0)
				return Left != null && Left.Contains(item);
			else if (cmp > 0)
				return Right != null && Right.Contains(item);
			else
				return true;
		}
	}
	static partial class Node
	{
		public static Node<T> New< T>(T Value, BinaryTree<T> Left, BinaryTree<T> Right) where T: IComparable<T>
		{
			return new Node<T>(Value, Left, Right);
		}
	}
	public static class Leaf
	{
		public static BinaryTree<T> New< T>(T item) where T: IComparable<T>
		{
			return new BinaryTree<T>(item);
		}
	}
	public abstract class Rectangle
	{
		public Rectangle(int X, int Y, int Width, int Height)
		{
			this.X = X;
			this.Y = Y;
			this.Width = Width;
			this.Height = Height;
		}
		public int X
		{
			get;
			set;
		}
		public int Y
		{
			get;
			set;
		}
		public int Width
		{
			get;
			set;
		}
		public int Height
		{
			get;
			set;
		}
		public abstract Rectangle WithX(int newValue);
		public abstract Rectangle WithY(int newValue);
		public abstract Rectangle WithWidth(int newValue);
		public abstract Rectangle WithHeight(int newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public int Item1
		{
			get {
				return X;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public int Item2
		{
			get {
				return Y;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public int Item3
		{
			get {
				return Width;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public int Item4
		{
			get {
				return Height;
			}
		}
	}
	public abstract class Widget
	{
		public Widget(Rectangle Location)
		{
			this.Location = Location;
			if (Location == null)
				throw new ArgumentNullException("Location");
		}
		public Rectangle Location
		{
			get;
			set;
		}
		public abstract Widget WithLocation(Rectangle newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Rectangle Item1
		{
			get {
				return Location;
			}
		}
	}
	class Button : Widget
	{
		public Button(Rectangle Location, string Text) : base(Location)
		{
			this.Text = Text;
		}
		public string Text
		{
			get;
			set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new Button(newValue, Text);
		}
		public Button WithText(string newValue)
		{
			return new Button(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public string Item2
		{
			get {
				return Text;
			}
		}
	}
	class TextBox : Widget
	{
		public TextBox(Rectangle Location, string Text) : base(Location)
		{
			this.Text = Text;
		}
		public string Text
		{
			get;
			set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new TextBox(newValue, Text);
		}
		public TextBox WithText(string newValue)
		{
			return new TextBox(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public string Item2
		{
			get {
				return Text;
			}
		}
	}
	abstract class StringListWidget : Widget
	{
		public StringListWidget(Rectangle Location, string[] subItems) : base(Location)
		{
			this.subItems = subItems;
		}
		public string[] subItems
		{
			get;
			set;
		}
		public abstract override Widget WithLocation(Rectangle newValue);
		public abstract StringListWidget WithsubItems(string[] newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public string[] Item2
		{
			get {
				return subItems;
			}
		}
	}
	class ComboBox : StringListWidget
	{
		public ComboBox(Rectangle Location, string[] subItems) : base(Location, subItems)
		{
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new ComboBox(newValue, subItems);
		}
		public override StringListWidget WithsubItems(string[] newValue)
		{
			return new ComboBox(Location, newValue);
		}
	}
	class ListBox : StringListWidget
	{
		public ListBox(Rectangle Location, string[] subItems) : base(Location, subItems)
		{
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new ListBox(newValue, subItems);
		}
		public override StringListWidget WithsubItems(string[] newValue)
		{
			return new ListBox(Location, newValue);
		}
	}
	public abstract class Container : Widget
	{
		public Container(Rectangle Location) : base(Location)
		{
		}
		public abstract override Widget WithLocation(Rectangle newValue);
	}
	class TabControl : Container
	{
		public TabControl(Rectangle Location, TabPage[] Children) : base(Location)
		{
			this.Children = Children;
		}
		public TabPage[] Children
		{
			get;
			set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new TabControl(newValue, Children);
		}
		public TabControl WithChildren(TabPage[] newValue)
		{
			return new TabControl(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public TabPage[] Item2
		{
			get {
				return Children;
			}
		}
	}
	class Panel : Container
	{
		public Panel(Rectangle Location, Widget[] Children) : base(Location)
		{
			this.Children = Children;
		}
		public Widget[] Children
		{
			get;
			set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new Panel(newValue, Children);
		}
		public virtual Panel WithChildren(Widget[] newValue)
		{
			return new Panel(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Widget[] Item2
		{
			get {
				return Children;
			}
		}
	}
	class TabPage : Panel
	{
		public TabPage(Rectangle Location, Widget[] Children, string Title) : base(Location, Children)
		{
			this.Title = Title;
		}
		public string Title
		{
			get;
			set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new TabPage(newValue, Children, Title);
		}
		public override Panel WithChildren(Widget[] newValue)
		{
			return new TabPage(Location, newValue, Title);
		}
		public TabPage WithTitle(string newValue)
		{
			return new TabPage(Location, Children, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public string Item3
		{
			get {
				return Title;
			}
		}
	}
}
