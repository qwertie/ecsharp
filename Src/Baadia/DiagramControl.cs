using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reactive.Linq;
using System.Drawing.Drawing2D;
using Loyc;
using Loyc.Collections;
using Loyc.Math;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;

namespace BoxDiagrams
{
	public partial class DiagramControl : LLShapeControl
	{
		public DiagramControl()
		{
			var mouseMove = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");
			var lMouseDown = Observable.FromEventPattern<MouseEventArgs>(this, "MouseDown").Where(e => e.EventArgs.Button == System.Windows.Forms.MouseButtons.Left);
			var lMouseUp   = Observable.FromEventPattern<MouseEventArgs>(this, "MouseUp").Where(e => e.EventArgs.Button == System.Windows.Forms.MouseButtons.Left);
			var dragSequence =
				from down in lMouseDown
				from move in mouseMove.StartWith(down).TakeUntil(lMouseUp)
				select move;
			//dragSequence.ObserveOn(this).Subscribe()
		}

	}


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
	}

	public abstract class AnchorShape : Shape
	{
		public abstract IEnumerable<Func<PointT>> DefaultAnchors { get; }
		public abstract Func<PointT> GetNearestAnchor(PointT p);
	}
	public class Marker : AnchorShape
	{
		public override IEnumerable<Func<PointT>> DefaultAnchors 
		{
			get { return new Repeated<Func<PointT>>(() => this.Point, 1); }
		}
		public override Func<PointT> GetNearestAnchor(PointT p)
		{
 			return () => this.Point;
		}
		LLMarker LL;
		MarkerPolygon Type { get { return LL.Type; } set { LL.Type = Type; } }
		float Radius { get { return LL.Radius; } set { LL.Radius = value; } }
		PointT Point { get { return LL.Point; } set { LL.Point = value; } }
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

		public override IEnumerable<Func<PointT>> DefaultAnchors
		{
			get {
				return new Func<PointT>[] {
					()=>P(TopLeft.X+Size.X/2,TopLeft.Y),
					()=>P(TopLeft.X+Size.X, TopLeft.Y+Size.Y/2),
					()=>P(TopLeft.X+Size.X/2, TopLeft.Y+Size.Y),
					()=>P(TopLeft.X, TopLeft.Y+Size.Y/2),
				};
			}
		}
		public override Func<PointT> GetNearestAnchor(PointT p)
		{
			var vec = p - Center;
			bool vert = vec.Y / Size.Y > vec.X / Size.X;
			double frac = (p.Y - Top) / (Bottom - Top);
			if (vert) {
				frac = (p.X - Left) / (Right - Left);
				if (vec.Y > 0) // bottom
					return () => new PointT(MathEx.InRange(p.X, Left, Right), Bottom);
				else // top
					return () => new PointT(MathEx.InRange(p.X, Left, Right), Top);
			} else {
				if (vec.X > 0) // right
					return () => new PointT(Right, MathEx.InRange(p.Y, Top, Bottom));
				else // left
					return () => new PointT(Left, MathEx.InRange(p.Y, Top, Bottom));
			}
		}
	}

	public class Arrow : Shape
	{
		public TextBox From, To;
		public bool ArrowF, ArrowT;
		public LinearText TextTopLeft, TextBottomRight;
		public string Text;
		public double TextJustify; // 0..1
		public List<ArrowPoint> Points;
		
		public class ArrowPoint
		{
			public Func<PointT> Anchor;
			public VectorT Offs;
			public bool? ToSide;
			public bool Curve;
		}
	}

	public class DrawStyle
	{
		public Color LineColor;
		public float LineWidth;
		public DashStyle LineStyle;
		public Font Font;
		public Color FillColor;

		Pen _pen;
		public Pen Pen { 
			get {
				return _pen = _pen ?? new Pen(LineColor, LineWidth) { DashStyle = LineStyle };
			}
		}
		Brush _brush;
		public Brush Brush { 
			get {
				if (_brush == null)
					_brush = new SolidBrush(FillColor);
				return _brush;
			}
		}
	}

	// "Baadia": Boxes And Arrows Diagrammer
	//
	// Future flourishes:
	// - linear gradient brushes (modes: gradient across shape, or gradient across sheet)
	// - sheet background pattern/stretch bitmap
	// - box background pattern/stretch bitmap
	// - snap lines, plus a ruler on top and left to create and remove them
	//   - ruler itself can act as scroll bar
	// - text formatting override for parts of a box
}
