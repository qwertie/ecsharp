using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PointT = Loyc.Geometry.Point<float>;

namespace BoxDiagrams
{
	public enum BoxType
	{
		Rect, Ellipse, Borderless
	}
	public struct LinearText
	{
		public string Text;
		public float Justify; // 0..1
	}
	public enum SelType
	{
		No, Yes, Partial
	}
	public class Anchor
	{
		public Anchor(AnchorShape shape, Func<PointT> point, int angles = 0xFF) { _shape = shape; _point = point; _angles = angles; }
		Func<PointT> _point;
		AnchorShape _shape;
		int _angles;
		public AnchorShape Shape { get { return _shape; } }
		public int Mod8AngleFlags { get { return _angles; } }
		public PointT Point { get { return _point(); } }
	}
}
