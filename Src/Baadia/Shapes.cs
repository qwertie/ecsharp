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

namespace BoxDiagrams
{
	public enum BoxType
	{
		Rect, Ellipse, Borderless
	}
	public struct LinearText
	{
		public string Text;
		public float Justify; // 0..1
	}

	/// <summary>
	/// Represents a "complex" shape in Baadia (e.g. line, arrow, or textbox),
	/// </summary><remarks>
	/// A <see cref="Shape"/> may contain multiple <see cref="LLShape"/>s (e.g. 
	/// TextBox = LLTextShape + LLRectangle) and/or contain Baadia-specific 
	/// attributes (e.g. anchors, which attach shapes to other shapes).
	/// <para/>
	/// Baadia treats <see cref="DrawStyle"/>s as immutable by not modifying them.
	/// When the user modifies a draw style, the existing style is cloned and the
	/// new style is assigned to all shapes that use it. Therefore, the DrawStyle
	/// does not need to be cloned when the shape is cloned. The attributes of
	/// <see cref="DrawStyle"/> are not physically marked immutable because <see 
	/// cref="DrawStyle"/> is intended to be general-purpose (shared across 
	/// multiple applications) and I do not want it to make it immutable in ALL 
	/// applications.
	/// <para/>
	/// To support unlimited undo, a <see cref="Shape"/> must be cloned before
	/// it is modified, and the clone is saved on the undo stack. This rule 
	/// includes changes to the <see cref="Style"/>. Ideally, <see cref="Shape"/> 
	/// would be immutable, but in C# it is difficult to make it immutable 
	/// without a lot of clunky boilerplate code. 
	/// </remarks>
	public abstract class Shape : ICloneable<Shape>
	{
		public static readonly DrawStyle DefaultStyle = new DrawStyle { LineWidth = 2 };

		public DrawStyle Style;
		
		/// <summary>Gets shape(s) that are used to indicate that the shape is 
		/// under the mouse cursor, not including an anchor marker (a separate
		/// mechanism creates that).</summary>
		/// <returns>Zero or more hot tracking shapes, or null if the Shape is not 
		/// under the mouse cursor.</returns>
		public abstract IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius);

		public abstract void AddLLShapes(MSet<LLShape> list);

		public virtual Shape Clone()
		{
			return (Shape)MemberwiseClone();
		}
		public abstract BoundingBox<float> BBox { get; }
	}

	public class Anchor
	{
		public Anchor(Shape shape, Func<PointT> point, int angles = 0xFF) { _shape = shape; _point = point; _angles = angles; }
		Func<PointT> _point;
		Shape _shape;
		int _angles;
		public Shape Shape { get { return _shape; } }
		public int Mod8AngleFlags { get { return _angles; } }
		public PointT Point { get { return _point(); } }
	}

	/// <summary>A shape that has Anchors. An Anchor is a point that an arrow or 
	/// line can be attached to.</summary>
	public abstract class AnchorShape : Shape
	{
		public abstract IEnumerable<Anchor> DefaultAnchors { get; }
		public abstract Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1);
		protected Anchor Anchor(Func<PointT> func, int exitAngles = 0xFF) { return new Anchor(this, func, exitAngles); }
	}

	public class Marker : AnchorShape
	{
		public Marker(DrawStyle style, PointT point, float radius, MarkerPolygon type)
		{
			LL = new LLMarker(style, point, radius, type);
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

		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
		}
		public override void AddLLShapes(MSet<LLShape> list)
		{
			list.Add(LL);
		}
		public override BoundingBox<Coord> BBox
		{
			get { var bb = new BoundingBox<Coord>(Point, Point); bb.Inflate(Radius); return bb; }
		}
	}

	public class TextBox : AnchorShape
	{
		public TextBox(BoundingBox<float> bbox)
		{
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
			var vec = p - Center;
			bool vert = vec.Y / Size.Y > vec.X / Size.X;
			double frac = (p.Y - Top) / (Bottom - Top);
			Anchor a;
			if (vert) {
				frac = (p.X - Left) / (Right - Left);
				if (vec.Y > 0) // bottom
					a = Anchor(() => new PointT(MathEx.InRange(p.X, Left, Right), Bottom), 7 << 5);
				else // top
					a = Anchor(() => new PointT(MathEx.InRange(p.X, Left, Right), Top), 7 << 1);
			} else {
				if (vec.X > 0) // right
					a = Anchor(() => new PointT(Right, MathEx.InRange(p.Y, Top, Bottom)), 0x83);
				else // left
					a = Anchor(() => new PointT(Left, MathEx.InRange(p.Y, Top, Bottom)), 7 << 3);
			}
			return a;
		}

		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
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

		public override Shape Clone()
		{
			return (Shape)MemberwiseClone();
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
		public LineOrArrow(List<PointT> points = null) {
			Points = points ?? new List<PointT>();
			TextTopLeft.Justify = 0.5f;
			TextBottomRight.Justify = 0.5f;
		}
		public Arrowhead FromArrow, ToArrow;
		public LinearText TextTopLeft, TextBottomRight;
		public Anchor FromAnchor, ToAnchor;
		public List<PointT> Points; // includes cached anchor point(s)
		
		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
		}

		public override void AddLLShapes(MSet<LLShape> list)
		{
			int z = 0x01000000 - Points.Count * 4;
			if (Points.Count >= 2) {
				list.Add(new LLPolyline(Style, Points) { ZOrder = z });
				int half = (Points.Count-1)/2;
				
				if (TextTopLeft.Text != null)
					list.Add(
						new LLTextShape(Style, TextTopLeft.Text, LLTextShape.JustifyLowerLeft, Points[half])
							{ AngleDeg = (float)Points[half+1].Sub(Points[half]).AngleDeg() });
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
	}
}
