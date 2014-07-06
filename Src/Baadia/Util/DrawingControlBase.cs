using BoxDiagrams;
using Loyc;
using Loyc.Collections;
using Loyc.Geometry;
using Loyc.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

namespace Util.WinForms
{
	public enum SelType
	{
		No, Yes, Partial
	}

	/*public class WidgetLayer : LLShapeLayer, IListChanging<ShapeWidget>, IChildOf<WidgetControl>
	{
		public WidgetLayer(LLShapeControl control, bool? useAlpha = null) : base(control, useAlpha)
		{
			Widgets = new ShapeWidgetList(this);
		}
	
		public readonly ShapeWidgetList Widgets;

		void IListChanging<ShapeWidget>.OnListChanging(IListSource<ShapeWidget> sender, ListChangeInfo<ShapeWidget> e)
		{
		}

		public void OnBeingAdded(WidgetControl parent)
		{
			throw new NotImplementedException();
		}

		public void OnBeingRemoved(WidgetControl parent)
		{
			throw new NotImplementedException();
		}
	}

	public interface IShapeWidget : IChildOf<WidgetLayer>
	{
		HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel);
		void OnKeyDown(KeyEventArgs e) { }
		void OnKeyUp(KeyEventArgs e) { }
		void OnKeyPress(KeyPressEventArgs e) { }
	}
	public abstract class ShapeWidget : IShapeWidget
	{
		public virtual void OnBeingAdded(WidgetLayer parent) { }
		public virtual void OnBeingRemoved(WidgetLayer parent) { }
	}

	public class ShapeWidgetList : OwnedChildList<WidgetLayer, ShapeWidget>
	{
		public ShapeWidgetList(WidgetLayer parent) : base(parent) { }
	}

	public class ScrollThumb : ShapeWidget
	{
	}*/

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


	/// <summary>Base class for a control that supports drawing and zooming/scrolling</summary>
	public abstract class DrawingControlBase : LLShapeControl
	{
		public DrawingControlBase()
		{
			if (!IsDesignTime) // Rx crashes the designer
				SetUpMouseEventHandling();
		}


		private void SetUpMouseEventHandling()
		{
			var mouseMove = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");
			var lMouseDown = Observable.FromEventPattern<MouseEventArgs>(this, "MouseDown").Where(e => e.EventArgs.Button == MouseButtons.Left);
			var lMouseUp = Observable.FromEventPattern<MouseEventArgs>(this, "MouseUp").Where(e => e.EventArgs.Button == MouseButtons.Left);

			lMouseDown.SelectMany(start =>
			{
				int prevTicks = Environment.TickCount, msec;
				var state = MouseClickStarted(start.EventArgs);
				_dragState = state;
				Focus();
				return mouseMove
					.StartWith(start)
					.TakeUntil(lMouseUp)
					.Do(e =>
					{
						if (!state.IsComplete)
						{
							prevTicks += (msec = Environment.TickCount - prevTicks);
							var pt = (Point<float>)e.EventArgs.Location.AsLoyc();
							if (state.Points.Count == 0 || pt.Sub(state.Points.Last.Point) != Vector<float>.Zero)
							{
								var dp = new DragPoint(pt, msec, state.Points);
								state.UnfilteredPoints.Add(dp);
								AddFiltered(state, dp);
								AnalyzeGesture(state, false);
							}
						}
					}, () =>
					{
						if (!state.IsComplete)
							AnalyzeGesture(state, true);
						_dragState = null;
					});
			})
			.Subscribe();
		}

		protected virtual void AnalyzeGesture(DragState state, bool mouseUp)
		{
		}

		protected DragState _dragState; // beware of multitouch

		protected virtual DragState MouseClickStarted(MouseEventArgs e)
		{
			return new DragState(this, e);
		}

		/// <summary>Temporary state variables during click or drag operation</summary>
		public class DragState
		{
			public DragState(DrawingControlBase c, MouseEventArgs down) { Control = c; Down = down; }
			public DrawingControlBase Control;
			public DList<DragPoint> Points = new DList<DragPoint>();
			public DList<DragPoint> UnfilteredPoints = new DList<DragPoint>();
			public MouseEventArgs Down; // Initial mouse down event
			public int Clicks { get { return Down.Clicks; } } // 1 for single click, 2 for double-click
			
