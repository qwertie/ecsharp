using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.ComponentModel;
using System.IO;
using MiniTestRunner.TestDomain;

namespace MiniTestRunner
{
	/// <summary>See <see cref="RowModel"/></summary>
	//public interface IRowModel : INotifyPropertyChanged
	//{
	//    string Name { get; }
	//    TestNodeType Type { get; }
	//    IList<IRowModel> Children { get; }
	//    int BasePriority { get; set; }
	//    int InheritedPriority { get; set; }

	//    ITestTask Task { get; }

	//    // Properties may be copied from the ITestTask or ITaskEx
	//    string Summary { get; }
	//    TestStatus Status { get; }
	//    DateTime LastRunAt { get; }
	//    TimeSpan RunTime { get; }
	//}

	public enum TestNodeType
	{
		Assembly, TestFixture, TestSet, Test, Note
	}

	public enum TestStatus
	{
		None,
		NotRun,
		Success,
		SuccessWithMessage,
		Running,
		Inconclusive,
		Ignored,
		Error
	}
}
