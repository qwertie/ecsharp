using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Loyc.Collections;
using Loyc;
using System.Drawing.Drawing2D;
using Loyc.Math;
using Loyc.Geometry;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;
using BoundingBoxT = Loyc.Geometry.BoundingBox<float>;
using ProtoBuf;

namespace Util.WinForms
{
	/// <summary>
	/// Base class for a shape with PointF coordinates that supports drawing and 
	/// hit-testing.
	/// </summary>
	/// <remarks>
	/// These shapes are designed to be used in a <see cref="LLShapeControl"/>, but
	/// they could be drawn on other controls too.
	/// <para/>
	/// Ideally, the geometry of the shape would be somehow separated from code 
	/// that is specific to the GUI library (WinForms). But I'm in a hurry to get 
	/// this done.
	/// </remarks>
	public abstract class LLShape : IComparable<LLShape>, ICloneable<LLShape>
	{
		public static int NextZOrder;
		public static DrawStyle DefaultStyle = new DrawStyle { LineColor = Color.Black, TextColor = Color.Black, FillColor = Color.White };

		public LLShape(LLShape basis)
		{
			Style = basis.Style;
			Opacity = basis.Opacity;
			ZOrder = basis.ZOrder;
		}
		public LLShape(DrawStyle style)
		{
			ZOrder = NextZOrder++;
			Style = style ?? DefaultStyle;
			Opacity = 255;
		}
		
		/// <summary>Gets or sets the draw style of the shape.</summary>
		public DrawStyle Style;
		/// <summary>Holds the ZOrder (draw order, where higher-ZOrder shapes are 
		/// on top of lower-ZOrder shapes). Shapes with a negative ZOrder are not
		/// drawn.</summary>
		public int ZOrder;
		/// <summary>Gets or sets a flag that controls whether to draw the
		/// shape, which is the sign bit of ZOrder. The shape is hidden iff 
		/// the ZOrder is negative.</summary>
		public bool IsVisible { get { return ZOrder >= 0; } set { if (value != IsVisible) ZOrder = ~ZOrder; } }

		public byte Opacity { get; set; }

		public virtual void Invalidate() { }

		public abstract void Draw(Graphics g);
		
		/// <summary>Determines whether a test point is close to the shape.</summary>
		/// <param name="point">The test point.</param>
		/// <param name="radius">Maximum distance between the shape and the test point</param>
		/// <param name="projected">The point on the shape that is closest to the test point</param>
		/// <returns>null if not hit; if hit, an object that represents part of the shape that was hit.</returns>
		/// <remarks>
		/// Complex shapes should automatically track their bounding box to 
		/// optimize hit-testing.
		/// <para/>
		/// For polylines, the return value represents the line segment 
		/// that was hit plus the fraction along that segment, e.g. 3.33
		/// represents one-third of the distance from the beginning of the 
		/// fourth segment (between point 3 and 4). For polygons, the return
		/// value should be negative for the inside of the polygon, or 
		/// positive for one of the edges (number determined as for a line 
		/// string.)
		/// </remarks>
		public abstract object HitTest(PointT point, Coord radius, out PointT projected);

		/// <summary>Returns the bounding box of the shape (not transformed).</summary>
		public abstract BoundingBox<Coord> BBox { get; }
		
