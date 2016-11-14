using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Util.UI
{
	/// <summary>Represents an action that can be done once and then undone.
	/// The method must be called once to do the action and once to undo it.</summary>
	/// <param name="do">Whether to do or undo. @do is false to request undo.</param>
	/// <remarks>This delegate is typically used with <see cref="UndoStack"/>.
	/// The caller promises to properly pair "do" and "undo" calls, so the
	/// DoOrUndo method should not have to do sanity checking.</remarks>
	public delegate void DoOrUndo(bool @do);

	/// <summary>A simple, general class for managing an undo-redo stack.</summary>
	public class UndoStack : IAdd<DoOrUndo>
	{
		protected struct Command
		{
			public Command(DoOrUndo action, bool finishGroup)
			{ _action = action; _done = false; _finishGroup = finishGroup; }
			DoOrUndo _action;
			bool _done; // debug check
			// To group a series of actions into one undo command, _finishGroup should 
			// be false on all actions except the final one.
			bool _finishGroup;
			public bool FinishGroup { get { return _finishGroup; } }
			public Command Do() { Debug.Assert(!_done); _done = true; _action(true); return this; }
			public Command Undo() { Debug.Assert(_done); _done = false; _action(false); return this; }
			public Command WithSeparatorFlag(bool finishGroup) { var copy = this; copy._finishGroup = finishGroup; return copy; }
		}

		protected Stack<Command> _undoStack = new Stack<Command>();
		protected Stack<Command> _redoStack = new Stack<Command>();

		public virtual void AfterAction(bool @do) { }

		public virtual void Do(DoOrUndo action, bool finishGroup)
		{
			if (action != null) {
				AcceptTentativeAction(false);
				_undoStack.Push(new Command(action, finishGroup).Do());
				_redoStack.Clear();
				AfterAction(true);
			}
		}

		public void FinishGroup(bool finish = true)
		{
			Debug.Assert(_undoStack.Count > 0);
			_undoStack.Push(_undoStack.Pop().WithSeparatorFlag(finish));
		}
		
		[Command("Undo", "Undo")]
		public virtual bool Undo(bool run = true)
		{
			UndoTentativeAction();
			if (_undoStack.Count == 0)
				return false;
			if (run) {
				do {
					_redoStack.Push(_undoStack.Pop().Undo());
				} while(_undoStack.Count != 0 && !_undoStack.Peek().FinishGroup);
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
				do {
					_undoStack.Push(_redoStack.Pop().Do());
				} while(!_undoStack.Peek().FinishGroup);
				AfterAction(true);
			}
			return true;
		}

		#region Support for a tentative/preview action

		protected InternalList<Command> _tempStack = InternalList<Command>.Empty;

		void IAdd<DoOrUndo>.Add(DoOrUndo item) { DoTentatively(item); }

		/// <summary>Performs an action without clearing the redo stack.</summary>
		/// <remarks>All tentative actions pending at the same time are placed
		/// in the same undo group. Call <see cref="AcceptTentativeAction"/> or 
		/// <see cref="Do"/> to finalize, or <see cref="UndoTetativeAction"/> to
		/// undo.
		public virtual void DoTentatively(DoOrUndo action)
		{
			_tempStack.Add(new Command(action, false).Do());
			AfterAction(true);
		}
		/// <summary>If tentative action(s) have been performed, they are now added 
		/// to the undo stack and the redo stack is cleared.</summary>
		/// <param name="finish">Whether the tentative action is completed
		/// (if so, further actions are placed in a new undo group)</param>
		public bool AcceptTentativeAction(bool finish = true)
		{
			if (!_tempStack.IsEmpty)
			{
				_tempStack.Last = _tempStack.Last.WithSeparatorFlag(finish);

				_redoStack.Clear();
				foreach (var item in _tempStack)
					_undoStack.Push(item);
				_tempStack.Clear();
				return true;
			}
			return false;
		}

		/// <summary>Undoes any temporary actions performed with <see cref="DoTentatively"/>.</summary>
		public void UndoTentativeAction()
		{
			while (!_tempStack.IsEmpty)
			{
				_tempStack.Last.Undo();
				_tempStack.Pop();
			}
		}

		#endregion
	}
}
