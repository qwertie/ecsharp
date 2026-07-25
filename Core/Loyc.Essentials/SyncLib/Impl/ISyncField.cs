using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;

namespace Loyc.SyncLib
{
	/// <summary>Represents the low-level synchronization behavior for a single list 
	///   item. <see cref="SyncManagerHelper"/> needs this.</summary>
	/// <remarks>This interface is used instead of <see cref="SyncFieldFunc_Ref"/> 
	///   so that the implementation can be a struct, in order to take advantage of the
	///   CLR's ability to specialize generic methods for structs, which provides better
	///   performance by avoiding indirect calls and enabling inlining.
	/// <para/>
	///   Example usage:
	/// <code><![CDATA[
	/// public struct SyncFields<SM> : 
	///		ISyncField<SM, Color>,
	///		ISyncField<SM, BitArray>,
	///		ISyncObject<SM, TwoColorBitmap>
	///		where SM : ISyncManager
	/// {
	/// 	public Color Sync(ref SM sm, FieldId name, Color color)
	/// 	{
	/// 		return Color.FromArgb(sm.Sync(name, color.ToArgb()));
	/// 	}
	/// 	public BitArray? Sync(ref SM sm, FieldId name, BitArray? value)
	/// 	{
	/// 		if (sm.IsReading) {
	///				return new BitArray(sm.SyncArray(name, Array.Empty<byte>()));
	/// 		} else if (value is null) {
	/// 		    sm.SyncArray(name, (byte[]?) null);
	/// 		} else {
	/// 			byte[] bytes = new byte[(value.Length + 7) >> 3];
	/// 			value.CopyTo(bytes, 0);
	/// 			sm.SyncArray(name, bytes);
	/// 		}
	/// 		return value;
	/// 	}
	/// 	
	///     // This uses the Sync methods above
	///     public TwoColorBitmap? Sync(SM sm, TwoColorBitmap? obj)
	///     {
	///         obj ??= new TwoColorBitmap();
	///         
	///         obj.Color0 = sm.Sync("Color0", obj.Color0, this);
	///         obj.Color1 = sm.Sync("Color1", obj.Color1, this);
	///         obj.Bits = sm.Sync("Bits", obj.Bits, this)!;
	///         // You could also just call your methods directly:
	/// 		// obj.Color0 = Sync(ref sm, "Color0", obj.Color0);
	/// 		
	///         return obj;
	///     }
	/// }
	/// ]]></code>
	/// </remarks>
	public interface ISyncField<SyncManager, T>
	{
		T? Sync(ref SyncManager sync, FieldId name, T? value);
	}
}

namespace Loyc.SyncLib.Impl
{
	/// <summary>An adapter from <see cref="SyncFieldFunc_Ref{S,T}"/> to <see cref="ISyncField{S,T}"/></summary>
	public struct AsISyncField<SyncManager, T> : ISyncField<SyncManager, T>
	{
		public SyncFieldFunc_Ref<SyncManager, T> Func { get; set; }
		public AsISyncField(SyncFieldFunc_Ref<SyncManager, T> func) => Func = func;
		
		public T? Sync(ref SyncManager sync, FieldId name, T? x) => Func(ref sync, name, x);

		public static implicit operator AsISyncField<SyncManager, T>(SyncFieldFunc_Ref<SyncManager, T> func)
			=> new AsISyncField<SyncManager, T>(func);
	}
}
