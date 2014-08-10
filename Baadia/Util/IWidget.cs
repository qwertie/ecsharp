using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc.Collections;
using Util.Collections;
using Coord = System.Single;
using PointT = Loyc.Geometry.Point<float>;
using VectorT = Loyc.Geometry.Vector<float>;

namespace Util.WinForms
{
	public interface IInputWidget
	{
		HitTestResult HitTest(PointT pos, VectorT hitTestRadius, SelType sel);
		void OnKeyDown(KeyEventArgs e);
		void OnKeyUp(KeyEventArgs e);
		void OnKeyPress(KeyPressEventArgs e);
	}
	public interface IShapeWidget : IInputWidget
	{
		void AddLLShapesTo(ICollection<LLShape> list);
		void AddAdornersTo(ICollection<LLShape> list, SelType selMode, VectorT hitTestRadius);
	}
}
