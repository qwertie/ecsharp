using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateControls.Fields;

namespace MiniTestRunner
{
	public class OptionsModel
	{
		public OptionsModel()
		{
			//PartialTrust = true;
		}

		Independent<bool> _loadLastProjectOnStartup = new Independent<bool>("LoadLastProjectOnStartup", default(bool));
		public bool LoadLastProjectOnStartup { get { return _loadLastProjectOnStartup.Value; } set { _loadLastProjectOnStartup.Value = value; } }
		Independent<bool> _runTestsOnLoad = new Independent<bool>("RunTestsOnLoad", default(bool));
		public bool RunTestsOnLoad { get { return _runTestsOnLoad.Value; } set { _runTestsOnLoad.Value = value; } }
		Independent<bool> _runTestsOnChange = new Independent<bool>("RunTestsOnChange", default(bool));
		public bool RunTestsOnChange { get { return _runTestsOnChange.Value; } set { _runTestsOnChange.Value = value; } }
		Independent<bool> _autoUnload = new Independent<bool>("AutoUnload", default(bool));
		public bool AutoUnload { get { return _autoUnload.Value; } set { _autoUnload.Value = value; } }
		Independent<bool> _partialTrust = new Independent<bool>("PartialTrust", default(bool));
		public bool PartialTrust { get { return _partialTrust.Value; } set { _partialTrust.Value = value; } }
		Independent<bool> _alwaysOnTop = new Independent<bool>("AlwaysOnTop", default(bool));
		public bool AlwaysOnTop { get { return _alwaysOnTop.Value; } set { _alwaysOnTop.Value = value; } }
		Independent<int> _threadLimit = new Independent<int>("ThreadLimit", default(int));
		public int ThreadLimit { get { return _threadLimit.Value; } set { _threadLimit.Value = value; } }
		Independent<bool> _splitHorizontally = new Independent<bool>("SplitHorizontally", default(bool));
		public bool SplitHorizontally { get { return _splitHorizontally.Value; } set { _splitHorizontally.Value = value; } }
		Independent<bool> _outputPaneCollapsed = new Independent<bool>("OutputPaneCollapsed", default(bool));
		public bool OutputPaneCollapsed { get { return _outputPaneCollapsed.Value; } set { _outputPaneCollapsed.Value = value; } }
		Independent<bool> _outputWordWrap = new Independent<bool>("OutputWordWrap", default(bool));
		public bool OutputWordWrap { get { return _outputWordWrap.Value; } set { _outputWordWrap.Value = value; } }
	}
}
