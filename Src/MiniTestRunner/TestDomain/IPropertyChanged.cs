using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner.TestDomain
{
	/// <summary>Same as INotifyPropertyChanged, except that it can be used across 
	/// AppDomains because it does not use the PropertyChangedEventArgs argument
	/// (which is not serializable or marshalable).</summary>
	public interface IPropertyChanged
	{
		event PropertyChangedDelegate PropertyChanged;
	}

	public delegate void PropertyChangedDelegate(object sender, string propertyName);

	/// <summary>Helper base class for implementing <see cref="IPropertyChanged"/>.</summary>
	public class PropertyChangedHelper : MarshalByRefObject, IPropertyChanged
	{
		public event PropertyChangedDelegate PropertyChanged;

		protected virtual void Changed(string prop)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, prop);
		}
		protected void Set<T>(ref T var, T value, string propName)
		{
			if (var == null ? value != null : !var.Equals(value)) {
				var = value;
				Changed(propName);
			}
		}
		protected void Set(ref int var, int value, string propName)
		{
			if (var != value) {
				var = value;
				Changed(propName);
			}
		}
	}
}
