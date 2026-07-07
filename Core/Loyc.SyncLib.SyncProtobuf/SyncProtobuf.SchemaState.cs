using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	/// <summary>The kind of a <see cref="ProtoType"/>: a scalar, a reference to a message,
	///   or a repeated (list) element type.</summary>
	internal enum ProtoKind { Scalar, Message, List }

	/// <summary>The type of one field in a recorded <c>.proto</c> schema.</summary>
	internal class ProtoType
	{
		public ProtoKind Kind;
		public string? Scalar;     // Kind == Scalar (e.g. "int32", "string", "bytes")
		public object? MessageKey; // Kind == Message (a Type, or a synthetic key for anonymous messages)
		public ProtoType? Element; // Kind == List (the element type)

		public static ProtoType MakeScalar(string s) => new ProtoType { Kind = ProtoKind.Scalar, Scalar = s };
		public static ProtoType MakeMessage(object key) => new ProtoType { Kind = ProtoKind.Message, MessageKey = key };
		public static ProtoType MakeList(ProtoType? element) => new ProtoType { Kind = ProtoKind.List, Element = element };

		public static bool Equal(ProtoType? a, ProtoType? b)
		{
			if (ReferenceEquals(a, b)) return true;
			if (a == null || b == null || a.Kind != b.Kind) return false;
			return a.Kind switch {
				ProtoKind.Scalar => a.Scalar == b.Scalar,
				ProtoKind.Message => Equals(a.MessageKey, b.MessageKey),
				ProtoKind.List => Equal(a.Element, b.Element),
				_ => false,
			};
		}
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
		public string? TagName;      // from SyncTypeTag, or "Anonymous"
		public string? AssignedName; // final, unique, sanitized name (assigned at render time)
		public bool InProgress;      // true while being recorded (breaks cycles)
		public ProtoMessage(object key) { Key = key; }
	}

	/// <summary>The core of <see cref="SyncProtobuf.Schema"/>: records the messages that a
	///   synchronizer describes while running in <see cref="SyncMode.Schema"/> mode, then
	///   renders them as a proto3 <c>.proto</c> document.</summary>
	internal class SchemaState
	{
		internal Options _opt;

		// A defensive limit on anonymous (childKey == null) object nesting, which cannot be
		// deduplicated and so would recurse forever for a recursive anonymous type.
		const int MaxAnonymousDepth = 200;

		class Frame
		{
			public ProtoMessage? Msg;       // message that receives fields
			public ProtoType? ListType;     // if IsList: the list type whose Element we fill
			public bool IsList, IsTuple;
			public int ItemCount;
			public int ExpectedItems = 1;
			public int LastFieldId;
			public ProtoMessage? Def;        // definition recorded by this frame
			public ProtoMessage? CompareTo;  // if Def is a scratch re-record, the original
		}

		Dictionary<object, ProtoMessage> _defs = new Dictionary<object, ProtoMessage>();
		List<ProtoMessage> _defsInOrder = new List<ProtoMessage>();
		List<Frame> _stack = new List<Frame>();

		public SchemaState(Options options) { _opt = options; }

		Frame? Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

		public int Depth => _stack.Count;
		public bool IsInsideList => Top?.IsList ?? false;
		public bool? ReachedEndOfList {
			get {
				var top = Top;
				return top != null && top.IsList ? top.ItemCount >= top.ExpectedItems : (bool?)null;
			}
		}
		public int? MinimumListLength => IsInsideList ? 0 : (int?)null;

		#region Recording

		int ResolveNumber(Frame f, FieldId field)
		{
			int id = field.Id != int.MinValue ? field.Id : f.LastFieldId + 1;
			f.LastFieldId = id;
			return id;
		}

		// Attaches a sub-object/list field to its parent, unless this is the root value
		// (which has no parent to attach to — it is just recorded as a top-level message).
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
				// A schema saver visits one element (or, for a tuple, ExpectedItems of them);
				// the element type becomes the repeated field's element type.
				top.ItemCount++;
				top.ListType!.Element = type;
			} else {
				int num = ResolveNumber(top, field);
				top.Msg!.Fields.Add(new ProtoField {
					Name = FieldName(field.Name, num), Number = num, Optional = optional, Type = type,
				});
			}
		}

		public void SyncScalar(FieldId field, string protoType, bool optional)
			=> Attach(field, ProtoType.MakeScalar(protoType), optional);

		// For SyncListBoolImpl/Byte/Char, which do not call BeginSubObject.
		public void SyncScalarList(FieldId field, string elemProtoType)
			=> Attach(field, ProtoType.MakeList(ProtoType.MakeScalar(elemProtoType)), optional: false);

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId field, object? childKey, ObjectMode mode, int listLength)
		{
			if ((mode & ObjectMode.List) != 0) {
				if (Top == null)
					throw new InvalidOperationException(
						"SyncProtobuf.Schema: the root value must be a message/object, not a list.");
				bool isTuple = (mode & ObjectMode.Tuple) == ObjectMode.Tuple;
				var listType = ProtoType.MakeList(null);
				Attach(field, listType, optional: false);
				_stack.Add(new Frame {
					IsList = true, IsTuple = isTuple, ListType = listType,
					ExpectedItems = isTuple ? (listLength >= 0 ? listLength : int.MaxValue) : 1,
				});
				return (true, isTuple && listLength >= 0 ? listLength : 1, null);
			}

			// In Schema mode a non-Type childKey is only a sample instance; its type
			// identifies the message (see ObjectSyncher and the ISyncManager docs).
			if (childKey != null && !(childKey is Type))
				childKey = childKey.GetType();

			if (childKey == null) {
				// Anonymous message: recorded inline, not deduplicated (so it cannot recur).
				if (Depth >= MaxAnonymousDepth)
					throw new InvalidOperationException(
						"SyncProtobuf.Schema: object nesting is too deep. This usually means a recursive type " +
						"was synchronized via BeginSubObject with childKey == null; in Schema mode childKey should " +
						"identify the type (helper methods pass typeof(T)).");
				var anonKey = new object();
				var anon = new ProtoMessage(anonKey) { TagName = "Anonymous", InProgress = true };
				_defs.Add(anonKey, anon);
				_defsInOrder.Add(anon);
				AttachRef(field, ProtoType.MakeMessage(anonKey), optional: false);
				_stack.Add(new Frame { Msg = anon, Def = anon });
				return (true, 1, null);
			}

			if (_defs.TryGetValue(childKey, out var existing)) {
				if (existing.InProgress) {
					// A cycle (or a repeat while re-recording): reference without recording again.
					AttachRef(field, ProtoType.MakeMessage(childKey), optional: false);
					return (false, 0, DeclinedObject(childKey, mode));
				}
				// Re-record into a scratch definition and compare at EndSubObject, to catch a
				// type that is synchronized in two conflicting ways.
				existing.InProgress = true;
				var scratch = new ProtoMessage(childKey);
				AttachRef(field, ProtoType.MakeMessage(childKey), optional: false);
				_stack.Add(new Frame { Msg = scratch, Def = scratch, CompareTo = existing });
				return (true, 1, null);
			}

			var newDef = new ProtoMessage(childKey) { InProgress = true };
			_defs.Add(childKey, newDef);
			_defsInOrder.Add(newDef);
			AttachRef(field, ProtoType.MakeMessage(childKey), optional: false);
			_stack.Add(new Frame { Msg = newDef, Def = newDef });
			return (true, 1, null);
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
			if (top.Def != null && tag != null)
				top.Def.TagName ??= tag;
			return tag;
		}

		static bool FieldsEqual(List<ProtoField> a, List<ProtoField> b)
		{
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i].Name != b[i].Name || a[i].Number != b[i].Number || a[i].Optional != b[i].Optional
					|| !ProtoType.Equal(a[i].Type, b[i].Type))
					return false;
			}
			return true;
		}

		// Chooses the Object returned when BeginSubObject declines because the type is
		// already (being) recorded. ObjectSyncher casts this to T, so for a value type
		// return a boxed default(T) rather than null (unless the caller avoided boxing).
		static object? DeclinedObject(object childKey, ObjectMode mode)
		{
			bool avoidBoxing = (mode & (ObjectMode.Deduplicate | ObjectMode.NotNull)) == ObjectMode.NotNull;
			if (!avoidBoxing && childKey is Type type && type.IsValueType)
				return Activator.CreateInstance(type);
			return null;
		}

		#endregion

		#region Rendering

		public void Render(IBufferWriter<byte> output)
		{
			AssignNames();

			var sb = new StringBuilder();
			sb.Append("// Generated by Loyc.SyncLib SyncProtobuf.Schema.\n");
			sb.Append("// A proto3 schema describing the messages that SyncProtobuf.Writer produces.\n");
			sb.Append("syntax = \"proto3\";\n");

			foreach (var def in _defsInOrder) {
				sb.Append('\n');
				sb.Append("message ").Append(def.AssignedName).Append(" {");
				if (def.Fields.Count == 0) {
					sb.Append("}\n");
					continue;
				}
				sb.Append('\n');
				foreach (var f in def.Fields) {
					sb.Append("  ");
					if (f.Type.Kind == ProtoKind.List)
						sb.Append("repeated ");
					else if (f.Optional)
						sb.Append("optional ");
					sb.Append(TypeName(f.Type.Kind == ProtoKind.List ? f.Type.Element! : f.Type));
					sb.Append(' ').Append(f.Name).Append(" = ").Append(f.Number).Append(";\n");
				}
				sb.Append("}\n");
			}

			var bytes = Encoding.UTF8.GetBytes(sb.ToString());
			var span = output.GetSpan(bytes.Length);
			bytes.CopyTo(span);
			output.Advance(bytes.Length);
		}

		string TypeName(ProtoType type)
		{
			switch (type.Kind) {
				case ProtoKind.Scalar: return type.Scalar!;
				case ProtoKind.Message: return _defs[type.MessageKey!].AssignedName!;
				default:
					throw new NotSupportedException(
						"SyncProtobuf.Schema cannot express a list whose elements are themselves lists " +
						"(repeated-of-repeated) in a .proto file. Wrap the inner list in a message type.");
			}
		}

		void AssignNames()
		{
			var used = new HashSet<string>();
			foreach (var def in _defsInOrder) {
				string baseName = SanitizeMessageName(def.TagName ?? TypeNameOf(def.Key));
				string name = baseName;
				for (int i = 2; !used.Add(name); i++)
					name = baseName + i;
				def.AssignedName = name;
			}
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
