using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc.Collections;
using Loyc.Geometry;
using Loyc.Math;
using Util.UI;
using Util.WinForms;
using Coord = System.Single;
using LineSegmentT = Loyc.Geometry.LineSegment<float>;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

namespace BoxDiagrams
{
	class DiagramGestureAnalyzer : GestureAnalyzer
	{
		DiagramControl _control;
		public new DiagramControl Control { get { return _control; } }
		public DiagramDocument _doc { get { return _control.Document; } }

		public DiagramGestureAnalyzer(DiagramControl control) : base(control)
		{
			_control = control;
		}

		public class DragState : Util.WinForms.DragState
		{
			public new DiagramControl Control;
			public readonly Matrix _inputTransform;

			public DragState(DiagramGestureAnalyzer ga, MouseEventArgs down)
				: base(ga, down)
			{
				Control = ga.Control;
				_inputTransform = Control.InputTransform;
				_points = ToShapeSpace(MousePoints);
				_unfilteredPoints = ToShapeSpace(UnfilteredMousePoints);
			}
			public Util.UI.UndoStack UndoStack { get { return Control.Document.UndoStack; } }
			public VectorT HitTestRadius { get { return Control.HitTestRadius; } }
			public IEnumerable<Shape> NearbyShapes { get { return Control.ShapesOnScreen(MousePoints.Last.Point); } }

			IListSource<DragPoint> _points, _unfilteredPoints;
			public IListSource<DragPoint> Points { get { return _points; } }
			public IListSource<DragPoint> UnfilteredPoints { get { return _unfilteredPoints; } }
			public PointT FirstPoint { get { return _inputTransform.Transform(UnfilteredMousePoints.First.Point); } }
			public IListSource<DragPoint> ToShapeSpace(IListSource<DragPoint> mousePoints)
			{
				return mousePoints.Select(dp => new DragPoint(dp, _inputTransform));
			}
			public Vector<float> TotalDelta
			{
				get { return _inputTransform.Transform(TotalMouseDelta); }
			}

			bool _gotAnchor;
			Anchor _startAnchor;
			public Anchor StartAnchor
			{
				get {
					if (!_gotAnchor)
						if (Points.Count > 1)
						{
							_gotAnchor = true;
							_startAnchor = Control.GetBestAnchor(Points[0].Point, Points[1].AngleMod8);
						}
					return _startAnchor;
				}
			}

			public HitTestResult ClickedShape;
		}

		#region Mouse input handling - general
		// The base class gathers mouse events and calls AnalyzeGesture()

		protected override bool AddFiltered(Util.WinForms.DragState state_, DragPoint dp)
		{
			DragState state = (DragState)state_;
			if (state.ClickedShape != null && state.ClickedShape.AllowsDrag)
				return false; // gesture recognition is off
			return base.AddFiltered(state, dp);
		}

		protected override Util.WinForms.DragState MouseClickStarted(MouseEventArgs e)
		{
			var htresult = Control.HitTest((PointT)e.Location.AsLoyc());
			if (htresult != null && htresult.AllowsDrag
				&& !Control.SelectedShapes.Contains(htresult.Shape)
				&& (System.Windows.Forms.Control.ModifierKeys & Keys.Control) == 0
				&& htresult.Shape is Shape)
				Control.ClickSelect(htresult.Shape as Shape);
			return new DragState(this, e)
			{
				ClickedShape = htresult,
			};
		}

		private readonly DiagramDrawStyle SelectorBoxStyle = new DiagramDrawStyle
		{
			Name = "(Temporary selection indicator)",
			LineColor = Shape.SelAdornerStyle.LineColor,
			LineStyle = DashStyle.Dash,
			FillColor = Shape.SelAdornerStyle.FillColor,
			LineWidth = Shape.SelAdornerStyle.LineWidth
		};

