using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Util.WinForms
{
	/// <summary>Base class for a control that supports zooming/scrolling, drawing, 
	/// mouse interaction with shapes.</summary>
	/// Design/refactoring is not finished...
	public class LLShapeWidgetControl : LLShapeControl
	{
		protected MSet<IShapeWidget> _widgets = new MSet<IShapeWidget>();
		protected LLShapeLayer _toolButtonLayer;

		protected LLShapeWidgetControl()
		{
			_widgets.Add(new ScrollThumb(this, 0, -1));
			_widgets.Add(new ScrollThumb(this, -1, 0));
			_widgets.Add(new ScrollThumb(this, 1, 0));
			_widgets.Add(new ScrollThumb(this, 0, 1));
			_toolButtonLayer = AddLayer(false);
			RefreshButtonsNonvirtual();
		}

		protected virtual void RefreshButtons() { RefreshButtonsNonvirtual(); }
		void RefreshButtonsNonvirtual()
		{
			var s = new MSet<LLShape>();
			foreach (var widget in _widgets)
				widget.AddLLShapesTo(s);
			_toolButtonLayer.Shapes = s;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			RefreshButtons();
		}
	}
}
