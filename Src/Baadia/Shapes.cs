using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Math;
using Util.WinForms;
using Coord = System.Single;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using Loyc.Geometry;
using Loyc;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Util.UI;

namespace BoxDiagrams
{
	/// <summary>
	/// Represents a "complex" shape in Baadia (e.g. line, arrow, or textbox),
	/// </summary><remarks>
	/// A <see cref="Shape"/> may use multiple <see cref="LLShape"/>s (e.g. 
	/// TextBox = LLTextShape + LLRectangle) and/or contain Baadia-specific 
	/// attributes (e.g. anchors, which attach shapes to other shapes).
	/// <para/>
	/// Baadia treats <see cref="DrawStyle"/>s as immutable by not modifying them.
	/// When the user modifies a draw style, the existing style is cloned and the
	/// new style is assigned to all shapes that use it. (Therefore, the DrawStyle
	/// does not need to be cloned when the shape is cloned.) The attributes of
	/// <see cref="DrawStyle"/> are not physically marked immutable because <see 
	/// cref="DrawStyle"/> is intended to be general-purpose (shared across 
	/// multiple applications) and I do not want it to make it immutable in ALL 
	/// applications.
	/// </remarks>
	public abstract class Shape : ICloneable<Shape>, IDisposable
	{
		public static readonly DrawStyle DefaultStyle = new DrawStyle { LineWidth = 2 };

		public DrawStyle Style;
		
		public abstract void AddLLShapes(MSet<LLShape> list);
		public abstract void AddAdorners(MSet<LLShape> list, SelType selMode, VectorT hitTestRadius);

		public virtual Shape Clone()
		{
			return (Shape)MemberwiseClone();
		}
		public abstract BoundingBox<float> BBox { get; }

		/// <summary>Base class for results returned from <see cref="Shape.HitTest()"/>.</summary>
		public class HitTestResult
		{
			public HitTestResult(Shape shape, Cursor cursor) 
				{ Shape = shape; MouseCursor = cursor; Debug.Assert(cursor != null && shape != null);}
			public Shape Shape;
			public Cursor MouseCursor;
			public virtual bool AllowsDrag
			{
				get { return MouseCursor != null && MouseCursor != Cursors.Arrow; }
			}
		}

		/// <summary>Hit-tests the shape and returns information that includes the 
		/// mouse cursor to use for it.</summary>
		/// <returns>Null indicates a failed hit test. Inside the result object, the 
		/// cursor <see cref="Cursors.Arrow"/> means that the shape is selectable, 
		/// <see cref="Cursors.SizeAll"/> indicates that the user will be moving something, 
		/// and a sizing cursor such as <see cref="SizeNS"/> indicates that something will 
		/// be resized or that one line in a collection of lines will be moved along the 
		/// indicated direction (e.g. up-down for SizeNS).</returns>
		public abstract HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel);

		protected static DrawStyle SelAdornerStyle = new DrawStyle(Color.Black, 1, Color.FromArgb(128, SystemColors.Highlight));

		public abstract int ZOrder { get; }

		protected static bool PointsAreNear(PointT mouse, PointT point, VectorT hitTestRadius)
		{
			var dif = mouse.Sub(point);
			return Math.Abs(dif.X) <= hitTestRadius.X && Math.Abs(dif.Y) <= hitTestRadius.Y;
		}

		public virtual void OnKeyDown(KeyEventArgs e, UndoStack undoStack) { }
		public virtual void OnKeyUp(KeyEventArgs e, UndoStack undoStack) { }
		public virtual void OnKeyPress(KeyPressEventArgs e, UndoStack undoStack) { }

		public virtual DoOrUndo GetDragMoveAction(HitTestResult htr, VectorT amount) { return null; }
		public virtual DoOrUndo AttachedShapeChanged(AnchorShape other) { return null; }

