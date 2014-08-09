using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
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
	// 10. Click empty space and type to create borderless text (DONE)
	// 11. Double-click empty space to create a marker (DONE)
	// 
	// Other behavior
	// --------------
	// 1. Ctrl+Z for unlimited undo (Ctrl+Y/Ctrl+Shift+Z for redo) (BASIC VERSION)
	// 2. Click and type for freefloating text with half-window default wrap width
	// 3. Click a shape to select it; Ctrl+Click for multiselect (DONE)
	// 4  Double-click near an endpoint of a selected polyline/curve to change 
	//    arrow type (DONE, except you must click the endpoint square)
	// 5. Double-click a polyline or curve to toggle curve-based display; 
	// 6. Double-click a text box to cycle between box, ellipse, and borderless (DONE)
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
	//    the action that will occur). (DONE)
	// 10. When moving a box, midpoints of attached free-form lines/arrows will 
	//     move in proportion to how close those points are to the anchored 
	//     endpoint, e.g. a midpoint at the halfway point of its line/arrow will 
	//     move at 50% of the speed of the box being moved, assuming that 
	//     line/arrow is anchored to the box.
	// 11. When moving a box, midpoints of attached non-freeform arrows...
	//     well, it's complicated. (DONE)
	// 11b. Non-freeform lines truncate themselves at the edges of a box they are 
	//     attached to.
	// 13. Automatic Z-order. Larger textboxes are always underneath smaller ones.
	//     Free lines are on top of all boxes. Anchored lines are underneath 
	//     their boxes. (DONE)

	/// <summary>A control that manages a set of <see cref="Shape"/> objects and 
	/// manages a mouse-based user interface for drawing things.</summary>
	/// <remarks>
	/// This class has the following responsibilities: TODO
	/// </remarks>
	public partial class DiagramControl : LLShapeWidgetControl
	{
		DiagramGestureAnalyzer _gestureAnalyzer;

		public DiagramControl()
		{
			_mainLayer = Layers[0]; // predefined
			_mainLayer.Shapes.Add(_shapeGroup);
			_selAdornerLayer = AddLayer(false);
			_selAdornerLayer.Shapes.Add(_selAdornerGroup);
			_dragAdornerLayer = AddLayer(false);
			_dragAdornerLayer.Shapes.Add(_dragAdornerGroup);
			_shapeGroup.Transform = _scrollZoom;
			_selAdornerGroup.Transform = _scrollZoom;
			_dragAdornerGroup.Transform = _scrollZoom;

			_gestureAnalyzer = new DiagramGestureAnalyzer(this);
			Document = new DiagramDocument();
			
			LineStyle = new DiagramDrawStyle { LineColor = Color.Black, LineWidth = 2, TextColor = Color.Blue, FillColor = Color.FromArgb(64, Color.Gray) };
			LineStyle.Name = "Default";
			BoxStyle = (DiagramDrawStyle)LineStyle.Clone();
			BoxStyle.LineColor = Color.DarkGreen;
			MarkerRadius = 5;
			MarkerType = MarkerPolygon.Circle;
			FromArrow = null;
			ToArrow = Arrowhead.Arrow30deg;

		}

		// This oversized attribute tells the WinForms designer to ignore the property
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DiagramDrawStyle LineStyle { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DiagramDrawStyle BoxStyle { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public MarkerPolygon MarkerType { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Arrowhead FromArrow { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Arrowhead ToArrow { get; set; }

		public float MarkerRadius { get; set; }

		// most recently drawn shape is "partially selected". Must also be in _selectedShapes
		internal Shape _partialSelShape;
		protected Shape _focusShape; // most recently clicked or created (gets keyboard input)
		protected MSet<Shape> _selectedShapes = new MSet<Shape>();
		public MSet<Shape> SelectedShapes { get { return _selectedShapes; } }

		LLShapeLayer _mainLayer;          // shows current Document (lowest layer)
		LLShapeLayer _selAdornerLayer;    // shows adornments for selected shapes
		LLShapeLayer _dragAdornerLayer;   // shows drag line and a shape to potentially create

		LLShapeGroup _shapeGroup = new LLShapeGroup();
		LLShapeGroup _selAdornerGroup = new LLShapeGroup();
		LLShapeGroup _dragAdornerGroup = new LLShapeGroup();

		Matrix _scrollZoom = new Matrix();

		internal MSet<LLShape> DragAdornerShapes { get { return _dragAdornerGroup.Shapes; } }
		internal void DragAdornersChanged() { _dragAdornerLayer.Invalidate(); }
		internal Point<float>? _lastClickLocation; // shape space. used to let user click blank space and start typing

		internal PointT ToShapeSpace(PointT px) { return _shapeGroup.InverseTransform.Transform(px); }
		internal VectorT ToShapeSpace(VectorT px) { return _shapeGroup.InverseTransform.Transform(px); }
		internal PointT ToPixelSpace(PointT px) { return _shapeGroup.Transform.Transform(px); }
		internal Matrix InputTransform { get { return _shapeGroup.InverseTransform; } }

		internal void Scroll(VectorT pxAmount)
		{
			var amount = ToShapeSpace(pxAmount);
			_scrollZoom.Translate(-amount.X, -amount.Y);
			MatrixChanged();
		}
		internal void Zoom(float factor)
		{
			var size = ClientSize;
			PointT center = ToShapeSpace(new PointT(size.Width / 2, size.Height / 2));
			_scrollZoom.Translate(center.X, center.Y);
			_scrollZoom.Scale(factor, factor);
			_scrollZoom.Translate(-center.X, -center.Y);
			MatrixChanged();
		}
		void MatrixChanged()
		{
			_shapeGroup.Transform = _scrollZoom; // notify that it changed
			_selAdornerGroup.Transform = _scrollZoom;
			_dragAdornerGroup.Transform = _scrollZoom;
			_mainLayer.Invalidate();
			_selAdornerLayer.Invalidate();
			_dragAdornerLayer.Invalidate();
		}

		internal void AddShape(Shape newShape)
		{
			_doc.AddShape(newShape);
		}

		internal void DeleteShapes(Set<Shape> eraseSet)
		{
			_doc.RemoveShapes(eraseSet);
		}

		internal void ClickSelect(Shape clickedShape)
		{
			if ((Control.ModifierKeys & Keys.Control) == 0)
				SelectedShapes.Clear();
			if ((_focusShape = clickedShape) != null)
				SelectedShapes.Toggle(_focusShape);
			RecreateSelectionAdorners();
		}

		internal void RecreateSelectionAdorners()
		{
			_selAdornerGroup.Shapes.Clear();

			_selectedShapes.IntersectWith(_doc.Shapes);
			foreach (var shape in _selectedShapes)
				shape.AddAdornersTo(_selAdornerGroup.Shapes, shape == _partialSelShape ? SelType.Partial : SelType.Yes, HitTestRadius);
			_selAdornerLayer.Invalidate();
		}

		protected SelType GetSelType(Shape shape)
		{
			if (shape == _partialSelShape)
				return SelType.Partial;
			return _selectedShapes.Contains(shape) ? SelType.Yes : SelType.No;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_gestureAnalyzer.IsMouseDown)
				return;
			var mouseLoc = (PointT)e.Location.AsLoyc();
			var result = HitTest(mouseLoc);
			Cursor = result != null ? result.MouseCursor : Cursors.Cross;
		}

		[Command(null, "Duplicate")]
		public bool DuplicateSelected(bool run = true)
		{
			if (_selectedShapes.Count == 0)
				return false;
			if (run)
			{
				// Equivalent to copy + paste
				var buf = SerializeSelected();
				buf.Position = 0;
				PasteAndSelect(buf, new VectorT(20, 20));
			}
			return true;
		}

		[Command(null, "Copy")]
		public bool Cut(bool run = true)
		{
			if (Copy(run))
			{
				DeleteSelected(run);
				return true;
			}
			return false;
		}

		[Command(null, "Copy")]
		public bool Copy(bool run = true)
		{
			if (_selectedShapes.Count == 0)
				return false;
			if (run)
			{
				var buf = SerializeSelected();
				var data = new DataObject();

				data.SetData("DiagramDocument", buf.ToArray());

				var sortedShapes = _selectedShapes.OrderBy(s =>
				{
					var c = s.BBox.Center();
					return c.Y + c.X / 10;
				});
				var text = StringExt.Join("\n\n", sortedShapes
					.Select(s => s.PlainText()).Where(t => !string.IsNullOrEmpty(t)));
				if (!string.IsNullOrEmpty(text))
					data.SetText(text);

				// Crazy Clipboard deletes data by default on app exit!
				// need 'true' parameter to prevent loss of data on exit
				Clipboard.SetDataObject(data, true);
			}
			return true;
		}

		[Command(null, "Paste")]
		public bool Paste(bool run = true)
		{
			if (Clipboard.ContainsData("DiagramDocument"))
			{
				if (run)
				{
					var buf = Clipboard.GetData("DiagramDocument") as byte[];
					if (buf != null)
						PasteAndSelect(new MemoryStream(buf), VectorT.Zero);
				}
				return true;
			}
			else if (Clipboard.ContainsText())
			{
				if (run)
				{
					var text = Clipboard.GetText();

					DoOrUndo act = null;
					if (_focusShape != null && (act = _focusShape.AppendTextAction(text)) != null)
						_doc.UndoStack.Do(act, true);
					else
					{
						var textBox = new TextBox(new BoundingBox<Coord>(0, 0, 300, 200))
						{
							Text = text,
							TextJustify = LLTextShape.JustifyMiddleCenter,
							BoxType = BoxType.Borderless,
							Style = BoxStyle
						};
						_doc.AddShape(textBox);
					}
				}
				return true;
			}
			return false;
		}

		[Command(null, "Clear text")]
		public bool ClearText(bool run = true)
		{
			bool success = false;
			foreach (var shape in _selectedShapes)
			{
				var act = shape.GetClearTextAction();
				if (act != null)
				{
					success = true;
					if (run)
						_doc.UndoStack.Do(act, false);
				}
			}
			_doc.UndoStack.FinishGroup();
			return success;
		}

		private MemoryStream SerializeSelected()
		{
			var doc = new DiagramDocumentCore();
			doc.Shapes.AddRange(_selectedShapes);
			// no need to populate doc.Styles, it is not used for copy/paste
			var buf = new MemoryStream();
			doc.Save(buf);
			return buf;
		}

		private DiagramDocumentCore PasteAndSelect(Stream buf, VectorT offset)
		{
			var doc = DeserializeAndEliminateDuplicateStyles(buf);
			foreach (var shape in doc.Shapes)
				shape.MoveBy(offset);
			_doc.MergeShapes(doc);

			_selectedShapes.Clear();
			_selectedShapes.AddRange(doc.Shapes);
			return doc;
		}

		private DiagramDocumentCore DeserializeAndEliminateDuplicateStyles(Stream buf)
		{
			var doc = DiagramDocumentCore.Load(buf);
			doc.Styles.Clear();
			foreach (var shape in doc.Shapes)
			{
				var style = _doc.Styles.Where(s => s.Equals(shape.Style)).FirstOrDefault();
				if (style != null)
					shape.Style = style;
				else
					doc.Styles.Add(shape.Style);
			}
			return doc;
		}

		public VectorT HitTestRadius = new VectorT(8, 8);

		internal Util.WinForms.HitTestResult HitTest(PointT mouseLoc)
		{
			var mouseSS = ToShapeSpace(mouseLoc); // SS=Shape Space
			var htRadiusSS = ToShapeSpace(HitTestRadius);
			Util.WinForms.HitTestResult best = null;
			bool bestSel = false;
			foreach (Shape shape in ShapesOnScreen(mouseLoc))
			{
				var result = shape.HitTest(mouseSS, htRadiusSS, GetSelType(shape));
				if (result != null)
				{
					Debug.Assert(result.Shape == shape);
					bool resultSel = _selectedShapes.Contains(result.Shape);
					// Prefer to hit test against an already-selected shape (unless 
					// it's a panel), otherwise the thing with the highest Z-order.
					if (shape.IsPanel)
						resultSel = false;
					if (best == null || (resultSel && !bestSel) || (bestSel == resultSel && ((Shape)best.Shape).HitTestZOrder < shape.HitTestZOrder))
					{
						best = result;
						bestSel = resultSel;
					}
				}
			}
			return best;
		}

		// TODO optimization: return a cached subset rather than all shapes
		public IEnumerable<Shape> ShapesOnScreen(PointT mousePos) { return _doc.Shapes; }

		const int MinDistBetweenDragPoints = 2;

		internal static readonly DrawStyle MouseLineStyle = new DrawStyle { LineColor = Color.FromArgb(96, Color.Gray), LineWidth = 10 };
		internal static readonly DrawStyle EraseLineStyle = new DrawStyle { LineColor = Color.FromArgb(128, Color.White), LineWidth = 10 };

		#region Commands and keyboard input handling

		static Symbol S(string s) { return GSymbol.Get(s); }
		public Dictionary<Pair<Keys, Keys>, Symbol> KeyMap = new Dictionary<Pair<Keys, Keys>, Symbol>()
		{
			{ Pair.Create(Keys.Z, Keys.Control), S("Undo") },
			{ Pair.Create(Keys.Y, Keys.Control), S("Redo") },
			{ Pair.Create(Keys.Z, Keys.Control | Keys.Shift), S("Redo") },
			{ Pair.Create(Keys.A, Keys.Control), S("SelectAll") },
			{ Pair.Create(Keys.Delete, (Keys)0), S("DeleteSelected") },
			{ Pair.Create(Keys.Delete, Keys.Control), S("ClearText") },
			{ Pair.Create(Keys.X, Keys.Control), S("Cut") },
			{ Pair.Create(Keys.C, Keys.Control), S("Copy") },
			{ Pair.Create(Keys.V, Keys.Control), S("Paste") },
			{ Pair.Create(Keys.Delete, Keys.Shift), S("Cut") },
			{ Pair.Create(Keys.Insert, Keys.Control), S("Copy") },
			{ Pair.Create(Keys.Insert, Keys.Shift), S("Paste") },
			{ Pair.Create(Keys.Up, Keys.None), S("ScrollUp") },
			{ Pair.Create(Keys.Down, Keys.None), S("ScrollDown") },
			{ Pair.Create(Keys.Left, Keys.None), S("ScrollLeft") },
			{ Pair.Create(Keys.Right, Keys.None), S("ScrollRight") },
			{ Pair.Create(Keys.PageUp, Keys.None), S("PageUp") },
			{ Pair.Create(Keys.PageDown, Keys.None), S("PageDown") },
			{ Pair.Create(Keys.Add, Keys.Control), S("ZoomIn") },
			{ Pair.Create(Keys.Subtract, Keys.Control), S("ZoomOut") },
		};
		
		Map<Symbol, ICommand> _commands;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Map<Symbol, ICommand> Commands
		{
			get { 
				return _commands = _commands ?? 
				    (((Map<Symbol, ICommand>)CommandAttribute.GetCommandMap(this))
				                      .Union(CommandAttribute.GetCommandMap(_doc.UndoStack)));
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

		protected override bool IsInputKey(Keys keyData)
		{
			return true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!(e.Handled = e.Handled || ProcessShortcutKey(e)))
				if (_focusShape != null)
					_focusShape.OnKeyDown(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (!e.Handled && _focusShape != null)
				_focusShape.OnKeyUp(e);
		}
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			if (!e.Handled) {
				// Should we add text to _focusShape or create a new text shape?
				bool ignorePanel = false;
				if (_focusShape != null && _focusShape.IsPanel && string.IsNullOrEmpty(_focusShape.PlainText()))
					ignorePanel = true;
				if (_focusShape != null && !ignorePanel) {
					_focusShape.OnKeyPress(e);
				} else if (e.KeyChar >= 32 && _lastClickLocation != null) {
					var pt = _lastClickLocation.Value;
					int w = MathEx.InRange(Width / 4, 100, 400);
					int h = MathEx.InRange(Height / 8, 50, 200);
					var newShape = new TextBox(new BoundingBox<float>(pt.X - w / 2, pt.Y, pt.X + w / 2, pt.Y + h)) {
						Text = e.KeyChar.ToString(),
						BoxType = BoxType.Borderless,
						TextJustify = LLTextShape.JustifyUpperCenter,
						Style = BoxStyle
					};
					AddShape(newShape);
				}
			}
		}

		[Command(null, "Delete selected shapes")]
		public bool DeleteSelected(bool run = true)
		{
			if (_partialSelShape == null && _selectedShapes.Count == 0)
				return false;
			if (run) {
				if (_partialSelShape != null)
					_selectedShapes.Add(_partialSelShape);
				DeleteShapes((Set<Shape>)_selectedShapes);
			}
			return true;
		}

		#endregion


		DiagramDocument _doc;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DiagramDocument Document
		{
			get { return _doc; }
			set {
				if (_doc != value) {
					if (_doc != null) {
						_doc.AfterAction -= AfterAction;
						_doc.AfterShapesAdded -= AfterShapesAdded;
						_doc.AfterShapesRemoved -= AfterShapesRemoved;
					}
					_doc = value;
					_doc.AfterAction += AfterAction;
					_doc.AfterShapesAdded += AfterShapesAdded;
					_doc.AfterShapesRemoved += AfterShapesRemoved;
				}
			}
		}

		public void Save(string filename)
		{
			using (var stream = File.Open(filename, FileMode.Create)) {
				_doc.Save(stream);
			}
		}

		public void Load(string filename)
		{
			using (var stream = File.OpenRead(filename)) {
				Document = DiagramDocument.Load(stream);
				AfterAction(true);
			}
		}

		void AfterShapesAdded(IReadOnlyCollection<Shape> newShapes)
		{
			_selectedShapes.Clear();
			_selectedShapes.Add(_partialSelShape);
			if (newShapes.Count == 1) {
				var s = newShapes.First();
				_partialSelShape = s;
				_focusShape = s;
			}
		}

		void AfterShapesRemoved(IReadOnlyCollection<Shape> eraseSet)
		{
			MSet<LLShape> eraseSetLL = new MSet<LLShape>();
			foreach (var s in eraseSet)
				s.AddLLShapesTo(eraseSetLL);
			BeginRemoveAnimation(eraseSetLL);
		}

		void AfterAction(bool @do)
		{
			ShapesChanged();
			RecreateSelectionAdorners();
		}

		protected void ShapesChanged()
		{
			_shapeGroup.Shapes.Clear();
			foreach (Shape shape in _doc.Shapes)
				shape.AddLLShapesTo(_shapeGroup.Shapes);
			_mainLayer.Invalidate();
		}

		internal const int AnchorSnapDistance = 10;

		public Anchor GetBestAnchor(PointT input, int exitAngleMod8 = -1)
		{
			var candidates =
				from shape in _doc.Shapes
				let anchor = shape.GetNearestAnchor(input, exitAngleMod8)
				where anchor != null && anchor.Point.Sub(input).Quadrance() <= MathEx.Square(AnchorSnapDistance)
				select anchor;
			return candidates.MinOrDefault(a => a.Point.Sub(input).Quadrance());
		}

		[Command(null, "Select all shapes")] public bool SelectAll(bool run = true)
		{
			if (run) {
				_selectedShapes.AddRange(_doc.Shapes);
				RecreateSelectionAdorners();
			}
			return _doc.Shapes.Count != 0;
		}

		internal void BeginRemoveAnimation(MSet<LLShape> erasedShapes)
		{
			var cancellingShapes = erasedShapes.Select(s => Pair.Create(s, s.Opacity)).ToList();
			var cancellingTimer = new Timer { Interval = 30, Enabled = true };
			var cancellingLayer = AddLayer();
			cancellingLayer.Shapes.AddRange(erasedShapes);
			int opacity = 255;
			cancellingTimer.Tick += (s, e) =>
			{
				opacity -= 32;
				if (opacity > 0)
				{
					foreach (var pair in cancellingShapes)
						pair.A.Opacity = (byte)(pair.B * opacity >> 8);
					cancellingLayer.Invalidate();
				}
				else
				{
					DisposeLayerAt(Layers.IndexOf(cancellingLayer));
					cancellingTimer.Dispose();
					cancellingLayer.Dispose();
				}
			};
		}

		int OneLineScrollAmt = 16;
		[Command(null, "Scroll down")]
		public bool ScrollDown(bool run = true)
		{
			if (run) Scroll(new VectorT(0, OneLineScrollAmt));
			return true;
		}
		[Command(null, "Scroll up")]
		public bool ScrollUp(bool run = true)
		{
			if (run) Scroll(new VectorT(0, -OneLineScrollAmt));
			return true;
		}
		[Command(null, "Scroll left")]
		public bool ScrollLeft(bool run = true)
		{
			if (run) Scroll(new VectorT(-OneLineScrollAmt, 0));
			return true;
		}
		[Command(null, "Scroll right")]
		public bool ScrollRight(bool run = true)
		{
			if (run) Scroll(new VectorT(OneLineScrollAmt, 0));
			return true;
		}
		[Command(null, "Page up")]
		public bool PageUp(bool run = true)
		{
			if (run) Scroll(new VectorT(0, -Math.Max(ClientSize.Height - OneLineScrollAmt, ClientSize.Height / 2)));
			return true;
		}
		[Command(null, "Page down")]
		public bool PageDown(bool run = true)
		{
			if (run) Scroll(new VectorT(0, Math.Max(ClientSize.Height - OneLineScrollAmt, ClientSize.Height / 2)));
			return true;
		}
		[Command(null, "Zoom in")]
		public bool ZoomIn(bool run = true)
		{
			if (run) Zoom(256f/181);
			return true;
		}
		[Command(null, "Zoom out")]
		public bool ZoomOut(bool run = true)
		{
			if (run) Zoom(181/256f);
			return true;
		}
	}


	public interface IRecognizerResult
	{
		IEnumerable<LLShape> RealtimeDisplay { get; }
		int Quality { get; }
		void Accept();
	}

}
