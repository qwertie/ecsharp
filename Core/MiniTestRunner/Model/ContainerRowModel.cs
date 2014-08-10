using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Loyc.Collections;
using MiniTestRunner.TestDomain;
using UpdateControls.Fields;

namespace MiniTestRunner
{
	[Serializable]
	class ContainerRowModel : RowModel
	{
		string _name;
		TestNodeType _type;
		IList<RowModel> _children;

		public ContainerRowModel(string name, TestNodeType type, List<RowModel> children)
		{
			_name = name;
			_type = type;
			_children = children;
		}

		public override string Name
		{
			get { return _name; }
		}
		
		IndependentS<string> _Summary = new IndependentS<string>("Summary", "");
		public override string Summary
		{
			get { return _Summary.Value; }
		}
		public void SetSummary(string value)
		{
			_Summary.Value = value;
		}

		public override IList<RowModel> Children
		{
			get { return _children; }
		}
		public override TestNodeType Type
		{
			get { return _type; }
		}
		public override ITestTask Task
		{
			get { return null; }
		}
	}
}