			/// <summary>Gets the total distance that the mouse moved since the button press.</summary>
			public Vector<float> TotalDelta { 
				get { 
					var pts = UnfilteredPoints; 
					return pts.Last.Point.Sub(pts.First.Point);
				}
			}
			/// <summary>Gets the distance that the mouse moved since the previous event.</summary>
			public Vector<float> Delta {
				get {
					var pts = UnfilteredPoints;
					return pts.Count <= 1 ? Vector<float>.Zero : pts.Last.Point.Sub(pts[pts.Count - 2].Point);
				}
			}

			int _isDragState = 0; // -1 if dragging
			public bool IsDrag
			{
				get {
					if (_isDragState <= -1)
						return true;
					if (_isDragState == UnfilteredPoints.Count)
						return false;
					_isDragState = UnfilteredPoints.Count;
					
					if (UnfilteredPoints.Count < 2)
						return false;
					Point<float> first = UnfilteredPoints[0].Point;
					Size ds = SystemInformation.DragSize;
					if (UnfilteredPoints.Any(p => {
						var delta = p.Point.Sub(first);
						return Math.Abs(delta.X) > ds.Width || Math.Abs(delta.Y) > ds.Height;
					})) {
						_isDragState = -1;
						return true;
					}
					return false;
				}
			}

			public bool IsComplete; // or cancelled. Causes further dragging to be ignored.
		}

		public struct DragPoint
		{
			public DragPoint(Point<float> p, int ms, IList<DragPoint> prevPts)
			{
				Point = p;
				MsecSincePrev = (ushort)MathEx.InRange(ms, 0, 65535);
				RootSecPer1000px = MathEx.Sqrt(SecPer1000px(Point, ms, prevPts));
				AngleMod256 = (byte)(prevPts.Count == 0 ? 0 : (int)
					((Point.Sub(prevPts[prevPts.Count - 1].Point)).Angle() * (128.0 / Math.PI)));
			}

			static float SecPer1000px(Point<float> next, int ms, IList<DragPoint> prevPts)
			{
				// Gather up 100ms+ worth of previous points
				float dist = 0;
				for (int i = prevPts.Count - 1; i >= 0; i--) {
					var dif = next - (next = prevPts[i].Point);
					dist += dif.Length();
					if (ms > 100)
						break;
					ms += prevPts[i].MsecSincePrev;
				}
				if (dist < 1) dist = 1;
				return (float)ms / dist;
			}
			public readonly Point<float> Point;
			public readonly ushort MsecSincePrev;
			// Angle between this point and the previous point,
			// 0..256 for 0..360; 0=right, 64=down
			public byte AngleMod256;
			public int AngleMod8 { get { return ((AngleMod256 + 15) >> 5) & 7; } }
			public float RootSecPer1000px; // Sqrt(seconds per 1000 pixels of movement)
			public override string ToString()
			{
				return string.Format("{0} m256={1} m8={2}", Point, AngleMod256, AngleMod8); // for debugging
			}
		}

		const float EraseThreshold1 = 1.5f;
		const int EraseThreshold2 = 10;
		const int MinDistBetweenDragPoints = 2;

