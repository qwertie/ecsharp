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
		public DrawStyle Style;
		
		/// <summary>Gets shape(s) that are used to indicate that the shape is 
		/// under the mouse cursor, not including an anchor marker (a separate
		/// mechanism creates that).</summary>
		/// <returns>Zero or more hot tracking shapes, or null if the Shape is not 
		/// under the mouse cursor.</returns>
		public abstract IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius);
	}

	public class Anchor
	{
		public Anchor(Shape shape, Func<PointT> point) { _shape = shape; _point = point; }
		Func<PointT> _point;
		Shape _shape;
		public Shape Shape { get { return _shape; } }
		public PointT Point { get { return _point(); } }
	}

	/// <summary>A shape that has Anchors. An Anchor is a point that an arrow or 
	/// line can be attached to.</summary>
	public abstract class AnchorShape : Shape
	{
		public abstract IEnumerable<Anchor> DefaultAnchors { get; }
		public abstract Anchor GetNearestAnchor(PointT p);
		protected Anchor Anchor(Func<PointT> func) { return new Anchor(this, func); }
	}

	public class Marker : AnchorShape
	{
		public override IEnumerable<Anchor> DefaultAnchors 
		{
			get { return new Repeated<Anchor>(Anchor(() => this.Point), 1); }
		}
		public override Anchor GetNearestAnchor(PointT p)
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
	}

	public class TextBox : AnchorShape
	{
		public BoxType Type;
		public string Text;
		public PointT TopLeft;
		public VectorT Size;
		public PointT Center { get { return P(TopLeft.X + Size.X/2, TopLeft.Y + Size.Y/2); } }
		public float Top { get { return TopLeft.Y; } }
		public float Left { get { return TopLeft.X; } }
		public float Right { get { return TopLeft.X + Size.X; } }
		public float Bottom { get { return TopLeft.Y + Size.Y; } }
		public bool AutoSize;
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
					Anchor(()=>P(TopLeft.X+Size.X/2,TopLeft.Y)),
					Anchor(()=>P(TopLeft.X+Size.X, TopLeft.Y+Size.Y/2)),
					Anchor(()=>P(TopLeft.X+Size.X/2, TopLeft.Y+Size.Y)),
					Anchor(()=>P(TopLeft.X, TopLeft.Y+Size.Y/2)),
				};
			}
		}
		public override Anchor GetNearestAnchor(PointT p)
		{
			var vec = p - Center;
			bool vert = vec.Y / Size.Y > vec.X / Size.X;
			double frac = (p.Y - Top) / (Bottom - Top);
			if (vert) {
				frac = (p.X - Left) / (Right - Left);
				if (vec.Y > 0) // bottom
					return Anchor(() => new PointT(MathEx.InRange(p.X, Left, Right), Bottom));
				else // top
					return Anchor(() => new PointT(MathEx.InRange(p.X, Left, Right), Top));
			} else {
				if (vec.X > 0) // right
					return Anchor(() => new PointT(Right, MathEx.InRange(p.Y, Top, Bottom)));
				else // left
					return Anchor(() => new PointT(Left, MathEx.InRange(p.Y, Top, Bottom)));
			}
		}

		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
		}
	}

	public class LineOrArrow : Shape
	{
		public TextBox From, To;
		public bool ArrowF, ArrowT;
		public LinearText TextTopLeft, TextBottomRight;
		public string Text;
		public double TextJustify; // 0..1
		public List<ArrowPoint> Points;
		
		public class ArrowPoint
		{
			public Anchor Anchor;
			public VectorT Offs;
			public bool? ToSide;
			public bool Curve;
		}

		public override IEnumerable<LLShape> HotTrackingShapes(PointT mousePos, VectorT hitTestRadius)
		{
			throw new NotImplementedException();
		}
	}
}