		/// <summary>Draws a polygon with holes.</summary>
		/// <param name="points">All points of all parts of the shape. Holes must 
		/// wind in the opposite direction as the shape in which they are embedded.</param>
		/// <param name="divisions">Indexes of divisions between sub-polygons. Must be sorted.</param>
		protected static void DrawPolygon(Graphics g, DrawStyle style, IList<PointT> points, IList<int> divisions, byte opacity)
		{
			if (style.FillColor.A + style.LineColor.A == 0)
				return;
			Pen pen = style.Pen(opacity);
			Brush brush = style.Brush(opacity);
			if (divisions != null && divisions.Count != 0) {
				using (var gp = new GraphicsPath()) {
					AddPolygon(points, divisions, gp);
					if (style.OutlineBehindFill && style.LineColor.A > 0)
						g.DrawPath(pen, gp);
					if (style.FillColor.A > 0)
						g.FillPath(brush, gp);
					if (!style.OutlineBehindFill && style.LineColor.A > 0)
						g.DrawPath(pen, gp);
				}
			} else {
				var array = points.SelectArray(p => p.AsBCL());
				if (style.OutlineBehindFill && style.LineColor.A > 0)
					g.DrawPolygon(pen, array);
				if (style.FillColor.A > 0)
					g.FillPolygon(brush, array);
				if (!style.OutlineBehindFill && style.LineColor.A > 0)
					g.DrawPolygon(pen, array);
			}
		}
		protected static void AddPolygon(IList<PointT> points, IList<int> divisions, GraphicsPath gp)
		{
			int prev = 0;
			var points2 = points.SelectArray(p => p.AsBCL());
			for (int i = 0; i < divisions.Count; i++) {
				AddPolygon(gp, points2.Slice(prev, divisions[i] - prev).ToArray());
				prev = divisions[i];
			}
			AddPolygon(gp, points2.Slice(prev, points.Count - prev).ToArray());
		}
		protected static void AddPolygon(GraphicsPath gp, PointF[] points)
		{
			if (points.Length > 2)
				gp.AddPolygon(points);
			else if (points.Length == 2)
				gp.AddLine(points[0], points[1]);
		}
		protected static Coord QuadranceTo(PointT p, LineSegmentT seg, out Coord frac, out PointT proj)
		{
			frac = p.GetFractionAlong(seg);
			proj = seg.PointAlong(frac);
			return p.Sub(proj).Quadrance();
		}
		public static Coord? HitTestLine(PointF point_, Coord radius, LineSegmentT line, out PointT proj)
		{
			Coord frac;
			if (QuadranceTo(point_.AsLoyc(), line, out frac, out proj) <= radius*radius)
				return frac;
			else
				return null;
		}
		public static Coord? HitTestPolyline(PointT point, Coord radius, IEnumerable<PointT> points, IList<int> divisions, out PointT projected)
		{
			var maxQuadrance = radius*radius;
			int i = 0;
			Coord? bestHit = null;
			PointT bestProj = point;
			foreach (var line in points.AdjacentPairsCircular()) {
				Coord frac;
				PointT p;
				var q = QuadranceTo(point, line, out frac, out p);
				if (q < maxQuadrance) {
					maxQuadrance = q;
					bestHit = i + frac;
					bestProj = p;
				}
				i++;
			}
			projected = bestProj;
			return bestHit;
		}
		public static Coord? HitTestPolygon(PointT point, Coord radius, IEnumerable<PointT> points, IList<int> divisions, out PointT projected)
		{
			if (PolygonMath.GetWindingNumber(points, point) != 0) {
				projected = point;
				return -1;
			} else
				return HitTestPolyline(point, radius, points, divisions, out projected);
		}

		public int CompareTo(LLShape other)
		{
			return ZOrder.CompareTo(other.ZOrder);
		}

		public abstract LLShape Clone();
	}

	/// <summary>A group of shapes that share the same transformation matrix.</summary>
	/// <remarks><see cref="LLShapeGroup"/> is derived from <see cref="LLShape"/> 
	/// so that users can either place individual shapes or shape groups into a
	/// <see cref="LLShapeLayer"/>. However, the ZOrder, Opacity and Style of the
	/// <see cref="LLShapeGroup"/> is ignored. Hit testing does work, however.</remarks>
	public class LLShapeGroup : LLShape
	{
		public LLShapeGroup(LLShape basis) : base(basis) { }
		
