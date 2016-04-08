using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Util.UI;
using Loyc.Collections;
using System.IO;
using System.Diagnostics;

namespace BoxDiagrams
{
	/// <summary><see cref="DiagramDocumentCore"/> plus an undo stack and methods 
	/// for adding and removing shapes.</summary>
	public class DiagramDocument
	{
		DiagramDocumentCore _core;
		UndoStack _undoStack;

		public UndoStack UndoStack { get { return _undoStack; } }
		
		class DDUndoStack : Util.UI.UndoStack
		{
			DiagramDocument _self;
			public DDUndoStack(DiagramDocument self) { _self = self; }
			public override void AfterAction(bool @do)
			{
				if (_self.AfterAction != null)
					_self.AfterAction(@do);
			}
		}

		public DiagramDocument(DiagramDocumentCore core = null)
		{
			_undoStack = new DDUndoStack(this);
			_core = core ?? new DiagramDocumentCore();
			foreach (var shape in _core.Shapes)
				shape.OnBeingAdded(this);
		}

		/// <summary>Gets the shapes in this document.</summary>
		public IReadOnlyCollection<Shape> Shapes { get { return _core.Shapes; } }

		public IListSource<DiagramDrawStyle> Styles { get { return _core.Styles; } }

		public void Save(Stream stream)
		{
			_core.Save(stream);
		}

		public static DiagramDocument Load(Stream stream)
		{
			var core = DiagramDocumentCore.Load(stream);
			return new DiagramDocument(core);
		}

		public void MarkPanels() { _core.MarkPanels(); }

		public void AddShape(Shape newShape)
		{
			_undoStack.Do(@do => {
				if (@do) {
					AddShapeCore(newShape, true);
				} else {
					RemoveShapesCore(ListExt.Single(newShape), @do);
				}
			}, true);
		}

		private void AddShapeCore(Shape newShape, bool @do)
		{
			newShape.OnBeingAdded(this);
			_core.Shapes.Add(newShape);
			if (AfterShapesAdded != null && @do)
				AfterShapesAdded(ListExt.Single(newShape));
		}

		private void RemoveShapesCore(IReadOnlyCollection<Shape> shapes, bool @do)
		{
			if (shapes.Count == 0)
				return;
			foreach (var shape in shapes) {
				shape.OnBeingRemoved(this);
				_core.Shapes.Remove(shape);
			}
			if (AfterShapesRemoved != null && @do)
				AfterShapesRemoved(shapes);
		}

		public void RemoveShapes(Set<Shape> eraseSet)
		{
			if (eraseSet.Any()) {
				_undoStack.Do(@do => {
					if (@do)
						RemoveShapesCore(eraseSet, @do);
					else
						foreach (Shape shape in eraseSet)
							AddShapeCore(shape, false);
				}, false);
				
				// We also have to remove references from other shapes to the removed shapes
				foreach (var shape in _core.Shapes)
					_undoStack.Do(shape.OnShapesDeletedAction(eraseSet), false);

				_undoStack.FinishGroup();
			}
		}

		public void MergeShapes(DiagramDocumentCore other)
		{
			var otherShapes = other.Shapes.Clone();
			var otherStyles = other.Styles.Clone();
			var oldShapes = _core.Shapes.Clone();
			int oldStyleCount = _core.Styles.Count;
			_undoStack.Do(@do => {
				if (@do) {
					foreach (Shape shape in otherShapes)
						shape.OnBeingAdded(this);
					int added = _core.Shapes.AddRange(otherShapes);
					Debug.Assert(added == otherShapes.Count);
					_core.Styles.AddRange(otherStyles);
					if (AfterShapesAdded != null)
						AfterShapesAdded(otherShapes);
				} else {
					foreach (Shape shape in otherShapes)
						shape.OnBeingRemoved(this);
					_core.Shapes = oldShapes;
					Debug.Assert(_core.Styles.Count - other.Styles.Count == oldStyleCount);
					_core.Styles.Resize(oldStyleCount);
				}
			}, true);
		}

		public event Action<IReadOnlyCollection<Shape>> AfterShapesAdded;
		public event Action<IReadOnlyCollection<Shape>> AfterShapesRemoved;
		public event Action<bool> AfterAction;
	}
}
