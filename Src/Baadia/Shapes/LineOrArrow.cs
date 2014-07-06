using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Loyc;
using Loyc.Collections;
using Loyc.Geometry;
using Util.UI;
using Util.WinForms;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using ProtoBuf;
using Loyc.Collections.Impl;

namespace BoxDiagrams
{
	[ProtoContract(AsReferenceDefault=true, SkipConstructor=true)]
	public class Arrowhead
	{
		public Arrowhead(MarkerPolygon geometry, float width, float shift = 0, float scale = 10) { Geometry = geometry; Width = width; Shift = shift; Scale = scale; }
		[ProtoMember(1)]
		public readonly MarkerPolygon Geometry;
		[ProtoMember(2)]
		public readonly float Width;
		[ProtoMember(3)]
		public readonly float Shift;
		[ProtoMember(4)]
		public float Scale;

		public LLMarkerRotated LLShape(DrawStyle style, ref LineSegment<Coord> toArrow)
		{
			Coord frac = Width * Scale / toArrow.Length();
			var markerPoint = toArrow.PointAlong(1 - frac * 0.5f);
			toArrow = toArrow.A.To(toArrow.PointAlong(1 - Math.Min(frac, 1)));
			return new LLMarkerRotated(style, markerPoint, Scale, Geometry, (Coord)toArrow.Vector().AngleDeg());
		}

		protected static PointT P(Coord x, Coord y) { return new PointT(x, y); }
		static readonly MarkerPolygon arrow45deg = new MarkerPolygon(
			new[] { P(-1, 1), P(0,0), P(0,0), P(-1,-1) }.AsListSource(), new[] { 2 }.AsListSource());
		static readonly MarkerPolygon arrow30deg = new MarkerPolygon(
			new[] { P(-1, 0.5f), P(0,0), P(0,0), P(-1,-0.5f) }.AsListSource(), new[] { 2 }.AsListSource());
		public static readonly Arrowhead Arrow45deg = new Arrowhead(arrow45deg, 0);
		public static readonly Arrowhead Arrow30deg = new Arrowhead(arrow30deg, 0);
		public static readonly Arrowhead Diamond = new Arrowhead(MarkerPolygon.Diamond, 2, -1, 7);
		public static readonly Arrowhead Circle = new Arrowhead(MarkerPolygon.Circle, 2, -1, 6);
	}

	[ProtoContract]
	public class LineOrArrow : Shape
	{
		static int NextZOrder = 0x10000000;

		public LineOrArrow() : this(null) { }
		public LineOrArrow(List<PointT> points) {
			Points = points ?? new List<PointT>();
			TextTopLeft.Justify = 0.5f;
			TextBottomRight.Justify = 0.5f;
			_detachedZOrder = NextZOrder++;
		}
		[ProtoMember(3)]
		public List<PointT> Points; // includes cached anchor point(s)
		[ProtoMember(4)]
		public Arrowhead FromArrow;
		[ProtoMember(5)]
		public Arrowhead ToArrow;
		[ProtoMember(6)]
		public LinearText TextTopLeft;
		[ProtoMember(7)]
		public LinearText TextBottomRight;
		[ProtoMember(8)]
		int _detachedZOrder;

		[ProtoIgnore]
		Anchor _fromAnchor;
		public Anchor FromAnchor
		{
			get { return _fromAnchor; }
			set { _fromAnchor = value; }
		}
		[ProtoIgnore]
		Anchor _toAnchor;
		public Anchor ToAnchor
		{
			get { return _toAnchor; }
			set { _toAnchor = value; }
		}

