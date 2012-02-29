using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aga.Controls.Tree;
using Loyc.Essentials;
using Loyc.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MiniTestRunner
{
	class TreeViewAdvModel : ITreeModel
	{
		TaskTreeModel _model;

		public TreeViewAdvModel(TaskTreeModel model)
		{
			_model = model;
			_model.Roots.CollectionChanged += Roots_CollectionChanged;
		}

		List<RowVM> _roots = new List<RowVM>();

		void Roots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RowVM.Synchronize(_roots, _model.Roots, null);

			// We subscribe to property-change events ONLY on the roots.
			foreach (var vm in _roots)
			{
				vm.PropertyChanged -= RowPropertyChanged;
				vm.PropertyChanged += RowPropertyChanged;
				vm.ChildPropertyChanged -= RowPropertyChanged;
				vm.ChildPropertyChanged += RowPropertyChanged;
			}

			StructureChanged(this, new TreePathEventArgs(TreePath.Empty));
		}

		void RowPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var row = (RowVM)sender;
			if (e.PropertyName == "Children")
				StructureChangedUnder(row);
			else if (RowChanged != null)
				RowChanged(row);
		}
		private void StructureChangedUnder(RowVM row)
		{
			if (StructureChanged != null)
			{
				TreePath path;
				if (row.Parent == null)
					path = new TreePath(row);
				else {
					var list = new DList<RowVM>();
					for (; row != null; row = row.Parent)
						list.PushFirst(row);
					path = new TreePath(list.ToArray());
				}
				StructureChanged(this, new TreePathEventArgs(path));
			}
		}

		public System.Collections.IEnumerable GetChildren(TreePath treePath)
		{
			if (treePath == null || treePath.LastNode == null)
				return _roots;
			else
				return ((RowVM)treePath.LastNode).Children;
		}

		public bool IsLeaf(TreePath treePath)
		{
			return ((RowVM)treePath.LastNode).Children.Count == 0;
		}

		public event Action<RowVM> RowChanged;
		public event EventHandler<TreeModelEventArgs> NodesChanged { add { } remove { } }
		public event EventHandler<TreeModelEventArgs> NodesInserted { add { } remove { } }
		public event EventHandler<TreeModelEventArgs> NodesRemoved { add { } remove { } }
		public event EventHandler<TreePathEventArgs> StructureChanged;
	}
}
