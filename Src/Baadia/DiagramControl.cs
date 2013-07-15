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
	public partial class DiagramControl : LLShapeControl
	{
		public DiagramControl()
		{
			if (!IsDesignTime) // Rx crashes the designer
			{
				var mouseMove = Observable.FromEventPattern<MouseEventArgs>(this, "MouseMove");
				var lMouseDown = Observable.FromEventPattern<MouseEventArgs>(this, "MouseDown").Where(e => e.EventArgs.Button == MouseButtons.Left);
				var lMouseUp = Observable.FromEventPattern<MouseEventArgs>(this, "MouseUp").Where(e => e.EventArgs.Button == MouseButtons.Left);

				lMouseDown.SelectMany(start =>
				{
					int prevTicks = Environment.TickCount, msec;
					var dragSeq = new DList<DragPoint>();
					var dragSeqWithErasure = new DList<DragPoint>();
					return mouseMove
						.StartWith(start)
						.TakeUntil(lMouseUp)
						.Do(e => {
							prevTicks += (msec = Environment.TickCount - prevTicks);
							var pt = (Point<float>)e.EventArgs.Location.AsLoyc();
							if (dragSeq.Count == 0 || pt.Sub(dragSeq.Last.Point) != Vector<float>.Zero) {
								var dp = new DragPoint(pt, msec, dragSeq);
								dragSeq.Add(dp);
								AddWithErasure(dragSeqWithErasure, dp);
								AnalyzeGesture(dragSeq, dragSeqWithErasure, false);
							}
						}, () => AnalyzeGesture(dragSeq, dragSeqWithErasure, true));
				})
				.Subscribe();
			}
		}

		private void AddWithErasure(DList<DragPoint> dragSeq, DragPoint dp)
		{
			if (dragSeq.Count < 2)
				dragSeq.Add(dp);
			
 			var newSeg = (LineSegment<float>)dragSeq.Last.Point.To(dp.Point);
			// Strategy:
			// 1. Stroke the new segment with a simple rectangle with no endcap.
			//    The rectangle will be a thin box around the point (halfwidth 
			//    is 1.0 .. 1.5)
			var newRect = SimpleStroke(newSeg, 1.33333333f);
			var newRectBB = newRect.ToBoundingBox();

			// 2. Identify the most recent intersection point between this rectangle
			//    (newRect) and the line being drawn. (if there is no such point, 
			//    there is no erasure. Done.)
			// 2b. That intersection point is the one _entering_ the rectangle. Find 
			//    the previous intersection point, the one that exits the rectangle.
			//    this is the beginning of the region to potentially erase.
			var older = dragSeq.ReverseView().AdjacentPairs().Select(pair => pair.B.Point.To(pair.A.Point));
			Point<float> beginning = default(Point<float>);
			bool keepLooking = false;

			int offs = 0;
			for (var e = older.GetEnumerator(); e.MoveNext(); offs++)
			{
				var seg = e.Current;
				var list = FindIntersectionsWith(seg, newRect).ToList();
				if (list.Count != 0) {
					beginning = list.MinOrDefault(p => p.A).B;
					keepLooking = PolygonMath.IsPointInPolygon(newRect, seg.A);
					break;
				} else if (offs == 0) { } // todo: use IsPointInPolygon if itscs unstable

				offs++;
			}

			// 3. Stroke each of the line segments between that intersection point 
			//    and the current mouse location, with a larger halfwidth (e.g. 5px) 
			//    and no endcap, starting with the final segment and working backward.
			//    Build a list of these rectangles, and after adding each one, check
			//    whether all of the previous line segments (in the erasure region) 
			//    are fully within the union of all the stroked polygons. If so, 
			//    then erasure has been detected.


			// 4. Respond to erasure by deleting all the points between there
			//    and here, not including the first or last point.
			// 4b. Consider the short line segment from the first point (the point 
			//    identified in 2b) to its previous point. Change the first 
			//    point to be equal to the intersection between this line segment
			//    and the box generated in step 1, thus shortening the line.
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

		void DetectEraseFlagIncrementally(DList<DragPoint> dragSeq)
		{
			// The user can "back up" the pen simply by moving the mouse or finger 
			// in the reverse direction to retrace his steps. This works as long as
		}

		const int MinDistBetweenDragPoints = 2;

		struct DragPoint
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

		// The drag recognizers take a list of points as input, and produce a shape 
		// and a "pain factor" as output (the shape is null if recognition fails). 
		// In case of ambiguity, the lowest pain factor wins.
		List<Func<IList<DragPoint>, IRecognizerResult>> DragRecognizers;

		void AnalyzeGesture(IList<DragPoint> dragSeq, DList<DragPoint> dragSeqWithErasure, bool mouseUp)
		{
			// TODO: Analyze on separate thread 

			if (IsDrag(dragSeq)) {
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

		//IRecognizerResult RecognizeBoxOrLines(IList<DragPoint> pts)
		//{
		//    // Okay so this is a rectangular recognizer that only sees things at 
		//    // 45-degree angles.
		//    List<LineSegment<int>> segs;
		//    int i = 1, j;
		//    for (; i < pts.Count; i = j) {
		//        int angleMod8 = pts[i].AngleMod8;
		//        for (j = i + 1; j < pts.Count; j++)
		//            if (pts[j].AngleMod8 != angleMod8)
		//                break;
		//        var startPt = pts[i - 1].Point;
		//        var endPt = pts[j - 1]
		//        if (j < pts.Count)
		//    }
		//}

		//void RecognizeErase(IList<DragPoint> pts)
		


	}

	public interface IRecognizerResult
	{
		IEnumerable<LLShape> RealtimeDisplay { get; }
		int Quality { get; }
		void Accept();
	}
}