		public override void AddLLShapesTo(MSet<LLShape> list) // Draw!
		{
			int z = DrawZOrder;
			if (Points.Count >= 2) {
				// Add arrows
				LineSegment<float> firstLine = Points[1].To(Points[0]);
				LineSegment<float> lastLine = Points[Points.Count - 2].To(Points[Points.Count - 1]);
				if (FromArrow != null) {
					var arrow = FromArrow.LLShape(Style, ref firstLine);
					arrow.ZOrder = z;
					list.Add(arrow);
				}
				if (ToArrow != null) {
					var arrow = ToArrow.LLShape(Style, ref lastLine);
					arrow.ZOrder = z;
					list.Add(arrow);
				}

				// Adjust endpoints if necessary to subtract space used by the arrows
				List<PointT> points = Points;
				if (firstLine.B != Points[0] || lastLine.B != Points[Points.Count - 1]) {
					points = new List<PointT>(points);
					points[0] = firstLine.B;
					points[points.Count-1] = lastLine.B;
				}

				// Add main line
				list.Add(new LLPolyline(Style, points) { ZOrder = z });

				// Hacky temporary solution for text
				int half = (Points.Count - 1) / 2;
				var midVec = Points[half + 1].Sub(Points[half]);
				if (TextTopLeft.Text != null) {
					list.Add(new LLTextShape(
						Style, TextTopLeft.Text, LLTextShape.JustifyUpperCenter, Points[half], new VectorT(midVec.Length(), 100)) { AngleDeg = (float)midVec.AngleDeg() });
				}
			}
		}

		static int AngleMod256(VectorT v)
		{
			return (byte)(v.Angle() * (128.0 / Math.PI));
		}

		public override IEnumerable<DoOrUndo> AutoHandleAnchorsChanged()
		{
			var list = InternalList<DoOrUndo>.Empty;
			Debug.Assert(Points.Count >= 2);
			if (_fromAnchor != null && _fromAnchor.Point != Points[0])
				list.Add(AttachedAnchorChanged(_fromAnchor, false));
			if (_toAnchor != null && _toAnchor.Point != Points[Points.Count-1])
				list.Add(AttachedAnchorChanged(_toAnchor, true));
			return list;
		}

		private DoOrUndo AttachedAnchorChanged(Anchor anchor, bool toSide)
		{
			PointT p0 = default(PointT), p1 = default(PointT);
			List<PointT> old = null;
			return @do =>
			{
				IList<PointT> points = Points;
				if (toSide) points = points.Reverse();
				
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
		
		public override Shape Clone()
		{
			var copy = (LineOrArrow)MemberwiseClone();
			// Points are often changed after cloning... yeah, it's hacky.
			_bbox = null; copy._bbox = null; 
			return copy;
		}
		[ProtoIgnore]
		BoundingBox<float> _bbox;
		public override BoundingBox<float> BBox
		{
			get {
				if (_bbox != null)
					Debug.Assert(_bbox.Contains(Points[0]) && _bbox.Contains(Points[Points.Count - 1]));
				return _bbox = _bbox ?? Points.ToBoundingBox();
			}
		}
		
		public override void AddAdornersTo(MSet<LLShape> list, SelType selMode, VectorT hitTestRadius)
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

		public override int DrawZOrder
		{
			get {
				int z = _detachedZOrder;
				if (FromAnchor != null)
					z = Math.Min(z, FromAnchor.Shape.DrawZOrder - 1);
				if (ToAnchor != null)
					z = Math.Min(z, ToAnchor.Shape.DrawZOrder - 1);
				return z;
			}
		}
		public override int HitTestZOrder
		{
			get { return _detachedZOrder; } // on top of boxes rather than underneath
		}

		static int AngleMod8(VectorT v)
		{
			return (int)Math.Round(v.Angle() * (4 / Math.PI)) & 7;
		}

		class HitTestResult : Util.WinForms.HitTestResult
		{
			public HitTestResult(Shape shape, Cursor cursor, int pointOrSeg) 
				: base(shape, cursor) { PointOrSegment = pointOrSeg; }
			public readonly int PointOrSegment;
			public bool IsPointHit { get { return MouseCursor == Cursors.SizeAll; } }
		}

		public override Util.WinForms.HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
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

		public override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
			char ch = e.KeyChar;
			if (ch >= ' ') {
				UndoStack.Do(@do => {
					if (@do) 
						TextTopLeft.Text += ch;
					else
						TextTopLeft.Text = TextTopLeft.Text.Left(TextTopLeft.Text.Length - 1);
				}, true);
			}
		}
		public override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Modifiers == 0 && e.KeyCode == Keys.Back && TextTopLeft.Text.Length > 0)
			{
				char last = TextTopLeft.Text[TextTopLeft.Text.Length - 1];
				UndoStack.Do(@do => {
					if (@do)
						TextTopLeft.Text = TextTopLeft.Text.Left(TextTopLeft.Text.Length - 1);
					else
						TextTopLeft.Text += last;
				}, true);
			}
		}

