using Loyc;
using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Util.UI
{
	/// <summary>An interface for "commands": simple actions in a user interface 
	/// that are typically associated with buttons and shortcut keys.</summary>
	/// <remarks>A typical way of implementing commands is to use the [Command] 
	/// attribute on a method that takes and returns bool:
	/// <code>
	/// [Command("SaveFoos", "Saves the foos.")] public bool SaveFoos(bool run)
	/// {
	///	   if (NumFoosToSave == 0)
	///	      return false;
	///	   if (run)
	///	      SaveFoos();
	///	   return true;
	/// }
	/// </code>
	/// The static method <see cref="CommandAttribute.GetCommandList()"/> converts 
	/// all the methods that are marked with [Command] into ICommand objects. If 
	/// 'run' is false, the action must not be taken; the method must simply return 
	/// true if the command is available and false if not.
	/// <para/>
	/// Typically a program will separately contain a table of keyboard shortcuts
	/// which associates shortcuts with names of commands, something like this:
	/// <code>
	///	public Dictionary&lt;Pair&lt;Keys, Keys>, Symbol> KeyMap = new Dictionary&lt;Pair&lt;Keys, Keys>, Symbol>()
	///	{
	///		{ Pair.Create(Keys.Z, Keys.Control), S("Undo") },
	///		{ Pair.Create(Keys.Y, Keys.Control), S("Redo") },
	///		{ Pair.Create(Keys.Z, Keys.Control | Keys.Shift), S("Redo") },
	///		{ Pair.Create(Keys.Delete, 0), S("Delete") },
	///	};
	/// </code>
	/// Of course, code is needed somewhere to detect when shortcut key are pressed...
	/// </remarks>
	public interface ICommand
	{
		/// <summary>A constant identifier for the command.</summary>
		/// <remarks>The name is typically written in PascalCase and used to 
		/// identify a command in a configuration or as a key in a dictionary. It 
		/// may also unify similar commands in different contexts, e.g. two 
		/// different editors in a program may each have their own separate "Undo" 
		/// command which is associated with the same shortcut key.</remarks>
		Symbol Name { get; }
		/// <summary>Returns true if the command can be used right now.</summary>
		bool CanExecute { get; }
		/// <summary>Runs the command. Has no effect if CanExecute is false.</summary>
		void Run();
		/// <summary>Localized description of a command. This is optional and is 
		/// null if not provided.</summary>
		string Description { get; }

		//string UnavailableReason { get; }
	}

	/// <summary>Signature for a method that represents a command (something that 
	/// is typically associated with a button or a shortcut key).</summary>
	/// <param name="run">If true, the command will execute if possible. If false,
	/// the command is not executed; the caller wants to know if the command can
	/// be executed.</param>
	/// <returns>true if the command can be executed (and, if run is true, was 
	/// executed), false if the command cannot be executed or was not executed.</returns>
	public delegate bool CommandMethod(bool run = true);

	/// <summary>Used to mark a method (see <see cref="CommandMethod"/>) that is 
	/// used as a command.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class CommandAttribute : Attribute
	{
		/// <summary>Initializes a CommandAttribute.</summary>
		/// <param name="name">Internal command name, typically in English with no 
		/// spaces (CamelCase). If null, the method name is used as the command name.</param>
		/// <param name="descr">A description of the command.</param>
		public CommandAttribute(string name = null, string descr = null) 
			{ _name = GSymbol.Get(name); _descr = descr; }
		Symbol _name;
		public Symbol Name { get { return _name; } }
		string _descr;
		public string Description { get { return _descr; } }

		/// <summary>A command that wraps a <see cref="CommandMethod"/> method.</summary>
		public class Command : ICommand
		{
			Symbol _name;
			string _descr;
			CommandMethod _cmd;

			public Command(Symbol name, string descr, CommandMethod cmd)
			{
				_name = name; _descr = descr; _cmd = cmd;
			}

			public Symbol Name { get { return _name; } }
			public bool CanExecute { get { return _cmd(false); } }
			public void Run() { _cmd(true); }
			public string Description { get { return Localize.From(_descr); } }
		}

		/// <summary>Gets a list of commands from the methods in the specified 
		/// object that have a [Command] attribute.</summary>
		/// <exception cref="ArgumentException">A method has the wrong signature 
		/// (doesn't match CommandMethod)</exception>
		public static List<ICommand> GetCommandList(object obj, Type type = null)
		{
			type = type ?? obj.GetType();
			var results = new List<ICommand>();
			foreach (var m in type.GetMethods())
			{
				var a = m.GetCustomAttributes(typeof(CommandAttribute), true);
				if (a.Length > 0)
				{
					results.Add(new Command(
						((CommandAttribute)a[0]).Name ?? GSymbol.Get(m.Name),
						((CommandAttribute)a[0]).Description,
						(CommandMethod)Delegate.CreateDelegate(typeof(CommandMethod), obj, m)));
				}
			}
			return results;
		}

		/// <summary>Get a dictionary of commands from the methods in the specified 
		/// object that have a [Command] attribute.</summary>
		public static MMap<Symbol, ICommand> GetCommandMap(object obj, Type type = null)
		{
			var list = GetCommandList(obj, type);
			var dict = new MMap<Symbol, ICommand>();
			foreach (var c in list)
				dict.Add(c.Name, c);
			return dict;
		}
	}
}