		/// <summary>Adds a point to DragState with a filtering algorithm. The 
		/// default filtering algorithm supports "backing up the mouse" for erasure.</summary>
		/// <returns>true if a point was added, false if not.</returns>
		protected virtual bool AddFiltered(DragState state, DragPoint dp)
		{
			var points = state.Points;
			if (points.Count < 2)
				return AddIfFarEnough(points, dp);

			var newSeg = (LineSegment<float>)points.Last.Point.To(dp.Point);
			// Strategy:
			// 1. Stroke the new segment with a simple rectangle with no endcap.
			//    The rectangle will be a thin box around the point (halfwidth 
			//    is 1..2)
			var newRect = SimpleStroke(newSeg, EraseThreshold1);
			var newRectBB = newRect.ToBoundingBox();

			// 2. Identify the most recent intersection point between this rectangle
			//    (newRect) and the line being drawn. (if there is no such point, 
			//    there is no erasure. Done.)
			// 2b. That intersection point is the one _entering_ the rectangle. Find 
			//    the previous intersection point, the one that exits the rectangle.
			//    this is the beginning of the region to potentially erase.
			var older = points.Reverse().AdjacentPairs().Select(pair => pair.B.Point.To(pair.A.Point));
			Point<float> beginning = default(Point<float>);
			bool keepLooking = false;
			int offs = 0;
			var e = older.GetEnumerator();
			for (; e.MoveNext(); offs++)
			{
				var seg = e.Current;
				var list = FindIntersectionsWith(seg, newRect, true).ToList();
				if (list.Count != 0) {
					var min = list.MinOrDefault(p => p.A);
					beginning = min.B;
					if (!(offs == 0 && min.A == 1)) {
						if (keepLooking || !PolygonMath.IsPointInPolygon(newRect, seg.A))
							break;
						keepLooking = true;
					}
				} else if (offs == 0) { } // todo: use IsPointInPolygon if itscs unstable
			}

			int iFirst = points.Count - 1 - offs; // index of the first point inside the region (iFirst-1 is outside)
			if (iFirst > 0) {
				// 3. Between here and there, identify the farthest point away from the
				//    new point (dp.Point).
				var region = ((IList<DragPoint>)points).Slice(iFirst);
				int offsFarthest = region.IndexOfMax(p => (p.Point.Sub(dp.Point)).Quadrance());
				int iFarthest = iFirst + offsFarthest;
				// 4. Make sure that all the points between here and there are close to
				//    this line (within, say... 8 pixels). If so, we have erasure.
				var seg = dp.Point.To(points[iFarthest].Point);
				if (region.All(p => p.Point.DistanceTo(seg) < EraseThreshold2)) {
					// 5. Respond to erasure by deleting all the points between there
					//    and here, not including the first or last point.
					// 5b. Consider adding the intersection point found in step 2b to
					//    the point list, before adding the new point.
					points.Resize(iFirst);
					if (points.Count == 0 || (points.Last.Point.Sub(beginning)).Length() >= MinDistBetweenDragPoints)
						points.Add(new DragPoint(beginning, 10, points));
				}
			}

			return AddIfFarEnough(points, dp);
		}
		static bool AddIfFarEnough(DList<DragPoint> points, DragPoint dp)
		{
			if (points.Count == 0 || points.Last.Point.Sub(dp.Point).Quadrance() >= MinDistBetweenDragPoints * MinDistBetweenDragPoints) {
				points.Add(dp);
				return true;
			}
			return false;
		}

		public static IEnumerable<Pair<float, Point<float>>> FindIntersectionsWith(LineSegment<float> seg, IEnumerable<Point<float>> lines, bool isClosedShape)
		{
			return new FIWEnumerable { Poly = lines, Seg = seg, Closed = isClosedShape };
		}
		class FIWEnumerable : IEnumerable<Pair<float, Point<float>>>
		{
			internal IEnumerable<Point<float>> Poly; 
			internal LineSegment<float> Seg;
			internal bool Closed;
			public IEnumerator<Pair<float, Point<float>>> GetEnumerator() { return FindIntersectionsWith(Seg, Poly.GetEnumerator(), Closed); }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}
		public static IEnumerator<Pair<float, Point<float>>> FindIntersectionsWith(LineSegment<float> seg, IEnumerator<Point<float>> e, bool isClosedShape)
		{
			int i = 0;
			if (e.MoveNext()) {
				Point<float> first = e.Current, prev = first;
				float frac;
				while (e.MoveNext()) {
					Point<float> cur = e.Current;
					if (seg.ComputeIntersection(prev.To(cur), out frac))
						yield return Pair.Create(frac, seg.PointAlong(frac));
					prev = cur;
					i++;
				}
				if (i > 1 && isClosedShape)
					if (seg.ComputeIntersection(prev.To(first), out frac))
						yield return Pair.Create(frac, seg.PointAlong(frac));
			}
		}
		private List<Point<float>> SimpleStroke(LineSegment<float> newSeg, float halfWidth)
		{
 			var unit = newSeg.Vector().Normalized();
			var perp = unit.Rot90() * halfWidth;
			return new List<Point<float>> { 
				newSeg.A.Add(perp),
				newSeg.A.Add(-perp),
				newSeg.B.Add(-perp),
				newSeg.B.Add(perp)
			};
		}
	}
}
