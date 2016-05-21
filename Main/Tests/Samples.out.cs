// Generated from Samples.ecs by LeMP custom tool. LeMP version: 1.7.4.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Les;
using Loyc.Ecs;
using TT = Loyc.Syntax.Les.TokenType;
namespace Samples
{
	using ADT;
	[TestFixture]
	partial class Samples : Assert
	{
		public static int Run()
		{
			return RunTests.Run(new Samples());
		}
		[Test]
		public void ContainsTest()
		{
			var tree = Node.New(5, Node.New(1, null, Leaf.New(3)), Node.New(9, Leaf.New(7), null));
			for (int i = 0; i <= 12; i++)
				if (tree.Contains(i))
					Console.Write(" {0}", i);
			Console.WriteLine(" were found");
		}
		[Test]
		public void RangeTest()
		{
			IsTrue(5.IsInRangeExcludeHi(4, 6));
			IsTrue(6.IsInRange(5, 6));
			IsFalse(5.IsInRangeExcludeHi(4, 5));
			IsFalse(4.IsInRangeExcludeHi(4, 3));
			AreEqual(10, Range.ExcludeHi(1, 5).Sum());
			AreEqual(15, Range.Inclusive(1, 5).Sum());
			AreEqual(0, Range.ExcludeHi(1, 1).Sum());
			AreEqual(0, Range.Inclusive(1, 0).Sum());
		}
		public void PrintAllTheNames()
		{
			try {
				PlayPen.PrintAllTheNames("..\\..\\Main\\LeMP.StdMacros");
			} catch {
			}
		}
		[Test]
		public void SExprTest()
		{
			LNode @using = SExprParser.Parse("(#import (. System Collections))");
			Console.WriteLine(EcsLanguageService.Value.Print(@using));
			LNode assign = SExprParser.Parse("(= x (+ x 2))");
			Console.WriteLine(EcsLanguageService.Value.Print(assign));
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
				if (tmp_0.IsInRangeExcludeHi(1, 10)) {
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
				if (tmp_0.IsInRange(10, 99)) {
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
			private set;
		}
		public virtual BinaryTree<T> WithValue(T newValue)
		{
			return new BinaryTree<T>(newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public T Item1
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
			private set;
		}
		public BinaryTree<T> Right
		{
			get;
			private set;
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
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public BinaryTree<T> Item2
		{
			get {
				return Left;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public BinaryTree<T> Item3
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
			private set;
		}
		public int Y
		{
			get;
			private set;
		}
		public int Width
		{
			get;
			private set;
		}
		public int Height
		{
			get;
			private set;
		}
		public abstract Rectangle WithX(int newValue);
		public abstract Rectangle WithY(int newValue);
		public abstract Rectangle WithWidth(int newValue);
		public abstract Rectangle WithHeight(int newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public int Item1
		{
			get {
				return X;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public int Item2
		{
			get {
				return Y;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public int Item3
		{
			get {
				return Width;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public int Item4
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
			private set;
		}
		public abstract Widget WithLocation(Rectangle newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public Rectangle Item1
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
			private set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new Button(newValue, Text);
		}
		public Button WithText(string newValue)
		{
			return new Button(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public string Item2
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
			private set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new TextBox(newValue, Text);
		}
		public TextBox WithText(string newValue)
		{
			return new TextBox(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public string Item2
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
			private set;
		}
		public abstract override Widget WithLocation(Rectangle newValue);
		public abstract StringListWidget WithsubItems(string[] newValue);
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public string[] Item2
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
			private set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new TabControl(newValue, Children);
		}
		public TabControl WithChildren(TabPage[] newValue)
		{
			return new TabControl(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public TabPage[] Item2
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
			private set;
		}
		public override Widget WithLocation(Rectangle newValue)
		{
			return new Panel(newValue, Children);
		}
		public virtual Panel WithChildren(Widget[] newValue)
		{
			return new Panel(Location, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public Widget[] Item2
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
			private set;
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
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public string Item3
		{
			get {
				return Title;
			}
		}
	}
}
struct EmailAddress
{
	public UString UserName;
	public UString Domain;
	public EmailAddress(UString userName, UString domain)
	{
		UserName = userName;
		Domain = domain;
	}
	public override string ToString()
	{
		return (UserName + "@" + Domain).ToString();
	}
	[ThreadStatic]
	static LexerSource<UString> src;
	static readonly HashSet<int> UsernameChars_set0 = LexerSource.NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', '-', '/', '9', '=', '=', '?', '?', 'A', 'Z', '^', '~');
	static void UsernameChars(LexerSource<UString> src)
	{
		int la0;
		src.Match(UsernameChars_set0);
		// Line 145: ([!#-'*+\-/-9=?A-Z^-~])*
		for (;;) {
			la0 = src.LA0;
			if (UsernameChars_set0.Contains(la0))
				src.Skip();
			else
				break;
		}
	}
	static readonly HashSet<int> DomainCharSeq_set0 = LexerSource.NewSetOfRanges('0', '9', 'A', 'Z', 'a', 'z');
	static readonly HashSet<int> DomainCharSeq_set1 = LexerSource.NewSetOfRanges('-', '-', '0', '9', 'A', 'Z', 'a', 'z');
	static void DomainCharSeq(LexerSource<UString> src)
	{
		int la0;
		src.Match(DomainCharSeq_set0);
		// Line 150: (([\-])? [0-9A-Za-z])*
		for (;;) {
			la0 = src.LA0;
			if (DomainCharSeq_set1.Contains(la0)) {
				// Line 150: ([\-])?
				la0 = src.LA0;
				if (la0 == '-')
					src.Skip();
				src.Match(DomainCharSeq_set0);
			} else
				break;
		}
	}
	public static EmailAddress Parse(UString email)
	{
		int la0;
		#line 158 "Samples.ecs"
		if (src == null)
			src = new LexerSource<UString>(email, "", 0, false);
		else
			src.Reset(email, "", 0, false);
		#line default
		UsernameChars(src);
		// Line 163: ([.] UsernameChars)*
		for (;;) {
			la0 = src.LA0;
			if (la0 == '.') {
				src.Skip();
				UsernameChars(src);
			} else
				break;
		}
		int at = src.InputPosition;
		UString userName = email.Substring(0, at);
		src.Match('@');
		DomainCharSeq(src);
		// Line 167: ([.] DomainCharSeq)*
		for (;;) {
			la0 = src.LA0;
			if (la0 == '.') {
				src.Skip();
				DomainCharSeq(src);
			} else
				break;
		}
		src.Match(-1);
		UString domain = email.Substring(at + 1);
		return new EmailAddress(userName, domain);
	}
}
public partial class SExprParser : BaseParserForList<Token,int>
{
	public static LNode Parse(UString sexpr, string filename = "", IMessageSink msgs = null)
	{
		var lexer = LesLanguageService.Value.Tokenize(sexpr, filename, msgs);
		var withoutComments = new WhitespaceFilter(lexer).Buffered();
		var parser = new SExprParser(withoutComments, lexer.SourceFile, msgs);
		return parser.Atom();
	}
	protected LNodeFactory F;
	public SExprParser(IList<Token> tokens, ISourceFile file, IMessageSink messageSink, int startIndex = 0) : base(tokens, default(Token), file, startIndex)
	{
		ErrorSink = messageSink;
	}
	protected override void Reset(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
	{
		base.Reset(list, eofToken, file, startIndex);
		F = new LNodeFactory(file);
	}
	protected override string ToString(int tokenType)
	{
		return ((TT) tokenType).ToString();
	}
	LNode Atom()
	{
		LNode result = default(LNode);
		Token t = default(Token);
		// Line 209: ( List | (TT.Assignment|TT.BQString|TT.Colon|TT.Dot|TT.Id|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) | TT.Literal )
		switch ((TT) LA0) {
		case TT.LParen:
		case TT.SpaceLParen:
			result = List();
			break;
		case TT.Assignment:
		case TT.BQString:
		case TT.Colon:
		case TT.Dot:
		case TT.Id:
		case TT.NormalOp:
		case TT.Not:
		case TT.PrefixOp:
		case TT.PreOrSufOp:
			{
				t = MatchAny();
				#line 211 "Samples.ecs"
				result = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
				#line default
			}
			break;
		default:
			{
				t = Match((int) TT.Literal);
				#line 212 "Samples.ecs"
				result = F.Literal(t.Value, t.StartIndex, t.EndIndex);
				#line default
			}
			break;
		}
		return result;
	}
	LNode List()
	{
		TT la1;
		Token lit_lpar = default(Token);
		Token lit_rpar = default(Token);
		LNode target = default(LNode);
		// Line 215: ((TT.LParen|TT.SpaceLParen) TT.RParen | (TT.LParen|TT.SpaceLParen) Atom (Atom)* TT.RParen)
		la1 = (TT) LA(1);
		if (la1 == TT.RParen) {
			lit_lpar = Match((int) TT.LParen, (int) TT.SpaceLParen);
			lit_rpar = MatchAny();
			#line 216 "Samples.ecs"
			return F.List(VList<LNode>.Empty, lit_lpar.StartIndex, lit_rpar.EndIndex);
			#line default
		} else {
			#line 217 "Samples.ecs"
			var parts = VList<LNode>.Empty;
			#line default
			lit_lpar = Match((int) TT.LParen, (int) TT.SpaceLParen);
			target = Atom();
			// Line 218: (Atom)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Assignment:
				case TT.BQString:
				case TT.Colon:
				case TT.Dot:
				case TT.Id:
				case TT.Literal:
				case TT.LParen:
				case TT.NormalOp:
				case TT.Not:
				case TT.PrefixOp:
				case TT.PreOrSufOp:
				case TT.SpaceLParen:
					parts.Add(Atom());
					break;
				default:
					goto stop;
				}
			}
		stop:;
			lit_rpar = Match((int) TT.RParen);
			#line 219 "Samples.ecs"
			return F.Call(target, parts, lit_lpar.StartIndex, lit_rpar.EndIndex);
			#line default
		}
	}
}
