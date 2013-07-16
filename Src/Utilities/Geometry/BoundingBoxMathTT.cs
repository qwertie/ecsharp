
// This is a generated file
using System;
using System.Collections.Generic;
using Loyc.Math;

namespace Loyc.Geometry
{
	using T = System.Int32;
	using BoundingBox = BoundingBox<int>;
	using System;

	public static partial class BoundingBoxMath
	{
		public static void Deflate(this BoundingBox self, T amountX, T amountY) { Inflate(self, -amountX, -amountY); }
		public static void Inflate(this BoundingBox self, T amountX, T amountY)
		{
			if (amountX < 0 && -amountX * 2 >= self.Width())
				self.SetXAndWidth(MathEx.Average(self.X1, self.X2), 0);
			else
				self.SetXAndWidth(self.X1 - amountX, self.X2 + amountX);
			if (amountY < 0 && -amountY * 2 <= self.Height())
				self.SetYAndHeight(MathEx.Average(self.Y1, self.Y2), 0);
			else
				self.SetYAndHeight(self.Y1 - amountY, self.Y2 + amountY);
		}
		public static BoundingBox Deflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, -amountX, -amountY);
			return copy;
		}
		public static BoundingBox Inflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, amountX, amountY);
			return copy;
		}

		public static T Width(this BoundingBox bb) { return bb.X2 - bb.X1; }
		public static T Height(this BoundingBox bb) { return bb.Y2 - bb.Y1; }

		public static Point<T> ProjectOnto(this Point<T> p, BoundingBox bbox)
		{
			return new Point<T>(MathEx.InRange(p.X, bbox.X1, bbox.X2), MathEx.InRange(p.X, bbox.X1, bbox.X2));
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Single;
	using BoundingBox = BoundingBox<float>;
	using System;

	public static partial class BoundingBoxMath
	{
		public static void Deflate(this BoundingBox self, T amountX, T amountY) { Inflate(self, -amountX, -amountY); }
		public static void Inflate(this BoundingBox self, T amountX, T amountY)
		{
			if (amountX < 0 && -amountX * 2 >= self.Width())
				self.SetXAndWidth(MathEx.Average(self.X1, self.X2), 0);
			else
				self.SetXAndWidth(self.X1 - amountX, self.X2 + amountX);
			if (amountY < 0 && -amountY * 2 <= self.Height())
				self.SetYAndHeight(MathEx.Average(self.Y1, self.Y2), 0);
			else
				self.SetYAndHeight(self.Y1 - amountY, self.Y2 + amountY);
		}
		public static BoundingBox Deflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, -amountX, -amountY);
			return copy;
		}
		public static BoundingBox Inflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, amountX, amountY);
			return copy;
		}

		public static T Width(this BoundingBox bb) { return bb.X2 - bb.X1; }
		public static T Height(this BoundingBox bb) { return bb.Y2 - bb.Y1; }

		public static Point<T> ProjectOnto(this Point<T> p, BoundingBox bbox)
		{
			return new Point<T>(MathEx.InRange(p.X, bbox.X1, bbox.X2), MathEx.InRange(p.X, bbox.X1, bbox.X2));
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Double;
	using BoundingBox = BoundingBox<double>;
	using System;

	public static partial class BoundingBoxMath
	{
		public static void Deflate(this BoundingBox self, T amountX, T amountY) { Inflate(self, -amountX, -amountY); }
		public static void Inflate(this BoundingBox self, T amountX, T amountY)
		{
			if (amountX < 0 && -amountX * 2 >= self.Width())
				self.SetXAndWidth(MathEx.Average(self.X1, self.X2), 0);
			else
				self.SetXAndWidth(self.X1 - amountX, self.X2 + amountX);
			if (amountY < 0 && -amountY * 2 <= self.Height())
				self.SetYAndHeight(MathEx.Average(self.Y1, self.Y2), 0);
			else
				self.SetYAndHeight(self.Y1 - amountY, self.Y2 + amountY);
		}
		public static BoundingBox Deflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, -amountX, -amountY);
			return copy;
		}
		public static BoundingBox Inflated(this BoundingBox self, T amountX, T amountY)
		{
			var copy = self.Clone();
			Inflate(copy, amountX, amountY);
			return copy;
		}

		public static T Width(this BoundingBox bb) { return bb.X2 - bb.X1; }
		public static T Height(this BoundingBox bb) { return bb.Y2 - bb.Y1; }

		public static Point<T> ProjectOnto(this Point<T> p, BoundingBox bbox)
		{
			return new Point<T>(MathEx.InRange(p.X, bbox.X1, bbox.X2), MathEx.InRange(p.X, bbox.X1, bbox.X2));
		}
	}
}

namespace Loyc.Geometry
{
	public static partial class BoundingBoxExt
	{
		public static BoundingBox<T> ToBoundingBox<T>(this IEnumerable<Point<T>> pts) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return ToBoundingBox(pts.GetEnumerator());
		}
		public static BoundingBox<T> ToBoundingBox<T>(this IEnumerator<Point<T>> e) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			if (!e.MoveNext())
				return null;
			var bb = new BoundingBox<T>(e.Current);
			while (e.MoveNext())
				RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(bb, e.Current);
			return bb;
		}
		public static BoundingBox<T> ToBoundingBox<T>(this LineSegment<T> seg) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return new BoundingBox<T>(seg.A, seg.B);
		}
		public static Point<T> ProjectOnto<T>(this Point<T> p, BoundingBox<T> bbox) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return new Point<T>(MathEx.InRange(p.X, bbox.X1, bbox.X2), MathEx.InRange(p.X, bbox.X1, bbox.X2));
		}
		public static System.Drawing.Rectangle ToBCL(this BoundingBox<int> bbox)
		{
			return new System.Drawing.Rectangle(bbox.X1, bbox.Y1, bbox.X2 - bbox.X1, bbox.Y2 - bbox.Y1);
		}
		public static System.Drawing.RectangleF ToBCL(this BoundingBox<float> bbox)
		{
			return new System.Drawing.RectangleF(bbox.X1, bbox.Y1, bbox.X2 - bbox.X1, bbox.Y2 - bbox.Y1);
		}
	}
}
