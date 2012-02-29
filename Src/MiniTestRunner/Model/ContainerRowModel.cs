using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Loyc.Collections;
using MiniTestRunner.TestDomain;

namespace MiniTestRunner
{
	[Serializable]
	class ContainerRowModel : RowModel
	{
		string _name, _summary;
		TestNodeType _type;
		IList<IRowModel> _children;

		public ContainerRowModel(string name, TestNodeType type, List<IRowModel> children)
		{
			_name = name;
			_type = type;
			_children = children;
		}

		public override string Name
		{
			get { return _name; }
		}
		public override string Summary
		{
			get { return _summary; }
		}
		public void SetSummary(string summary)
		{
			Set(ref _summary, summary, "Summary");
		}
		public override IList<IRowModel> Children
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
