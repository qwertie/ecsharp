using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PointD = System.Windows.Point;
using VectorD = System.Windows.Vector;
using System.Drawing;
using Loyc.Collections;
using Loyc;
using System.Drawing.Drawing2D;

namespace BoxDiagrams
{
	public abstract class LLShape
	{
		public DrawStyle Style;
		public int ZOrder;
		public abstract void Draw(Graphics g);
		
		public static void DrawPolygon(Graphics g, DrawStyle style, PointF[] points, int[] divisions)
		{
			if (divisions != null && divisions.Length != 0) {
				using (var gp = new GraphicsPath()) {
					int prev = 0;
					for (int i = 0; i < divisions.Length; i++) {
						gp.AddPolygon(points.Slice(prev, divisions[i] - prev).ToArray());
						prev = divisions[i];
					}
					gp.AddPolygon(points.Slice(prev, points.Length - prev).ToArray());
					g.FillPath(style.Brush, gp);
				}
			} else
				g.DrawPolygon(style.Pen, points);
		}
	}
	public class LLMarker : LLShape
	{
		public MarkerPolygon Type;
		public double Radius;
		public PointF Point;
		public override void Draw(Graphics g)
		{
			var pts = Type.Points.ToArray();
			var divs = Type.Divisions.ToArray();
			DrawPolygon(g, Style, pts, divs);
		}
	}
	public class LLPolygon : LLShape
	{
		public PointF[] Points;
		public int[] Divisions;
		public override void Draw(Graphics g)
		{
			DrawPolygon(g, Style, Points, Divisions);
		}
	}
	public class LLLine : LLShape
	{
		public PointF[] Points;
		public override void Draw(Graphics g)
		{
			g.DrawLines(Style.Pen, Points);
		}
	}

}