		protected override void AnalyzeGesture(Util.WinForms.DragState state_, bool mouseUp)
		{
			// TODO: Analyze on separate thread and maybe even draw on a separate thread.
			//       Otherwise, on slow computers, mouse input may be missed or laggy due to drawing/analysis
			DragState state = (DragState)state_;
			var adorners = Control.DragAdornerShapes;
			adorners.Clear();
			Shape newShape = null;
			IEnumerable<Shape> eraseSet = null;

			if (state.IsDrag)
			{
				if (state.ClickedShape != null && state.ClickedShape.AllowsDrag)
					HandleShapeDrag(state);
				else
				{
					List<PointT> simplified;
					bool cancel;
					eraseSet = RecognizeScribbleForEraseOrCancel(state, out cancel, out simplified);
					if (eraseSet != null)
					{
						ShowEraseDuringDrag(state, adorners, eraseSet, simplified, cancel);
					}
					else
					{
						bool potentialSelection = false;
						newShape = DetectNewShapeDuringDrag(state, adorners, out potentialSelection);
						if (potentialSelection)
						{
							var selecting = ShapesInside(newShape.BBox).ToList();
							if (selecting.Count != 0)
								newShape.Style = SelectorBoxStyle;
						}
					}
				}
			}

			if (mouseUp)
			{
				adorners.Clear();
				HandleMouseUp(state, newShape, eraseSet);
			}
			else if (newShape != null)
			{
				newShape.AddLLShapesTo(adorners);
				newShape.Dispose();
			}

			Control.DragAdornersChanged();
		}

		private void HandleShapeDrag(DragState state)
		{
			_doc.UndoStack.UndoTentativeAction();

			var movingShapes = Control.SelectedShapes;
			var panels = Control.SelectedShapes.Where(s => s.IsPanel);
			if (panels.Any() && (Control.SelectedShapes.Count > 1 ||
				state.ClickedShape.MouseCursor == Cursors.SizeAll))
			{
				// Also move shapes that are inside the panel
				movingShapes = Control.SelectedShapes.Clone();
				foreach (var panel in panels)
					movingShapes.AddRange(ShapesInsidePanel(panel));
			}

			if (movingShapes.Count <= 1)
			{
				var shape = state.ClickedShape.Shape;
				if (shape is Shape)
				{
					DoOrUndo action = ((Shape)shape).DragMoveAction(state.ClickedShape, state.TotalDelta);
					if (action != null)
					{
						_doc.UndoStack.DoTentatively(action);
						AutoHandleAnchorsChanged();
					}
				}
			}
			else
			{
				foreach (Shape shape in movingShapes)
				{
					DoOrUndo action = shape.DragMoveAction(state.ClickedShape, state.TotalDelta);
					if (action != null)
						_doc.UndoStack.DoTentatively(action);
				}
				AutoHandleAnchorsChanged();
			}
		}

		private IEnumerable<Shape> ShapesInsidePanel(Shape panel) { return ShapesInside(panel.BBox, panel); }
		private IEnumerable<Shape> ShapesInside(BoundingBox<Coord> bbox, Shape panel = null)
		{
			foreach (var shape in _doc.Shapes)
			{
				if (shape != panel && bbox.Contains(shape.BBox))
					yield return shape;
			}
		}

		private void AutoHandleAnchorsChanged()
		{
			foreach (var shape in _doc.Shapes)
			{
				var changes = shape.AutoHandleAnchorsChanged();
				if (changes != null)
					foreach (var change in changes)
						_doc.UndoStack.DoTentatively(change);
			}
		}

		private void HandleMouseUp(DragState state, Shape newShape, IEnumerable<Shape> eraseSet)
		{
			if (!state.IsDrag)
			{
				if (state.Clicks >= 2)
				{
					if (Control.SelectedShapes.Count != 0)
					{
						var htr = state.ClickedShape;
						foreach (var shape in Control.SelectedShapes)
						{
							DoOrUndo action = shape.DoubleClickAction(htr.Shape == shape ? htr : null);
							if (action != null)
								_doc.UndoStack.Do(action, false);
						}
						_doc.UndoStack.FinishGroup();
					}
					else
					{
						// Create marker shape
						newShape = new Marker(Control.BoxStyle, state.FirstPoint, Control.MarkerRadius, Control.MarkerType);
					}
				}
				else
				{
					Control.ClickSelect(state.ClickedShape != null ? state.ClickedShape.Shape as Shape : null);
					Control._lastClickLocation = state.FirstPoint;
				}
			}

			_doc.UndoStack.AcceptTentativeAction(); // if any
			Control._partialSelShape = null;
			if (newShape != null)
			{
				if (newShape.Style == SelectorBoxStyle)
					SelectByBox(newShape.BBox);
				else
					Control.AddShape(newShape);
			}
			if (eraseSet != null)
				Control.DeleteShapes(new Set<Shape>(eraseSet));
			_doc.MarkPanels();
		}

		private void SelectByBox(BoundingBox<Coord> bbox)
		{
			Control.SelectedShapes.Clear();
			Control.SelectedShapes.AddRange(ShapesInside(bbox));
			Control.RecreateSelectionAdorners();
		}

