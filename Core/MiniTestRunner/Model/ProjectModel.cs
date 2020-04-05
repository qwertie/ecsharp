using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using Loyc.MiniTest;
using Loyc.Collections;
using MiniTestRunner.TestDomain;
using UpdateControls;
using UpdateControls.Collections;
using UpdateControls.Fields;
using UpdateControls.Forms;

namespace MiniTestRunner.Model
{
	public class ProjectModel
	{
		public ProjectModel(TaskRunner runner) : this(runner, new OptionsModel()) { }
		public ProjectModel(TaskRunner runner, OptionsModel options) 
		{ 
			_runner = runner; 
			_options = options;
			_runner.TaskComplete += new Action<ITask, Exception>(OnTaskComplete);
			_depFileSystemWatchers = new Dependent(UpdateFileSystemWatchers);
			_updater = new GuiUpdateHelper(_depFileSystemWatchers);
		}

		#region Data members + properties

		GuiUpdateHelper _updater;

		private TaskRunner _runner;
		public TaskRunner Runner { get { return _runner; } internal set { _runner = value; } }

		readonly OptionsModel _options;
		public OptionsModel Options { get { return _options; } }

		IndependentList<Assembly> _assemblies = new IndependentList<Assembly>();
		public IEnumerable<Assembly> Assemblies { get { return _assemblies; } }
		public class Assembly
		{
			// The Path to the assembly. Generally absolute, but it is saved in relative form.
			public string Path { get; internal set; }
			public AppDomainHolder Domain { get; internal set; }
			public TaskRowModel Row { get; set; }
			
			Independent<bool> _updatePending = new Independent<bool>();
			public bool UpdatePending { get { return _updatePending.Value; } set { _updatePending.Value = value; } }
		}

		public IEnumerable<TaskRowModel> Roots { get { return _assemblies.Select(a => a.Row); } }

		public class AppDomainHolder
		{
			AppDomain _domain = null;
			public AppDomain Domain {
				get { 
					if (_domain == null)
						_domain = CreateAppDomain(BaseFolder, Name, PartialTrust);
					return _domain; 
				}
			}
			public bool DomainCreated { get { return _domain != null; } }
			public bool PartialTrust { get; private set; }
			public string BaseFolder { get; private set; }
			public string Name { get; private set; }
			public AppDomainHolder(string baseFolder, string appDomainName, bool partialTrust) 
				{ BaseFolder = baseFolder; Name = appDomainName; PartialTrust = partialTrust; }