		public virtual void Dispose() { }
	}

	/// <summary>A shape that has Anchors. An Anchor is a point that an arrow or 
	/// line can be attached to.</summary>
	public abstract class AnchorShape : Shape
	{
		public abstract IEnumerable<Anchor> DefaultAnchors { get; }
		public abstract Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1);
		protected Anchor Anchor(Func<PointT> func, int exitAngles = 0xFF) { return new Anchor(this, func, exitAngles); }

		public MSet<Shape> AttachedShapes = new MSet<Shape>();
		//public event Action<Shape> GeometryChanged;
		//protected void FireGeometryChanged() { if (GeometryChanged != null) GeometryChanged(this); }
	}

	public class Marker : AnchorShape
	{
		static int NextZOrder = 0x20000000;

		public Marker(DrawStyle style, PointT point, float radius, MarkerPolygon type)
		{
			LL = new LLMarker(style, point, radius, type) { ZOrder = NextZOrder++ };
		}
		public override IEnumerable<Anchor> DefaultAnchors 
		{
			get { return new Repeated<Anchor>(Anchor(() => this.Point), 1); }
		}
		public override Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1)
		{
			return Anchor(() => this.Point);
		}
		public LLMarker LL;
		MarkerPolygon Type { get { return LL.Type; } set { LL.Type = Type; } }
		float Radius { get { return LL.Radius; } set { LL.Radius = value; } }
		PointT Point { get { return LL.Point; } set { LL.Point = value; } }

		public override void AddLLShapes(MSet<LLShape> list)
		{
			LL.Style = Style;
			list.Add(LL);
		}
		public override BoundingBox<Coord> BBox
		{
			get { var bb = new BoundingBox<Coord>(Point, Point); bb.Inflate(Radius); return bb; }
		}
		public override HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			var dif = pos.Sub(Point).Abs();
			if (dif.X <= hitTestRadius.X && dif.Y <= hitTestRadius.Y)
				return new HitTestResult(this, sel != SelType.No ? Cursors.SizeAll : Cursors.Arrow);
			return null;
		}
		public override void AddAdorners(MSet<LLShape> list, SelType selMode, VectorT hitTestRadius)
		{
			var copy = (LLMarker)LL.Clone();
			copy.Type = MarkerPolygon.Square;
			copy.Radius = hitTestRadius.X;
			copy.Style = SelAdornerStyle;
			list.Add(copy);
		}

		public override int ZOrder { get { return LL.ZOrder; } }
	}

	public class TextBox : AnchorShape
	{
		public TextBox(BoundingBox<float> bbox)
		{
			TextJustify = LLTextShape.JustifyMiddleCenter;
			_bbox = bbox;
		}
		public BoxType Type;
		public string Text;
		public StringFormat TextJustify;
		BoundingBox<float> _bbox;
		public override BoundingBox<float> BBox { get { return _bbox; } }
		public void SetBBox(BoundingBox<float> bb) { _bbox = bb; }
		public PointT Center { get { return BBox.Center(); } }
		public VectorT Size { get { return BBox.MaxPoint.Sub(BBox.MinPoint); } }
		public float Top { get { return BBox.Y1; } }
		public float Left { get { return BBox.X1; } }
		public float Right { get { return BBox.X2; } }
		public float Bottom { get { return BBox.Y2; } }

		/// <summary>A panel is a box that has at least one other box fully 
		/// contained within it. When a panel is dragged, the boxes (and 
		/// parts of lines) on top are moved at the same time.</summary>
		/// <remarks>
		/// A panel cannot be dragged until after it is selected with a single
		/// click; this permits one to draw boxes and lines on top of the panel. 
		/// A second click will select the panel's text if the text was clicked, 
		/// otherwise it unselects the panel. If a panel's on-screen area is 
		/// currently larger than the viewport, it cannot be selected at all.
		/// <para/>
		/// If a panel does not have text, the user can't add text to it because 
		/// when you click it and type, that will create a new text object on 
		/// top of the panel, rather than editing the text of the panel itself. 
		/// The UI goal is to make a large panel behave almost like a region of 
		/// blank space (apart from the ability to select the panel).
		/// </remarks>
		public bool IsPanel;

		static PointT P(float x, float y) { return new PointT(x,y); }

		public override IEnumerable<Anchor> DefaultAnchors
		{
			get {
				return new Anchor[] {
					Anchor(()=>P(BBox.Center().X,BBox.Y1)),
					Anchor(()=>P(BBox.X2, BBox.Center().Y)),
					Anchor(()=>P(BBox.Center().X, BBox.Y2)),
					Anchor(()=>P(BBox.X1, BBox.Center().Y)),
				};
			}
		}
		public override Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1)
		{
			VectorT vec = p - Center, vecAbs = vec.Abs();
			bool vert = vecAbs.Y / Size.Y > vecAbs.X / Size.X;
			Coord frac = MathEx.InRange((p.Y - Top) / (Bottom - Top), 0, 1);
			Anchor a;
			if (vert) {
				frac = MathEx.InRange((p.X - Left) / (Right - Left), 0, 1);
				if (vec.Y > 0) // bottom
					a = Anchor(() => new PointT(Left + frac * (Right - Left), Bottom), 7 << 5);
				else // top
					a = Anchor(() => new PointT(Left + frac * (Right - Left), Top), 7 << 1);
			} else {
				if (vec.X > 0) // right
					a = Anchor(() => new PointT(Right, Top + frac * (Bottom - Top)), 0x83);
				else // left
					a = Anchor(() => new PointT(Left, Top + frac * (Bottom - Top)), 7 << 3);
			}
			return a;
		}

		public override void AddLLShapes(MSet<LLShape> list)
		{
			if (Type != BoxType.Borderless) {
				float area = BBox.Width * BBox.Height;
				if (Type == BoxType.Ellipse)
					list.Add(new LLEllipse(Style, BBox) { ZOrder = 0x10000000 - ((int)(area * (Math.PI/4)) >> 3) } );
				else
					list.Add(new LLRectangle(Style, BBox) { ZOrder = 0x10000000 - ((int)area >> 3) } );
			}
			if (Text != null)
				list.Add(new LLTextShape(Style, Text, TextJustify, BBox.MinPoint, BBox.MaxPoint.Sub(BBox.MinPoint)));
		}
		
		public override void AddAdorners(MSet<LLShape> list, SelType selMode, VectorT hitTestRadius)
		{
			PointT tl = BBox.MinPoint, tr = new PointT(Right, Top);
			PointT br = BBox.MaxPoint, bl = new PointT(Left, Bottom);
			if (selMode == SelType.Yes)
			{
				AddCornerAdorner(list, tl, hitTestRadius.Neg());
				AddCornerAdorner(list, br, hitTestRadius);
			}
			if (selMode != SelType.No)
			{
				hitTestRadius = hitTestRadius.Rot90();
				AddCornerAdorner(list, bl, hitTestRadius);
				AddCornerAdorner(list, tr, hitTestRadius.Neg());
			}
		}

		protected static DrawStyle SelAdornerLineStyle = new DrawStyle(SelAdornerStyle.LineColor, SelAdornerStyle.LineWidth, Color.Transparent) { LineStyle = SelAdornerStyle.LineStyle };
		protected static DrawStyle SelAdornerFillStyle = new DrawStyle(Color.Transparent, 0, SelAdornerStyle.FillColor);

		private void AddCornerAdorner(MSet<LLShape> list, PointT point, VectorT vector)
		{
			VectorT up = new VectorT(0, -vector.Y), down = new VectorT(0, vector.Y);
			VectorT left = new VectorT(-vector.X, 0), right = new VectorT(vector.X, 0);
			var points = new[] { 
				point, point.Add(up), point.Add(up).Add(right), 
				point.Add(vector), point.Add(left).Add(down), point.Add(left)
			};
			list.Add(new LLPolygon(SelAdornerFillStyle, points));
			list.Add(new LLPolyline(SelAdornerLineStyle, points.Slice(1).AsList()));
		}

		public override Shape Clone()
		{
			return (Shape)MemberwiseClone();
		}

		[Flags] enum RF { Left = 1, Top = 2, Right = 4, Bottom = 8 }
		new class HitTestResult : Shape.HitTestResult
		{
			public HitTestResult(Shape shape, Cursor cursor, RF resizeFlags) : base(shape, cursor) { ResizeFlags = resizeFlags; }
			public RF ResizeFlags;
		}

		public override Shape.HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			if (sel != SelType.No) {
				var bbox2 = BBox.Inflated(hitTestRadius.X, hitTestRadius.Y);
				PointT tl = BBox.MinPoint, tr = new PointT(Right, Top);
				PointT br = BBox.MaxPoint, bl = new PointT(Left, Bottom);
				if (PointsAreNear(pos, tr, hitTestRadius))
					return new HitTestResult(this, Cursors.SizeNESW, RF.Top | RF.Right);
				if (PointsAreNear(pos, bl, hitTestRadius))
					return new HitTestResult(this, Cursors.SizeNESW, RF.Bottom | RF.Right);
				if (sel == SelType.Yes) {
					if (PointsAreNear(pos, tl, hitTestRadius))
						return new HitTestResult(this, Cursors.SizeNWSE, RF.Top | RF.Left);
					if (PointsAreNear(pos, br, hitTestRadius))
						return new HitTestResult(this, Cursors.SizeNWSE, RF.Bottom | RF.Right);
				}
			}
			if (sel != SelType.No || !IsPanel)
			{
				if (sel != SelType.Yes)
					hitTestRadius *= 2;
				var bbox2 = BBox.Deflated(hitTestRadius.X, hitTestRadius.Y);
				if (bbox2.Contains(pos))
					return new HitTestResult(this, Cursors.SizeAll, RF.Top | RF.Bottom | RF.Left | RF.Right);
			}

			return BBox.Contains(pos) ? new HitTestResult(this, Cursors.Arrow, 0) : null;
		}

		public override int ZOrder
		{
			get { var size = Size; return (int)(size.X * size.Y); }
		}

		public override void OnKeyPress(KeyPressEventArgs e, UndoStack undoStack)
		{
			e.Handled = true;
			char ch = e.KeyChar;
			if (ch >= ' ') {
				undoStack.Do(@do => {
					if (@do)
						this.Text += ch;
					else
						this.Text = this.Text.Left(this.Text.Length - 1);
				}, true);
			}
		}
		public override void OnKeyDown(KeyEventArgs e, UndoStack undoStack)
		{
			if (e.Modifiers == 0 && e.KeyCode == Keys.Back && Text.Length > 0)
			{
				char last = Text[Text.Length-1];
				undoStack.Do(@do => {
					if (@do)
						this.Text = this.Text.Left(this.Text.Length - 1);
					else
						this.Text += last;
				}, true);
			}
		}

		public override DoOrUndo GetDragMoveAction(Shape.HitTestResult htr, VectorT amount) 
		{
			var rf = ((HitTestResult)htr).ResizeFlags;
			BoundingBox<float> old = null;
			return @do =>
			{
				if (@do) {
					old = _bbox.Clone();
					Coord x1 = _bbox.X1 + ((rf & RF.Left) != 0 ? amount.X : 0);
					Coord x2 = _bbox.X2 + ((rf & RF.Right) != 0 ? amount.X : 0);
					Coord y1 = _bbox.Y1 + ((rf & RF.Top) != 0 ? amount.Y : 0);
					Coord y2 = _bbox.Y2 + ((rf & RF.Bottom) != 0 ? amount.Y : 0);
					_bbox = new BoundingBox<float>(x1, y1, x2, y2);
					_bbox.Normalize();
				} else
					_bbox = old;
			};
		}
	}

	public class Arrowhead
	{
		public Arrowhead(MarkerPolygon geometry, float width, float shift = 0, float scale = 6) { Geometry = geometry; Width = width; Shift = shift; Scale = scale; }
		public readonly MarkerPolygon Geometry;
		public readonly float Width, Shift;
		public float Scale;

		protected static PointT P(Coord x, Coord y) { return new PointT(x, y); }
		static readonly MarkerPolygon arrow45deg = new MarkerPolygon {
			Points = new[] { P(-1, 1), P(0,0), P(0,0), P(-1,-1) }.AsListSource(),
			Divisions = new[] { 2 }.AsListSource()
		};
		static readonly MarkerPolygon arrow30deg = new MarkerPolygon {
			Points = new[] { P(-1, 0.5f), P(0,0), P(0,0), P(-1,-0.5f) }.AsListSource(),
			Divisions = new[] { 2 }.AsListSource()
		};
		public static readonly Arrowhead Arrow45deg = new Arrowhead(arrow45deg, 0);
		public static readonly Arrowhead Arrow30deg = new Arrowhead(arrow30deg, 0);
		public static readonly Arrowhead Diamond = new Arrowhead(MarkerPolygon.Diamond, 2, -1, 4);
		public static readonly Arrowhead Circle = new Arrowhead(MarkerPolygon.Circle, 2, -1, 3);
	}

	public class LineOrArrow : Shape
	{
		static int NextZOrder = 0x10000000;

		public LineOrArrow(List<PointT> points = null) {
			Points = points ?? new List<PointT>();
			TextTopLeft.Justify = 0.5f;
			TextBottomRight.Justify = 0.5f;
			_detachedZOrder = NextZOrder++;
		}
		public Arrowhead FromArrow, ToArrow;
		public LinearText TextTopLeft, TextBottomRight;
		public Anchor _fromAnchor, _toAnchor;
		public Anchor FromAnchor
		{
			get { return _fromAnchor; }
			set {
				if (_fromAnchor != null)
					_fromAnchor.Shape.AttachedShapes.Remove(this);
				if ((_fromAnchor = value) != null)
					_fromAnchor.Shape.AttachedShapes.Add(this);
			}
		}
		public Anchor ToAnchor
		{
			get { return _toAnchor; }
			set {
				if (_toAnchor != null)
					_toAnchor.Shape.AttachedShapes.Remove(this);
				if ((_toAnchor = value) != null)
					_toAnchor.Shape.AttachedShapes.Add(this);
			}
		}
		public List<PointT> Points; // includes cached anchor point(s)
		public int _detachedZOrder;

		public override DoOrUndo AttachedShapeChanged(AnchorShape other)
		{
			DoOrUndo fromAct = null, toAct = null;
			if (_fromAnchor != null && _fromAnchor.Shape == other)
				fromAct = AttachedAnchorChanged(_fromAnchor, false);
			if (_toAnchor != null && _toAnchor.Shape == other)
				toAct = AttachedAnchorChanged(_toAnchor, true);
			if (fromAct != null && toAct != null) {
				return @do => {
					fromAct(@do);
					toAct(@do);
				};
			}
			return fromAct ?? toAct;
		}

		static int AngleMod256(VectorT v)
		{
			return (byte)(v.Angle() * (128.0 / Math.PI));
		}

		private DoOrUndo AttachedAnchorChanged(Anchor anchor, bool toSide)
		{
			PointT p0 = default(PointT), p1 = default(PointT);
			List<PointT> old = null;
			return @do =>
			{
				IList<PointT> points = Points;
				if (toSide) points = points.ReverseView();
				
				Debug.Assert(points.Count >= 2);
				if (points.Count < 2)
					return;

				_bbox = null;
				if (@do) {
					// save undo info in either (p0, p1) or (old) for complicated cases
					p0 = points[0];
					p1 = points[1];
					old = new List<PointT>(Points);
					
					var newAnchor = anchor.Point;
					LineSegment<float> one = newAnchor.To(newAnchor.Add(points[1].Sub(points[0]))), two;
					if (points.Count > 2)
						two = points[1].To(points[2]);
					else
						two = one.B.To(one.B.Add(one.B.Sub(one.A).Rot90())); // fake it

					points[0] = newAnchor;
					PointT? itsc = one.ComputeIntersection(two, LineType.Infinite), itsc2;
					if (itsc != null && !(points.Count == 2 && (toSide ? _fromAnchor : _toAnchor) != null))
						points[1] = itsc.Value;
					else
						itsc = points[1];

					if (points.Count >= 3 && points[1] == points[2]) {
						int remove = 1;
						if (points.Count > 3) {
							int a0 = AngleMod256(points[1].Sub(points[0])), a1 = AngleMod256(points[3].Sub(points[2]));
							if ((a0 & 127) == (a1 & 127))
								remove = 2;
						}
						points.RemoveRange(1, remove);
					} else if (points.Count >= 4 && (itsc2 = points[0].To(points[1]).ComputeIntersection(points[2].To(points[3]))) != null) {
						points.RemoveRange(1, 2);
						points.Insert(1, itsc2.Value);
					} else
						old = null; // save memory
				} else {
					if (old != null)
						Points = old;
					else {
						points[0] = p0;
						points[1] = p1;
					}
				}
			};
		}
		
		public override void AddLLShapes(MSet<LLShape> list)
		{
			int z = ZOrder;
			if (Points.Count >= 2) {
				list.Add(new LLPolyline(Style, Points) { ZOrder = z });
				
				// hacky temporary solution
				int half = (Points.Count-1)/2;
				var midVec = Points[half+1].Sub(Points[half]);
				if (TextTopLeft.Text != null) {
					list.Add(new LLTextShape(
						Style, TextTopLeft.Text, LLTextShape.JustifyUpperCenter, Points[half], new VectorT(midVec.Length(), 100))
						{ AngleDeg = (float)midVec.AngleDeg() });
				}
			}
		}

		public override Shape Clone()
		{
			var copy = (LineOrArrow)MemberwiseClone();
			// Points are often changed after cloning... yeah, it's hacky.
			_bbox = null; copy._bbox = null; 
			return copy;
		}
		BoundingBox<float> _bbox;
		public override BoundingBox<float> BBox
		{
			get {
				if (_bbox != null)
					Debug.Assert(_bbox.Contains(Points[0]) && _bbox.Contains(Points[Points.Count - 1]));
				return _bbox = _bbox ?? Points.ToBoundingBox();
			}
		}
		
		public override void AddAdorners(MSet<LLShape> list, SelType selMode, VectorT hitTestRadius)
		{
			if (selMode == SelType.Partial)
			{
				AddAdorner(list, Points[0], hitTestRadius);
				AddAdorner(list, Points[Points.Count - 1], hitTestRadius);
			}
			else if (selMode == SelType.Yes)
				for (int i = 0; i < Points.Count; i++)
					AddAdorner(list, Points[i], hitTestRadius);
		}

		private void AddAdorner(MSet<LLShape> list, PointT point, VectorT hitTestRadius)
		{
			list.Add(new LLMarker(SelAdornerStyle, point, hitTestRadius.X, MarkerPolygon.Square));
		}

		public override int ZOrder
		{
			get {
				int z = _detachedZOrder;
				if (FromAnchor != null)
					z = Math.Min(z, FromAnchor.Shape.ZOrder - 1);
				if (ToAnchor != null)
					z = Math.Min(z, ToAnchor.Shape.ZOrder - 1);
				return z;
			}
		}

		static int AngleMod8(VectorT v)
		{
			return (int)Math.Round(v.Angle() * (4 / Math.PI)) & 7;
		}

		new class HitTestResult : Shape.HitTestResult
		{
			public HitTestResult(Shape shape, Cursor cursor, int pointOrSeg) 
				: base(shape, cursor) { PointOrSegment = pointOrSeg; }
			public readonly int PointOrSegment;
		}

		public override Shape.HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			if (!BBox.Inflated(hitTestRadius.X, hitTestRadius.Y).Contains(pos))
				return null;
			
			if (sel == SelType.Partial) {
				if (PointsAreNear(pos, Points[0], hitTestRadius))
					return new HitTestResult(this, Cursors.SizeAll, 0);
				if (PointsAreNear(pos, Points[Points.Count-1], hitTestRadius))
					return new HitTestResult(this, Cursors.SizeAll, Points.Count-1);
			} else if (sel == SelType.Yes) {
				for (int i = 0; i < Points.Count; i++)
				{
					if (PointsAreNear(pos, Points[i], hitTestRadius))
						return new HitTestResult(this, Cursors.SizeAll, i);
				}
			}
			
			PointT projected;
			float? where = LLShape.HitTestPolyline(pos, hitTestRadius.X, Points, EmptyList<int>.Value, out projected);
			if (where != null)
			{
				if (sel == SelType.Yes) {
					int seg = (int)where.Value;
					var angle = AngleMod8(Points[seg+1].Sub(Points[seg]));
					switch (angle & 3) {
						case 0: return new HitTestResult(this, Cursors.SizeNS, seg);
						case 1: return new HitTestResult(this, Cursors.SizeNESW, seg);
						case 2: return new HitTestResult(this, Cursors.SizeWE, seg);
						case 3: return new HitTestResult(this, Cursors.SizeNWSE, seg);
					}
				} else {
					return new HitTestResult(this, Cursors.Arrow, -1);
				}
			}
			return null;
		}

		public override void OnKeyPress(KeyPressEventArgs e, UndoStack undoStack)
		{
			e.Handled = true;
			char ch = e.KeyChar;
			if (ch >= ' ') {
				undoStack.Do(@do => {
					if (@do) 
						TextTopLeft.Text += ch;
					else
						TextTopLeft.Text = TextTopLeft.Text.Left(TextTopLeft.Text.Length - 1);
				}, true);
			}
		}
		public override void OnKeyDown(KeyEventArgs e, UndoStack undoStack)
		{
			if (e.Modifiers == 0 && e.KeyCode == Keys.Back && TextTopLeft.Text.Length > 0)
			{
				char last = TextTopLeft.Text[TextTopLeft.Text.Length - 1];
				undoStack.Do(@do => {
					if (@do)
						TextTopLeft.Text = TextTopLeft.Text.Left(TextTopLeft.Text.Length - 1);
					else
						TextTopLeft.Text += last;
				}, true);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			FromAnchor = null; // detach
			ToAnchor = null; // detach
		}
	}
}
