using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc;
using Loyc.Collections;
using Loyc.Geometry;
using Util.UI;
using Util.WinForms;
using Coord = System.Single;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

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
		public static readonly DiagramDrawStyle DefaultStyle = new DiagramDrawStyle { LineWidth = 2 };

		public DiagramDrawStyle Style;

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
				{ Shape = shape; MouseCursor = cursor; Debug.Assert(cursor != null && shape != null); }
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
		public virtual DoOrUndo GetDoubleClickAction(HitTestResult htr) { return null; }

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
}
