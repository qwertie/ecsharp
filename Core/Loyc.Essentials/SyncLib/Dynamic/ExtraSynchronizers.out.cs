using System;
using System.Collections.Generic;
using Loyc.SyncLib.Impl;

#nullable enable

namespace Loyc.SyncLib
{

	// Lists of bytes, chars and bools get special treatment from the SyncManager itself
	// (e.g. SyncBinary stores a byte list as raw bytes, and SyncJson stores it as a Base64
	// or BAIS string). These non-generic overloads take priority over the generic
	// collection synchronizers in DefaultSynchronizer.cs, so that DefaultSynchronizer
	// gives every kind of list of these types that special treatment, not only arrays.
	static partial class ExtraSynchronizers<SyncManager>
	{
		public static byte[]? Sync(ref SyncManager sync, FieldId name, byte[]? value) => 
		  sync.SyncArray(name, value);

		public static List<byte>? Sync(ref SyncManager sync, FieldId name, List<byte>? value) => 
		  sync.SyncList(name, value);

		public static IList<byte>? Sync(ref SyncManager sync, FieldId name, IList<byte>? value) => 
		  sync.SyncList(name, value);

		public static IReadOnlyList<byte>? Sync(ref SyncManager sync, FieldId name, IReadOnlyList<byte>? value) => 
		  sync.SyncList(name, value);

		public static HashSet<byte>? Sync(ref SyncManager sync, FieldId name, HashSet<byte>? value) => 
		  sync.SyncColl(name, value, Alloc<byte>.HashSet);
		public static char[]? Sync(ref SyncManager sync, FieldId name, char[]? value) => 
		  sync.SyncArray(name, value);

		public static List<char>? Sync(ref SyncManager sync, FieldId name, List<char>? value) => 
		  sync.SyncList(name, value);

		public static IList<char>? Sync(ref SyncManager sync, FieldId name, IList<char>? value) => 
		  sync.SyncList(name, value);

		public static IReadOnlyList<char>? Sync(ref SyncManager sync, FieldId name, IReadOnlyList<char>? value) => 
		  sync.SyncList(name, value);

		public static HashSet<char>? Sync(ref SyncManager sync, FieldId name, HashSet<char>? value) => 
		  sync.SyncColl(name, value, Alloc<char>.HashSet);
		public static bool[]? Sync(ref SyncManager sync, FieldId name, bool[]? value) => 
		  sync.SyncArray(name, value);

		public static List<bool>? Sync(ref SyncManager sync, FieldId name, List<bool>? value) => 
		  sync.SyncList(name, value);

		public static IList<bool>? Sync(ref SyncManager sync, FieldId name, IList<bool>? value) => 
		  sync.SyncList(name, value);

		public static IReadOnlyList<bool>? Sync(ref SyncManager sync, FieldId name, IReadOnlyList<bool>? value) => 
		  sync.SyncList(name, value);

		public static HashSet<bool>? Sync(ref SyncManager sync, FieldId name, HashSet<bool>? value) => 
		  sync.SyncColl(name, value, Alloc<bool>.HashSet);
	}
}