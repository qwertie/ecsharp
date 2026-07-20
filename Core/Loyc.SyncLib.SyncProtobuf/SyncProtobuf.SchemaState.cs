using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	/// <summary>The kind of a <see cref="ProtoType"/>. Scalar and Message correspond
	///   directly to .proto types; List, Opt and Ref are the container messages that
	///   <see cref="SyncProtobuf.Writer"/> uses for lists, nullable list elements and
	///   deduplicated values (see <see cref="SyncProtobuf"/>). Each of the latter is
	///   rendered as a generated wrapper message type.</summary>
	internal enum ProtoKind {
		Scalar,  // e.g. "int32", "string", "bytes"
		Message, // a recorded message (identified by MessageKey)
		List,    // list container: { repeated Element items = 1; }
		Opt,     // nullable element wrapper: { optional Element value = 1; }
		Ref,     // dedup wrapper: { uint64 id = 1; Element value = 2; }
	}

	/// <summary>The type of one field in a recorded <c>.proto</c> schema.</summary>
	internal class ProtoType
	{
		public ProtoKind Kind;
		public string? Scalar;     // Kind == Scalar
		public object? MessageKey; // Kind == Message (a Type, or a synthetic key for anonymous messages)
		public ProtoType? Element; // Kind == List/Opt/Ref

		public static ProtoType MakeScalar(string s) => new ProtoType { Kind = ProtoKind.Scalar, Scalar = s };
		public static ProtoType MakeMessage(object key) => new ProtoType { Kind = ProtoKind.Message, MessageKey = key };
		public static ProtoType MakeList(ProtoType? element) => new ProtoType { Kind = ProtoKind.List, Element = element };
		public static ProtoType MakeOpt(ProtoType element) => new ProtoType { Kind = ProtoKind.Opt, Element = element };
		public static ProtoType MakeRef(ProtoType element) => new ProtoType { Kind = ProtoKind.Ref, Element = element };
	}

	internal class ProtoField
	{
		public string Name = "";
		public int Number;
		public bool Optional;
		public ProtoType Type = null!;
	}

	/// <summary>A recorded <c>message</c> definition (a type's fields).</summary>
	internal class ProtoMessage
	{
		public object Key;
		public List<ProtoField> Fields = new List<ProtoField>();
		public string? TagName;      // from SyncTypeTag, or "Tuple"/"Anonymous"
		public string? AssignedName; // final, unique, sanitized name (assigned at render time)
		public bool InProgress;      // true while being recorded (breaks cycles)
		public bool Merged;          // true if merged into an identical anonymous message
		public ProtoMessage(object key) { Key = key; }
	}

	/// <summary>The core of <see cref="SyncProtobuf.SchemaWriter"/>: records the messages that a
	///   synchronizer describes while running in <see cref="SyncMode.Schema"/> mode, then
	///   renders them as a proto3 <c>.proto</c> document that exactly describes the wire
	///   output of <see cref="SyncProtobuf.Writer"/>.</summary>
	internal class SchemaState
	{
		internal Options _opt;

		// A defensive limit on anonymous (childKey == null) object nesting, which cannot be
		// deduplicated and so would recurse forever for a recursive anonymous type.
		const int MaxAnonymousDepth = 200;

		class Frame
		{
			// Message frame (objects and tuples):
			public ProtoMessage? Msg;       // message that receives fields
			public bool IsTuple;
			public int LastFieldId;
			public ProtoMessage? Def;       // definition recorded by this frame
			public ProtoMessage? CompareTo; // if Def is a scratch re-record, the original
			// List frame:
			public ProtoType? ListType;     // the List type whose Element we fill
			public bool IsList;
			public int ItemCount;
			public int ExpectedItems = 1;
		}

		readonly Dictionary<object, ProtoMessage> _defs = new Dictionary<object, ProtoMessage>();
		readonly List<ProtoMessage> _defsInOrder = new List<ProtoMessage>();
		readonly List<Frame> _stack = new List<Frame>();
		ProtoType? _rootType;           // the type of the root value (Ref(...) if RootMode deduplicates)
		ProtoMessage? _rootPlainDef;    // the root message, if written bare (gets the _present field)

		public SchemaState(Options options) { _opt = options; }

		Frame? Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

		public int Depth => _stack.Count;
		public bool IsInsideList => Top != null && (Top.IsList || Top.IsTuple);
		public bool? ReachedEndOfList {
			get {
				var top = Top;
				if (top == null || !top.IsList)
					return null; // tuples: length is not stored (mirrors the reader)
				return top.ItemCount >= top.ExpectedItems;
			}
		}
		public int? MinimumListLength => Top != null && Top.IsList ? 0 : (int?)null;

		#region Recording

		int ResolveNumber(Frame f, FieldId field)
		{
			int id = field.Id != int.MinValue ? field.Id : f.LastFieldId + 1;
			if ((uint)(id - 1) >= MaxUserFieldNumber || (id >= 19000 && id <= 19999))
				throw new ArgumentException(
					"SyncProtobuf: field '{0}' has invalid Protobuf field number {1}. Field numbers must be in the range 1 to {2}, excluding 19000-19999 (reserved by Protobuf)."
					.Localized(field.Name ?? "(unnamed)", id, MaxUserFieldNumber));
			f.LastFieldId = id;
			return id;
		}

		// Attaches a field/element to the current message or list, unless this is the
		// root value (which has no parent; it is just recorded as a top-level message).
		void AttachRef(FieldId field, ProtoType type, bool optional)
		{
			if (Top != null)
				Attach(field, type, optional);
		}

		void Attach(FieldId field, ProtoType type, bool optional)
		{
			var top = Top;
			if (top == null)
				throw new InvalidOperationException(
					"SyncProtobuf.Schema cannot describe a value at depth zero. " +
					"Wrap it in a root object (e.g. use SyncProtobuf.WriteSchema).");
			if (top.IsList) {
				// In the wire format, nullable elements are wrapped: { optional T value = 1; }
				var elemType = optional ? ProtoType.MakeOpt(type) : type;
				top.ItemCount++;
				if (top.ListType!.Element == null)
					top.ListType.Element = elemType;
				else if (!TypeEqual(top.ListType.Element, elemType))
					throw new InvalidOperationException(
						"SyncProtobuf.Schema: a list was synchronized with elements of more than one type. " +
						"All elements of a list must be synchronized the same way (use a tuple for heterogeneous sequences).");
			} else {
				int num = ResolveNumber(top, field);
				top.Msg!.Fields.Add(new ProtoField {
					Name = FieldName(field.Name, num), Number = num, Optional = optional, Type = type,
				});
			}
		}

		public void SyncScalar(FieldId field, string protoType, bool optional, bool dedup = false)
		{
			if (dedup)
				Attach(field, ProtoType.MakeRef(ProtoType.MakeScalar(protoType)), optional: false);
			else
				Attach(field, ProtoType.MakeScalar(protoType), optional);
		}

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId field, object? childKey, ObjectMode mode, int listLength)
		{
			bool isTuple = (mode & ObjectMode.Tuple) == ObjectMode.Tuple;
			bool isList = !isTuple && (mode & ObjectMode.List) != 0;
			bool dedup = (mode & ObjectMode.Deduplicate) != 0;
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;

			if (isList) {
				if (Top == null)
					throw new NotSupportedException(
						"SyncProtobuf: the root value must be an object (a Protobuf message), not a list or tuple.");
				var listType = ProtoType.MakeList(null);
				ProtoType fieldType = dedup ? ProtoType.MakeRef(listType) : listType;
				// A null list is an omitted field / empty Opt wrapper; the list field
				// itself is a message type, so no `optional` keyword is involved.
				Attach(field, fieldType, optional: nullable && !dedup && Top.IsList);
				_stack.Add(new Frame {
					IsList = true, ListType = listType,
					ExpectedItems = 1, // a schema saver visits one element per list
				});
				return (true, 1, null);
			}

			if (Top == null && isTuple)
				throw new NotSupportedException(
					"SyncProtobuf: the root value must be an object (a Protobuf message), not a list or tuple.");

			return BeginMessage(field, childKey, dedup, nullable, isTuple);
		}

		(bool Begun, int Length, object? Object) BeginMessage(FieldId field, object? childKey, bool dedup, bool nullable, bool isTuple)
		{
			// In Schema mode a non-Type childKey is only a sample instance; its type
			// identifies the message (see ObjectSyncher and the ISyncManager docs).
			if (childKey != null && !(childKey is Type))
				childKey = childKey.GetType();

			bool atRoot = Top == null;

			if (childKey == null) {
				// Anonymous message (typically a tuple): recorded inline, not deduplicated
				if (Depth >= MaxAnonymousDepth)
					throw new InvalidOperationException(
						"SyncProtobuf.Schema: object nesting is too deep. This usually means a recursive type " +
						"was synchronized via BeginSubObject with childKey == null; in Schema mode childKey should " +
						"identify the type (helper methods pass typeof(T)).");
				var anonKey = new object();
				var anon = new ProtoMessage(anonKey) { TagName = isTuple ? "Tuple" : "Anonymous", InProgress = true };
				_defs.Add(anonKey, anon);
				_defsInOrder.Add(anon);
				AttachMessageRef(field, anonKey, dedup, nullable, atRoot);
				_stack.Add(new Frame { Msg = anon, Def = anon, IsTuple = isTuple });
				return (true, 1, null);
			}

			if (_defs.TryGetValue(childKey, out var existing)) {
				if (existing.InProgress) {
					// A cycle (or a repeat while re-recording): reference without recording again.
					AttachMessageRef(field, childKey, dedup, nullable, atRoot);
					return (false, 0, DeclinedObject(childKey, dedup, nullable));
				}
				// Re-record into a scratch definition and compare at EndSubObject, to catch a
				// type that is synchronized in two conflicting ways.
				existing.InProgress = true;
				var scratch = new ProtoMessage(childKey);
				AttachMessageRef(field, childKey, dedup, nullable, atRoot);
				_stack.Add(new Frame { Msg = scratch, Def = scratch, CompareTo = existing, IsTuple = isTuple });
				return (true, 1, null);
			}

			var newDef = new ProtoMessage(childKey) { InProgress = true, TagName = isTuple ? "Tuple" : null };
			_defs.Add(childKey, newDef);
			_defsInOrder.Add(newDef);
			AttachMessageRef(field, childKey, dedup, nullable, atRoot);
			_stack.Add(new Frame { Msg = newDef, Def = newDef, IsTuple = isTuple });
			return (true, 1, null);
		}

		void AttachMessageRef(FieldId field, object key, bool dedup, bool nullable, bool atRoot)
		{
			if (atRoot) {
				if (_rootType == null) {
					_rootType = dedup ? ProtoType.MakeRef(ProtoType.MakeMessage(key)) : ProtoType.MakeMessage(key);
					if (!dedup && _defs.TryGetValue(key, out var rootDef))
						_rootPlainDef = rootDef;
				}
				return;
			}
			ProtoType type = ProtoType.MakeMessage(key);
			if (dedup)
				type = ProtoType.MakeRef(type);
			// Nullable message ELEMENTS are wrapped in an Opt message on the wire;
			// nullable message FIELDS are simply omitted (message fields have presence).
			bool wrapAsOpt = nullable && !dedup && Top != null && Top.IsList;
			Attach(field, type, optional: wrapAsOpt);
		}

		public void EndSubObject()
		{
			if (_stack.Count == 0)
				throw new InvalidOperationException("SyncProtobuf.Schema: EndSubObject called more than BeginSubObject.");
			var frame = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);

			if (frame.Def != null) {
				if (frame.CompareTo != null) {
					frame.CompareTo.InProgress = false;
					frame.CompareTo.TagName ??= frame.Def.TagName;
					if (!FieldsEqual(frame.CompareTo.Fields, frame.Def.Fields))
						throw new InvalidOperationException(
							$"SyncProtobuf.Schema: the type '{TypeNameOf(frame.Def.Key)}' was synchronized in two " +
							"conflicting ways. Each type (identified by the childKey given to BeginSubObject, normally " +
							"typeof(T)) must always be synchronized with the same fields, numbers and types.");
				} else {
					frame.Def.InProgress = false;
				}
			}
		}

		public string? SyncTypeTag(string? tag)
		{
			var top = Top;
			if (top == null || top.IsList)
				throw new InvalidOperationException("SyncTypeTag can only be used inside an object (not a list).");
			if (top.Def != null && tag != null) {
				top.Def.TagName ??= tag;
				// The tag is stored on the wire as a string field with a reserved number
				if (top.Msg != null && !HasField(top.Msg, TypeTagFieldNumber))
					top.Msg.Fields.Add(new ProtoField {
						Name = "_type", Number = TypeTagFieldNumber, Optional = true,
						Type = ProtoType.MakeScalar("string"),
					});
			}
			return tag;
		}
		static bool HasField(ProtoMessage msg, int number)
		{
			foreach (var f in msg.Fields)
				if (f.Number == number)
					return true;
			return false;
		}

		bool FieldsEqual(List<ProtoField> a, List<ProtoField> b)
		{
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i].Name != b[i].Name || a[i].Number != b[i].Number || a[i].Optional != b[i].Optional
					|| !TypeEqual(a[i].Type, b[i].Type))
					return false;
			}
			return true;
		}

		// Structural type equality. Two anonymous messages (e.g. two occurrences of the
		// same tuple shape) are equal if their definitions are structurally equal.
		bool TypeEqual(ProtoType? a, ProtoType? b)
		{
			if (ReferenceEquals(a, b)) return true;
			if (a == null || b == null || a.Kind != b.Kind) return false;
			switch (a.Kind) {
				case ProtoKind.Scalar: return a.Scalar == b.Scalar;
				case ProtoKind.Message:
					if (Equals(a.MessageKey, b.MessageKey)) return true;
					if (a.MessageKey is Type && b.MessageKey is Type) return false;
					return MessagesEqual(a.MessageKey!, b.MessageKey!);
				default: return TypeEqual(a.Element, b.Element);
			}
		}
		bool MessagesEqual(object keyA, object keyB)
		{
			if (!_defs.TryGetValue(keyA, out var a) || !_defs.TryGetValue(keyB, out var b))
				return false;
			return a.TagName == b.TagName && FieldsEqual(a.Fields, b.Fields);
		}

		// Chooses the Object returned when BeginSubObject declines because the type is
		// already (being) recorded. ObjectSyncher casts this to T, so for a value type
		// return a boxed default(T) rather than null (unless the caller avoided boxing).
		static object? DeclinedObject(object childKey, bool dedup, bool nullable)
		{
			bool avoidBoxing = !dedup && !nullable;
			if (!avoidBoxing && childKey is Type type && type.IsValueType)
				return Activator.CreateInstance(type);
			return null;
		}

		#endregion

		#region Rendering

		// Wrapper messages generated for List/Opt/Ref types, keyed by structural signature
		Dictionary<string, (string Name, ProtoType Type)>? _wrappers;
		List<(string Name, ProtoType Type)>? _wrappersInOrder;
		HashSet<string>? _usedNames;

		public void Render(IBufferWriter<byte> output)
		{
			MergeAnonymousDuplicates();
			_usedNames = new HashSet<string>();
			_wrappers = new Dictionary<string, (string, ProtoType)>();
			_wrappersInOrder = new List<(string, ProtoType)>();
			AssignNames();

			// Synthesize wrapper messages for every List/Opt/Ref type, in a deterministic order
			foreach (var def in _defsInOrder)
				if (!def.Merged)
					foreach (var f in def.Fields)
						TypeRefName(f.Type);
			string? rootName = _rootType != null ? TypeRefName(_rootType) : null;

			var sb = new StringBuilder();
			sb.Append("// Generated by Loyc.SyncLib SyncProtobuf.Schema.\n");
			sb.Append("// A proto3 schema describing the messages that SyncProtobuf.Writer produces.\n");
			if (rootName != null)
				sb.Append("// The root of the serialized data is one ").Append(rootName).Append(" message.\n");
			sb.Append("syntax = \"proto3\";\n");

			foreach (var def in _defsInOrder) {
				if (def.Merged)
					continue;
				sb.Append('\n');
				sb.Append("message ").Append(def.AssignedName).Append(" {");
				bool hasPresent = def == _rootPlainDef;
				if (def.Fields.Count == 0 && !hasPresent) {
					sb.Append("}\n");
					continue;
				}
				sb.Append('\n');
				foreach (var f in def.Fields)
					AppendField(sb, f.Type, f.Optional, false, f.Name, f.Number);
				if (hasPresent) {
					// Written only when the root message body would otherwise be empty
					// (zero bytes is the encoding of a null root)
					AppendField(sb, ProtoType.MakeScalar("bool"), true, false, "_present", PresentFieldNumber);
				}
				sb.Append("}\n");
			}

			// _wrappersInOrder can grow while wrappers reference other wrappers, but all
			// were synthesized above, so a plain loop suffices
			foreach (var (name, type) in _wrappersInOrder) {
				sb.Append('\n');
				sb.Append("message ").Append(name).Append(" {\n");
				switch (type.Kind) {
					case ProtoKind.List:
						AppendField(sb, type.Element!, false, true, "items", 1);
						break;
					case ProtoKind.Opt:
						AppendField(sb, type.Element!, true, false, "value", 1);
						break;
					default:
						AppendField(sb, ProtoType.MakeScalar("uint64"), false, false, "id", 1);
						AppendField(sb, type.Element!, true, false, "value", 2);
						break;
				}
				sb.Append("}\n");
			}

			var bytes = Encoding.UTF8.GetBytes(sb.ToString());
			var span = output.GetSpan(bytes.Length);
			bytes.CopyTo(span);
			output.Advance(bytes.Length);
		}

		void AppendField(StringBuilder sb, ProtoType type, bool optional, bool repeated, string name, int number)
		{
			sb.Append("  ");
			if (repeated)
				sb.Append("repeated ");
			else if (optional && type.Kind == ProtoKind.Scalar)
				sb.Append("optional "); // message-typed fields already have explicit presence
			sb.Append(TypeRefName(type)).Append(' ').Append(name).Append(" = ").Append(number).Append(";\n");
		}

		// Returns the .proto type name for a field of type `type`, generating a wrapper
		// message for List/Opt/Ref types on first use.
		string TypeRefName(ProtoType type)
		{
			switch (type.Kind) {
				case ProtoKind.Scalar:
					return type.Scalar!;
				case ProtoKind.Message:
					return _defs[Canonical(type.MessageKey!)].AssignedName!;
				default:
					string sig = Signature(type);
					if (_wrappers!.TryGetValue(sig, out var wrapper))
						return wrapper.Name;
					// Name the wrapper after its element, e.g. Int32List, StringOpt, PersonRef
					string elemName = BaseNameOf(type.Element!);
					string suffix = type.Kind == ProtoKind.List ? "List" : type.Kind == ProtoKind.Opt ? "Opt" : "Ref";
					string name = UniqueName(elemName + suffix);
					_wrappers.Add(sig, (name, type));
					_wrappersInOrder!.Add((name, type));
					return name;
			}
		}

		string BaseNameOf(ProtoType type)
		{
			switch (type.Kind) {
				case ProtoKind.Scalar: return ScalarBaseName(type.Scalar!);
				case ProtoKind.Message: return _defs[Canonical(type.MessageKey!)].AssignedName!;
				default: return TypeRefName(type); // a wrapper's own name (e.g. Int32OptList)
			}
		}

		static string ScalarBaseName(string scalar) => scalar switch {
			"bool" => "Bool", "int32" => "Int32", "int64" => "Int64",
			"uint32" => "UInt32", "uint64" => "UInt64",
			"float" => "Float", "double" => "Double",
			"string" => "String", "bytes" => "Bytes",
			_ => SanitizeMessageName(scalar),
		};

		string Signature(ProtoType type) => type.Kind switch {
			ProtoKind.Scalar => "s:" + type.Scalar,
			ProtoKind.Message => "m:" + _defs[Canonical(type.MessageKey!)].AssignedName,
			ProtoKind.List => "L(" + Signature(type.Element!) + ")",
			ProtoKind.Opt => "O(" + Signature(type.Element!) + ")",
			_ => "R(" + Signature(type.Element!) + ")",
		};

		#endregion

		#region Name assignment and anonymous-message merging

		// Keys of anonymous messages that were merged into an identical earlier one
		Dictionary<object, object>? _keyRemap;

		object Canonical(object key)
			=> _keyRemap != null && _keyRemap.TryGetValue(key, out var canon) ? canon : key;

		// Two structurally identical anonymous messages (e.g. two (int, string) tuples)
		// become a single message in the rendered schema.
		void MergeAnonymousDuplicates()
		{
			_keyRemap = new Dictionary<object, object>();
			var canonicalAnons = new List<ProtoMessage>();
			foreach (var def in _defsInOrder) {
				def.Merged = false;
				if (def.Key is Type)
					continue;
				foreach (var canon in canonicalAnons) {
					if (canon.TagName == def.TagName && FieldsEqual(canon.Fields, def.Fields)) {
						def.Merged = true;
						_keyRemap[def.Key] = canon.Key;
						break;
					}
				}
				if (!def.Merged)
					canonicalAnons.Add(def);
			}
		}

		void AssignNames()
		{
			foreach (var def in _defsInOrder) {
				if (def.Merged)
					continue;
				string baseName = SanitizeMessageName(def.TagName ?? TypeNameOf(def.Key));
				def.AssignedName = UniqueName(baseName);
			}
		}

		string UniqueName(string baseName)
		{
			string name = baseName;
			for (int i = 2; !_usedNames!.Add(name); i++)
				name = baseName + i;
			return name;
		}

		static string TypeNameOf(object key)
			=> key is Type type ? type.Name : (key.ToString() ?? "Message");

		static string SanitizeMessageName(string name)
		{
			var sb = new StringBuilder(name.Length);
			foreach (char c in name)
				sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
			if (sb.Length == 0 || !(char.IsLetter(sb[0]) || sb[0] == '_'))
				sb.Insert(0, 'M');
			return sb.ToString();
		}

		static string FieldName(string? name, int number)
		{
			if (string.IsNullOrEmpty(name))
				return "field" + number;
			var sb = new StringBuilder(name!.Length);
			foreach (char c in name)
				sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
			if (sb.Length == 0 || !(char.IsLetter(sb[0]) || sb[0] == '_'))
				sb.Insert(0, 'f');
			return sb.ToString();
		}

		#endregion
	}
}
