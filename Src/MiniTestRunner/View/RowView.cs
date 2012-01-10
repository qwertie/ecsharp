using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using MiniTestRunner.Properties;

namespace MiniTestRunner
{
	// WinForms-specific code of TestRowVM
	partial class RowVM
	{
		static Dictionary<TestNodeType, Image> _typeIcons;
		static Dictionary<TestStatus, Image> _statusIcons;
		static Image _highPriorityIcon, _lowPriorityIcon;

		public Image TypeIcon
		{
			get {
				if (_typeIcons == null)
				{
					_typeIcons = new Dictionary<TestNodeType, Image>();
					_typeIcons[TestNodeType.Assembly] = Resources.Assembly;
					_typeIcons[TestNodeType.Note] = Resources.Note;
					_typeIcons[TestNodeType.TestFixture] = Resources.Class;
					_typeIcons[TestNodeType.TestSet] = Resources.TestSet;
					_typeIcons[TestNodeType.Test] = Resources.Test;
				}
				Image icon;
				_typeIcons.TryGetValue(Type, out icon);
				return icon;
			}
		}

		public Image PriorityIcon
		{
			get {
				if (_highPriorityIcon == null)
				{
					_highPriorityIcon = Resources.HighPriority;
					_lowPriorityIcon = Resources.LowPriority;
				}
				if (Priority > 0)
					return _highPriorityIcon;
				else if (Priority < 0)
					return _lowPriorityIcon;
				return null;
			}
		}
		public Image StatusIcon
		{
			get {
				if (_statusIcons == null)
				{
					_statusIcons = new Dictionary<TestStatus, Image>();
					_statusIcons[TestStatus.Running] = Resources.StatusRunning;
					_statusIcons[TestStatus.Success] = Resources.StatusSuccess;
					_statusIcons[TestStatus.SuccessWithMessage] = Resources.StatusSuccessWithMessage;
					_statusIcons[TestStatus.Error] = Resources.StatusError;
					_statusIcons[TestStatus.Inconclusive] = Resources.StatusInconclusive;
					_statusIcons[TestStatus.Ignored] = Resources.StatusIgnored;
				}
				Image icon;
				_statusIcons.TryGetValue(Status, out icon);
				return icon;
			}
		}
	}
}
