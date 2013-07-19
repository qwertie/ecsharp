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
using System.Diagnostics;
using System.Reactive;
using Util.WinForms;
using Loyc.Geometry;
using Coord = System.Single;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

namespace BoxDiagrams
{
	// "Baadia": Boxes And Arrows Diagrammer
	//
	// Future flourishes:
	// - linear gradient brushes (modes: gradient across shape, or gradient across sheet)
	// - sheet background pattern/stretch bitmap
	// - box background pattern/stretch bitmap
	// - snap lines, plus a ruler on top and left to create and remove them
	//   - ruler itself can act as scroll bar
	// - text formatting override for parts of a box

	/// <summary>A control that manages a set of <see cref="Shape"/> objects and 
	/// manages a mouse-based user interface for drawing things.</summary>
	/// <remarks>
	/// This class has the following responsibilities:
	/// - Showing "tooltip" <see cref="LLShape"/>s on mouse-over
	/// - Showing the "adorner" LLShapes for selected Shapes
	/// - Invoking <see cref="IInputRecognizer"/>s for detecting that new shapes 
	///   have been drawn, or that existing shapes have been clicked.
	/// <para/>
	/// 
	/// </remarks>
	public partial class DiagramControl : LLShapeControl
	{
		public DiagramControl()
		{
			if (!IsDesignTime) // Rx crashes the designer
				SetUpMouseEventHandling();
		}

		#region Mouse input handling

		private void SetUpMouseEventHandling()
		{
			var mouseMove = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");
			var lMouseDown = Observable.FromEventPattern<MouseEventArgs>(this, "MouseDown").Where(e => e.EventArgs.Button == MouseButtons.Left);
			var lMouseUp = Observable.FromEventPattern<MouseEventArgs>(this, "MouseUp").Where(e => e.EventArgs.Button == MouseButtons.Left);

			lMouseDown.SelectMany(start => {
				int prevTicks = Environment.TickCount, msec;
				var state = new DragState(this);
				return mouseMove
					.StartWith(start)
					.TakeUntil(lMouseUp)
					.Do(e => {
						prevTicks += (msec = Environment.TickCount - prevTicks);
						var pt = (Point<float>)e.EventArgs.Location.AsLoyc();
						if (state.Points.Count == 0 || pt.Sub(state.Points.Last.Point) != Vector<float>.Zero) {
							var dp = new DragPoint(pt, msec, state.Points);
							state.UnfilteredPoints.Add(dp);
							AddWithErasure(state, dp);
							AnalyzeGesture(state, false);
						}
					}, () => AnalyzeGesture(state, true));
			})
			.Subscribe();
		}

		// TODO optimization: return a cached subset rather than all shapes
		public IEnumerable<Shape> NearbyShapes(PointT mousePos) { return _shapes; }

		/// <summary>Temporary state variables during drag operation</summary>
		public class DragState
		{
			public DragState(DiagramControl c) { Control = c; }
			public DiagramControl Control;
			public Anchor StartAnchor;
			public Shape StartShape;
			public IEnumerable<Shape> NearbyShapes { get { return Control.NearbyShapes(Points.Last.Point); } }
			public DList<DragPoint> Points = new DList<DragPoint>();
			public DList<DragPoint> UnfilteredPoints = new DList<DragPoint>();
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
		}

		public struct DragPoint
		{
			public DragPoint(Point<float> p, int ms, IList<DragPoint> prevPts)
			{
				Point = p;
				MsecSincePrev = (ushort)MathEx.InRange(ms, 0, 65535);
				RootSecPer1000px = MathEx.Sqrt(SecPer1000px(Point, ms, prevPts));
				AngleMod256 = (byte)(prevPts.Count == 0 ? 0 : (int)
					((prevPts[prevPts.Count - 1].Point - Point).Angle() * (128.0 / Math.PI)));
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
			public int AngleMod8 { get { return (AngleMod256 + 15) >> 5; } }
			public float RootSecPer1000px; // Sqrt(seconds per 1000 pixels of movement)
		}

		const float EraseThreshold1 = 1.333333333f;
		const int EraseThreshold2 = 7;

