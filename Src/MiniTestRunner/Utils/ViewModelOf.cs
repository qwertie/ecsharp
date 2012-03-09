using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner.ViewModel
{
	public class ViewModelOf<T>
	{
		protected T _model;

		public ViewModelOf(T model)
		{
			_model = model;
		}
		public T Model
		{
			get { return _model; }
		}
		public override bool Equals(object obj)
		{
			var vm = obj as ViewModelOf<T>;
			return vm != null && vm._model.Equals(_model);
		}
		public override int GetHashCode()
		{
			return _model.GetHashCode();
		}
	}
}
