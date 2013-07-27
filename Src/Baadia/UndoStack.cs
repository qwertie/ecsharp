using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Util.UI
{
	/// <summary>A simple class for managing an undo-redo stack.</summary>
	public class UndoStack
	{
		protected class Command
		{
			public Command(Action @do, Action undo) { _do = @do; _undo = undo; _done = false; }
			Action _do, _undo;
			bool _done;
			public Command Do() { Debug.Assert(!_done); _done = true; _do(); return this; }
			public Command Undo() { Debug.Assert(_done); _done = false; _undo(); return this; }
		}

		Stack<Command> _undoStack = new Stack<Command>();
		Stack<Command> _redoStack = new Stack<Command>();

		public virtual void AfterAction(bool @do) { }

		public virtual void Do(Action action, Action undo)
		{
			_undoStack.Push(new Command(action, undo).Do());
			_redoStack.Clear();
			AfterAction(true);
		}
		
		[Command("Undo", "Undo")]
		public virtual bool Undo(bool run = true)
		{
			if (_undoStack.Count == 0)
				return false;
			if (run) {
				_redoStack.Push(_undoStack.Pop().Undo());
				AfterAction(false);
			}
			return true;
		}
		
		[Command("Redo", "Redo")]
		public virtual bool Redo(bool run = true)
		{
			if (_redoStack.Count == 0)
				return false;
			if (run) {
				_undoStack.Push(_redoStack.Pop().Do());
				AfterAction(true);
			}
			return true;
		}
	}
}