		private bool AddWithErasure(DragState state, DragPoint dp)
		{
			var points = state.Points;
			if (points.Count < 2) {
				if (points.Count == 0 || dp.Point.Sub(points.First.Point).Length() >= MinDistBetweenDragPoints)
					points.Add(dp);
			}

			var newSeg = (LineSegment<float>)points.Last.Point.To(dp.Point);
			// Strategy:
			// 1. Stroke the new segment with a simple rectangle with no endcap.
			//    The rectangle will be a thin box around the point (halfwidth 
			//    is 1.0 .. 1.5)
			var newRect = SimpleStroke(newSeg, EraseThreshold1);
			var newRectBB = newRect.ToBoundingBox();

			// 2. Identify the most recent intersection point between this rectangle
			//    (newRect) and the line being drawn. (if there is no such point, 
			//    there is no erasure. Done.)
			// 2b. That intersection point is the one _entering_ the rectangle. Find 
			//    the previous intersection point, the one that exits the rectangle.
			//    this is the beginning of the region to potentially erase.
			var older = points.ReverseView().AdjacentPairs().Select(pair => pair.B.Point.To(pair.A.Point));
			Point<float> beginning = default(Point<float>);
			bool keepLooking = false;
			int offs = 0;
			var e = older.GetEnumerator();
			for (; e.MoveNext(); offs++)
			{
				var seg = e.Current;
				var list = FindIntersectionsWith(seg, newRect).ToList();
				if (list.Count != 0) {
					beginning = list.MinOrDefault(p => p.A).B;
					if (keepLooking || !PolygonMath.IsPointInPolygon(newRect, seg.A))
						break;
					keepLooking = true;
				} else if (offs == 0) { } // todo: use IsPointInPolygon if itscs unstable
				offs++;
			}
			int iFirst = points.Count - 1 - offs; // index of the first point inside the region (iFirst-1 is outside)
			if (iFirst > 0) {
				// 3. Between here and there, identify the farthest point away from the
				//    new point (dp.Point).
				var region = ((IList<DragPoint>)points).Slice(iFirst);
				int offsFarthest = region.IndexOfMax(p => (p.Point.Sub(dp.Point)).Quadrance());
				int iFarthest = iFirst + offsFarthest;
				// 4. Make sure that all the points between here and there are close to
				//    this line (within, say... 7 pixels). If so, we have erasure.
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

			if ((dp.Point.Sub(points.Last.Point)).Length() >= MinDistBetweenDragPoints) {
				points.Add(dp);
				return true;
			}
			return false;
		}

		public static IEnumerable<Pair<float, Point<float>>> FindIntersectionsWith(LineSegment<float> seg, IEnumerable<Point<float>> polygon)
		{
			return new FIWEnumerable { Poly = polygon, Seg = seg };
		}
		class FIWEnumerable : IEnumerable<Pair<float, Point<float>>>
		{
			internal IEnumerable<Point<float>> Poly; 
			internal LineSegment<float> Seg;
			public IEnumerator<Pair<float, Point<float>>> GetEnumerator() { return FindIntersectionsWith(Seg, Poly.GetEnumerator()); }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}
		public static IEnumerator<Pair<float, Point<float>>> FindIntersectionsWith(LineSegment<float> seg, IEnumerator<Point<float>> e)
		{
			int i = 0;
			if (e.MoveNext()) {
				Point<float> first = e.Current, prev = first;
				while (e.MoveNext()) {
					Point<float> cur = e.Current;
					float frac;
					if (seg.ComputeIntersection(prev.To(cur), out frac))
						yield return Pair.Create(frac, seg.PointAlong(frac));
					prev = cur;
					i++;
				}
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

		const int MinDistBetweenDragPoints = 2;

		// The drag recognizers take a list of points as input, and produce a shape 
		// and a "pain factor" as output (the shape is null if recognition fails). 
		// In case of ambiguity, the lowest pain factor wins.
		List<Func<IList<DragPoint>, IRecognizerResult>> DragRecognizers;

		void AnalyzeGesture(DragState state, bool mouseUp)
		{
			// TODO: Analyze on separate thread 
			if (state.IsDrag) {
				var results = new List<Pair<Shape,int>>();
				foreach (var rec in DragRecognizers) {
				}
			}
		}

		static bool IsDrag(IList<DragPoint> dragSeq)
		{
			Point<float> first = dragSeq[0].Point;
			Size ds = SystemInformation.DragSize;
			return dragSeq.Any(p => {
				var delta = p.Point.Sub(first);
				return Math.Abs(delta.X) > ds.Width || Math.Abs(delta.Y) > ds.Height;
			});
		}

		IRecognizerResult RecognizeBoxOrLines(DragState state)
		{
			var pts = state.Points;
			// Okay so this is a rectangular recognizer that only sees things at 
			// 45-degree angles.
			var segs = new List<Pair<Point<float>, Vector<float>>>();
			int i = 1, j;
			for (; i < pts.Count; i = j) {
				int angleMod8 = pts[i].AngleMod8;
				for (j = i + 1; j < pts.Count; j++)
					if (pts[j].AngleMod8 != angleMod8)
						break;
				bool oneLine = (i == 1 && j == pts.Count);
				var startPt = pts[i - 1].Point;
				var endPt = pts[j - 1].Point;
				Vector<float> vector = PointMath.PolarToVector(1.0f, angleMod8 * (Math.PI / 4));
				segs.Add(Pair.Create(j == pts.Count ? endPt : startPt, vector));
			}
			throw new NotImplementedException();
		}

		#endregion

		MSet<Shape> _shapes = new MSet<Shape>();

		const int AnchorSnapDistance = 10;

		Anchor GetBestAnchor(PointT input)
		{
			var candidates = 
				from shape in _shapes.OfType<AnchorShape>()
				let anchor = shape.GetNearestAnchor(input)
				where anchor.Point.Sub(input).Quadrance() <= MathEx.Square(AnchorSnapDistance)
				select anchor;
			return candidates.MinOrDefault(a => a.Point.Sub(input).Quadrance());
		}
	}

	public interface IRecognizerResult
	{
		IEnumerable<LLShape> RealtimeDisplay { get; }
		int Quality { get; }
		void Accept();
	}
}
