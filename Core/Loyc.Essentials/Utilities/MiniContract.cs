using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Threading;

namespace Loyc.MiniContract
{
	/// <summary>A minimal set of Contract methods, handy when the
	/// Microsoft Code Contracts rewriter is not installed in a 
	/// given project.</summary>
	/// <remarks>
	/// This class only includes <see cref="Requires(bool)"/> and
	/// <see cref="Assert(bool)"/> methods, since it is not possible
	/// to implement <c>Contract.Ensures</c>, <c>Contract.EnsuresOnThrow</c> or 
	/// <c>Contract.Invariant</c> without rewriting the body of a method.
	/// <para/>
	/// The LeMP contract attributes <c>notnull</c>, <c>[requires(...)]</c>
	/// <c>[ensures(...)]</c> and <c>[ensuresOnThrow(...)]</c> are 
	/// designed to use the methods in this class by default. 
	/// <c>[ensures(...)]</c> and <c>[ensuresOnThrow(...)]</c> rely
	/// on the <c>on_return</c> and <c>on_throw</c> macros to rewrite
	/// the method body and call <c>Contract.Assert</c>, which has 
	/// roughly the same effect as <c>Contract.Ensures</c> or 
	/// <c>Contract.EnsuresOnThrow</c>. 
	/// <para/>
	/// If you have the real MS Code Contracts rewriter installed, you
	/// can simply replace <c>using Loyc.MiniContract;</c> with 
	/// <c>using System.Diagnostics.Contracts; #haveContractRewriter;</c>
	/// where <c>#haveContractRewriter</c> is an optional flag that gives 
	/// permission to the contract attributes to invoke <c>Contract.Ensures</c> 
	/// and <c>Contract.EnsuresOnThrow</c>, not just <c>Contract.Assert</c>.
	/// </remarks>
	public static class Contract
	{
		/// <summary>Invokes FailHandler(true) if the condition is false.</summary>
		public static void Requires(bool condition)
		{
			if (!condition)
				FailHandler(true);
		}
		/// <summary>Invokes FailHandler(true, message) if the condition is false.</summary>
		public static void Requires(bool condition, string message)
		{
			if (!condition)
				FailHandler(true, message);
		}
		/// <summary>Invokes FailHandler(false) if the condition is false.</summary>
		public static void Assert(bool condition)
		{
			if (!condition)
				FailHandler(false);
		}
		/// <summary>Invokes FailHandler(false, message) if the condition is false.</summary>
		public static void Assert(bool condition, string message)
		{
			if (!condition)
				FailHandler(false, message);
		}

		public delegate void FailHandlerDelegate(bool isPrecondition, string message = null);
		public static ThreadLocalVariable<FailHandlerDelegate> _failHandler = new ThreadLocalVariable<FailHandlerDelegate>(DefaultFailHandler);
		
		/// <summary>Gets or sets the (thread-local) method to be called when 
		/// a precondition, assertion or postcondition fails. To revert to the 
		/// default handler, which just throws an exception, set this to null.</summary>
		public static FailHandlerDelegate FailHandler
		{
			get { return _failHandler.Value; }
			set { _failHandler.Value = value ?? DefaultFailHandler; }
		}

		static void DefaultFailHandler(bool isPrecondition, string message = null)
		{
			if (isPrecondition)
				throw new ContractRequiresException(message);
			else
				throw new ContractAssertException(message);
		}
	}

	[Serializable]
	public class ContractAssertException : InvalidOperationException
	{
		public ContractAssertException() : this(null) { }
		public ContractAssertException(string msg) : base(msg ?? "A contract assertion failed.".Localized()) { }
		public ContractAssertException(string msg, Exception innerException) : base(msg, innerException) { }
	}

	[Serializable]
	public class ContractRequiresException : ArgumentException
	{
		public ContractRequiresException() : this(null) { }
		public ContractRequiresException(string msg) : base(msg ?? "A contract assertion failed.".Localized()) { }
		public ContractRequiresException(string msg, Exception innerException) : base(msg, innerException) { }
	}
}
