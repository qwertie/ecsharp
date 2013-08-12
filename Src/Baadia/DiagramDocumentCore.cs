using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Util.WinForms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace BoxDiagrams
{
	public class DiagramDocumentCore
	{
		public DiagramDocumentCore()
		{
			Shapes = new MSet<Shape>();
			Styles = new List<DiagramDrawStyle>();
		}

		public MSet<Shape> Shapes { get; set; }

		public List<DiagramDrawStyle> Styles { get; set; }

		public void Save(Stream stream)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				var ser = new YamlDotNet.RepresentationModel.Serialization.Serializer(YamlDotNet.RepresentationModel.Serialization.SerializationOptions.Roundtrip);
				ser.Serialize(writer, this);
			}
		}

		public static DiagramDocumentCore Load(Stream stream)
		{
			var reader = new StreamReader(stream, Encoding.UTF8);
			var des = new YamlDotNet.RepresentationModel.Serialization.Deserializer();
			return (DiagramDocumentCore)des.Deserialize(reader, typeof(DiagramDocumentCore));
		}

		//public class YamlConverter : IYamlTypeConverter
		//{
		//    public bool Accepts(Type type) { return type == typeof(???); }
		//
		//    public object ReadYaml(Parser parser, Type type)
		//    {
		//        var value = ((Scalar)parser.Current).Value;
		//        parser.MoveNext();
		//        return new ???(value);
		//    }
		//
		//    public void WriteYaml(Emitter emitter, object value, Type type)
		//    {
		//        emitter.Emit(new Scalar(((???)value).Value));
		//    }
		//}
	}

	public class DiagramDrawStyle : DrawStyle
	{
		public string Name;

		//public Color LineColor { get { return base.LineColor; } }
		//public float LineWidth { get { return base.LineWidth; } }
		//public DashStyle LineStyle { get { return base.LineStyle; } }
		//public Color FillColor { get { return base.FillColor; } }
		//public Color TextColor { get { return base.TextColor; } }
		//public Font Font { get { return base.Font; } }

		public override DrawStyle Clone()
		{
			var copy = base.Clone();
			Debug.Assert(((DiagramDrawStyle)copy).Name == Name);
			return copy;
		}
	}
}
