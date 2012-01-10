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
	public interface IRowModel : INotifyPropertyChanged
	{
		string Name { get; }
		TestNodeType Type { get; }
		TestStatus Status { get; }
		string Summary { get; }
		IList<IRowModel> Children { get; }
		int Priority { get; set; }
		/// <summary>Sum of priorities of parents.</summary>
		int InheritedPriority { get; }
		/// <summary>Original list index. Used for stable sorting.</summary>
		int ListOrder { get; set; }
		ITask Task { get; }
	}

	public static class TestRowModelExt
	{
		public static int TotalPriority(this IRowModel m)
		{
			return m.InheritedPriority + m.Priority;
		}
	}

	public enum TestNodeType
	{
		Assembly, TestFixture, TestSet, Test, Note
	}

	public enum TestStatus
	{
		NotRun, Running, Success, SuccessWithMessage, Inconclusive, Ignored, Error, AggregateStatus
	}
}
