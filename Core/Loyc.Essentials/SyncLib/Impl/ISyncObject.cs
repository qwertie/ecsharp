using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>
	/// This interface is equivalent to <see cref="SyncObjectFunc{SyncManager,T}"/>,
	/// reformulated as an interface. It only exists because the CLR (C# runtime)
	/// supports optimizations on generic parameters that are structs, but similar
	/// optimizations don't exist for delegates. For this reason it is possible to 
	/// achieve higher performance by providing a synchronizer in the form of a 
	/// struct rather than a delegate (see Remarks)
	/// </summary>
	/// <remarks>
	/// TODO: explain how to use this interface.
	/// </remarks>
	public interface ISyncObject<SyncManager, T>
	{
		T Sync(SyncManager sync, T? value);
	}
}

namespace Loyc.SyncLib.Impl
{
	/// <summary>An adapter from <see cref="SyncObjectFunc{S,T}"/> to <see cref="ISyncObject{S,T}"/>.</summary>
	public struct AsISyncObject<SyncManager, T> : ISyncObject<SyncManager, T>
	{
		public SyncObjectFunc<SyncManager, T> Func { get; set; }
		public AsISyncObject(SyncObjectFunc<SyncManager, T> func) => Func = func;

		public T Sync(SyncManager sync, T? x) => Func(sync, x);
		
		public static implicit operator AsISyncObject<SyncManager, T>(SyncObjectFunc<SyncManager, T> func)
			=> new AsISyncObject<SyncManager, T>(func);
	}
}
