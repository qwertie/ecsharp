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

namespace BoxDiagrams
{
	public enum BoxType
	{
		Rect, Ellipse, Borderless
	}
	public class LinearText
	{
		public string Text;
		public float Justify = 0.5f; // 0..1
	}

	public abstract class Shape
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
	}

	public class TextBox : AnchorShape
	{
		public TextBox(BoundingBox<float> bbox)
		{
			BBox = bbox;
		}
		public BoxType Type;
		public string Text;
		public StringFormat TextJustify;
		public BoundingBox<float> BBox;
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
					list.Add(new LLEllipse(BBox) { Style = Style, ZOrder = 0x10000000 - ((int)(area * (Math.PI/4)) >> 3) } );
				else
					list.Add(new LLRectangle(BBox) { Style = Style, ZOrder = 0x10000000 - ((int)area >> 3) } );
			}
			if (Text != null)
				list.Add(new LLTextShape(Text, TextJustify, BBox.MinPoint, BBox.MaxPoint.Sub(BBox.MinPoint)));
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
		public Arrowhead FromArrow, ToArrow;
		public LinearText TextTopLeft, TextBottomRight;
		public Anchor FromAnchor, ToAnchor;
		public List<PointT> Points; // includes cached anchor point
		
		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
		}

		public override void AddLLShapes(MSet<LLShape> list)
		{
			int z = 0x01000000 - Points.Count * 4;
			if (Points.Count >= 2) {
				list.Add(new LLPolyline(Points) { Style = Style, ZOrder = z });
				int half = (Points.Count-1)/2;
				
				if (TextTopLeft.Text != null)
					list.Add(
						new LLTextShape(TextTopLeft.Text, LLTextShape.JustifyLowerLeft, Points[half])
							{ AngleDeg = (float)Points[half+1].Sub(Points[half]).AngleDeg() });
			}
		}
	}
}
