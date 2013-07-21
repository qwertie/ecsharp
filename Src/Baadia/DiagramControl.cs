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
	/// This class has the following responsibilities: TODO
	/// </remarks>
	public partial class DiagramControl : LLShapeControl
	{
		public DiagramControl()
		{
			if (!IsDesignTime) // Rx crashes the designer
				SetUpMouseEventHandling();

			_mainLayer = Layers[0]; // predefined
			_selAdorners = AddLayer(false);
			_dragAdorners = AddLayer(false);
			
			LineStyle = new DrawStyle { LineColor = Color.Black, LineWidth = 2, TextColor = Color.Blue, FillColor = Color.Gray };
			BoxStyle = LineStyle.Clone();
			MarkerStyle = new DrawStyle { LineColor = Color.Black, LineWidth = 1, FillColor = Color.Red, TextColor = Color.DarkRed };
			MarkerRadius = 5;
			MarkerType = MarkerPolygon.Circle;
		}

		public DrawStyle LineStyle { get; set; }
		public DrawStyle BoxStyle { get; set; }
		public DrawStyle MarkerStyle { get; set; }
		public float MarkerRadius { get; set; }
		public MarkerPolygon MarkerType { get; set; }

		LLShapeLayer _mainLayer;
		LLShapeLayer _selAdorners;
		LLShapeLayer _dragAdorners;

		void RecreateSelectionAdorners()
		{
			// TODO
			_selAdorners.Shapes.Clear();
			_selAdorners.Invalidate();
		}

		#region Mouse input handling - general

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

			bool _gotAnchor;
			Anchor _startAnchor;
			public Anchor StartAnchor
			{
				get {
					if (!_gotAnchor)
						if (Points.Count > 1) {
							_gotAnchor = true;
							_startAnchor = Control.GetBestAnchor(Points[0].Point, Points[1].AngleMod8);
						}
					return _startAnchor;
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

		const float EraseThreshold1 = 1.333333333f;
		const int EraseThreshold2 = 8;

		private bool AddWithErasure(DragState state, DragPoint dp)
		{
			var points = state.Points;
			if (points.Count < 2)
				return AddIfFarEnough(points, dp);

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

		static readonly DrawStyle MouseLine = new DrawStyle { LineColor = Color.FromArgb(96,Color.Gray), LineWidth = 10 };

		void AnalyzeGesture(DragState state, bool mouseUp)
		{
			// TODO: Analyze on separate thread and maybe even draw on a separate thread.
			var shapes = _dragAdorners.Shapes;
			shapes.Clear();
			shapes.Add(new LLPolyline(MouseLine, state.Points.Select(p => p.Point).AsList()) { 
				ZOrder = 0x100
			});
			Shape shape = null;
			if (state.IsDrag)
			{
				if (state.Points.Count == 1) {
					shape = new Marker(MarkerStyle, state.Points[0].Point, MarkerRadius, MarkerType);
				} else if (state.Points.Count > 1) {
					//TEMP
					var ss = BreakIntoSections(state);
					EliminateTinySections(ss, 10 + (int)(ss.Sum(s => s.Length) * 0.05));
					foreach (Section s in ss)
						shapes.Add(new LLMarker(new DrawStyle { LineColor = Color.Red, FillColor = Color.Gray }, s.StartPt, 5, MarkerPolygon.Circle));

					shape = RecognizeBoxOrLines(state);
				}
			}
			if (mouseUp) {
				shapes.Clear();
				if (shape != null) {
					_shapes.Add(shape);
					ShapesChanged();
				};
			} else if (shape != null)
				shape.AddLLShapes(shapes);

			_dragAdorners.Invalidate();
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

		static VectorT V(float x, float y) { return new VectorT(x, y); }
		static readonly VectorT[] Mod8Vectors = new[] { 
			V(1, 0), V(1, 1),
			V(0, 1), V(-1, 1),
			V(-1, 0), V(-1, -1),
			V(0, -1), V(1, -1),
		};
		
		static int AngleMod8(VectorT v)
		{
			return (int)Math.Round(v.Angle() * (4 / Math.PI)) & 7;
		}

		#endregion

		#region Mouse input handling - RecognizeBoxOrLines and its helper methods

		Shape RecognizeBoxOrLines(DragState state)
		{
			var pts = state.Points;
			// Okay so this is a rectangular recognizer that only sees things at 
			// 45-degree angles.
			List<Section> sections1 = BreakIntoSections(state);
			List<Section> sections2 = new List<Section>(sections1);
			
			// Figure out if a box or a line string is a better interpretation
			EliminateTinySections(sections1, 10);
			LineOrArrow line = InterpretAsPolyline(state, sections1);
			Shape shape = line;
			// Conditions to detect a box:
			// 0. If both endpoints are anchored, a box cannot be formed.
			// continued below...
			EliminateTinySections(sections2, 10 + (int)(sections1.Sum(s => s.Length) * 0.05));
			if (line.ToAnchor == null || line.FromAnchor == null || line.FromAnchor.Equals(line.ToAnchor))
				shape = (Shape)TryInterpretAsBox(sections2, (line.FromAnchor ?? line.ToAnchor) != null) ?? line;
			return shape;
		}

		static int TurnBetween(Section a, Section b)
		{
			return (b.AngleMod8 - a.AngleMod8) & 7;
		}
		private TextBox TryInterpretAsBox(List<Section> sections, bool oneSideAnchored)
		{
			// Conditions to detect a box (continued):
			// 1. If one endpoint is anchored, 4 points are required to confirm 
			//    that the user really does want to create a (non-anchored) box.
			// 2. There are 2 to 4 points.
			// 3. The initial line is vertical or horizontal.
			// 4. The rotation between all adjacent lines is the same, either 90 
			//    or -90 degrees
			// 5. If there are two lines, the endpoint must be down and right of 
			//    the start point
			// 6. The dimensions of the box enclose the first three lines. The 
			//    endpoint of the fourth line, if any, must not be far outside the 
			//    box.
			int minSides = oneSideAnchored ? 4 : 2;
			if (sections.Count >= minSides && sections.Count <= 5) {
				int turn = TurnBetween(sections[0], sections[1]);
				if ((sections[0].AngleMod8 & 1) == 0 && (turn == 2 || turn == 6)) {
					for (int i = 1; i < sections.Count; i++)
						if (TurnBetween(sections[i - 1], sections[i]) != turn)
							return null;
					VectorT dif;
					if (sections.Count > 2 || (dif = sections[1].EndPt.Sub(sections[0].StartPt)).X > 0 && dif.Y > 0) {
						var extents = sections.Take(3).Select(s => s.StartPt.To(s.EndPt).ToBoundingBox()).Union();
						if (sections.Count < 4 || extents.Inflated(20, 20).Contains(sections[3].EndPt)) {
							// Confirmed, we can interpret as a box
							return new TextBox(extents) { Style = BoxStyle };
						}
					}
				}
			}
			return null;
		}

		private LineOrArrow InterpretAsPolyline(DragState state, List<Section> sections)
		{
			var shape = new LineOrArrow { Style = LineStyle };
			shape.FromAnchor = state.StartAnchor;
			LineSegmentT prevLine = new LineSegmentT(), curLine;

			for (int i = 0; i < sections.Count; i++) {
				int angleMod8 = sections[i].AngleMod8;
				var startPt = sections[i].StartPt;
				var endPt = sections[i].EndPt;

				Vector<float> vector = Mod8Vectors[angleMod8];
				Vector<float> perpVector = vector.Rot90();

				bool isStartLine = i == 0;
				bool isEndLine = i == sections.Count - 1;
				if (isStartLine) {
					if (shape.FromAnchor != null)
						startPt = shape.FromAnchor.Point;
				}
				if (isEndLine) {
					if ((shape.ToAnchor = GetBestAnchor(endPt, angleMod8 + 4)) != null)
						endPt = shape.ToAnchor.Point;
					// Also consider forming a closed shape
					else if (shape.Points.Count > 1 
						&& shape.Points[0].Sub(endPt).Length() <= AnchorSnapDistance 
						&& Math.Abs(vector.Cross(shape.Points[1].Sub(shape.Points[0]))) > 0.001f)
						endPt = shape.Points[0];
				}

				if (isStartLine)
					curLine = startPt.To(startPt.Add(vector));
				else {
					curLine = endPt.Sub(vector).To(endPt);
					PointT? itsc = prevLine.ComputeIntersection(curLine, LineType.Infinite);
					if (itsc.HasValue)
						startPt = itsc.Value;
				}

				shape.Points.Add(startPt);

				if (isEndLine) {
					if (isStartLine) {
						Debug.Assert(shape.Points.Count == 1);
						var adjustedStart = startPt.ProjectOntoInfiniteLine(endPt.Sub(vector).To(endPt));
						var adjustedEnd = endPt.ProjectOntoInfiniteLine(curLine);
						if (shape.FromAnchor != null) {
							if (shape.ToAnchor != null) {
								// Both ends anchored => do nothing, allow unusual angle
							} else {
								// Adjust endpoint to maintain angle
								endPt = adjustedEnd;
							}
						} else {
							if (shape.ToAnchor != null)
								// End anchored only => alter start point
								shape.Points[0] = adjustedStart;
							else {
								// Neither end anchored => use average line
								shape.Points[0] = startPt.To(adjustedStart).Midpoint();
								endPt = endPt.To(adjustedEnd).Midpoint();
							}
						}
					}
					shape.Points.Add(endPt);
				}
				prevLine = curLine;
			}
			return shape;
		}

		class Section
		{ 
			public int AngleMod8; 
			public PointT StartPt, EndPt;
			public VectorT Vector() { return EndPt.Sub(StartPt); }
			public int iStart, iEnd; 
			public float Length;
			
			public override string ToString()
			{
				return string.Format("a8={0}, Len={1}, indexes={2}..{3}", AngleMod8, Length, iStart, iEnd); // for debug
			}
		}

		static List<Section> BreakIntoSections(DragState state)
		{
			var list = new List<Section>();
			var pts = state.Points;
			int i = 1, j;
			for (; i < pts.Count; i = j) {
				int angleMod8 = pts[i].AngleMod8;
				float length = pts[i - 1].Point.To(pts[i].Point).Length();
				for (j = i + 1; j < pts.Count; j++) {
					if (pts[j].AngleMod8 != angleMod8)
						break;
					length += pts[j - 1].Point.To(pts[j].Point).Length();
				}
				var startPt = pts[i - 1].Point;
				var endPt = pts[j - 1].Point;
				list.Add(new Section { 
					AngleMod8 = angleMod8, 
					StartPt = startPt, EndPt = endPt, 
					iStart = i - 1, iEnd = j - 1,
					Length = length
				});
			}
			return list;
		}
		static void EliminateTinySections(List<Section> list, int minLineLength)
		{
			// Eliminate tiny sections
			Section cur;
			int i;
			while ((cur = list[i = list.IndexOfMin(s => s.Length)]).Length < minLineLength)
			{
				var prev = list.TryGet(i - 1, null);
				var next = list.TryGet(i + 1, null);
				if (PickMerge(ref prev, cur, ref next)) {
					if (prev != null)
						list[i - 1] = prev;
					if (next != null)
						list[i + 1] = next;
					list.RemoveAt(i);
				} else
					break;
			}

			// Merge adjacent sections that now have the same mod-8 angle
			for (i = 1; i < list.Count; i++) {
				Section s0 = list[i-1], s1 = list[i];
				if (s0.AngleMod8 == s1.AngleMod8) {
					s0.EndPt = s1.EndPt;
					s0.iEnd = s1.iEnd;
					s0.Length += s1.Length;
					list.RemoveAt(i);
					i--;
				}
			}
		}
		static double AngleError(VectorT vec, int angleMod8)
		{
			double dif = vec.Angle() - angleMod8 * (Math.PI / 4);
			dif = MathEx.Mod(dif, 2 * Math.PI);
			if (dif > Math.PI)
				dif = 2 * Math.PI - dif;
			return dif;
		}

		const int NormalMinLineLength = 10;

		static bool PickMerge(ref Section s0, Section s1, ref Section s2)
		{
			if (s0 == null) {
				if (s2 == null)
					return false;
				else {
					s2 = Merged(s1, s2);
					return true;
				}
			} else if (s2 == null) {
				s0 = Merged(s0, s1);
				return true;
			}
			// decide the best way to merge
			double e0Before = AngleError(s0.Vector(), s0.AngleMod8), e0After = AngleError(s1.EndPt.Sub(s0.StartPt), s0.AngleMod8);
			double e2Before = AngleError(s2.Vector(), s2.AngleMod8), e2After = AngleError(s2.EndPt.Sub(s1.StartPt), s2.AngleMod8);
			if (e0Before - e0After > e2Before - e2After) {
				s0 = Merged(s0, s1);
				return true;
			} else {
				s2 = Merged(s1, s2);
				return true;
			}
		}
		static Section Merged(Section s1, Section s2)
		{
			return new Section {
				StartPt = s1.StartPt, EndPt = s2.EndPt,
				iStart = s1.iStart, iEnd = s2.iEnd,
				Length = s1.Length + s2.Length,
				AngleMod8 = AngleMod8(s2.EndPt.Sub(s1.StartPt))
			};
		}
		//static void Merge(Section target, Section s1, Section s2)
		//{
		//    Debug.Assert(target == s1 || target == s2);
		//    target.StartPt = s1.StartPt;
		//    target.iStart = s1.iStart;
		//    target.EndPt = s2.EndPt;
		//    target.iEnd = s2.iEnd;
		//    target.Length = s1.Length + s2.Length;
		//}

		#endregion

		List<Shape> _shapes = new List<Shape>();

		protected void ShapesChanged()
		{
			_mainLayer.Shapes.Clear();
			foreach (Shape shape in _shapes)
				shape.AddLLShapes(_mainLayer.Shapes);
			_mainLayer.Invalidate();
		}

		const int AnchorSnapDistance = 10;

		public Anchor GetBestAnchor(PointT input, int exitAngleMod8 = -1)
		{
			var candidates = 
				from shape in _shapes.OfType<AnchorShape>()
				let anchor = shape.GetNearestAnchor(input, exitAngleMod8)
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
