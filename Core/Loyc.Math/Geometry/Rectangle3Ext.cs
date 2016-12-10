using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Geometry
{
	/// <summary>Contains methods to manipulate rectangles.</summary>
	/// <remarks>Unfortunately, C# often can't infer the type parameters. Many of 
	/// these methods must be called with explicit type parameters.</remarks>
	public static class Rectangle3Ext
	{
		public static bool IsNormal<Rect, T>(this Rect r)
			where Rect : IRectangle3Reader<T>
			where T : IComparable<T>
		{
			// Hey Microsoft, this would probably be faster if the built-in types 
			// implemented simple boolean methods: IsLess(), IsLessOrEqual().
			return r.X2.CompareTo(r.X1) >= 0 && r.Y2.CompareTo(r.Y1) >= 0 && r.Z2.CompareTo(r.Z1) >= 0;
		}
		public static void Normalize<Rect, T>(this Rect r)
			where Rect : IRectangle3Base<T>
			where T : IComparable<T>
		{
			T z1 = r.X1, z2 = r.X2;
			if (r.Z2.CompareTo(r.Z1) < 0)
				r.SetZRange(z2, z1);
			RectangleExt.Normalize<Rect, T>(r);
		}
		
		public static void SetRect<Rect, T>(this Rect r, T x, T y, T z, T width, T height, T depth)
			where Rect : IRectangle3Base<T>
		{
			r.SetXAndWidth(x, width);
			r.SetYAndHeight(y, height);
			r.SetZAndDepth(z, depth);
		}
		public static void SetRange<Rect, T>(this Rect r, T x1, T y1, T z1, T x2, T y2, T z2)
			where Rect : IRectangle3Base<T>
		{
			r.SetXRange(x1, x2);
			r.SetYRange(y1, y2);
			r.SetZRange(z1, z2);
		}
	}
}