			static AppDomain CreateAppDomain(string baseFolder, string appDomainName, bool partialTrust)
			{
				AppDomainSetup setup = new AppDomainSetup();
				setup.ApplicationBase = baseFolder;
				if (partialTrust) {
					// I can't get this to work 
					var permSet = new PermissionSet(PermissionState.None);
					permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
					permSet.AddPermission(new UIPermission(PermissionState.Unrestricted));
					permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, baseFolder));
					string folderOfT = Path.GetFullPath(Path.Combine(typeof(AppDomainHolder).Assembly.Location, ".."));
					permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read, folderOfT));
					permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.AllFlags)); // Required to call Assembly.GetExportedTypes
					return AppDomain.CreateDomain(appDomainName, null, setup, permSet);
				} else {
					return AppDomain.CreateDomain(appDomainName, null, setup);
				}
			}

			internal void Unload()
			{
				AppDomain.Unload(_domain);
				_domain = null;
			}
		}

		#endregion

		#region Load/Save project

		public void Save(string to) { Save(File.OpenWrite(to)); }
		public void Save(Stream to) { }
		public static ProjectModel Load(string from) { return Load(File.OpenRead(from)); }
		public static ProjectModel Load(Stream from) { throw new NotImplementedException(); }
		
		#endregion

		#region File system monitoring

		Dependent _depFileSystemWatchers;
		Independent _indUnloadSignal = new Independent();
		List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

		void UpdateFileSystemWatchers()
		{
			_indUnloadSignal.OnGet(); // update watchers when something is unloaded
			// - We only need watchers when Options.AutoUnload (TODO: when AutoUnload
			//   changes, unload/reload assemblies)
			// - We only want to watch folders that contain unloaded assemblies. To 
			//   approximate this, look for assemblies that have no tasks (these
			//   may actually still be loaded by other assemblies, but that's ok)
			if (Options.AutoUnload) 
			{
				var activeAssemblies = new Dictionary<string, object>();
				foreach (var task in _runner.QueuedAndRunningTasks().OfType<ITaskEx>())
					activeAssemblies[task.AssemblyPath] = null;
				var inactiveAssemblies = _assemblies.Where(a => !activeAssemblies.ContainsKey(a.Path));
				var inactivePaths = new Dictionary<string, FileSystemWatcher>();
				foreach (var a in inactiveAssemblies)
					inactivePaths[Path.GetFullPath(Path.Combine(a.Path, ".."))] = null;
				
				// inactivePaths contains the set of paths to watch.
				// Add/remove watchers in _watchers to synchronize with that set.
				var removes = new List<FileSystemWatcher>();
				foreach (var watcher in _watchers)
					if (!inactivePaths.ContainsKey(watcher.Path))
						removes.Add(watcher);
					else
						inactivePaths[watcher.Path] = watcher;
				RemoveWatchers(removes);
				foreach (var pair in inactivePaths.Where(pair => pair.Value == null))
				{
					Trace.WriteLine("Starting to watch " + pair.Key);
					var watcher = new FileSystemWatcher(pair.Key);
					watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
					   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
					
					FileSystemEventHandler OnChanged = (s, e) => {
						foreach (var a in _assemblies)
							if (string.Compare(a.Path, e.FullPath, true) == 0)
								a.UpdatePending = true;
					};
					watcher.Changed += OnChanged;
					watcher.Created += OnChanged;
					watcher.Renamed += new RenamedEventHandler(OnChanged);
					_watchers.Add(watcher);
				}
			} else
				RemoveWatchers(_watchers);
		}
		private void RemoveWatchers(ICollection<FileSystemWatcher> ws)
		{
			foreach (var w in ws)
			{
				Trace.WriteLine("No longer watching " + w.Path);
				w.Dispose();
			}
			_watchers.RemoveAll(w => ws.Contains(w));
		}

		#endregion
		
		void OnTaskComplete(ITask task, Exception error)
		{
			var taskEx = task as ITaskEx;
			if (_options.RunTestsOnLoad && task is AssemblyScanTask)
				StartTesting(_assemblies.Where(a => a.Row.Task == task).Select(a => a.Row));
			// Could be a bottleneck because QueuedAndRunningTasks() copies the whole
			// list (although Any() often terminates after an iteration or two).
			if (_options.AutoUnload && taskEx != null &&
				!_runner.QueuedAndRunningTasks().Any(t => t.IsPending && t is ITaskEx && (t as ITaskEx).Domain == taskEx.Domain))
				UnloadDomainOf(taskEx);
		}

		private void UnloadDomainOf(ITaskEx task)
		{
			foreach (var assembly in _assemblies.Where(a => a.Domain.DomainCreated && a.Domain.Domain == task.Domain)) {
				try {
					assembly.Domain.Unload();
				} catch(Exception ex) {
					Trace.WriteLine(ex.GetType().Name + " occurred while unloading domain of " + assembly.Path + ": " + ex.Message);
				}
			}
			_indUnloadSignal.OnSet();
		}

		public void BeginOpenAssemblies(string[] filenames, bool partialTrust)
		{
			if (filenames.Length == 0) return;

			// Create Assembly objects with AppDomains
			foreach (string fn in filenames) {
				string baseFolder = Path.GetFullPath(Path.Combine(fn, ".."));
				if (!_assemblies.Any(a => a.Path == fn)) {
					var ass = _assemblies.FirstOrDefault(a => a.Domain.BaseFolder == baseFolder && a.Domain.PartialTrust == partialTrust);
					var adh = ass != null ? ass.Domain : new AppDomainHolder(baseFolder, Path.GetFileNameWithoutExtension(fn), partialTrust);
					_assemblies.Add(new Assembly { Path = fn, Domain = adh });
				}
			}

			// Start scan tasks
			var newTasks = new List<AssemblyScanTask>();
			foreach (var ass in _assemblies.Where(a => a.Row == null))
			{
				#pragma warning disable 618 // method is "obsolete" in .NET4 but its replacement does not exist in .NET 3.5
				var task = (AssemblyScanTask)Activator.CreateInstanceFrom(ass.Domain.Domain,
					typeof(AssemblyScanTask).Assembly.ManifestModule.FullyQualifiedName,
					typeof(AssemblyScanTask).FullName, false,
					0, null, new object[] { ass.Path, ass.Domain.BaseFolder }, null, null, null).Unwrap();
				ass.Row = new TaskRowModel(Path.GetFileName(ass.Path), TestNodeType.Assembly, task, false);
				newTasks.Add(task);
			}
			_runner.AddTasks(newTasks.Upcast<ITask, AssemblyScanTask>());
			_runner.AutoStartTasks();
		}

		public void StartTesting(IEnumerable<RowModel> roots)
		{
			var queue = new List<RowModel>();
			FindTasksToRun(roots, queue);
			_runner.AddTasks(queue.Select(row => row.Task).Upcast<ITask, ITestTask>());
			_runner.AutoStartTasks();
		}
		private void FindTasksToRun(IEnumerable<RowModel> list, List<RowModel> queue)
		{
			foreach (var row in list)
			{
				FindTasksToRun(row.Children, queue);
				if (row.Task != null && row.Status == TestStatus.NotRun)
					queue.Add(row);
			}
		}
	}

	[TestFixture]
	class TestProjectModel : Assert
	{
		[Test]
		void SaveLoad()
		{
			// We can use Loyc.Essentials as a test assembly to put in the project
			var proj = new ProjectModel(new TaskRunner(), new OptionsModel());
			// First try an empty project
			var mstream = new MemoryStream();
			Expect(!proj.Assemblies.Any());
			proj.Save(mstream);
			proj = ProjectModel.Load(mstream);
			Expect(!proj.Assemblies.Any());
		}
	}
}
