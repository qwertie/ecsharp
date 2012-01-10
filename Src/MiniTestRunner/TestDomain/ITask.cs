using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MiniTestRunner.TestDomain
{
	/// <summary>Task interface needed by TaskRunner and the GUI</summary>
	public interface ITask : IPropertyChanged
	{
		void RunOnCurrentThread();

		TestStatus Status { get; }
		int ThreadLimit { get; }
		bool CanAbort { get; }
		DateTime LastRunAt { get; }
		TimeSpan RunTime { get; }
	}

	/// <summary>Task interface needed by TaskRowModel</summary>
	public interface ITaskEx : ITask
	{
		Stream OutputStream { get; set; }
		IList<IRowModel> Children { get; }
		string Summary { get; }
	}
}
