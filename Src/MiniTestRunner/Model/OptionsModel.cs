using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner
{
	public class OptionsModel : NPCHelper
	{
		public OptionsModel()
		{
			ThreadLimit = 1;
			//PartialTrust = true;
		}

		public bool _loadLastProjectOnStartup, _runTestsOnLoad, _runTestsOnChange, _autoUnload, _partialTrust, _alwaysOnTop;
		public bool _splitHorizontally, _outputPaneCollapsed, _outputWordWrap;
		public int _threadLimit;

		public bool LoadLastProjectOnStartup { get { return _loadLastProjectOnStartup; } set { Set(ref _loadLastProjectOnStartup, value, "LoadLastProjectOnStartup"); } }
		public bool RunTestsOnLoad { get { return _runTestsOnLoad; } set { Set(ref _runTestsOnLoad, value, "RunTestsOnLoad"); } }
		public bool RunTestsOnChange { get { return _runTestsOnChange; } set { Set(ref _runTestsOnChange, value, "RunTestsOnChange"); } }
		public bool AutoUnload { get { return _autoUnload; } set { Set(ref _autoUnload, value, "AutoUnload"); } }
		public bool PartialTrust { get { return _partialTrust; } set { Set(ref _partialTrust, value, "PartialTrust"); } }
		public bool AlwaysOnTop { get { return _alwaysOnTop; } set { Set(ref _alwaysOnTop, value, "AlwaysOnTop"); } }
		public int ThreadLimit { get { return _threadLimit; } set { Set(ref _threadLimit, value, "ThreadLimit"); } }
		public bool SplitHorizontally { get { return _splitHorizontally; } set { Set(ref _splitHorizontally, value, "SplitHorizontally"); } }
		public bool OutputPaneCollapsed { get { return _outputPaneCollapsed; } set { Set(ref _outputPaneCollapsed, value, "OutputPaneCollapsed"); } }
		public bool OutputWordWrap { get { return _outputWordWrap; } set { Set(ref _outputWordWrap, value, "OutputWordWrap"); } }
	}
}
