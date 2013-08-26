using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;

namespace BoxDiagrams
{
	public class ScrollRuler
	{
		public ScrollRuler(bool vertical)
		{
			
		}

		

		public bool Vertical { get; set; }
		public bool NearSide { get; set; }
		public int Thickness { get; set; }
		public float MinorTickInterval { get; set; }
		public float MajorTickInterval { get; set; }
		public Matrix DocumentTransform { get; set; }
		//public float ScaleFactor {
		//    get { return DocumentTransform ?? }
		//}
	}

}
