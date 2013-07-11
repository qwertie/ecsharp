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
					return mouseMove
						.StartWith(start)
						.TakeUntil(lMouseUp)
						.Do(e => {
							prevTicks += (msec = Environment.TickCount - prevTicks);
							var pt = e.EventArgs.Location.AsLoyc();
							if (dragSeq.Count == 0 || (pt - dragSeq.Last.Point).Length() >= MinDistBetweenDragPoints) {
								dragSeq.Add(new DragPoint(pt, msec, dragSeq));
								AnalyzeGesture(dragSeq, false);
							}
						}, () => AnalyzeGesture(dragSeq, true));
				})
				.Subscribe();
			}
		}

		const int MinDistBetweenDragPoints = 2;

		struct DragPoint
		{
			public DragPoint(Point<int> p, int ms, IList<DragPoint> prevPts)
			{
				Point = p;
				MsecSincePrev = ms;
				RootSecPer1000px = MathEx.Sqrt(SecPer1000px(Point, ms, prevPts));
			}
			static float SecPer1000px(Point<int> next, int ms, IList<DragPoint> prevPts)
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
			public readonly Point<int> Point;
			public readonly int MsecSincePrev;
			public float RootSecPer1000px; // Sqrt(seconds per 1000 pixels of movement)
		}

		// The drag recognizers take a list of points as input, and produce a shape 
		// and a "pain factor" as output (the shape is null if recognition fails). 
		// In case of ambiguity, the lowest pain factor wins.
		List<Func<IList<DragPoint>, Pair<Shape, int>>> DragRecognizers;

		void AnalyzeGesture(IList<DragPoint> dragSeq, bool mouseUp)
		{
			// TODO: Analyze on separate thread 

			if (IsDrag(dragSeq)) {
				var results = new List<Pair<Shape,int>>();
				foreach (var rec in DragRecognizers) {
					var r = rec(dragSeq);
					if (r.A != null)
						results.Add(r);
				}
				
			}
		}
		static bool IsDrag(IList<DragPoint> dragSeq)
		{
			Point<int> first = dragSeq[0].Point;
			Size ds = SystemInformation.DragSize;
			return dragSeq.Any(p => {
				var delta = (p.Point - first);
				return Math.Abs(delta.X) > ds.Width || Math.Abs(delta.Y) > ds.Height;
			});
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
