using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoundingBoxF = Loyc.Math.BoundingBox<float>;
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
		public abstract float HitTest(float radius, out PointF projected);
		
		public static void DrawPolygon(Graphics g, DrawStyle style, PointF[] points, int[] divisions)
		{
			if (divisions != null && divisions.Length != 0) {
				using (var gp = new GraphicsPath()) {
					AddPolygon(points, divisions, gp);
					g.FillPath(style.Brush, gp);
				}
			} else
				g.DrawPolygon(style.Pen, points);
		}
		private static void AddPolygon(PointF[] points, int[] divisions, GraphicsPath gp)
		{
			int prev = 0;
			for (int i = 0; i < divisions.Length; i++) {
				gp.AddPolygon(points.Slice(prev, divisions[i] - prev).ToArray());
				prev = divisions[i];
			}
			gp.AddPolygon(points.Slice(prev, points.Length - prev).ToArray());
		}
		public static void HitTest(float radius, PointF[] points, int[] divisions)
		{
			using (var gp = new GraphicsPath()) {
				AddPolygon(points, divisions, gp);
				//gp.IsVisible(
			}

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
		public BoundingBoxF? BBox;
		public PointF[] Points;
		public int[] Divisions;
		public override void Draw(Graphics g)
		{
			DrawPolygon(g, Style, Points, Divisions);
		}
		//public override float HitTest(float radius, out PointF projected)
		//{
		//    if (BBox == null) {
		//        if (Points.Length == 0)
		//            return false;
		//        BBox = new BoundingBoxF(Points);
		//    }
			
		//    return BBox.Value.Contains(
		//}
	}
	public class LLLine : LLShape
	{
		public PointF[] Points;
		public override void Draw(Graphics g)
		{
			g.DrawLines(Style.Pen, Points);
		}
	}
	public class LLQuadraticCurve : LLShape
	{
		public PointF[] Points;
		public int PointsPerSeg = 8;
		public PointF[] Flattened;
		
		public override void Draw(Graphics g)
		{
			if (Flattened == null)
				Flatten();
			g.DrawLines(Style.Pen, Flattened);
		}

		PointF Between(PointF a, PointF b)
		{
			return new PointF((a.X + b.X) * .5f, (a.Y + b.Y) * .5f);
		}
		PointF Between(PointF a, PointF b, float percent)
		{
			float rest = 1 - percent;
			return new PointF(a.X * rest + b.X * percent, a.Y * rest + b.Y * percent);
		}

		public void Flatten()
		{
			if (Points.Length <= 2)
				Flattened = Points;
			else {
				int totalCount = PointsPerSeg * (Points.Length - 1) + 1;
				Flattened = new PointF[totalCount];
				int offs = 0;
				float per = 1f / PointsPerSeg;
				for (int i = 0; i < Points.Length-2; i++) {
					bool last = i == Points.Length - 2;
					PointF a = Points[i], b = Points[i + 1], c = Points[i + 2];
					PointF a_ = i == 0 ? a : Between(a, b);
					PointF c_ = last ? c : Between(b, c);
					Flatten(a_, b, c_, Flattened.Slice(offs, PointsPerSeg), per);
					offs += PointsPerSeg;
				}
				Flattened[Flattened.Length - 1] = Points[Points.Length - 1];
			}
		}
		private void Flatten(PointF a, PointF b, PointF c, ArraySlice<PointF> @out, float per)
		{
			@out[0] = a;
			float frac = per;
			for (int i = 1; i < @out.Count; i++, frac += per) {
				PointF d = Between(a, b, frac), e = Between(b, c, frac);
				@out[i] = Between(d, e, frac);
			}
		}
	}
}
