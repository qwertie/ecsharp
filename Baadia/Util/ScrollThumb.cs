using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc.Geometry;
using Util.WinForms;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;
using System.Drawing;

namespace Util.WinForms
{
	public class ScrollThumb : IShapeWidget
	{
		LLShapeControl _container;
		float _xAlign, _yAlign;
		float _radius;

		public PointT Center { 
			get {
				var csize = _container.ClientSize;
				return new PointT((_xAlign + 1) / 2 * csize.Width,
								  (_yAlign + 1) / 2 * csize.Height);
			}
		}

		public ScrollThumb(LLShapeControl container, float xAlign, float yAlign, float radius = 24) 
			{ _container = container; _xAlign = xAlign; _yAlign = yAlign; _radius = radius; }

		#region IInputWidget Members

		public HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel)
		{
			var c = Center;
			if (c.Sub(pos).Length() > _radius)
				return null;

			return new HitTestResult(this, Cursors.SizeAll);
		}

		public void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
		{
		}

		public void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
		{
		}

		public void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
		}

		#endregion

		static DrawStyle style = new DrawStyle(Color.Gray, 2, Color.FromArgb(192, Color.White));

		public void AddLLShapesTo(ICollection<LLShape> list)
		{
			list.Add(new LLMarker(style, Center, _radius, MarkerPolygon.ScrollThumb));
		}

		public void AddAdornersTo(ICollection<LLShape> list, SelType selMode, Loyc.Geometry.Vector<float> hitTestRadius)
		{
			throw new NotImplementedException();
		}
	}
}
