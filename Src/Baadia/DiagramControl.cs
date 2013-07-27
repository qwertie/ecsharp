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
using Util.UI;
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
	// - change to proper model-view-viewmodel pattern so that the program can easily be
	//   ported to Android and iOS via Xamarin tools (also allows multiple views of one
	//   document)
	//
	// Gestures for drawing shapes
	// ---------------------------
	// 1. User can back up the line he's drawing (DONE)
	// 2. Line or arrow consisting of straight segments (DONE)
	// 2b. User can start typing to add text above the line (VERY RUDIMENTARY)
	// 2c. User-customized angles: 45, 30/60, 30/45/60, 15/30/45/60/75
	// 3. Box detected via two to four straight-ish segments (DONE)
	// 4. Ellipse detected by similarity to ideal ellipse, angles
	// 3b/4b. User can start typing to add text in the box/ellipse (BASIC VERSION)
	// 5. Closed shape consisting of straight segments
	// 5b. User can start typing to add text in the closed shape,
	//    which is treated as if it were a rectangle.
	// 6. Free-form line by holding Alt or by poor fit to straight-line model
	// 7. Free-form closed shape by holding Alt or by poor fit to straight-line
	//    and ellipse models
	// 8. Scribble-erase detected by repeated pen reversal (DONE)
	// 9. Scribble-cancel current shape (DONE)
	// 
	// Other behavior
	// --------------
	// 1. Ctrl+Z for unlimited undo (Ctrl+Y/Ctrl+Shift+Z for redo) (BASIC VERSION)
	// 2. Click and type for freefloating text with half-window default wrap width
	// 3. Click a shape to select it; Ctrl+Click for multiselect (DONE)
	// 4  Double-click near an endpoint of a selected polyline/curve to change 
	//    arrow type
	// 5. Double-click a polyline or curve to toggle curve-based display; 
	// 6. Double-click a text box to cycle between box, ellipse, and borderless
	// 5. Double-click a free-form shape to simplify and make it curvy, 
	//    double-click again for simplified line string.
	// 6. When adding text to a textbox, its height increases automatically when 
	//    space runs out.
	// 7. Long-press or right-click to show style menu popup
	// 8. When clicking to select a shape, that shape's DrawStyle becomes the 
	//    active style for new shapes also (does not affect arrowheads)
	// 9. When nothing is selected and user clicks and drags, this can either
	//    move the shape under the cursor or it can draw a new shape. Normally
	//    the action will be to draw a new shape, but when the mouse is clearly
	//    within a non-panel textbox, the textbox moves (the mouse cursor reflects
	//    the action that will occur).
	// 10. When moving a box, midpoints of attached free-form lines/arrows will 
	//     move in proportion to how close those points are to the anchored 
	//     endpoint, e.g. a midpoint at the halfway point of its line/arrow will 
	//     move at 50% of the speed of the box being moved, assuming that 
	//     line/arrow is anchored to the box.
	// 11. When moving a box, midpoints of attached non-freeform arrows...
	//     well, it's complicated.
	// 11b. Non-freeform lines truncate themselves at the edges of a box they are 
	//     attached to.
	// 13. Automatic Z-order. Larger textboxes are always underneath smaller ones.
	//     Free lines are on top of all boxes. Anchored lines are underneath 
	//     their boxes.

	/// <summary>A control that manages a set of <see cref="Shape"/> objects and 
	/// manages a mouse-based user interface for drawing things.</summary>
	/// <remarks>
	/// This class has the following responsibilities: TODO
	/// </remarks>
	public partial class DiagramControl : DrawingControlBase
	{
		public DiagramControl()
		{
			_undoStack = new UndoStack(this);
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
			_selAdorners.Shapes.Clear();

			_selectedShapes.IntersectWith(_shapes);
			foreach (var shape in _selectedShapes)
				shape.AddAdorners(_selAdorners.Shapes, shape == _partialSelShape ? SelType.Partial : SelType.Yes, HitTestRadius);
			_selAdorners.Invalidate();
		}

		#region Unlimited undo support code

		class UndoStack : Util.UI.UndoStack
		{
			DiagramControl _self;
			public UndoStack(DiagramControl self) { _self = self; }
			public override void AfterAction(bool @do)
			{
				_self.ShapesChanged();
				_self.RecreateSelectionAdorners();
			}
		}
		UndoStack _undoStack;

		#endregion

		#region Commands and keyboard input handling

		static Symbol S(string s) { return GSymbol.Get(s); }
		public Dictionary<Pair<Keys, Keys>, Symbol> KeyMap = new Dictionary<Pair<Keys, Keys>, Symbol>()
		{
			{ Pair.Create(Keys.Z, Keys.Control), S("Undo") },
			{ Pair.Create(Keys.Y, Keys.Control), S("Redo") },
			{ Pair.Create(Keys.Z, Keys.Control | Keys.Shift), S("Redo") },
			{ Pair.Create(Keys.Delete, (Keys)0), S("DeleteSelected") },
		};
		
		Map<Symbol, ICommand> _commands;
		public Map<Symbol, ICommand> Commands
		{
			get { 
				return _commands = _commands ?? 
				    (((Map<Symbol, ICommand>)CommandAttribute.GetCommandMap(this))
				                      .Union(CommandAttribute.GetCommandMap(_undoStack)));
			}
		}

		public bool ProcessShortcutKey(KeyEventArgs e)
		{
			Symbol name;
			if (KeyMap.TryGetValue(Pair.Create(e.KeyCode, e.Modifiers), out name)) {
				ICommand cmd;
				if (Commands.TryGetValue(name, out cmd) && cmd.CanExecute) {
					cmd.Run();
					return true;
				}
			}
			return false;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!(e.Handled = e.Handled || ProcessShortcutKey(e)))
				if (_focusShape != null)
					_focusShape.OnKeyDown(e, _undoStack);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (!e.Handled && _focusShape != null)
				_focusShape.OnKeyUp(e, _undoStack);
		}
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			if (!e.Handled && _focusShape != null)
				_focusShape.OnKeyPress(e, _undoStack);
		}

		[Command(null, "Delete selected shapes")]
		public bool DeleteSelected(bool run = true)
		{
			if (_partialSelShape == null && _selectedShapes.Count == 0)
				return false;
			if (run) {
				if (_partialSelShape != null)
					_selectedShapes.Add(_partialSelShape);
				DeleteShapes(_selectedShapes);
			}
			return true;
		}

		#endregion

		#region Mouse input handling - general
		// The base class gathers mouse events and calls AnalyzeGesture()

		new protected class DragState : DrawingControlBase.DragState
		{
			public DragState(DiagramControl c) : base(c) { Control = c; }
			public new DiagramControl Control;
			public Shape StartShape;
			public IEnumerable<Shape> NearbyShapes { get { return Control.NearbyShapes(Points.Last.Point); } }

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

			public bool ClickedShapeAllowsDrag;
			public Shape ClickedShape;
		}

		protected override DrawingControlBase.DragState MouseClickStarted(MouseEventArgs e)
		{
			Shape clicked;
			var cursor = ChooseCursor((PointT)e.Location.AsLoyc(), out clicked);
			return new DragState(this) { 
				ClickedShape = clicked, 
				ClickedShapeAllowsDrag = cursor != null && cursor != Cursors.Arrow
			};
		}

		protected VectorT HitTestRadius = new VectorT(8, 8);

		// most recently drawn shape is "partially selected". Must also be in _selectedShapes
		protected Shape _partialSelShape;
		protected Shape _focusShape; // most recently clicked or created (gets keyboard input)
		protected MSet<Shape> _selectedShapes = new MSet<Shape>();

		protected SelType GetSelType(Shape shape)
		{
			if (shape == _partialSelShape)
				return SelType.Partial;
			return _selectedShapes.Contains(shape) ? SelType.Yes : SelType.No;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_dragState != null)
				return;
			var mouseLoc = (PointT)e.Location.AsLoyc();
			Shape _;
			Cursor foundCursor = ChooseCursor(mouseLoc, out _);
			Cursor = foundCursor ?? Cursors.Cross;
		}

		private Cursor ChooseCursor(PointT mouseLoc, out Shape foundShape)
		{
			foundShape = null;
			Cursor foundCursor = null;
			foreach (Shape shape in NearbyShapes(mouseLoc))
			{
				var cursor = shape.MouseCursor(mouseLoc, HitTestRadius, GetSelType(shape));
				if (cursor != null)
				{
					if (foundShape == null || foundShape.ZOrder < shape.ZOrder)
					{
						foundShape = shape;
						foundCursor = cursor;
					}
				}
			}
			return foundCursor;
		}

		// TODO optimization: return a cached subset rather than all shapes
		public IEnumerable<Shape> NearbyShapes(PointT mousePos) { return _shapes; }

		const int MinDistBetweenDragPoints = 2;

		static readonly DrawStyle MouseLineStyle = new DrawStyle { LineColor = Color.FromArgb(96, Color.Gray), LineWidth = 10 };
		static readonly DrawStyle EraseLineStyle = new DrawStyle { LineColor = Color.FromArgb(128, Color.White), LineWidth = 10 };

		protected override void AnalyzeGesture(DrawingControlBase.DragState state_, bool mouseUp)
		{
			// TODO: Analyze on separate thread and maybe even draw on a separate thread.
			DragState state = (DragState)state_;
			var adorners = _dragAdorners.Shapes;
			adorners.Clear();
			Shape newShape = null;
			IEnumerable<Shape> eraseSet = null;
			if (state.IsDrag)
			{
				List<PointT> simplified;
				bool cancel;
				eraseSet = RecognizeScribbleForEraseOrCancel(state, out cancel, out simplified);
				if (eraseSet != null) {
					ShowEraseDuringDrag(state, adorners, eraseSet, simplified, cancel);
				} else {
					newShape = DetectNewShapeDuringDrag(state, adorners);
				}
			}
			if (mouseUp) {
				adorners.Clear();
				HandleMouseUp(state, newShape, eraseSet);
			} else if (newShape != null)
				newShape.AddLLShapes(adorners);

			_dragAdorners.Invalidate();
		}

		private void HandleMouseUp(DragState state, Shape newShape, IEnumerable<Shape> eraseSet)
		{
			_partialSelShape = null;
			if (newShape != null)
				AddShape(newShape);
			if (eraseSet != null)
				DeleteShapes(eraseSet);
			
			if (!state.IsDrag)
			{
				if ((Control.ModifierKeys & Keys.Control) == 0)
					_selectedShapes.Clear();
				ChooseCursor(state.UnfilteredPoints[0].Point, out _focusShape);
				if (_focusShape != null)
					_selectedShapes.Toggle(_focusShape);
				RecreateSelectionAdorners();
			}
		}

		private void AddShape(Shape newShape)
		{
			_undoStack.Do(() => {
				_shapes.Add(newShape);
				_partialSelShape = newShape;
				_focusShape = newShape;
				_selectedShapes.Clear();
				_selectedShapes.Add(_partialSelShape);
			}, () => {
				_shapes.Remove(newShape);
			});
		}

		void DeleteShapes(IEnumerable<Shape> eraseSet)
		{
			Set<Shape> eraseSet2 = new Set<Shape>(eraseSet);
			if (!eraseSet2.IsEmpty) {
				_undoStack.Do(() => {
					LLDeleteShapes(eraseSet2);
				}, () => {
					foreach (Shape shape in eraseSet2)
						_shapes.Add(shape);
				});
			}
		}
		private void LLDeleteShapes(Set<Shape> eraseSet)
		{
			if (!eraseSet.Any())
				return;

			MSet<LLShape> eraseSetLL = new MSet<LLShape>();
			foreach (var s in eraseSet)
				s.AddLLShapes(eraseSetLL);
			BeginRemoveAnimation(eraseSetLL);

			foreach (var s in eraseSet)
				G.Verify(_shapes.Remove(s));
		}
		private void ShowEraseDuringDrag(DragState state, MSet<LLShape> adorners, IEnumerable<Shape> eraseSet, List<PointT> simplified, bool cancel)
		{
			EraseLineStyle.LineColor = Color.FromArgb(128, BackColor);
			var eraseLine = new LLPolyline(EraseLineStyle, simplified);
			adorners.Add(eraseLine);

			if (cancel) {
				eraseLine.Style = LineStyle;
				BeginRemoveAnimation(adorners);
				adorners.Clear();
				state.IsComplete = true;
			} else {
				// Show which shapes are erased by drawing them in the background color
				foreach (Shape s in eraseSet) {
					Shape s_ = s.Clone();
					s_.Style = s.Style.Clone();
					s_.Style.FillColor = s_.Style.LineColor = s_.Style.TextColor = Color.FromArgb(192, BackColor);
					// avoid an outline artifact, in which color from the edges of the 
					// original shape bleeds through by a variable amount that depends 
					// on subpixel offsets.
					s_.Style.LineWidth++;
					s_.AddLLShapes(adorners);
				}
			}
		}
		private Shape DetectNewShapeDuringDrag(DragState state, MSet<LLShape> adorners)
		{
			Shape newShape = null;
			adorners.Add(new LLPolyline(MouseLineStyle, state.Points.Select(p => p.Point).AsList()) { ZOrder = 0x100 });

			if (state.Points.Count == 1)
			{
				newShape = new Marker(MarkerStyle, state.Points[0].Point, MarkerRadius, MarkerType);
			}
			else if (state.Points.Count > 1)
			{
				#if DEBUG
				var ss = BreakIntoSections(state);
				EliminateTinySections(ss, 10 + (int)(ss.Sum(s => s.Length) * 0.05));
				foreach (Section s in ss)
					adorners.Add(new LLMarker(new DrawStyle { LineColor = Color.Gainsboro, FillColor = Color.Gray }, s.StartPt, 5, MarkerPolygon.Circle));
				#endif

				newShape = RecognizeBoxOrLines(state);
			}
			return newShape;
		}

		Timer _cancellingTimer = new Timer { Interval = 30 };

		private void BeginRemoveAnimation(MSet<LLShape> erasedShapes)
		{
			var cancellingShapes = erasedShapes.Select(s => Pair.Create(s, s.Opacity)).ToList();
			var cancellingTimer = new Timer { Interval = 30, Enabled = true };
			var cancellingLayer = AddLayer();
			cancellingLayer.Shapes.AddRange(erasedShapes);
			int opacity = 255;
			cancellingTimer.Tick += (s, e) =>
			{
				opacity -= 32;
				if (opacity > 0) {
					foreach (var pair in cancellingShapes)
						pair.A.Opacity = (byte)(pair.B * opacity >> 8);
					cancellingLayer.Invalidate();
				} else {
					RemoveLayerAt(Layers.IndexOf(cancellingLayer));
					cancellingTimer.Dispose();
					cancellingLayer.Dispose();
				}
			};
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
		IEnumerable<Shape> RecognizeScribbleForEraseOrCancel(DragState state, out bool cancel, out List<PointT> simplified_)
		{
			cancel = false;
			var simplified = simplified_ = LineMath.SimplifyPolyline(
				state.UnfilteredPoints.Select(p => p.Point).Buffered(), 10);
			List<int> reversals = FindReversals(simplified, 3);
			if (reversals.Count >= 3)
			{
				// 3 reversals confirmed. Now decide: erase or cancel?
				int[] timeStampsMs = FindTimeStamps(state.UnfilteredPoints, simplified);
				int t0 = timeStampsMs[reversals[0]], t1 = timeStampsMs[reversals[2]] - t0;
				cancel = t0 > t1 + 500;

				// Now test the formula LL*c > CHA as explained above
				IListSource<PointT> simplified__ = cancel ? simplified.Slice(reversals[0]) : simplified.AsListSource();
				float LL = simplified__.AdjacentPairs().Sum(pair => pair.A.Sub(pair.B).Length());
				var hull = PointMath.ComputeConvexHull(simplified_);
				float CHA = PolygonMath.PolygonArea(hull);
				if (LL * EraseNubWidth > CHA)
				{
					// Erasure confirmed.
					if (cancel)
						return EmptyList<Shape>.Value;
					
					// Figure out which shapes to erase. To do this, we compute for 
					// each shape the amount of the scribble that overlaps that shape.
					return _shapes.Where(s => ShouldErase(s, simplified)).ToList();
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
					foreach(var seg in AsLineSegments(line.Points)) {
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
				if (bbox != null) {
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
		private static int[] FindTimeStamps(DList<DragPoint> original, List<PointT> simplified)
		{
			int o = -1, timeMs = 0;
			int[] times = new int[simplified.Count];
			for (int s = 0; s < simplified.Count; s++)
			{
				var p = simplified[s];
				do {
					o++;
					timeMs += original[o].MsecSincePrev;
				} while (original[o].Point != p);
				times[s] = timeMs;
			}
			return times;
		}

		#endregion

		MSet<Shape> _shapes = new MSet<Shape>();

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
