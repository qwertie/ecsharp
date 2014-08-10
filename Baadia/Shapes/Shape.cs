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
using ProtoBuf;
using Util.Collections;

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
	[ProtoContract(AsReferenceDefault=true, SkipConstructor=true)]
	[ProtoInclude(100, typeof(TextBox))]
	[ProtoInclude(101, typeof(LineOrArrow))]
	[ProtoInclude(102, typeof(Marker))]
	public abstract class Shape : ChildOfOneParent<DiagramDocument>, IShapeWidget, ICloneable<Shape>, IDisposable
	{
		public static readonly DiagramDrawStyle DefaultStyle = new DiagramDrawStyle { LineWidth = 2 };

		[ProtoMember(1)]
		public DiagramDrawStyle Style;

		public abstract void AddLLShapesTo(ICollection<LLShape> list);
		public abstract void AddAdornersTo(ICollection<LLShape> list, SelType selMode, VectorT hitTestRadius);

		public virtual Shape Clone()
		{
			return (Shape)MemberwiseClone();
		}
		public abstract BoundingBox<float> BBox { get; }

		public UndoStack UndoStack { get { return _parent.UndoStack; } }

		/// <summary>Hit-tests the shape and returns information that includes the 
		/// mouse cursor to use for it.</summary>
		/// <returns>Null indicates a failed hit test. Inside the result object, the 
		/// cursor <see cref="Cursors.Arrow"/> means that the shape is selectable, 
		/// <see cref="Cursors.SizeAll"/> indicates that the user will be moving something, 
		/// and a sizing cursor such as <see cref="SizeNS"/> indicates that something will 
		/// be resized or that one line in a collection of lines will be moved along the 
		/// indicated direction (e.g. up-down for SizeNS).</returns>
		public abstract HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel);

		protected internal static DrawStyle SelAdornerStyle = new DrawStyle(Color.Black, 1, Color.FromArgb(128, SystemColors.Highlight));

		public abstract int DrawZOrder { get; }
		public virtual int HitTestZOrder { get { return DrawZOrder; } }

		protected static bool PointsAreNear(PointT mouse, PointT point, VectorT hitTestRadius)
		{
			var dif = mouse.Sub(point);
			return Math.Abs(dif.X) <= hitTestRadius.X && Math.Abs(dif.Y) <= hitTestRadius.Y;
		}

		#region Anchor support

		public virtual IEnumerable<Anchor> DefaultAnchors { get { return EmptyList<Anchor>.Value; } }
		public virtual Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1) { return null; }
		protected Anchor Anchor(Func<PointT> func, int exitAngles = 0xFF) { return new Anchor(this, func, exitAngles); }

		#endregion

		public virtual void OnKeyDown(KeyEventArgs e) { }
		public virtual void OnKeyUp(KeyEventArgs e) { }
		public virtual void OnKeyPress(KeyPressEventArgs e) { }

		public virtual IEnumerable<DoOrUndo> AutoHandleAnchorsChanged() { return null; }
		public virtual DoOrUndo DragMoveAction(HitTestResult htr, VectorT amount) { return null; }
		public virtual DoOrUndo DoubleClickAction(HitTestResult htr) { return null; }
		public virtual DoOrUndo OnShapesDeletedAction(Set<Shape> deleted) { return null; }
		internal void MoveBy(VectorT amount) // used during paste
		{
			var act = DragMoveAction(null, amount);
			if (act != null) act(true);
		}

		public virtual void Dispose() { }

		public abstract string PlainText();
		public virtual bool IsPanel { get { return false; } }
		public virtual DoOrUndo GetClearTextAction() { return null; }
		public virtual DoOrUndo AppendTextAction(string text) { return null; }
	}
}
