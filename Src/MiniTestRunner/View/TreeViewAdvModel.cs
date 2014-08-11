using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aga.Controls.Tree;
using Loyc.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using MiniTestRunner.ViewModel;
using UpdateControls.Forms;
using System.Diagnostics;

namespace MiniTestRunner.WinForms
{
	// The Aga tree control is mostly reflection-based, i.e. the control's 
	// "Node Controls" use reflection to read the properties of each row of the
	// tree. The ITreeModel's job is simply
	// - to report the tree roots 
	// - to report the children of each node
	// - to notify the tree control when the tree structure changes. There seems
	//   to be no event to notify the tree when the content of a row changes, so 
	//   we must refresh the tree control in that case.
	class TreeViewAdvModel : Aga.Controls.Tree.ITreeModel
	{
		HashSet<RowVM> _structureChanges = new HashSet<RowVM>();
		ProjectVM _tree;
		TreeViewAdv _treeView;
		bool _refreshing;
		GuiUpdateHelper _updater;

		public TreeViewAdvModel(ProjectVM treeVM, TreeViewAdv treeView)
		{
			_tree = treeVM;
			_treeView = treeView;
			_tree.ChildrenInvalidated += row => AutoRefresh(row, true);
			_tree.RowInvalidated += row => AutoRefresh(row, false);
		}

		void AutoRefresh(RowVM row, bool structureChanged)
		{
			if (structureChanged)
				_structureChanges.Add(row);
			if (!_refreshing) {
				_treeView.BeginInvoke(new Action(DoRefresh));
				_refreshing = true;
			}
		}
		void DoRefresh()
		{
			try {
				_treeView.Invalidate();
				if (StructureChanged != null) {
				Retry:
					try {
						foreach (var row in _structureChanges)
							FireStructureChangedUnder(row);
					} catch (InvalidOperationException) {
						Trace.WriteLine("TODO: Figure out why this happens");
						goto Retry;
					}
				}
			} finally {
				_structureChanges.Clear();
				_refreshing = false;
			}
		}
		void FireStructureChangedUnder(RowVM row)
		{
			if (row == null) { // roots changed
				// Reestablish dependencies from model to ViewModel
				_tree.Roots.DependentSentry.OnGet();
				StructureChanged(this, new TreePathEventArgs());
			} else {
				// Reestablish dependencies from model to ViewModel
				row.Children.DependentSentry.OnGet();
				TreePath path;
				if (row.Model.Parent == null)
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

		//void Roots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		//{
		//    RowVM.Synchronize(_roots, _model.Roots, null);

		//    // We subscribe to property-change events ONLY on the roots.
		//    foreach (var vm in _roots)
		//    {
		//        vm.PropertyChanged -= RowPropertyChanged;
		//        vm.PropertyChanged += RowPropertyChanged;
		//        vm.ChildPropertyChanged -= RowPropertyChanged;
		//        vm.ChildPropertyChanged += RowPropertyChanged;
		//    }

		//    StructureChanged(this, new TreePathEventArgs(TreePath.Empty));
		//}

		//void RowPropertyChanged(object sender, PropertyChangedEventArgs e)
		//{
		//    var row = (RowVM)sender;
		//    if (e.PropertyName == "Children")
		//        StructureChangedUnder(row);
		//    else if (RowChanged != null)
		//        RowChanged(row);
		//}

		public System.Collections.IEnumerable GetChildren(TreePath treePath)
		{
			if (treePath == null || treePath.LastNode == null)
				return _tree.Roots;
			else
				return ((RowVM)treePath.LastNode).Children;
		}

		public bool IsLeaf(TreePath treePath)
		{
			return ((RowVM)treePath.LastNode).Children.Count == 0;
		}

		public event EventHandler<TreeModelEventArgs> NodesChanged { add { } remove { } }
		public event EventHandler<TreeModelEventArgs> NodesInserted { add { } remove { } }
		public event EventHandler<TreeModelEventArgs> NodesRemoved { add { } remove { } }
		public event EventHandler<TreePathEventArgs> StructureChanged;
	}
}