		Matrix _transform, _inverse;
		public Matrix Transform 
		{
			get { return _transform; }
			set { 
				if (!object.Equals(_transform, value)) {
					_transform = value;
					_inverse = null;
					Invalidate();
				}
			}
		}
		public Matrix InverseTransform
		{
			get {
				if (_inverse == null && _transform != null) {
					var inverse = _transform.Clone();
					inverse.Invert();
					_inverse = inverse;
				}
				return _inverse;
			}
		}

		MSet<LLShape> _shapes = new MSet<LLShape>();
		public MSet<LLShape> Shapes { get { return _shapes; } }

		public override void Draw(Graphics g)
		{
			throw new NotImplementedException();
		}

		public override object HitTest(PointT point, float radius, out PointT projected)
		{
			LLShape bestShape = null;
			object bestResult = null;
			float bestQuad = float.PositiveInfinity;
			projected = default(PointT);
			foreach (var shape in _shapes) {
				PointT proj;
				var r = shape.HitTest(point, radius, out proj);
				if (r != null) {
					float quad = projected.Sub(point).Quadrance();
					if (bestShape == null || quad < bestQuad) {
						bestShape = shape;
						bestResult = r;
						bestQuad = quad;
						projected = proj;
					}
				}
			}
			return new Pair<LLShape, object>(bestShape, bestResult);
		}

		public override BoundingBox<float> BBox
		{
			get { return _shapes.Select(s => s.BBox).Union(); }
		}

		public override LLShape Clone()
		{
			var copy = (LLShapeGroup)MemberwiseClone();
			copy._shapes = _shapes.Clone();
			return copy;
		}
	}

	public class LLMarker : LLShape
	{
		public LLMarker(DrawStyle style, PointT point, Coord radius, MarkerPolygon type) 
			: base(style) { 
			Point = point; 
			Radius = radius;
			Type = type;
		}
		public MarkerPolygon Type;
		public Coord Radius;
		public PointT Point;
		