		public override DoOrUndo DragMoveAction(Util.WinForms.HitTestResult htr, VectorT amount)
		{
			if (htr == null || htr.MouseCursor == Cursors.SizeAll) {
				return @do => {
					_bbox = null;
					var amount2 = @do ? amount : amount.Neg();
					for (int i = 0; i < Points.Count; i++)
						Points[i] = Points[i].Add(amount2);
				};
			}
			return null;
		}

		public override DoOrUndo DoubleClickAction(Util.WinForms.HitTestResult htr_)
		{
			var htr = (HitTestResult)htr_;
			Arrowhead old = null;
			if (htr.IsPointHit) {
				if (htr.PointOrSegment == 0)
					return @do => {
						if (@do) {
							old = FromArrow; FromArrow = NextArrow(FromArrow);
						} else
							FromArrow = old;
					};
				else if (htr.PointOrSegment == Points.Count-1)
					return @do => {
						if (@do) {
							old = ToArrow; ToArrow = NextArrow(ToArrow);
						} else
							ToArrow = old;
					};
			}
			else
			{
				// TODO: switch between curved and straight
			}
			return null;
		}

		public override DoOrUndo OnShapesDeletedAction(Set<Shape> deleted)
		{
			if (_fromAnchor != null && deleted.Contains(_fromAnchor.Shape)
				|| _toAnchor != null && deleted.Contains(_toAnchor.Shape))
			{
				Anchor oldTo = null, oldFrom = null;
				return @do => {
					if (@do) {
						oldTo = _toAnchor; oldFrom = _fromAnchor;
						if (_toAnchor != null && deleted.Contains(_toAnchor.Shape))
							_toAnchor = null;
						if (_fromAnchor != null && deleted.Contains(_fromAnchor.Shape))
							_fromAnchor = null;
					} else {
						_toAnchor = oldTo; _fromAnchor = oldFrom;
					}
				};
			}
			return null;

		}

		static readonly Arrowhead[] StdArrows = new[] { null, Arrowhead.Arrow30deg, Arrowhead.Diamond, Arrowhead.Circle };
		public static Arrowhead NextArrow(Arrowhead arrow)
		{
			return StdArrows[(StdArrows.IndexOf(arrow) + 1) % StdArrows.Length];
		}

		public override void Dispose()
		{
			base.Dispose();
			FromAnchor = null; // detach
			ToAnchor = null; // detach
		}

		public override string PlainText()
		{
			if (TextTopLeft.Text == null || TextBottomRight.Text == null)
				return TextTopLeft.Text ?? TextBottomRight.Text;
			else
				return TextTopLeft.Text + "\n" + TextBottomRight.Text;
		}

		public override DoOrUndo GetClearTextAction() {
			string t1 = null, t2 = null;
			if (string.IsNullOrEmpty(TextTopLeft.Text + TextBottomRight.Text))
				return null;
			return @do => {
				if (@do) {
					t1 = TextTopLeft.Text; TextTopLeft.Text = null;
					t2 = TextBottomRight.Text; TextBottomRight.Text = null;
				} else {
					TextTopLeft.Text = t1;
					TextBottomRight.Text = t2;
				}
			};
		}
	}
}
