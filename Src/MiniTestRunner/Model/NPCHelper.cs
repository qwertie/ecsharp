using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MiniTestRunner
{
	/// <summary>Helper class for implementing INotifyPropertyChanged</summary>
	[Serializable]
	public class NPCHelper : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void Changed(string prop)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}
		protected bool Set<T>(ref T var, T value, string propName)
		{
			if (var == null ? value != null : !var.Equals(value)) {
				var = value;
				Changed(propName);
				return true;
			}
			return false;
		}
		protected bool Set(ref int var, int value, string propName)
		{
			if (var != value) {
				var = value;
				Changed(propName);
				return true;
			}
			return false;
		}
		protected bool Set(ref bool var, bool value, string propName)
		{
			if (var != value) {
				var = value;
				Changed(propName);
				return true;
			}
			return false;
		}
	}
}
