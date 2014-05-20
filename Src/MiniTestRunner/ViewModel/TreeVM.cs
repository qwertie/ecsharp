using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateControls.Collections;
using UpdateControls.Fields;
using MiniTestRunner.Model;
using Loyc.Collections;

namespace MiniTestRunner.ViewModel
{
	public class ProjectVM : ViewModelOf<ProjectModel>
	{
		public ProjectVM(ProjectModel model, FilterVM filter) : base(model)
		{
			_filter = filter ?? new FilterVM();
			_roots = new DependentList<RowVM>(() => Filter.ApplyTo(_model.Roots.Upcast<RowModel, TaskRowModel>()).Select(m => new RowVM(m, this, null)));
			_roots.DependentSentry.Invalidated += () => FireChildrenInvalidated(null);
		}

		readonly FilterVM _filter;
		public FilterVM Filter { get { return _filter; } }

		DependentList<RowVM> _roots;
		public DependentList<RowVM> Roots { get { return _roots; } }

		internal void FireRowInvalidated(RowVM row) { var p = RowInvalidated; if (p != null) p(row); }
		internal void FireChildrenInvalidated(RowVM row) { var p = ChildrenInvalidated; if (p != null) p(row); }
		public event Action<RowVM> RowInvalidated;
		public event Action<RowVM> ChildrenInvalidated;
	}
}