		public override void Draw(Graphics g)
		{
			var pts = Type.Points;
			var divs = Type.Divisions;
			var scaledPts = pts.SelectArray(p => Point.Add((VectorT)p.Mul(Radius)));
			DrawPolygon(g, Style, scaledPts, divs.AsList(), Opacity);
		}
		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			projected = Point;
			if (point.Sub(Point).Quadrance() <= MathEx.Square(radius + Radius))
				return 0;
			else
				return null;
		}
		public override BoundingBox<Coord> BBox
		{
			get {
				var radius = new Vector<Coord>(Radius, Radius);
				return new BoundingBox<Coord>(Point - radius, Point + radius);
			}
		}
		public override LLShape Clone()
		{
			return new LLMarker(Style, Point, Radius, Type) { ZOrder = ZOrder };
		}
	}
	public class LLMarkerRotated : LLMarker
	{
		public LLMarkerRotated(DrawStyle style, PointT point, Coord radius, MarkerPolygon type, Coord angleDeg) : base(style, point, radius, type)
		{
			AngleDeg = angleDeg;
		}
		public override void Draw(Graphics g)
		{
			Matrix mat = new Matrix();
			mat.Translate(Point.X, Point.Y);
			mat.Scale(Radius, Radius);
			mat.Rotate(AngleDeg);
			PointF[] pts = Type.Points.SelectArray(p => p.AsBCL());
			mat.TransformPoints(pts);
			DrawPolygon(g, Style, pts.SelectArray(p => p.AsLoyc()), Type.Divisions.AsList(), Opacity);
		}
		public Coord AngleDeg;
		public override LLShape Clone()
		{
			return new LLMarkerRotated(Style, Point, Radius, Type, AngleDeg) { ZOrder = ZOrder };
		}
	}

	/// <summary>An unclosed line string.</summary>
	public class LLPolyline : LLShape
	{
		public LLPolyline(DrawStyle style, IList<PointT> points) : base(style) { _points = points; }
		protected BoundingBox<Coord> _bbox;
		protected IList<PointT> _points;
		public IList<PointT> Points { get { return _points; } set { _points = value; Invalidate(); } }
		protected IList<int> _divisions = EmptyList<int>.Value;
		public IList<int> Divisions { get { return _divisions; } set { _divisions = value; Invalidate(); } }

		public override void Invalidate() { _bbox = null; base.Invalidate(); }

		public override void Draw(Graphics g)
		{
			int start = 0;
			var points = Points.SelectArray(p => p.AsBCL());
			for (int i = 0, c = Divisions.Count; i < c; i++) {
				int end = Divisions[c];
				if (end - start > 1)
					g.DrawLines(Style.Pen(Opacity), points.Slice(start, end).ToArray());
				start = end;
			}
			if (points.Length - start > 1)
				g.DrawLines(Style.Pen(Opacity), points.Slice(start).ToArray());
		}
		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			projected = point;
			if (!BBox.Inflated(radius, radius).Contains(point))
				return null;
			return HitTestPolyline(point, radius, Points, Divisions, out projected);
		}
		public override BoundingBox<Coord> BBox
		{
			get { 
				if (_bbox == null) {
					var bb = Points.ToBoundingBox();
					if (Points.Count > 3) return bb;
					_bbox = bb;
				}
				return _bbox;
			}
		}
		public override LLShape Clone()
		{
			LLPolyline copy = (LLPolyline)MemberwiseClone();
			if (!_points.IsReadOnly)
				copy._points = new List<PointT>(_points);
			if (!_divisions.IsReadOnly)
				copy._divisions = new List<int>(_divisions);
			return copy;
		}
	}

	/// <summary>A filled rectangle.</summary>
	public class LLRectangle : LLShape
	{
		public LLRectangle(DrawStyle style, BoundingBoxT rect) : base(style) { Rect = rect; }
		public BoundingBoxT Rect;

		public override void Draw(Graphics g)
		{
			var pen = Style.Pen(Opacity);
			if (pen != null)
				g.DrawRectangle(pen, Rect.X1, Rect.Y1, Rect.X2 - Rect.X1, Rect.Y2 - Rect.Y1);
			var br = Style.Brush(Opacity);
			if (br != null)
				g.FillRectangle(br, Rect.AsBCL());
		}
		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			var infl = Style.LineWidth * 0.5f + radius;
			projected = point.ProjectOnto(Rect);
			return projected.X + projected.Y <= infl ? (float?)0 : null;
		}
		public override BoundingBoxT BBox
		{
			get { return Rect; }
		}
		public override LLShape Clone()
		{
			return (LLRectangle)MemberwiseClone();
		}
	}
	
	/// <summary>A filled ellipse.</summary>
	public class LLEllipse : LLRectangle
	{
		public LLEllipse(DrawStyle style, BoundingBoxT rect) : base(style, rect) { }
		public override void Draw(Graphics g)
		{
			var pen = Style.Pen(Opacity);
			if (pen != null)
				g.DrawEllipse(pen, Rect.AsBCL());
			var br = Style.Brush(Opacity);
			if (br != null)
				g.FillEllipse(br, Rect.AsBCL());
		}
		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			var infl = Style.LineWidth * 0.5f + radius;
			projected = point.ProjectOnto(Rect);
			return projected.X + projected.Y <= infl ? (float?)0 : null;
		}
	}

	/// <summary>A filled polygon.</summary>
	public class LLPolygon : LLPolyline
	{
		public LLPolygon(DrawStyle style, IList<PointT> points, IList<int> divisions = null) : base(style, points)
		{
			_divisions = divisions ?? EmptyList<int>.Value;
		}
		
		public override void Draw(Graphics g)
		{
			DrawPolygon(g, Style, Points, Divisions, Opacity);
		}
		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			projected = point;
			if (!BBox.Inflated(radius, radius).Contains(point))
				return null;
			if (PolygonMath.GetWindingNumber(Points, point) != 0) {
				projected = point;
				return -1;
			}
			return HitTestPolyline(point, radius, Points, Divisions, out projected);
		}
	}

	/// <summary>A simple curve shape made from quadratic curve segments.</summary>
	public class LLQuadraticCurve : LLShape
	{
		LLQuadraticCurve(DrawStyle style, IList<PointT> points) : base(style) 
		{
			_points = points ?? EmptyList<PointT>.Value;
		}

		protected BoundingBoxT _bbox;
		protected IList<PointT> _points;
		public IList<PointT> Points { get { return _points; } set { _points = value; Invalidate(); } }
		public int _pointsPerSeg = 8;
		public int PointsPerSeg { get { return _pointsPerSeg; } set { _pointsPerSeg = value; Invalidate(); } }
		public PointF[] Flattened;

		public override void Invalidate() { _bbox = null; Flattened = null; base.Invalidate(); }

		public override void Draw(Graphics g)
		{
			AutoFlatten();
			g.DrawLines(Style.Pen(Opacity), Flattened);
		}

		public void AutoFlatten() { if (Flattened == null) Flatten(); }

		public void Flatten()
		{
			if (Points.Count <= 2)
				Flattened = Points.SelectArray(p => p.AsBCL());
			else {
				int totalCount = PointsPerSeg * (Points.Count - 1) + 1;
				Flattened = new PointF[totalCount];
				int offs = 0;
				Coord per = 1f / PointsPerSeg;
				for (int i = 0; i < Points.Count - 2; i++) {
					bool last = i == Points.Count - 2;
					PointT a = Points[i], b = Points[i + 1], c = Points[i + 2];
					PointT a_ = i == 0 ? a : a.To(b).Midpoint();
					PointT c_ = last ? c : b.To(c).Midpoint();
					Flatten(a_, b, c_, Flattened.Slice(offs, PointsPerSeg), per);
					offs += PointsPerSeg;
				}
				Flattened[Flattened.Length - 1] = Points[Points.Count - 1].AsBCL();
			}
		}
		private void Flatten(PointT a, PointT b, PointT c, ArraySlice<PointF> @out, Coord per)
		{
			@out[0] = a.AsBCL();
			Coord frac = per;
			for (int i = 1; i < @out.Count; i++, frac += per) {
				PointT d = a.To(b).PointAlong(frac), e = b.To(c).PointAlong(frac);
				@out[i] = d.To(e).PointAlong(frac).AsBCL();
			}
		}

		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			projected = point;
			if (!BBox.Inflated(radius, radius).Contains(point))
				return null;
			AutoFlatten();
			float? result = HitTestPolyline(point, radius, Flattened.Select(p => p.AsLoyc()), EmptyList<int>.Value, out projected);
			return result / _pointsPerSeg;
		}
		public override BoundingBox<Coord> BBox
		{
			get { return _bbox = _bbox ?? Points.ToBoundingBox(); }
		}
		public override LLShape Clone()
		{
			var copy = (LLQuadraticCurve)MemberwiseClone();
			if (!_points.IsReadOnly)
				copy._points = new List<PointT>(_points);
			return copy;
		}
	}

	/// <summary>A single line or multiple lines of text with word wrap (line breaks chosen by GDI+).</summary>
	public class LLTextShape : LLShape
	{
		public LLTextShape(DrawStyle style) : base(style) { }
		public LLTextShape(DrawStyle style, string text, StringFormat justify, PointT location, VectorT? maxSize = null)
			: base(style) { Text = text; Justify = justify ?? Justify; Location = location; MaxSize = maxSize; }
		public string Text;
		public float AngleDeg;
		public PointT Location;
		public VectorT? MaxSize;
		VectorT? _measuredSize;
		public StringFormat Justify = StringFormat.GenericTypographic;

		public void SetJustify(StringAlignment horizontal, StringAlignment vertical = StringAlignment.Near)
		{
			Justify = Justification(horizontal, vertical);
		}
		public static StringFormat Justification(StringAlignment horizontal, StringAlignment vertical = StringAlignment.Near)
		{
			switch (horizontal) {
				case StringAlignment.Near:
					switch (vertical) {
						case StringAlignment.Near: return JustifyUpperLeft;
						case StringAlignment.Far: return JustifyUpperRight;
						default: return JustifyUpperCenter;
					}
				case StringAlignment.Center:
					switch (vertical) {
						case StringAlignment.Near: return JustifyMiddleLeft;
						case StringAlignment.Far: return JustifyMiddleRight;
						default: return JustifyMiddleCenter;
					}
				case StringAlignment.Far:
					switch (vertical) {
						case StringAlignment.Near: return JustifyLowerLeft;
						case StringAlignment.Far: return JustifyLowerRight;
						default: return JustifyLowerCenter;
					}
				default:
					return StringFormat.GenericTypographic;
			}
		}

		public static readonly StringFormat JustifyUpperLeft   = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
		public static readonly StringFormat JustifyMiddleLeft  = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
		public static readonly StringFormat JustifyLowerLeft   = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far };
		public static readonly StringFormat JustifyUpperCenter = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
		public static readonly StringFormat JustifyMiddleCenter= new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		public static readonly StringFormat JustifyLowerCenter = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
		public static readonly StringFormat JustifyUpperRight  = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
		public static readonly StringFormat JustifyMiddleRight = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
		public static readonly StringFormat JustifyLowerRight  = new StringFormat(0) { Trimming = StringTrimming.EllipsisWord, Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };

		static Graphics _measureGraphics;

		public VectorT MeasuredSize
		{
			get {
				if (_measuredSize == null) {
					if (_measureGraphics == null)
						_measureGraphics = Graphics.FromImage(new Bitmap(1, 1));
					SizeF size;
					if (MaxSize.HasValue)
						size = _measureGraphics.MeasureString(Text, Style.Font, new SizeF(MaxSize.Value.X, MaxSize.Value.Y), Justify);
					else
						size = _measureGraphics.MeasureString(Text, Style.Font);
					_measuredSize = new VectorT(size.Width, size.Height);
				}
				return _measuredSize.Value;
			}
		}
		
		public override void Invalidate()
		{
			base.Invalidate();
		}
		public override void Draw(Graphics g)
		{
			var old = g.Transform.Clone();
			g.TranslateTransform(Location.X, Location.Y);
			g.RotateTransform(AngleDeg);
			if (MaxSize != null)
				g.DrawString(Text, Style.Font, Style.TextBrush(Opacity), new RectangleF(0, 0, MaxSize.Value.X, MaxSize.Value.Y), Justify);
			else
				g.DrawString(Text, Style.Font, Style.TextBrush(Opacity), new Point(), Justify);
			g.Transform = old;
		}

		public override object HitTest(PointT point, Coord radius, out PointT projected)
		{
			var size = MeasuredSize;
			projected = point;
			Coord x = point.X, y = point.Y;
			if (HitTest1D(ref x, Location.X, size.X, MaxSize.HasValue ? MaxSize.Value.X : size.X, Justify.Alignment) ||
				HitTest1D(ref y, Location.Y, size.Y, MaxSize.HasValue ? MaxSize.Value.Y : size.Y, Justify.LineAlignment))
				return 0f;
			projected = new PointT(x, y);
			return null;
		}
		private bool HitTest1D(ref float point, float location, float size, float maxSize, StringAlignment alignment)
		{
			if (alignment == StringAlignment.Far)
				location += maxSize - size;
			else if (alignment == StringAlignment.Center)
				location += (maxSize - size) * 0.5f;
			float old = point;
			point = MathEx.InRange(point, location, location + size);
			return point == old;
		}

		public override BoundingBoxT BBox
		{
			get {
				if (MaxSize == null)
					return new BoundingBoxT(Location, Location + MeasuredSize);
				else
					return new BoundingBoxT(Location, Location + MaxSize.Value);
			}
		}

		public override LLShape Clone()
		{
			return (LLTextShape)MemberwiseClone();
		}
	}

	/// <summary>A <see cref="MarkerPolygon"/> represents a simple, fixed shape 
	/// such as a circle or a square. The polygon's coordinates should give the
	/// shape an approximate radius of 1.0, with (0, 0) as the centerpoint.</summary>
	/// <remarks>
	/// An <see cref="LLMarker"/> is a point shape that draws itself using one of 
	/// these <see cref="MarkerPolygon"/>s.
	/// </remarks>
	[ProtoContract(AsReferenceDefault=true, SkipConstructor=true)]
	public class MarkerPolygon
	{
		public MarkerPolygon(IList<Point<Coord>> points, IList<int> divisions = null)
			: this(points.AsListSource(), divisions.AsListSource()) { }
		public MarkerPolygon(IListSource<Point<Coord>> points, IListSource<int> divisions = null)
			{ Points = points; Divisions = divisions ?? EmptyList<int>.Value; }
		public IListSource<Point<Coord>> Points;
		public IListSource<int> Divisions;

		[ProtoMember(1, Options=MemberSerializationOptions.OverwriteList)]
		private IEnumerable<Point<Coord>> PB_Points { get { return Points; } set { Points = ((IList<Point<Coord>>)value).AsListSource(); } }
		[ProtoMember(2, Options=MemberSerializationOptions.OverwriteList)]
		private IEnumerable<int> PB_Divisions { get { return Divisions; } set { Divisions = ((IList<int>)value).AsListSource(); } }

		protected static PointT P(Coord x, Coord y) { return new PointT(x, y); }

		public static readonly MarkerPolygon Square = new MarkerPolygon(
			new[] { P(-1,-1),P(1,-1),P(1,1),P(-1,1) }.AsListSource());
		public static readonly MarkerPolygon Circle = new MarkerPolygon(new[]
			{
				P(-1, 0),
				P(-1 + 0.0761f, -1 + 0.6173f),
				P(-1 + 0.2929f, -1 + 0.2929f),
				P(-1 + 0.6173f, -1 + 0.0761f),
				P(0, -1),
				P(1 - 0.6173f, -1 + 0.0761f),
				P(1 - 0.2929f, -1 + 0.2929f),
				P(1 - 0.0761f, -1 + 0.6173f),
				P(1, 0),
				P(1 - 0.0761f, 1 - 0.6173f),
				P(1 - 0.2929f, 1 - 0.2929f),
				P(1 - 0.6173f, 1 - 0.0761f),
				P(0, 1),
				P(-1 + 0.6173f, 1 - 0.0761f),
				P(-1 + 0.2929f, 1 - 0.2929f),
				P(-1 + 0.0761f, 1 - 0.6173f),
				P(-1, 0f),
			}.AsListSource());
		public static readonly MarkerPolygon Donut = new MarkerPolygon(
			Circle.Points.Concat(Circle.Points.Reverse().Select(p => P(p.X/2,p.Y/2))).Buffered(),
			new Repeated<int>(Circle.Points.Count, 1));
		public static readonly MarkerPolygon Diamond = new MarkerPolygon(
			new[] { P(0,-1), P(1,0), P(0,1), P(-1,0) }.AsListSource());
		public static readonly MarkerPolygon DownTriangle = new MarkerPolygon(
			new[] { P(1,-0.8f), P(-1,-0.8f), P(0,0.932f) }.AsListSource());
		public static readonly MarkerPolygon UpTriangle = new MarkerPolygon(
			new[] { P(1,0.8f), P(-1,0.8f), P(0,-0.932f) }.AsListSource());
		public static readonly IListSource<MarkerPolygon> Markers = new[] {
			Square, Circle, Donut, Diamond, DownTriangle, UpTriangle
		}.AsListSource();
	}
}
