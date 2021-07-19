using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Loyc.SyncLib;

namespace Loyc.SyncLib.Impl
{
	/// <summary>Represents the low-level synchronization behavior for a single list 
	/// item. <see cref="SyncManagerHelper"/> needs this.</summary>
	/// <remarks>This interface is used instead of <see cref="SyncFieldFunc_Ref"/> 
	/// so that the implementation can be a struct, in order to take advantage of the
	/// CLR's ability to specialize generic methods for structs, which provides better
	/// performance by avoiding indirect calls and enabling inlining.</remarks>
	public interface ISyncField<SyncManager, T>
	{
		T? Sync(ref SyncManager sync, Symbol? name, T? value);
	}
	//public interface ISyncWrite<SyncManager, T>
	//{
	//	void Write(ref SyncManager sync, Symbol? name, T? value);
	//}

	/// <summary>An adapter from <see cref="SyncFieldFunc_Ref{S,T}"/> to <see cref="ISyncField{S,T}"/></summary>
	public struct AsISyncField<SyncManager, T> : ISyncField<SyncManager, T>
	{
		public SyncFieldFunc_Ref<SyncManager, T> Func { get; set; }
		public AsISyncField(SyncFieldFunc_Ref<SyncManager, T> func) => Func = func;
		
		public T? Sync(ref SyncManager sync, Symbol? propName, T? x) => Func(ref sync, propName, x);

		public static implicit operator AsISyncField<SyncManager, T>(SyncFieldFunc_Ref<SyncManager, T> func)
			=> new AsISyncField<SyncManager, T>(func);
	}
}
