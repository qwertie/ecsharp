using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using MiniTestRunner.ViewModel;
using MiniTestRunner.WinForms;
using MiniTestRunner.Model;

namespace MiniTestRunner
{
	public class Program : MarshalByRefObject
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			CodeSnippet();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var appModel = new ApplicationModel(new ProjectModel(new TaskRunner(), new OptionsModel()));
			var treeVM = new ProjectVM(appModel.Project, new FilterVM());
			
			Application.Run(new TestingForm(treeVM));
		}

		[Conditional("DEBUG")]
		public static void CodeSnippet()
		{
			string loc = @"C:\Temp";//Path.GetFullPath(Assembly.GetExecutingAssembly().Location + @"\..");
			Domain domain = AppDomainStarter.Start<Domain>(loc, "Domain", null, true);
			//domain.TestEvent += new Action(new Program().HandleTestEvent);
			
			// A place for trying out random code snippets
			Func<int, bool> ai = new Foo().Positive;
			Predicate<int> ai2 = new Predicate<int>(ai);

			ai(0);
			ai2(0);

		}

		static void OpenDelegateTest()
		{
			Action<int> d = new Foo().GetAction();
			Action<Foo, int> act = (Action<Foo, int>)Delegate.CreateDelegate(typeof(Action<Foo, int>), null, d.Method);
			//var act = (Action<Foo, int>)CreateOpenDelegateFrom<Action<int>>.BasedOn(d);
			act(new Foo(), 5);
		}

		public void HandleTestEvent() { }

		class Domain : MarshalByRefObject
		{
			#pragma warning disable 67 //unused event
			public event Action TestEvent;
			public Domain() { Console.WriteLine("Domain created OK"); }
		}
	}

	internal class Foo
	{
		public Action<int> GetAction()
		{
			return Action;
		}
		private void Action(int i)
		{
			System.Console.WriteLine("{0}.Action", this);
		}
		public bool Positive(int i)
		{
			System.Console.WriteLine("{0}.Positive", this);
			return i > 0;
		}
	}

	/// <summary>
	/// Removes the "Target" from a delegate, converting it from the standard closed form 
	/// to an open Action delegate that accepts a correctly-typed class as its first 
	/// argument. For example, if an event handler exists in class Foo and you create an
	/// EventHandler delegate from it, this class can be used to create an open delegate 
	/// of type Action&lt;Foo, object, EventArgs>.
	/// </summary>
	/// <remarks>
	///  The "normal"
	/// way to convert an open delegate to a closed delegate is to use 
	/// Delegate.CreateDelegate like so:
	/// <code>
	///   Action&lt;int> d = new Foo().Action;
	///   Action&lt;Foo, int> act = (Action&lt;Foo, int>)Delegate
	///       .CreateDelegate(typeof(Action&lt;Foo, int>), null, d.Method);
	///   act(new Foo(), 42);
	/// </code>
	/// </remarks>
	public static class CreateOpenDelegateFrom<TDelegate> where TDelegate : class
	{
		
		public static Delegate BasedOn(TDelegate @delegate)
		{
			Delegate delegate2 = (Delegate)(object)@delegate;
			MethodInfo method = delegate2.Method;
			MethodInfo DelgSignature = typeof(TDelegate).GetMethod("Invoke");
			ParameterInfo[] DelgParams = DelgSignature.GetParameters();
			Type[] DelgParamTypes = DelgParams.Select(p => p.ParameterType).ToArray();

			var genericAction = Type.GetType(string.Format("System.Action`{0}", 1 + DelgParamTypes.Length));
			var argTypes = new Type[DelgParamTypes.Length + 1];
			argTypes[0] = method.DeclaringType;
			DelgParamTypes.CopyTo(argTypes, 1);
			Type openDelegateType = genericAction.MakeGenericType(argTypes);
			return Delegate.CreateDelegate(openDelegateType, null, method, true);
		}
	}
}
