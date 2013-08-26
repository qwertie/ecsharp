using Loyc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Util.WinForms
{
	/// <summary>Holds display attributes used by one or more shapes.</summary>
	/// <remarks>DrawStyle is meant to be shared among multiple shapes.</remarks>
	[System.Xml.Serialization.XmlType] // protobuf-net recognizes this
	public class DrawStyle : ICloneable<DrawStyle>
	{
		public DrawStyle() { }
		public DrawStyle(Color lineColor, float lineWidth, Color fillColor) 
		{
			_lineColor = lineColor;
			_lineWidth = lineWidth;
			_fillColor = fillColor;
		}

		private Color _lineColor = Color.Black;
		[XmlElement(Order = 1)]
		public Color LineColor
		{
			get { return _lineColor; }
			set { _lineColor = value; DisposePen(); }
		}

		private float _lineWidth = 1f;
		[XmlElement(Order = 2)]
		public float LineWidth
		{
			get { return _lineWidth; }
			set { _lineWidth = value; DisposePen(); }
		}
		public bool OutlineBehindFill = false;
		private DashStyle _lineStyle;
		[XmlElement(Order = 3)]
		public DashStyle LineStyle
		{
			get { return _lineStyle; }
			set { _lineStyle = value; DisposePen(); }
		}

		private Color _fillColor = Color.WhiteSmoke;
		[XmlElement(Order = 4)]
		public Color FillColor
		{
			get { return _fillColor; }
			set { _fillColor = value; DisposeBrush(); }
		}
		private Color _textColor = Color.Black;
		[XmlElement(Order = 5)]
		public Color TextColor
		{
			get { return _textColor; }
			set { _textColor = value; DisposeTextBrush(); }
		}
		static Font DefaultFont = new Font(FontFamily.GenericSansSerif, 10f);
		[XmlElement(Order = 6)]
		public Font Font = DefaultFont;

		static Color MixOpacity(Color one, int two) { return Color.FromArgb(one.A * (two + (two >> 7)) >> 8, one); }

		Pen _pen;
		public Pen Pen(int opacity) {
			if (opacity >= 255)
				return _pen = _pen ?? NewPen(LineColor, LineWidth, LineStyle);
			else
				return NewPen(MixOpacity(LineColor, opacity), LineWidth, LineStyle);
		}
		static Pen NewPen(Color c, float w, DashStyle ls)
		{
			return c.A < 5 ? null : new Pen(c, w) { DashStyle = ls };
		}

		Brush _brush;
		public Brush Brush(int opacity) {
			if (opacity >= 255)
				return _brush = _brush ?? NewBrush(FillColor);
			else
				return NewBrush(MixOpacity(FillColor, opacity));
		}
		static Brush NewBrush(Color c)
		{
			return c.A < 5 ? null : new SolidBrush(c);
		}

		Brush _textBrush;
		public Brush TextBrush(int opacity)
		{
			if (opacity >= 255)
				return _textBrush = _textBrush ?? NewBrush(TextColor);
			else
				return NewBrush(MixOpacity(TextColor, opacity));
		}
		private void DisposePen()
		{
			if (_pen != null) {
				_pen.Dispose();
				_pen = null;
			}
		}
		private void DisposeBrush()
		{
			if (_brush != null) {
				_brush.Dispose();
				_brush = null;
			}
		}
		private void DisposeTextBrush()
		{
			if (_textBrush != null) {
				_textBrush.Dispose();
				_textBrush = null;
			}
		}

		public virtual DrawStyle Clone()
		{
			var copy = (DrawStyle)MemberwiseClone();
			// resources owned by original instance
			copy._pen = null;
			copy._brush = null; 
			copy._textBrush = null; 
			return copy;
		}
	}
}