		private void ShowEraseDuringDrag(DragState state, MSet<LLShape> adorners, IEnumerable<Shape> eraseSet, List<PointT> simplified, bool cancel)
		{
			DiagramControl.EraseLineStyle.LineColor = Color.FromArgb(128, Control.BackColor);
			var eraseLine = new LLPolyline(DiagramControl.EraseLineStyle, simplified);
			adorners.Add(eraseLine);

			if (cancel)
			{
				eraseLine.Style = Control.LineStyle;
				Control.BeginRemoveAnimation(adorners);
				adorners.Clear();
				state.IsComplete = true;
			}
			else
			{
				// Show which shapes are erased by drawing them in the background color
				foreach (Shape s in eraseSet)
				{
					Shape s_ = s.Clone();
					s_.Style = (DiagramDrawStyle)s.Style.Clone();
					s_.Style.FillColor = s_.Style.LineColor = s_.Style.TextColor = Color.FromArgb(192, Control.BackColor);
					// avoid an outline artifact, in which color from the edges of the 
					// original shape bleeds through by a variable amount that depends 
					// on subpixel offsets.
					s_.Style.LineWidth++;
					s_.AddLLShapesTo(adorners);
				}
			}
		}
		private Shape DetectNewShapeDuringDrag(DragState state, MSet<LLShape> adorners, out bool potentialSelection)
		{
			potentialSelection = false;
			Shape newShape = null;
			adorners.Add(new LLPolyline(DiagramControl.MouseLineStyle, state.Points.Select(p => p.Point).AsList()) { ZOrder = 0x100 });

			if (state.Points.Count == 1)
			{
				newShape = new Marker(Control.BoxStyle, state.FirstPoint, Control.MarkerRadius, Control.MarkerType);
			}
			else if (state.MousePoints.Count > 1)
			{
#if DEBUG
				List<Section> ss = BreakIntoSections(state);
				EliminateTinySections(ss, 10 + (int)(ss.Sum(s => s.LengthPx) * 0.05));
				foreach (Section s in ss)
					adorners.Add(new LLMarker(new DrawStyle { LineColor = Color.Gainsboro, FillColor = Color.Gray }, s.StartSS, 5, MarkerPolygon.Circle));
#endif

				newShape = RecognizeBoxOrLines(state, out potentialSelection);
			}
			return newShape;
		}

		Timer _cancellingTimer = new Timer { Interval = 30 };

