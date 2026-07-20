using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncJson
{
	/// <summary>The set of primitive types that a JSON Schema node can have in its
	///   "type" keyword. These are flags because a node can allow multiple types,
	///   e.g. a nullable string has <c>"type": ["string", "null"]</c>.</summary>
	[Flags]
	internal enum JsonSchemaType : byte
	{
		None = 0,
		Object = 1,
		Array = 2,
		String = 4,
		Integer = 8,
		Number = 16,
		Boolean = 32,
		Null = 64,
	}

	/// <summary>A mutable in-memory representation of a JSON Schema node (the schema
	///   of one value: an object, array or primitive). <see cref="SchemaState"/> builds
	///   a tree of these while a synchronizer runs in <see cref="SyncMode.Schema"/>
	///   mode, then renders the tree as a JSON Schema document.</summary>
	internal class SchemaNode
	{
		/// <summary>Allowed primitive type(s), rendered as the "type" keyword.</summary>
		public JsonSchemaType Types;
		/// <summary>Adds "null" to the "type" keyword when rendered.</summary>
		public bool Nullable;
		/// <summary>If true, an optional "$id" (or "\f") marker property is added to
		///   this object when rendering, because at least one occurrence of the object
		///   used <see cref="ObjectMode.Deduplicate"/>. This flag is deliberately NOT
		///   part of shape equality, since it depends on how the object was reached,
		///   not on the object's own schema.</summary>
		public bool HasId;

		// Object schemas: properties in the order they were recorded. All of them are
		// listed in the "required" keyword when rendered (SyncJson.Writer always writes
		// every field that a synchronizer function touches).
		public List<KeyValuePair<string, SchemaNode>>? Properties;

		// Array schemas. PrefixItems is used for tuples (fixed-length, per-position
		// item types); Items is used for ordinary lists (and for tuples whose items
		// all have the same schema).
		public SchemaNode? Items;
		public List<SchemaNode>? PrefixItems;
		public int MinItems = -1, MaxItems = -1;

		/// <summary>If not null, this node is a reference to a definition in "$defs"
		///   and all other fields are ignored. The key is normally a Type; it is
		///   resolved to a name when the document is rendered.</summary>
		public object? RefKey;

		/// <summary>A union of alternative schemas ("anyOf"), e.g. a deduplicated
		///   object is either the object itself or a back-reference to it.</summary>
		public List<SchemaNode>? AnyOf;

		// Constraints on primitives
		public BigInteger? Minimum, Maximum;
		public int MinLength = -1, MaxLength = -1;
		public string? ContentEncoding;
		/// <summary>Fixed value of the property (used for type tags written by
		///   <see cref="ISyncManager.SyncTypeTag"/>).</summary>
		public string? Const;

		/// <summary>Returns true if the node has no constraints or structure other
		///   than <see cref="Types"/>/<see cref="Nullable"/>, meaning that another
		///   such node can be merged with it by ORing the type flags.</summary>
		public bool IsPlainTypeSet =>
			Properties == null && Items == null && PrefixItems == null && AnyOf == null
			&& RefKey == null && Const == null && ContentEncoding == null && !HasId
			&& Minimum == null && Maximum == null
			&& MinItems < 0 && MaxItems < 0 && MinLength < 0 && MaxLength < 0;

		/// <summary>Structural equality (ignores <see cref="HasId"/>; see its docs).
		///   Used to detect when the same type was synchronized in two conflicting
		///   ways, and to merge identical nodes.</summary>
		public static bool Equal(SchemaNode? a, SchemaNode? b)
		{
			if (ReferenceEquals(a, b))
				return true;
			if (a == null || b == null)
				return false;
			if (a.Types != b.Types || a.Nullable != b.Nullable
				|| !object.Equals(a.RefKey, b.RefKey)
				|| a.Minimum != b.Minimum || a.Maximum != b.Maximum
				|| a.MinItems != b.MinItems || a.MaxItems != b.MaxItems
				|| a.MinLength != b.MinLength || a.MaxLength != b.MaxLength
				|| a.ContentEncoding != b.ContentEncoding || a.Const != b.Const)
				return false;
			if ((a.Properties == null) != (b.Properties == null))
				return false;
			if (a.Properties != null) {
				if (a.Properties.Count != b.Properties!.Count)
					return false;
				for (int i = 0; i < a.Properties.Count; i++) {
					if (a.Properties[i].Key != b.Properties[i].Key
						|| !Equal(a.Properties[i].Value, b.Properties[i].Value))
						return false;
				}
			}
			return Equal(a.Items, b.Items)
				&& ListEqual(a.PrefixItems, b.PrefixItems)
				&& ListEqual(a.AnyOf, b.AnyOf);
		}

		static bool ListEqual(List<SchemaNode>? a, List<SchemaNode>? b)
		{
			if ((a == null) != (b == null))
				return false;
			if (a != null) {
				if (a.Count != b!.Count)
					return false;
				for (int i = 0; i < a.Count; i++)
					if (!Equal(a[i], b[i]))
						return false;
			}
			return true;
		}

		/// <summary>Combines two schemas recorded for the same location (e.g. a
		///   property recorded twice, or two different list items). Identical nodes
		///   merge to one; plain type sets are merged by ORing the type flags;
		///   otherwise an "anyOf" union is produced.</summary>
		public static SchemaNode Merge(SchemaNode a, SchemaNode b)
		{
			if (Equal(a, b))
				return a;
			if (a.IsPlainTypeSet && b.IsPlainTypeSet) {
				a.Types |= b.Types;
				a.Nullable |= b.Nullable;
				return a;
			}
			// If a is already a pure anyOf union, add b to it (unless already present)
			if (a.AnyOf != null && a.Types == JsonSchemaType.None && !a.Nullable && a.RefKey == null
				&& a.Properties == null && a.Items == null && a.PrefixItems == null) {
				foreach (var variant in a.AnyOf)
					if (Equal(variant, b))
						return a;
				a.AnyOf.Add(b);
				return a;
			}
			return new SchemaNode { AnyOf = new List<SchemaNode> { a, b } };
		}
	}

	/// <summary>The core logic of <see cref="SyncJson.SchemaWriter"/>: records the schema
	///   of objects that a synchronizer function describes to it, and renders the
	///   result as a JSON Schema (draft 2020-12) document in UTF-8 format.</summary>
	internal class SchemaState
	{
		internal Options _opt;

		/// <summary>Key of the shared "$defs" entry for back-references (the
		///   <c>{"$ref": "..."}</c> objects written for deduplicated values).</summary>
		static readonly object _backRefKey = new object();

		internal class Def
		{
			public object Key;
			public SchemaNode Root;
			/// <summary>Name override, from SyncTypeTag or (for the back-reference
			///   def) chosen by SchemaState itself.</summary>
			public string? TagName;
			/// <summary>True while the definition is being recorded. Occurrences of
			///   an in-progress type always become "$ref" without re-recording, which
			///   is what breaks cycles in recursive schemas.</summary>
			public bool InProgress;
			public string? AssignedName;
			public Def(object key, SchemaNode root) { Key = key; Root = root; }
		}

		class Frame
		{
			/// <summary>The node that receives fields (objects) or items (lists).</summary>
			public SchemaNode Node;
			/// <summary>The definition being recorded, if this frame records one.</summary>
			public Def? Def;
			/// <summary>If not null, Def is a scratch re-recording of an already-completed
			///   definition, which is compared against CompareTo when the frame ends in
			///   order to detect conflicting schemas for a single type.</summary>
			public Def? CompareTo;
			public bool IsList, IsTuple;
			public int ItemCount;
			/// <summary>Number of items after which ReachedEndOfList reports true
			///   (1 for lists, since a schema saver pretends lists have one item).</summary>
			public int ExpectedItems = 1;
			public Frame(SchemaNode node) { Node = node; }
		}

		Dictionary<object, Def> _defs = new Dictionary<object, Def>();
		List<Def> _defsInOrder = new List<Def>();
		List<Frame> _stack = new List<Frame>();
		SchemaNode? _root;
		Dictionary<string, string>? _nameCache;

		public SchemaState(Options options)
		{
			_opt = options;
			if (_opt.NameConverter != null)
				_nameCache = new Dictionary<string, string>();
		}

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

		// Applies _opt.NameConverter, memoizing its result per distinct property name.
		string ConvertName(string? propName)
		{
			propName ??= "";
			if (_nameCache == null)
				return propName;
			if (!_nameCache.TryGetValue(propName, out var converted))
				_nameCache[propName] = converted = _opt.NameConverter!(propName);
			return converted;
		}

		#region Recording

		/// <summary>Records the schema of a field of the current object, an item of
		///   the current list, or (at depth 0) the root value.</summary>
		internal void Attach(string? name, SchemaNode node, bool convertName = true)
		{
			var top = Top;
			if (top == null) {
				if (_root != null)
					throw new InvalidOperationException(
						"SyncJson.Schema cannot describe multiple values at depth zero. " +
						"Wrap the values in a root object (e.g. use SyncJson.WriteSchema).");
				_root = node;
			} else if (top.IsList) {
				top.ItemCount++;
				if (top.IsTuple)
					top.Node.PrefixItems!.Add(node);
				else
					top.Node.Items = top.Node.Items == null ? node : SchemaNode.Merge(top.Node.Items, node);
			} else {
				string key = convertName ? ConvertName(name) : (name ?? "");
				var props = top.Node.Properties ??= new List<KeyValuePair<string, SchemaNode>>();
				for (int i = 0; i < props.Count; i++) {
					if (props[i].Key == key) {
						props[i] = new KeyValuePair<string, SchemaNode>(key, SchemaNode.Merge(props[i].Value, node));
						return;
					}
				}
				props.Add(new KeyValuePair<string, SchemaNode>(key, node));
			}
		}

		/// <summary>Records one occurrence of a value, adding the wrappers implied by
		///   the <see cref="ObjectMode"/>: a "null" alternative if the value may be
		///   null, and (for deduplicated values) the {"$id",...} wrapper for lists
		///   and a back-reference alternative.</summary>
		void AttachOccurrence(string? name, SchemaNode main, ObjectMode mode, bool isList)
		{
			bool atRoot = _stack.Count == 0;
			// The root value's schema deliberately excludes "null": if the root object
			// is null, SyncJson.Writer emits the document `null`, and admitting that
			// would force every schema to start with a noisy anyOf wrapper.
			bool nullable = MayBeNullable(mode) && !atRoot;
			bool dedup = (mode & ObjectMode.Deduplicate) != 0;

			SchemaNode occurrence = main;
			if (dedup && isList)
				occurrence = DedupListWrapper(main);

			List<SchemaNode>? anyOf = null;
			if (dedup && !atRoot)
				anyOf = new List<SchemaNode> { occurrence, BackRefNode() };
			if (nullable) {
				if (anyOf == null && occurrence.RefKey == null && occurrence.AnyOf == null) {
					occurrence.Nullable = true;
				} else {
					anyOf ??= new List<SchemaNode> { occurrence };
					anyOf.Add(new SchemaNode { Types = JsonSchemaType.Null });
				}
			}
			Attach(name, anyOf != null ? new SchemaNode { AnyOf = anyOf } : occurrence);
		}

		public (bool Begun, int Length, object? Object) BeginSubObject(string? name, object? childKey, ObjectMode mode, int listLength)
		{
			bool dedup = (mode & ObjectMode.Deduplicate) != 0;

			if ((mode & ObjectMode.List) != 0) {
				bool isTuple = (mode & ObjectMode.Tuple) == ObjectMode.Tuple;
				var arrayNode = new SchemaNode { Types = JsonSchemaType.Array };
				if (isTuple)
					arrayNode.PrefixItems = new List<SchemaNode>();
				AttachOccurrence(name, arrayNode, mode, isList: true);
				_stack.Add(new Frame(arrayNode) {
					IsList = true, IsTuple = isTuple,
					ExpectedItems = isTuple ? (listLength >= 0 ? listLength : int.MaxValue) : 1,
				});
				return (true, isTuple && listLength >= 0 ? listLength : 1, null);
			}

			// Generic synchronization code written for all modes may pass a data object
			// (e.g. a boxed struct, or a field that was pre-initialized by the caller's
			// sync function) rather than a Type. Since there is no data in Schema mode,
			// such an object is only a sample instance: its type identifies the schema.
			if (childKey != null && !(childKey is Type))
				childKey = childKey.GetType();

			if (childKey == null) {
				// An anonymous object: its schema is recorded inline (no "$defs" entry).
				// Since anonymous objects cannot be recognized when they recur, guard
				// against infinite recursion with a depth limit.
				if (Depth >= _opt.Read.MaxDepth)
					throw new InvalidOperationException(
						"SyncJson.Schema: object nesting exceeded Options.Read.MaxDepth. This usually means " +
						"a recursive type was synchronized via BeginSubObject with childKey == null. In Schema " +
						"mode, childKey should identify the sub-object's type (helper methods pass typeof(T)).");
				var objNode = new SchemaNode { Types = JsonSchemaType.Object, HasId = dedup };
				AttachOccurrence(name, objNode, mode, isList: false);
				_stack.Add(new Frame(objNode));
				return (true, 1, null);
			}

			if (_defs.TryGetValue(childKey, out var def)) {
				def.Root.HasId |= dedup;
				if (def.InProgress) {
					// A cycle in the schema (or a repeat while re-recording): refer to
					// the definition without recording it again.
					AttachOccurrence(name, RefNode(childKey), mode, isList: false);
					return (false, 0, DeclinedObject(childKey, mode));
				}
				// This type's schema was recorded before. Record it again into a scratch
				// definition and compare at EndSubObject, to detect a type that is
				// synchronized in two conflicting ways.
				def.InProgress = true;
				var scratch = new Def(childKey, new SchemaNode { Types = JsonSchemaType.Object });
				AttachOccurrence(name, RefNode(childKey), mode, isList: false);
				_stack.Add(new Frame(scratch.Root) { Def = scratch, CompareTo = def });
				return (true, 1, null);
			}

			var newDef = new Def(childKey, new SchemaNode { Types = JsonSchemaType.Object, HasId = dedup }) { InProgress = true };
			_defs.Add(childKey, newDef);
			_defsInOrder.Add(newDef);
			AttachOccurrence(name, RefNode(childKey), mode, isList: false);
			_stack.Add(new Frame(newDef.Root) { Def = newDef });
			return (true, 1, null);
		}

		public void EndSubObject()
		{
			if (_stack.Count == 0)
				throw new InvalidOperationException("SyncJson.Schema: EndSubObject was called more times than BeginSubObject.");
			var frame = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);

			if (frame.IsTuple)
				NormalizeTuple(frame.Node, frame.ItemCount);

			if (frame.Def != null) {
				if (frame.CompareTo != null) {
					frame.CompareTo.InProgress = false;
					frame.CompareTo.TagName ??= frame.Def.TagName;
					if (!SchemaNode.Equal(frame.Def.Root, frame.CompareTo.Root))
						throw new InvalidOperationException(
							$"SyncJson.Schema: the type '{TypeNameOf(frame.Def.Key)}' was synchronized in two " +
							"conflicting ways" + DescribeDifference(frame.CompareTo.Root, frame.Def.Root) + ". " +
							"Each type (as identified by the childKey given to BeginSubObject, normally typeof(T)) " +
							"must always be synchronized with the same schema.");
				} else {
					frame.Def.InProgress = false;
				}
			}
		}

		static void NormalizeTuple(SchemaNode node, int itemCount)
		{
			node.MinItems = node.MaxItems = itemCount;
			var prefix = node.PrefixItems;
			if (prefix == null || prefix.Count == 0) {
				node.PrefixItems = null;
				return;
			}
			// If all the tuple's items have the same schema, "items" + minItems/maxItems
			// is simpler than a list of identical "prefixItems".
			for (int i = 1; i < prefix.Count; i++)
				if (!SchemaNode.Equal(prefix[i], prefix[0]))
					return;
			node.Items = prefix[0];
			node.PrefixItems = null;
		}

		static string DescribeDifference(SchemaNode first, SchemaNode second)
		{
			static string PropNames(SchemaNode n) =>
				n.Properties == null ? "(none)" : string.Join(", ", n.Properties.ConvertAll(p => p.Key));
			string a = PropNames(first), b = PropNames(second);
			return a != b ? $" (properties recorded first: [{a}]; recorded later: [{b}])" : "";
		}

		public string? SyncTypeTag(string? tag)
		{
			var top = Top;
			if (top == null || top.IsList)
				throw new InvalidOperationException("SyncTypeTag can only be used inside an object (not a list).");
			if (top.Def != null && tag != null)
				top.Def.TagName ??= tag;
			string propName = _opt.NewtonsoftCompatibility ? "$type" : "\t";
			Attach(propName, new SchemaNode { Types = JsonSchemaType.String, Nullable = tag == null, Const = tag },
				convertName: false);
			return tag;
		}

		/// <summary>Records the schema of a primitive field (no ObjectMode applies).</summary>
		public void SyncPrim(string? name, SchemaNode node) => Attach(name, node);

		/// <summary>Records the schema of a primitive value whose ObjectMode determines
		///   nullability (e.g. a string, or a byte array written as a string).</summary>
		public void SyncPrimValue(string? name, SchemaNode node, ObjectMode mode)
		{
			node.Nullable |= MayBeNullable(mode) && _stack.Count > 0;
			Attach(name, node);
		}

		/// <summary>Records the schema of a list of primitives (as written by
		///   SyncListBoolImpl and friends, which do not call BeginSubObject).</summary>
		public void SyncPrimList(string? name, SchemaNode itemNode, ObjectMode mode, int tupleLength)
		{
			var arrayNode = new SchemaNode { Types = JsonSchemaType.Array, Items = itemNode };
			if ((mode & ObjectMode.Tuple) == ObjectMode.Tuple && tupleLength >= 0)
				arrayNode.MinItems = arrayNode.MaxItems = tupleLength;
			AttachOccurrence(name, arrayNode, mode, isList: true);
		}

		/// <summary>Chooses the Object to return when BeginSubObject declines a
		///   request because the sub-object's schema is already known. Callers such
		///   as <see cref="Impl.ObjectSyncher{SM,SyncObj,T}"/> cast this value to T,
		///   so when T is a value type, return a boxed default(T) instead of null
		///   (casting null to a struct would throw). When the caller avoided boxing
		///   (NotNull without Deduplicate), it expects null and ignores the value.</summary>
		static object? DeclinedObject(object childKey, ObjectMode mode)
		{
			bool avoidBoxing = (mode & (ObjectMode.Deduplicate | ObjectMode.NotNull)) == ObjectMode.NotNull;
			if (!avoidBoxing && childKey is Type type && type.IsValueType)
				return Activator.CreateInstance(type); // boxed default(T); null if T is Nullable<X>
			return null;
		}

		SchemaNode RefNode(object key) => new SchemaNode { RefKey = key };

		/// <summary>Builds the schema of the two-object representation of a
		///   deduplicated list, e.g. <c>{"$id": "7", "$values": [...]}</c>.</summary>
		SchemaNode DedupListWrapper(SchemaNode arrayNode)
		{
			bool nc = _opt.NewtonsoftCompatibility;
			var wrapper = new SchemaNode { Types = JsonSchemaType.Object };
			wrapper.Properties = new List<KeyValuePair<string, SchemaNode>> {
				new KeyValuePair<string, SchemaNode>(nc ? "$id" : "\f", IdMarkerNode()),
				new KeyValuePair<string, SchemaNode>(nc ? "$values" : "", arrayNode),
			};
			return wrapper;
		}

		/// <summary>Gets a $ref to the shared definition of a back-reference, e.g.
		///   <c>{"$ref": "1"}</c> (Newtonsoft) or <c>{"\r": 1}</c>, creating the
		///   definition on first use.</summary>
		SchemaNode BackRefNode()
		{
			if (!_defs.TryGetValue(_backRefKey, out var def)) {
				bool nc = _opt.NewtonsoftCompatibility;
				var node = new SchemaNode { Types = JsonSchemaType.Object };
				node.Properties = new List<KeyValuePair<string, SchemaNode>> {
					new KeyValuePair<string, SchemaNode>(nc ? "$ref" : "\r",
						new SchemaNode { Types = nc ? JsonSchemaType.String : JsonSchemaType.Integer }),
				};
				def = new Def(_backRefKey, node) { TagName = "backReference" };
				_defs.Add(_backRefKey, def);
				_defsInOrder.Add(def);
			}
			return RefNode(_backRefKey);
		}

		/// <summary>Schema of the "$id" (or "\f") marker property that deduplicated
		///   objects can contain.</summary>
		SchemaNode IdMarkerNode() => new SchemaNode {
			Types = _opt.NewtonsoftCompatibility ? JsonSchemaType.String : JsonSchemaType.Integer
		};

		#endregion

		#region Rendering

		public void Render(IBufferWriter<byte> output)
		{
			AssignNames();

			// Reuse WriterState to get escaping/indentation identical to SyncJson output,
			// but without the NameConverter: recorded property names were converted
			// already, and JSON Schema keywords must not be converted at all.
			var renderOptions = new Options {
				NewtonsoftCompatibility = _opt.NewtonsoftCompatibility,
				NameConverter = null,
				Write = _opt.Write,
			};
			var ws = new WriterState(output, renderOptions);
			ws.BeginSubObject(null, this, ObjectMode.Normal);
			ws.WriteProp("$schema", "https://json-schema.org/draft/2020-12/schema");
			if (_root != null)
				RenderContents(ws, _root);
			if (_defsInOrder.Count != 0) {
				ws.BeginSubObject("$defs", this, ObjectMode.Normal);
				foreach (var def in _defsInOrder)
					RenderNode(ws, def.AssignedName, def.Root);
				ws.EndSubObject();
			}
			ws.EndSubObject();
			ws.Flush();
		}

		void AssignNames()
		{
			var used = new HashSet<string>();
			foreach (var def in _defsInOrder) {
				string baseName = SanitizeName(def.TagName ?? TypeNameOf(def.Key));
				string name = baseName;
				for (int i = 2; !used.Add(name); i++)
					name = baseName + i;
				def.AssignedName = name;
			}
		}

		static string TypeNameOf(object key)
			=> key is Type type ? type.NameWithGenericArgs() : (key.ToString() ?? "Object");

		static string SanitizeName(string name)
		{
			var sb = new StringBuilder(name.Length);
			foreach (char c in name)
				sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '.' ? c : '_');
			int length = sb.Length;
			while (length > 0 && sb[length - 1] == '_')
				length--;
			return length == 0 ? "T" : sb.ToString(0, length);
		}

		void RenderNode(WriterState ws, string? name, SchemaNode node)
		{
			// Nodes without any nested structure fit comfortably on one line
			bool compact = node.Properties == null && node.AnyOf == null
				&& node.Items == null && node.PrefixItems == null && !node.HasId;
			ws.BeginSubObject(name, this, compact ? ObjectMode.Compact : ObjectMode.Normal);
			RenderContents(ws, node);
			ws.EndSubObject();
		}

		void RenderContents(WriterState ws, SchemaNode node)
		{
			if (node.RefKey != null) {
				ws.WriteProp("$ref", "#/$defs/" + _defs[node.RefKey].AssignedName);
				return;
			}

			var typeNames = TypeNamesOf(node);
			if (typeNames.Count == 1) {
				ws.WriteProp("type", typeNames[0]);
			} else if (typeNames.Count > 1) {
				ws.BeginSubObject("type", this, ObjectMode.List | ObjectMode.Compact);
				foreach (string typeName in typeNames)
					ws.WriteProp("", typeName);
				ws.EndSubObject();
			}

			if (node.Const != null)
				ws.WriteProp("const", node.Const);
			if (node.Minimum != null)
				ws.WriteProp("minimum", node.Minimum.Value);
			if (node.Maximum != null)
				ws.WriteProp("maximum", node.Maximum.Value);
			if (node.MinLength >= 0)
				ws.WriteProp("minLength", (long)node.MinLength);
			if (node.MaxLength >= 0)
				ws.WriteProp("maxLength", (long)node.MaxLength);
			if (node.ContentEncoding != null)
				ws.WriteProp("contentEncoding", node.ContentEncoding);

			if (node.Properties != null || node.HasId) {
				ws.BeginSubObject("properties", this, ObjectMode.Normal);
				if (node.HasId)
					RenderNode(ws, _opt.NewtonsoftCompatibility ? "$id" : "\f", IdMarkerNode());
				if (node.Properties != null)
					foreach (var prop in node.Properties)
						RenderNode(ws, prop.Key, prop.Value);
				ws.EndSubObject();

				// Every recorded property is required (SyncJson.Writer always writes
				// them), but the "$id" marker is not (it only appears on the first
				// occurrence of a deduplicated object).
				if (node.Properties != null && node.Properties.Count != 0) {
					ws.BeginSubObject("required", this, ObjectMode.List | ObjectMode.Compact);
					foreach (var prop in node.Properties)
						ws.WriteProp("", prop.Key);
					ws.EndSubObject();
				}
			}

			if (node.PrefixItems != null) {
				ws.BeginSubObject("prefixItems", this, ObjectMode.List);
				foreach (var item in node.PrefixItems)
					RenderNode(ws, null, item);
				ws.EndSubObject();
				ws.WriteLiteralProp("items", "false");
			} else if (node.Items != null) {
				RenderNode(ws, "items", node.Items);
			}
			if (node.MinItems >= 0)
				ws.WriteProp("minItems", (long)node.MinItems);
			if (node.MaxItems >= 0)
				ws.WriteProp("maxItems", (long)node.MaxItems);

			if (node.AnyOf != null) {
				ws.BeginSubObject("anyOf", this, ObjectMode.List);
				foreach (var variant in node.AnyOf)
					RenderNode(ws, null, variant);
				ws.EndSubObject();
			}
		}

		static List<string> TypeNamesOf(SchemaNode node)
		{
			var types = node.Types | (node.Nullable ? JsonSchemaType.Null : JsonSchemaType.None);
			var names = new List<string>(2);
			if ((types & JsonSchemaType.Object) != 0) names.Add("object");
			if ((types & JsonSchemaType.Array) != 0) names.Add("array");
			if ((types & JsonSchemaType.String) != 0) names.Add("string");
			if ((types & JsonSchemaType.Integer) != 0) names.Add("integer");
			if ((types & JsonSchemaType.Number) != 0) names.Add("number");
			if ((types & JsonSchemaType.Boolean) != 0) names.Add("boolean");
			if ((types & JsonSchemaType.Null) != 0) names.Add("null");
			return names;
		}

		#endregion
	}
}
