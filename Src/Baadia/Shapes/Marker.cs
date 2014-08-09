using System.Collections.Generic;
using System.Windows.Forms;
using Loyc.Collections;
using Loyc.Geometry;
using Util.WinForms;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using ProtoBuf;
using System;
using System.Runtime.Serialization;

namespace BoxDiagrams
{
	[ProtoContract(SkipConstructor=true)]
	public class Marker : Shape
	{
		static int NextZOrder = 0x20000000;

		public Marker(DiagramDrawStyle style, PointT point, float radius, MarkerPolygon type)
		{
			LL = new LLMarker(style, point, radius, type) { ZOrder = NextZOrder++ };
			Style = style;
		}

		[OnDeserializing]
		void PB_BD(StreamingContext sc) { LL = new LLMarker(Style, default(PointT), 10, MarkerPolygon.UpTriangle); }
		[ProtoAfterDeserialization]
		void PB_AD() { LL.Style = Style; }
		
		public override IEnumerable<Anchor> DefaultAnchors 
		{
			get { return new Repeated<Anchor>(Anchor(() => this.Point), 1); }
		}
		public override Anchor GetNearestAnchor(PointT p, int exitAngleMod8 = -1)
		{
			return Anchor(() => this.Point);
		}
		[ProtoIgnore]
		public LLMarker LL;
		[ProtoMember(3)]
		PointT Point { get { return LL.Point; } set { LL.Point = value; } }
		[ProtoMember(4)]
		MarkerPolygon Type { get { return LL.Type; } set { LL.Type = Type; } }
		[ProtoMember(5)]
		float Radius { get { return LL.Radius; } set { LL.Radius = value; } }

		public override void AddLLShapesTo(ICollection<LLShape> list)
		{
			LL.Style = Style;
			list.Add(LL);
		}
		public override BoundingBox<Coord> BBox
		{
			get { var bb = new BoundingBox<Coord>(Point, Point); bb.Inflate(Radius); return bb; }
		}
		public override HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			var dif = pos.Sub(Point).Abs();
			if (dif.X <= hitTestRadius.X && dif.Y <= hitTestRadius.Y)
				return new HitTestResult(this, sel != SelType.No ? Cursors.SizeAll : Cursors.Arrow);
			return null;
		}
		public override void AddAdornersTo(ICollection<LLShape> list, SelType selMode, VectorT hitTestRadius)
		{
			var copy = (LLMarker)LL.Clone();
			copy.Type = MarkerPolygon.Square;
			copy.Radius = hitTestRadius.X;
			copy.Style = SelAdornerStyle;
			list.Add(copy);
		}

		public override int DrawZOrder { get { return LL.ZOrder; } }

		public override Util.UI.DoOrUndo DragMoveAction(HitTestResult htr, VectorT amount)
		{
			return @do => {
				if (@do) {
					Point += amount;
				} else {
					Point -= amount;
				}
			};
		}

		public override string PlainText() { return null; }
	}
}