		static bool IsDrag(IList<DragPoint> dragSeq)
		{
			Point<float> first = dragSeq[0].Point;
			Size ds = SystemInformation.DragSize;
			return dragSeq.Any(p =>
			{
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

		Shape RecognizeBoxOrLines(DragState state, out bool potentialSelection)
		{
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
			EliminateTinySections(sections2, 10 + (int)(sections1.Sum(s => s.LengthPx) * 0.05));
			if (line.ToAnchor == null || line.FromAnchor == null || line.FromAnchor.Equals(line.ToAnchor))
				shape = (Shape)TryInterpretAsBox(sections2, (line.FromAnchor ?? line.ToAnchor) != null, out potentialSelection) ?? line;
			else
				potentialSelection = false;
			return shape;
		}

		static int TurnBetween(Section a, Section b)
		{
			return (b.AngleMod8 - a.AngleMod8) & 7;
		}
		private TextBox TryInterpretAsBox(List<Section> sections, bool oneSideAnchored, out bool potentialSelection)
		{
			potentialSelection = false;
			// Conditions to detect a box (continued):
			// 1. If one endpoint is anchored, 4 sides are required to confirm 
			//    that the user really does want to create a (non-anchored) box.
			// 2. There are 2 to 4 points.
			// 3. The initial line is vertical or horizontal.
			// 4. The rotation between all adjacent lines is the same, either 90 
			//    or -90 degrees
			// 5. If there are two lines, the endpoint must be down and right of 
			//    the start point (this is also a potential selection box)
			// 6. The dimensions of the box enclose the first three lines. The 
			//    endpoint of the fourth line, if any, must not be far outside the 
			//    box.
			int minSides = oneSideAnchored ? 4 : 2;
			if (sections.Count >= minSides && sections.Count <= 5)
			{
				int turn = TurnBetween(sections[0], sections[1]);
				if ((sections[0].AngleMod8 & 1) == 0 && (turn == 2 || turn == 6))
				{
					for (int i = 1; i < sections.Count; i++)
						if (TurnBetween(sections[i - 1], sections[i]) != turn)
							return null;

					VectorT dif;
					if (sections.Count == 2)
						potentialSelection = (dif = sections[1].EndSS.Sub(sections[0].StartSS)).X > 0 && dif.Y > 0;
					if (sections.Count > 2 || potentialSelection)
					{
						var tolerance = Control.InputTransform.Transform(new VectorT(20, 20)).Abs();
						var extents = sections.Take(3).Select(s => s.StartSS.To(s.EndSS).ToBoundingBox()).Union();
						if (sections.Count < 4 || extents.Inflated(tolerance.X, tolerance.Y).Contains(sections[3].EndSS))
						{
							// Confirmed, we can interpret as a box
							return new TextBox(extents) { Style = Control.BoxStyle };
						}
					}
				}
			}
			return null;
		}

		private LineOrArrow InterpretAsPolyline(DragState state, List<Section> sections)
		{
			var shape = new LineOrArrow { Style = Control.LineStyle };
			shape.FromAnchor = state.StartAnchor;
			LineSegmentT prevLine = new LineSegmentT(), curLine;

			for (int i = 0; i < sections.Count; i++)
			{
				int angleMod8 = sections[i].AngleMod8;
				var startSS = sections[i].StartSS;
				var endSS = sections[i].EndSS;

				Vector<float> vector = Mod8Vectors[angleMod8];
				Vector<float> perpVector = vector.Rot90();

				bool isStartLine = i == 0;
				bool isEndLine = i == sections.Count - 1;
				if (isStartLine)
				{
					if (shape.FromAnchor != null)
						startSS = shape.FromAnchor.Point;
				}
				if (isEndLine)
				{
					if ((shape.ToAnchor = Control.GetBestAnchor(endSS, angleMod8 + 4)) != null)
						endSS = shape.ToAnchor.Point;
					// Also consider forming a closed shape
					else if (shape.Points.Count > 1
						&& shape.Points[0].Sub(endSS).Length() <= DiagramControl.AnchorSnapDistance
						&& Math.Abs(vector.Cross(shape.Points[1].Sub(shape.Points[0]))) > 0.001f)
						endSS = shape.Points[0];
				}

				if (isStartLine)
					curLine = startSS.To(startSS.Add(vector));
				else
				{
					curLine = endSS.Sub(vector).To(endSS);
					PointT? itsc = prevLine.ComputeIntersection(curLine, LineType.Infinite);
					if (itsc.HasValue)
						startSS = itsc.Value;
				}

				shape.Points.Add(startSS);

				if (isEndLine)
				{
					if (isStartLine)
					{
						Debug.Assert(shape.Points.Count == 1);
						var adjustedStart = startSS.ProjectOntoInfiniteLine(endSS.Sub(vector).To(endSS));
						var adjustedEnd = endSS.ProjectOntoInfiniteLine(curLine);
						if (shape.FromAnchor != null)
						{
							if (shape.ToAnchor != null)
							{
								// Both ends anchored => do nothing, allow unusual angle
							}
							else
							{
								// Adjust endpoint to maintain angle
								endSS = adjustedEnd;
							}
						}
						else
						{
							if (shape.ToAnchor != null)
								// End anchored only => alter start point
								shape.Points[0] = adjustedStart;
							else
							{
								// Neither end anchored => use average line
								shape.Points[0] = startSS.To(adjustedStart).Midpoint();
								endSS = endSS.To(adjustedEnd).Midpoint();
							}
						}
					}
					shape.Points.Add(endSS);
				}
				prevLine = curLine;
			}

			shape.FromArrow = Control.FromArrow;
			shape.ToArrow = Control.ToArrow;

			return shape;
		}

		/// <summary>Used during gesture recognition to represent a section of 
		/// mouse input that is being interpreted as single a line segment.</summary>
		class Section
		{
			public int AngleMod8;
			public PointT StartSS, EndSS; // shape space
			public VectorT Vector() { return EndSS.Sub(StartSS); }
			public int iStart, iEnd;
			public float LengthPx;

			public override string ToString()
			{
				return string.Format("a8={0}, Len={1}, indexes={2}..{3}", AngleMod8, LengthPx, iStart, iEnd); // for debug
			}
		}

		static List<Section> BreakIntoSections(DragState state)
		{
			var list = new List<Section>();
			var pts = state.MousePoints;
			int i = 1, j;
			for (; i < pts.Count; i = j)
			{
				int angleMod8 = pts[i].AngleMod8;
				float length = pts[i - 1].Point.To(pts[i].Point).Length();
				for (j = i + 1; j < pts.Count; j++)
				{
					if (pts[j].AngleMod8 != angleMod8)
						break;
					length += pts[j - 1].Point.To(pts[j].Point).Length();
				}
				var startPt = pts[i - 1].Point;
				var endPt = pts[j - 1].Point;
				list.Add(new Section
				{
					AngleMod8 = angleMod8,
					StartSS = state._inputTransform.Transform(startPt),
					EndSS = state._inputTransform.Transform(endPt),
					iStart = i - 1,
					iEnd = j - 1,
					LengthPx = length
				});
			}
			return list;
		}
		static void EliminateTinySections(List<Section> list, int minLineLengthPx)
		{
			// Eliminate tiny sections
			Section cur;
			int i;
			while ((cur = list[i = list.IndexOfMin(s => s.LengthPx)]).LengthPx < minLineLengthPx)
			{
				var prev = list.TryGet(i - 1, null);
				var next = list.TryGet(i + 1, null);
				if (PickMerge(ref prev, cur, ref next))
				{
					if (prev != null)
						list[i - 1] = prev;
					if (next != null)
						list[i + 1] = next;
					list.RemoveAt(i);
				}
				else
					break;
			}

			// Merge adjacent sections that now have the same mod-8 angle
			for (i = 1; i < list.Count; i++)
			{
				Section s0 = list[i - 1], s1 = list[i];
				if (s0.AngleMod8 == s1.AngleMod8)
				{
					s0.EndSS = s1.EndSS;
					s0.iEnd = s1.iEnd;
					s0.LengthPx += s1.LengthPx;
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
			if (s0 == null)
			{
				if (s2 == null)
					return false;
				else
				{
					s2 = Merged(s1, s2);
					return true;
				}
			}
			else if (s2 == null)
			{
				s0 = Merged(s0, s1);
				return true;
			}
			// decide the best way to merge
			double e0Before = AngleError(s0.Vector(), s0.AngleMod8), e0After = AngleError(s1.EndSS.Sub(s0.StartSS), s0.AngleMod8);
			double e2Before = AngleError(s2.Vector(), s2.AngleMod8), e2After = AngleError(s2.EndSS.Sub(s1.StartSS), s2.AngleMod8);
			if (e0Before - e0After > e2Before - e2After)
			{
				s0 = Merged(s0, s1);
				return true;
			}
			else
			{
				s2 = Merged(s1, s2);
				return true;
			}
		}
		static Section Merged(Section s1, Section s2)
		{
			return new Section
			{
				StartSS = s1.StartSS,
				EndSS = s2.EndSS,
				iStart = s1.iStart,
				iEnd = s2.iEnd,
				LengthPx = s1.LengthPx + s2.LengthPx,
				AngleMod8 = AngleMod8(s2.EndSS.Sub(s1.StartSS))
			};
		}

		#endregion

		#region Mouse input handling - RecognizeScribbleForEraseOrCancel

		// To recognize a scribble we require the simplified line to reverse 
		// direction at least three times. There are separate criteria for
		// erasing a shape currently being drawn and for erasing existing
		// shapes.
		//
		// The key difference between an "erase scribble" and a "cancel 
		// scribble" is that an erase scribble starts out as such, while
		// a cancel scribble indicates that the user changed his mind, so
		// the line will not appear to be a scribble at the beginning. 
		// The difference is detected by timestamps. For example, the
		// following diagram represents an "erase" operation and a "cancel"
		// operation. Assume the input points are evenly spaced in time,
		// and that the dots represent points where the input reversed 
		// direction. 
		//
		// Input points         ..........................
		// Reversals (erase)      .  .  .  .     .     .  
		// Reversals (cancel)              .   .   .   .  
		//
		// So, a scribble is considered an erasure if it satisfies t0 < t1, 
		// where t0 is the time between mouse-down and the first reversal, 
		// and t1 is the time between the first and third reversals. A cancel
		// operation satisfies t0 > t1 instead.
		//
		// Both kinds of scribble need to satisfy the formula LL*c > CHA, 
		// where c is a constant factor in pixels, LL is the drawn line 
		// length and CHA is the area of the Convex Hull that outlines the 
		// drawn figure. This formula basically detects that the user 
		// is convering the same ground repeatedly; if the pen reverses
		// direction repeatedly but goes to new places each time, it's not
		// considered an erasure scribble. For a cancel scribble, LL is
		// computed starting from the first reversal.
		IEnumerable<Shape> RecognizeScribbleForEraseOrCancel(DragState state, out bool cancel, out List<PointT> simplifiedSS)
		{
			cancel = false;
			simplifiedSS = null;
			var tolerance = state._inputTransform.Transform(new VectorT(0, 10)).Length();
			var simplifiedMP = LineMath.SimplifyPolyline(
				state.UnfilteredMousePoints.Select(p => p.Point), tolerance);
			List<int> reversals = FindReversals(simplifiedMP, 3);
			if (reversals.Count >= 3)
			{
				simplifiedSS = simplifiedMP.Select(p => state._inputTransform.Transform(p)).ToList();
				// 3 reversals confirmed. Now decide: erase or cancel?
				int[] timeStampsMs = FindTimeStamps(state.UnfilteredMousePoints, simplifiedMP);
				int t0 = timeStampsMs[reversals[0]], t1 = timeStampsMs[reversals[2]] - t0;
				cancel = t0 > t1 + 500;

				// Now test the formula LL*c > CHA as explained above
				IListSource<PointT> simplifiedMP_ = cancel ? simplifiedMP.Slice(reversals[0]) : simplifiedMP.AsListSource();
				float LL = simplifiedMP_.AdjacentPairs().Sum(pair => pair.A.Sub(pair.B).Length());
				var hull = PointMath.ComputeConvexHull(simplifiedMP);
				float CHA = PolygonMath.PolygonArea(hull);
				if (LL * EraseNubWidth > CHA)
				{
					// Erasure confirmed.
					if (cancel)
						return EmptyList<Shape>.Value;

					// Figure out which shapes to erase. To do this, we compute for 
					// each shape the amount of the scribble that overlaps that shape.
					var simplifiedSS_ = simplifiedSS;
					return _doc.Shapes.Where(s => ShouldErase(s, simplifiedSS_)).ToList();
				}
			}
			return null;
		}

		IEnumerable<LineSegmentT> AsLineSegments(IEnumerable<PointT> points)
		{
			return points.AdjacentPairs().Select(p => new LineSegmentT(p.A, p.B));
		}

		private bool ShouldErase(Shape s, List<PointT> mouseInput)
		{
			var mouseBBox = mouseInput.ToBoundingBox();
			var line = s as LineOrArrow;
			if (line != null)
			{
				// Count the number of crossings
				int crossings = 0;
				float lineLen = 0;
				if (line.BBox.Overlaps(mouseBBox))
					foreach (var seg in AsLineSegments(line.Points))
					{
						lineLen += seg.Length();
						if (seg.ToBoundingBox().Overlaps(mouseBBox))
							crossings += FindIntersectionsWith(seg, mouseInput, false).Count();
					}
				if (crossings * 40.0f > lineLen)
					return true;
			}
			else
			{
				// Measure how much of the mouse input is inside the bbox
				var bbox = s.BBox;
				if (bbox != null)
				{
					var amtInside = mouseInput.AdjacentPairs()
						.Select(seg => seg.A.To(seg.B).ClipTo(bbox))
						.WhereNotNull()
						.Sum(seg => seg.Length());
					if (amtInside * EraseBoxThreshold > bbox.Area())
						return true;
				}
			}
			return false;
		}

		float EraseNubWidth = 25, EraseBoxThreshold = 40;

		List<int> FindReversals(List<PointT> points, int stopAfter)
		{
			var reversals = new List<int>();
			for (int i = 1, c = points.Count; i < c - 1; i++)
			{
				PointT p0 = points[i - 1], p1 = points[i], p2 = points[i + 1];
				VectorT v1 = p1.Sub(p0), v2 = p2.Sub(p1);
				if (v1.Dot(v2) < 0 && MathEx.IsInRange(
					MathEx.Mod(v1.AngleDeg() - v2.AngleDeg(), 360), 150, 210))
				{
					reversals.Add(i);
					if (reversals.Count >= stopAfter)
						break;
				}
			}
			return reversals;
		}
		private static int[] FindTimeStamps(IListSource<DragPoint> original, List<PointT> simplified)
		{
			int o = -1, timeMs = 0;
			int[] times = new int[simplified.Count];
			for (int s = 0; s < simplified.Count; s++)
			{
				var p = simplified[s];
				do
				{
					o++;
					timeMs += original[o].MsecSincePrev;
				} while (original[o].Point != p);
				times[s] = timeMs;
			}
			return times;
		}

		#endregion
	}
}
