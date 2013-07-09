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

namespace BoxDiagrams
{
	enum Drag { Start, Move, Stop };

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
					var dragSeq = new List<Pair<Point,int>>();
					return mouseMove
						.StartWith(start)
						.TakeUntil(lMouseUp)
						.Do(e => {
							prevTicks += (msec = Environment.TickCount - prevTicks);
							dragSeq.Add(Pair.Create(e.EventArgs.Location, msec));
							AnalyzeGesture(dragSeq, false);
						}, () => AnalyzeGesture(dragSeq, true));
				})
				.Subscribe();
			}
			GC.Collect();
		}

		void AnalyzeGesture(List<Pair<Point, int>> dragSeq, bool mouseUp)
		{
			Trace.WriteLine(string.Format("{0} {1} {2}", dragSeq.Count, mouseUp, dragSeq.Select(p => p.A).Join(" ")));
		}

		private void Drag(Drag type)
		{
			
		}
	}

	// "Baadia": Boxes And Arrows Diagrammer
	//
	// Future flourishes:
	// - linear gradient brushes (modes: gradient across shape, or gradient across sheet)
	// - sheet background pattern/stretch bitmap
	// - box background pattern/stretch bitmap
	// - snap lines, plus a ruler on top and left to create and remove them
	//   - ruler itself can act as scroll bar
	// - text formatting override for parts of a box
}
