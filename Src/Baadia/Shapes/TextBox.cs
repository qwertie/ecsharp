using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Loyc;
using Loyc.Collections;
using Loyc.Geometry;
using Util.UI;
using Util.WinForms;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using Loyc.Math;
using ProtoBuf;

namespace BoxDiagrams
{
	[ProtoContract()]
	public class TextBox : Shape
	{
		private TextBox() { }
		public TextBox(BoundingBox<float> bbox)
		{
			TextJustify = LLTextShape.JustifyMiddleCenter;
			_bbox = bbox;
		}
		[ProtoMember(3)]
		BoundingBox<float> _bbox;
		public override BoundingBox<float> BBox { get { return _bbox; } }
		[ProtoMember(4)]
		public string Text;
		[ProtoMember(5)]
		public BoxType BoxType;
		[ProtoMember(6)]
		public StringFormat TextJustify;
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
		public override bool IsPanel { get { return _isPanel; } }
		public bool _isPanel; // public because C# won't allow a setter for IsPanel

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

		public override void AddLLShapesTo(ICollection<LLShape> list) // Draw!
		{
			int z = DrawZOrder;
			if (BoxType != BoxType.Borderless) {
				if (BoxType == BoxType.Ellipse)
					list.Add(new LLEllipse(Style, BBox) { ZOrder = z } );
				else
					list.Add(new LLRectangle(Style, BBox) { ZOrder = z } );
			}
			if (Text != null)
				list.Add(new LLTextShape(Style, Text, TextJustify, 
					BBox.MinPoint, BBox.MaxPoint.Sub(BBox.MinPoint)) 
					{ ZOrder = z + 0x10 });
		}

		public override void AddAdornersTo(ICollection<LLShape> list, SelType selMode, VectorT hitTestRadius)
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

		private void AddCornerAdorner(ICollection<LLShape> list, PointT point, VectorT vector)
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

		[Flags] enum RF { Left = 1, Top = 2, Right = 4, Bottom = 8, Move = Left|Top|Right|Bottom }
		class HitTestResult : Util.WinForms.HitTestResult
		{
			public HitTestResult(Shape shape, Cursor cursor, RF resizeFlags) : base(shape, cursor) { ResizeFlags = resizeFlags; }
			public RF ResizeFlags;
		}

		public override Util.WinForms.HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			if (sel != SelType.No) {
				var bbox2 = BBox.Inflated(hitTestRadius.X, hitTestRadius.Y);
				PointT tl = BBox.MinPoint, tr = new PointT(Right, Top);
				PointT br = BBox.MaxPoint, bl = new PointT(Left, Bottom);
				if (PointsAreNear(pos, tr, hitTestRadius))
					return new HitTestResult(this, Cursors.SizeNESW, RF.Top | RF.Right);
				if (PointsAreNear(pos, bl, hitTestRadius))
					return new HitTestResult(this, Cursors.SizeNESW, RF.Bottom | RF.Left);
				if (sel == SelType.Yes) {
					if (PointsAreNear(pos, tl, hitTestRadius))
						return new HitTestResult(this, Cursors.SizeNWSE, RF.Top | RF.Left);
					if (PointsAreNear(pos, br, hitTestRadius))
						return new HitTestResult(this, Cursors.SizeNWSE, RF.Bottom | RF.Right);
				}
			}
			if (sel != SelType.No || (BoxType != BoxType.Borderless && !_isPanel))
			{
				if (sel != SelType.Yes)
					hitTestRadius *= 2;
				var bbox2 = BBox.Deflated(hitTestRadius.X, hitTestRadius.Y);
				if (bbox2.Contains(pos))
					return new HitTestResult(this, Cursors.SizeAll, RF.Top | RF.Bottom | RF.Left | RF.Right);
			}

			return BBox.Contains(pos) ? new HitTestResult(this, Cursors.Arrow, 0) : null;
		}

		public override int DrawZOrder
		{
			get {
				float area = BBox.Width * BBox.Height;
				if (BoxType == BoxType.Rect)
					return 0x10000000 - ((int)area >> 3);
				else
					return 0x10000000 - ((int)(area * (Math.PI/4)) >> 3);
			}
		}

		public override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
			char ch = e.KeyChar;
			if (ch >= 32 || ch == '\r') {
				if (ch == '\r') ch = '\n';
				UndoStack.Do(@do => {
					if (@do)
						this.Text += ch;
					else
						this.Text = this.Text.Left(this.Text.Length - 1);
				}, true);
			}
		}

		public override DoOrUndo AppendTextAction(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;
			return @do => {
				if (@do)
					Text += text;
				else
					Text = Text.Left(Text.Length - text.Length);
			};
		}

		public override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Modifiers == 0 && e.KeyCode == Keys.Back && Text.Length > 0)
			{
				char last = Text[Text.Length-1];
				UndoStack.Do(@do => {
					if (@do)
						this.Text = this.Text.Left(this.Text.Length - 1);
					else
						this.Text += last;
				}, true);
			}
		}

		public override DoOrUndo DragMoveAction(Util.WinForms.HitTestResult htr_, VectorT amount) 
		{
			var htr = htr_ as HitTestResult;
			var rf = htr == null ? RF.Move : htr.ResizeFlags;
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

		public override DoOrUndo DoubleClickAction(Util.WinForms.HitTestResult htr)
		{
			return @do => {
				var t = BoxType;
				if (@do) {
					BoxType = (t == BoxType.Rect ? BoxType.Ellipse : t == BoxType.Ellipse ? BoxType.Borderless : BoxType.Rect);
				} else {
					BoxType = (t == BoxType.Rect ? BoxType.Borderless : t == BoxType.Borderless ? BoxType.Ellipse : BoxType.Rect);
				}
			};
		}

		public override string PlainText()
		{
			return Text;
		}

		public override DoOrUndo GetClearTextAction() {
			string old = null;
			if (string.IsNullOrEmpty(Text))
				return null;
			return @do => {
				if (@do) {
					old = Text; Text = null;
				} else {
					Text = old;
				}
			};
		}
	}
}
