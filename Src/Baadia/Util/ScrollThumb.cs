using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc.Geometry;
using Util.WinForms;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

namespace Util.WinForms
{
	class ScrollThumb : IShapeWidget
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

			throw new NotImplementedException();
		}

		public void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			throw new NotImplementedException();
		}

		#endregion

		public void AddLLShapesTo(ICollection<LLShape> list)
		{
			throw new NotImplementedException();
		}

		public void AddAdornersTo(ICollection<LLShape> list, SelType selMode, Loyc.Geometry.Vector<float> hitTestRadius)
		{
			throw new NotImplementedException();
		}

	}
}
