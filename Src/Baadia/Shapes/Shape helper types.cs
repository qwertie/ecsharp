using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PointT = Loyc.Geometry.Point<float>;
using ProtoBuf;

namespace BoxDiagrams
{
	public enum BoxType
	{
		Rect, Ellipse, Borderless
	}
	[ProtoContract]
	public struct LinearText
	{
		[ProtoMember(1)]
		public string Text;
		[ProtoMember(2)]
		public float Justify; // 0..1
	}
	public class Anchor
	{
		public Anchor(Shape shape, Func<PointT> point, int angles = 0xFF) { _shape = shape; _point = point; _angles = angles; }
		Func<PointT> _point;
		Shape _shape;
		int _angles;
		public Shape Shape { get { return _shape; } }
		public int Mod8AngleFlags { get { return _angles; } }
		public PointT Point { get { return _point(); } }
	}
}
