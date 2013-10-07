using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using Loyc.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using MiniTestRunner.TestDomain;
using UpdateControls.Collections;
using MiniTestRunner.Model;

namespace MiniTestRunner.Model
{
	/// <summary>
	/// Holds a tree of testing tasks and manages the running of the tasks.
	/// </summary>
	/// <remarks>
	/// 
	/// 
	/// The architecture????
	/// 
	/// Quite often, MiniTestRunner is configured to keep assemblies unloaded when 
	/// they are not running. Thus, the tree must be essentially independent from 
	/// the assemblies that it is based on. Whenever an assembly is loaded, its 
	/// structure is examined and used to update the tree. When test results are
	/// received, likewise they are used to update the tree.
	/// 
	/// First of all, to make MiniTestRunner easier to implement, it uses Update 
	/// Controls, a library that automatically tracks dependencies, and MVVM 
	/// (model-view-viewmodel), a convension for structuring code.
	/// 
	/// MVVM
	/// ----
	/// 
	/// When you use MVVM, you divide your code into three categories: 
	/// (1) "model" code, which describes and manages data independently of the 
	///     user interface;
	/// (2) "viewmodel" code, which adapts the model to a form that is convenient 
	///     for the user interface to use; and
	/// (3) "view" code, which describes only the visual aspects of the user 
	///     interface. The view avoids having any state of its own, preferring to
	///     get all data from the viewmodel where possible.
	///     
	/// The models (i.e. the classes in the "model" layer) should have no access 
	/// to the viewmodels, and the viewmodels should have no access to the view. 
	/// References can only go in the opposite direction:
	/// 
	///     View ==> ViewModel ==> Model
	///     
	/// Also, controls do not talk to each other and the view avoids making any 
	/// decisions. For example, suppose the user clicks an item in a list and this 
	/// causes a few textboxes to be updated with details about the selected item. 
	/// The code for the listbox (if any) should not directly update the text boxes; 
	/// instead, the listbox notifies the viewmodel that the current item changed, 
	/// and text boxes watch for changes to the selection state (for example, by 
	/// subscribing to INotifyPropertyChanged on the viewmodel) and update 
	/// themselves in response to these changes. Doing all this manually is a pain--
	/// certainly harder than if the listbox just updated the text boxes directly.
	/// Therefore, we use libraries that make the job easier. For example, WPF has
	/// fairly powerful data-binding support, and Update Controls makes MVVM viable
	/// easier under WinForms (as well as WPF).
	/// 
	/// Usually, a view should not have a direct references to a model. However, 
	/// in my opinion, it's okay for the ViewModel to have a "Model" property that 
	/// the view can use to access the model if the user interface presents a view 
	/// that closely matches the model. It is far more important that the models
	/// do not have access to the viewmodels, and that the viewmodels do not have
	/// access to the view. If the ViewModel needs to do any user-interface things,
	/// such as displaying a notification, it must do so through some kind of
	/// constructor argument or event (for example, a ShowMessage event that the
	/// view must subscribe to). And showing 
	/// 
	/// So what's the purpose of MVVM? First of all, if you've written 
	/// "unstructured" programs without a conceptual framework like MVVM or MVC, 
	/// then you know your code tends to become fairly messy. It also tends to be
	/// unscriptable: another program can't easily ask your program to do something
	/// unless you also provide some kind of command-line interface, and if you
	/// do provide a command-line interface, it will only provide the features
	/// you specifically decided to support; it won't necessarily be able to do 
	/// everything that the use can do with the GUI. Finally, an unstructured
	/// program tends to be more difficult to refactor; for example, it may take
	/// a lot of work to create brand-new user interfaces that work with the same 
	/// data in a different way.
	/// 
	/// When used properly, MVVM fixes these problems. Firstly, it provides 
	/// guidelines about where you should put your code, so it won't be so messy.
	/// Second, if you make your models and views "public" then they are scriptable:
	/// somebody can write a new assembly that uses those classes entirely without 
	/// a user interface, or, even with the user interface intact, scriping may 
	/// be easier. Third, it is easier to modify the user interface. In an 
	/// unstructured program, where most of the code ends up in the window classes,
	/// have you ever needed to break one window up into two windows, or create a
	/// new window whose behavior is synchronized with an existing window? In an
	/// unstructured program (where your code is in the window classes), this tends 
	/// to be a messy job, involving different windows talking to each other and
	/// no clear place to put shared state. In MVVM you put all the state in the
	/// viewmodels, which are then served to the views. Keeping two windows in
	/// sync should work exactly the same as keeping controls in sync on a single
	/// window, because controls do not communicate directly with each other, they
	/// only communicate with the viewmodel. The structure of the viewmodel doesn't
	/// have t *match* the view, it only has to be *convenient* for the view to use,
	/// so you can change the view around and experiment with different GUI designs
	/// without necessarily having to change the viewmodels very much.
	///
	/// One final advantage to MVVM is that you can create a program for multiple 
	/// OSs or GUI systems based on the same codebase. I recently wrote a program
	/// in WinForms and then (at the behest of coworkers) created a new WPF-based 
	/// user interface, while keeping the WinForms version operational. Now there
	/// are two user interfaces that both use the same ViewModel. The original 
	/// WinForms one is getting a little outdated, but at least it's faster.
	/// 
	/// Update Controls
	/// ---------------
	/// 
	/// When views use Update Controls, they use objects called "dependents" to 
	/// automatically receive notifications when things change in the model or 
	/// viewmodel. The model, in turn, uses "independents" to notify any watching
	/// dependents about changes. Some voodoo is involved: there is no obvious 
	/// connection between the two. The view simply requests the data that it wants 
	/// from the viewmodel, and the viewmodel accesses the model to get the data.
	/// If the view requests data in the context of a dependent, the dependent 
	/// prepares to record (via ThreadStatic global state) which "independents" were
	/// accessed so that when the values associated with those independents change,
	/// the dependent can be notified. In addition, dependents can depend on other
	/// dependents. The details are too complicated to explain here, so visit 
	/// http://updatecontrols.net/cs/ for more information.
	/// 
	/// The short version: use a Dependent sentry to get notifified of changes in a 
	/// model or viewmodel, and use an Independeny sentry in a model to ensure that
	/// Dependents get notified. You must let an Independent know both when you are
	/// reading data (by calling Independent.OnGet()) and when you are changing it
	/// (call Independent.OnSet()). The ViewModel may contain Independents, 
	/// Dependents or neither, depending on its needs. To make Independents and 
	/// Dependents more convenient to use, Update Control provides several classes 
	/// that bundle data together with a sentry: Independent(T) bundles an 
	/// Independent and a T together, Dependent(T) bundles a Dependent and a T 
	/// together, IndependentList(T) bundles an Independent with a list of Ts, and 
	/// so on. Sentries are easier to understand when bundled this way, because
	/// you can think of them as "self-propagating variables"--variables that push
	/// out updates all by themselves.
	/// 
	/// Using Update Controls always involves at least two libraries: a GUI-agnostic 
	/// library that knows nothing about any GUI framework, and a second library 
	/// that combines the first library with a particular GUI (WinForms, WPF, 
	/// Silverlight, etc.) The viewmodel and model only use the first library, 
	/// which is very small and contains the classes mentioned above (Independent, 
	/// Dependent, IndependentList(T), etc.)
	/// 
	/// Currently, Update Controls has problems with scalability. There are multiple
	/// reasons for this but the biggest is that a collection is always associated
	/// with just a single sentry, and there is no mechanism for incremental updates.
	/// So if a list in the viewmodel is built based on a list in the model, the 
	/// entire list will have to be rebuilt from scratch whenever one item in the
	/// list changes. Update Controls has no mechanism to indicate what part of
	/// a list changed or which Independent(s) caused a Dependent to be updated.
	/// However, as long as the dataset is not too large, it is well worth using
	/// Update Controls because it improves productivity so much. And luckily, 
	/// change notifications are not handled instantly when a model changes. The
	/// data associated with a Dependent is updated only when someone requests it.
	/// In the case of Dependents that are created automatically for GUI controls,
	/// they use windows messages to cause updates sometime after the sentries
	/// they depend on actually changed. So if you update many models all at once, 
	/// the GUI is updated only once.
	/// 
	/// The Model
	/// ---------
	/// 
	/// Consists of:
	/// 
	/// 1. A project: a set of assemblies and settings. A default project is loaded 
	///    on startup. The most recent result set is stored with the project.
	/// 2. A set of result sets (TODO)
	/// </remarks>
	public class ApplicationModel
	{
		public ApplicationModel() { }

		ProjectModel _project;
		public ProjectModel Project { get { return _project; } }

		public ApplicationModel(ProjectModel project)
		{
			_project = project;
		}

		// Result sets will go here
	}
}
