# SyncLib dynamic typing — design & implementation notes

**Status: implemented and green.** The netcore SyncLib suite passes (JSON × 4 option variants,
Binary × 2, Protobuf × 2, plus JSON-specific fixtures); on net472/mono only the pre-existing
Newtonsoft-under-mono quirks fail.

---

## The design in one example

```csharp
// ---- The model: NO serialization concerns on business objects ----
abstract class Shape { }
class Ellipse : Shape { public double Width, Height; }
class Polygon : Shape { public List<(double, double)>? Points; }

// ---- Synchronizers: [TypeTag] on each synchronizer function is the ONE
//      place tags are specified. Bodies never call SyncTypeTag. ----
static class ShapeSync<SM> where SM : ISyncManager
{
    [TypeTag("Ellipse")]
    public static Ellipse Sync(SM sm, Ellipse? e)
    {
        sm.CurrentObject = e ??= new Ellipse();
        e.Width  = sm.Sync("Width", e.Width);
        e.Height = sm.Sync("Height", e.Height);
        return e;
    }
    [TypeTag("Polygon")]
    public static Polygon Sync(SM sm, Polygon? p)
    {
        sm.CurrentObject = p ??= new Polygon();
        p.Points = sm.SyncAny("Points", p.Points);  // default synchronizer: list of tuples
        return p;
    }
}

// ---- Dynamic tier only: registration (typically at startup).
//      Add() scans the whole class; the tags it discovers are recorded in
//      TypeTagRegistry.Default (the instance ambient at the time of the call). ----
SyncTypeRegistry.Default.Add(typeof(ShapeSync<>));

// ---- Usage ----
class Drawing
{
    public Ellipse? Border;      // statically typed
    public List<Shape>? Shapes;  // dynamically typed items

    public static Drawing Sync<SM>(SM sm, Drawing? d) where SM : ISyncManager
    {
        sm.CurrentObject = d ??= new Drawing();
        // STATIC TIER: explicit synchronizer, static dispatch, no registry lookup.
        // The tag "Ellipse" is still written (and verified when reading), because
        // the synchronizer has a [TypeTag] — so this data stays readable dynamically.
        d.Border = sm.Sync("Border", d.Border, ShapeSync<SM>.Sync);
        // DYNAMIC TIER: opt-in mode; dispatches on GetType() (write) / tag (read).
        d.Shapes = sm.SyncDynamicList("Shapes", d.Shapes);
        return d;
    }
}
```

JSON output (`NewtonsoftCompatibility` on by default names the tag `"$type"`; off → `"\t"`):

```json
{ "Border": { "$type": "Ellipse", "Width": 10, "Height": 5 },
  "Shapes": [ { "$type": "Polygon", "Points": [[0,0],[1,0]] }, … ] }
```

The two headline properties: **business objects carry no serialization concerns** (tags live on
synchronizers, via `[TypeTag]`), and **statically- and dynamically-written data interoperate** —
the same tag is emitted in both tiers, so a field written with an explicit synchronizer can be read
back dynamically and vice-versa.

---

## Background: the storage primitive this builds on

Dynamic typing did not need new wire-format machinery; it composes two things that already existed.

### 1. `SyncTypeTag(string? tag)` on `ISyncManager`

Every backend can already read/write a type tag; dynamic typing is a policy layer on top:

- **`SyncJson.Writer`** writes a property named `"\t"` (or `"$type"` in `NewtonsoftCompatibility`
  mode) whose value is the tag.
- **`SyncJson.Reader`** — `ReadTypeTag()` returns the tag *only if the next field* is named `"\t"`
  or `"$type"`; otherwise null. So the tag must be written first in the object, and its presence is
  detectable (this is what lets foreign JSON without a `$type` fall through to the static type).
- **`SyncBinary`** writes an optional `'T'` marker byte + the string. **Not** self-describing: the
  reader must call `SyncTypeTag` iff the writer did. This is why the dynamic tier writes the tag
  *unconditionally* (see *Design decisions #3*) — binary can't detect an absent tag.
- **`SyncProtobuf`** stores the tag as a string field; both **Schema** modes rename the type
  definition after the tag (e.g. JSON Schema emits `"$type": {"const": "Ellipse"}` and names the
  `$defs` entry after the tag).

### 2. The `DefaultSynchronizer<SM, T>` caching trick

`DefaultSynchronizer<SM, T>.Default` is a **static field per closed (SM, T) pair** holding a
`SyncFieldFunc_Ref<SM, T>` delegate. Reflection runs **once per pair**, the result is cached in the
static field, and every subsequent call is a static-field load + delegate invoke. This is the core
no-codegen performance trick. `PredefinedSynchronizer<SM>` scans SM's `Sync` instance methods for
primitives; `TupleSynchronizer<SM>` covers tuples; `FallbackSync` self-patches so **late
registration works** — a type that threw at static-init retries `FindSynchronizer()` on each call.

Dynamic typing plugs into `FindSynchronizer()`: if the ambient `SyncTypeRegistry` `Handles(typeof(T))`
— T itself or any subtype is registered — it returns a registry-consulting synchronizer.

---

## Architecture: three focused modules

Rather than one global registry, responsibilities are split so each is independently customizable,
and both are exposed through the **Ambient Service Pattern** (`ThreadLocal<T>` + `Default` +
`SetDefault` returning a `SavedThreadLocal<T>`, cf. `Localize.SetLocalizer`). The thread-local
factory returns a single shared root instance, so registrations made on the main thread are visible
on every thread.

### `TypeTagRegistry` — *registers and resolves tags; knows nothing about synchronizers*

Owns three customizable things:

1. **The dictionary** — a bidirectional tag ↔ `System.Type` map, stored as copy-on-write immutable
   snapshots swapped under a mutex (lock-free reads). `Add(type, tag, replaceExisting)`,
   `TagOf(Type)`, `TypeOf(string)`. Re-adding an identical association is idempotent; a conflict
   throws unless `replaceExisting`.
2. **The tagging convention** — `virtual AttributeTagOf(MethodInfo)` / `AttributeTagOf(Delegate)` /
   `AttributeTagOf(Type synchronizerType, Type valueType)` read `[TypeTag]` from a synchronizer
   method (preferred), then from its declaring type; results are cached per instance. The
   `(synchronizerType, valueType)` overload looks for a `[TypeTag]` on a method whose return type is
   `valueType`, so one synchronizer class can serve several value types with different tags.
   **Override these to change the convention** (e.g. derive tags from type names, no attributes).
3. **The policy for non-matching tags in a stream**, pluggable by overriding:
   - `UnknownTag(tag, expectedType, field)` — a dynamic read hit a tag not in the dictionary.
     Throws `FormatException` by default; an override may return a substitute `Type` (which must
     have a registered synchronizer) or `null` to fall back to the statically-expected type.
   - `TagMismatch(expectedTag, tagInStream, expectedType, field)` — a *statically-typed* read found
     a different tag than the synchronizer's. Throws by default; if an override returns normally,
     the read proceeds with the expected synchronizer anyway.

### `SyncTypeRegistry` — *maps types to synchronizers; knows nothing about tags*

- `Add(typeof(ShapeSync<>))` scans an open generic class with a single type parameter constrained
  to `ISyncManager` for two synchronizer shapes: static **bodies** `T Sync(SM, T)` (the
  `SyncObjectFunc` shape — reads/writes fields, does *not* call `BeginSubObject`) and
  `ISyncObject<SM,T>` implementations. Each discovered tag is recorded in `TypeTagRegistry.Default`
  as a convenience, so **one call registers both halves**.
- `Add<T>(tag, openClass)` uses an explicit tag (`null` = untagged, readable only when T is the
  static type; explicit tag beats any `[TypeTag]`). `Add<T>(tag, SyncObjectFunc<ISyncManager,T>)` is
  **easy mode**: a plain delegate against `ISyncManager` — one line, but interface dispatch + a
  boxed SM per object make it the slow path.
- Internals: copy-on-write registration snapshots. Per-`(registry, SM)` dispatch tables
  (`Tables<SM>`) of type-erased `Func<SM, object?, object?>` are built lazily —
  `MakeGenericType` / `CreateDelegate` / `MakeGenericMethod` run **once per (registry, SM, T)**;
  staleness is detected by comparing the snapshot's `State` **reference identity**, so no per-call
  version counter is needed. The wrapper lambdas only cast, so **class hierarchies never box**.
  No `Reflection.Emit` anywhere → AOT-friendly (closing generics over structs may need
  `[DynamicDependency]` on NativeAOT). Late registration works and is thread-safe.

### `TypeTagAttribute` — *dumb data*

`Tag` property only; usable on a method (preferred) or a synchronizer struct/class. Deliberately
**not** intended for the business types themselves. `TypeTagRegistry` interprets it.

To run multiple synchronizers/tags for one type, swap registry *pairs*:
`using (TypeTagRegistry.SetDefault(tagsB)) using (SyncTypeRegistry.SetDefault(syncB)) {...}`, or skip
ambient state entirely with the explicit-instance overloads below.

---

## The tiers

### Static tier (the perf path)

- `sm.Sync(name, value, ShapeSync<SM>.Sync [, mode])`, or `sm.Sync(name, value, default(EllipseSync<SM>))`
  with an `ISyncObject<SM,T>` struct for fully static, JIT-specialized, inlineable dispatch. (C# has
  no partial type-argument inference, so `sm.Sync<EllipseSync<SM>>(...)` isn't expressible; a
  `default` struct instance produces identical codegen.)
- `ObjectSyncher` reads/writes the tag right after `BeginSubObject`, before the first field. **The
  tag is owned by the wrapper, not the body**, so a reader can read the tag first and choose a
  synchronizer. Tag sourcing: for struct synchronizers it's resolved once per closed generic into a
  `static readonly` field (zero per-call cost, using the ambient `TypeTagRegistry` at first use);
  for delegates, through the ambient registry's per-instance cache.
- On read, a present-but-different tag goes to `TagMismatch` (throws by default; a lenient override
  lets the read proceed). An **absent** tag (foreign JSON) is accepted.

### Dynamic tier (opt-in, per call site)

- `sm.SyncDynamic(name, value [, mode])`, `sm.SyncDynamicList(...)`, or compose `DynamicSync<SM, T>`
  (an ordinary `ISyncField`) with any list/collection helper.
- **Explicit-instance overloads** bypass the ambient services:
  `sm.SyncDynamic(name, value, synchronizers, tags)` / `new DynamicSync<SM, T>(synchronizers, tags, mode)`
  (`tags: null` still means `TypeTagRegistry.Default`). Use when different streams need different
  registrations concurrently, and to avoid dependence on thread identity — thread-local ambient
  state does not flow across `await`.
- **Write:** `value.GetType()` → dispatch table → `tags.TagOf` → write tag → body.
  **Read:** `SyncTypeTag(null)` → `tags.TypeOf(tag)`, falling back to `tags.UnknownTag(...)` → verify
  assignable to T → body → cast. An unregistered runtime type on write throws
  `NotSupportedException` (no silent base-class slicing); a tagless synchronizer written dynamically
  as a base type throws.
- **`childKey`:** `sync.Mode == SyncMode.Schema ? typeof(T) : value` — schema mode keys on the
  static type (there is no instance), the data path keys on the value for dedup.
- **Cost per object:** (a TLS read, unless explicit instances) + a dictionary hit + a delegate
  invoke + tag I/O. Reference-type hierarchies box nowhere; a value type registered dynamically
  boxes once per call — the unavoidable no-codegen cost, paid only when you *ask* for dynamic typing.

### Plain `sm.Sync(name, value)` (the default tier)

The 2-arg extension resolves through `DefaultSynchronizer<SM, T>`: primitives/tuples as before, plus
built-in defaults in `ExtraSynchronizers<SM>` — arrays (`byte[]`/`bool[]`/`char[]` keep the
SyncManager's special handling, e.g. Base64 in JSON), `List`/`IList`/`IReadOnlyList`/`HashSet`,
`Dictionary`/`IDictionary`, `KeyValuePair`, enums (numeric), `DateTime`/`TimeSpan` (ISO strings),
all composing recursively via `DefaultSyncField<SM, T>`. For user types it returns a
registry-consulting dynamic synchronizer — the process-global cache stores only **registry-agnostic**
delegates (they consult `SyncTypeRegistry.Default` on each call), so ambient swaps stay correct.

---

## Design decisions (proposal → final, and why)

1. **Two registries + attribute, not one global `SyncTypes` static.** The proposal had a single
   global class holding both maps and explicit `RegisterDynamic<T>("tag", ...)` calls. The final
   design splits tag-resolution (`TypeTagRegistry`) from synchronizer-resolution (`SyncTypeRegistry`),
   moves tags onto synchronizers via `[TypeTag]`, and makes both ambient/swappable. Result: business
   objects stay clean, the tag convention and error policy are override points, and concurrent
   streams with different registrations are expressible.

2. **`NoTypeTag` opt-*out*, not `DynamicType` opt-*in*.** The proposal's `ObjectMode.DynamicType = 16`
   would have gated tag emission behind a flag. The shipped flag is `ObjectMode.NoTypeTag = 16` with
   the opposite polarity: a tag flows automatically wherever a synchronizer has a `[TypeTag]` (both
   tiers), and `NoTypeTag` suppresses it at a call site (the same flag must be set when reading back).
   This is what makes static/dynamic interop the default rather than an opt-in.

3. **Always write the tag (when the synchronizer has one).** Even when `value.GetType() == typeof(T)`.
   JSON *could* omit it, but SyncBinary/Protobuf readers can't detect absence, so symmetry keeps all
   formats on one rule. The reader's "tag absent → static type" branch exists only for *foreign* JSON.

4. **Bodies, not field-synchronizers, are the registered unit.** Registered methods are
   `T Sync(SM, T)` bodies that the wrapper (`ObjectSyncher`/`DynamicSync`) surrounds with
   `BeginSubObject`/tag/`EndSubObject`. Uniform mental model; exotic representations (e.g.
   `Fraction`-as-string) are still served by the explicit `sm.Sync(name, val, syncFunc)` API.

5. **Unregistered runtime type on write → throw; unknown tag on read → policy (throws by default).**
   No walking up the inheritance chain on a write miss — silent base-class slicing is a
   data-corruption footgun; an exception naming the unregistered type is friendlier. Read-side
   unknown tags are recoverable via the `UnknownTag` override (substitute or fall back).

6. **Staleness via snapshot reference-identity, not a version int.** Dispatch tables cache a
   reference to the `State` they were built from and rebuild (copy-on-write) when they observe a new
   one. Cheaper than the version-compare the proposal weighed, and correct under late registration.

7. **Enum default is numeric** (Binary/Protobuf compatibility); string enums stay opt-in.

8. **Merge mode** (`IsReading && IsWriting`) is explicitly unsupported for dynamic typing.

---

## Performance & AOT

- Steady state, dynamic field: dictionary lookup + delegate call + tag I/O. Static registered field:
  static-field load + delegate call — same as today's tuple path.
- No `Reflection.Emit`, no expression compilation. `MakeGenericType`/`MakeGenericMethod` over
  **reference types** work under NativeAOT/iOS (shared canonical code); closing them over *value
  types* dynamically can fail without metadata hints — dynamic registration of structs should be
  documented as "may need `[DynamicDependency]` on AOT".
- Thread safety: copy-on-write snapshots throughout; `DefaultSynchronizer<SM,T>.Default` races are
  benign (idempotent patch); dispatch-table rebuilds race benignly (idempotent).

---

## Files

- `Core/Loyc.Essentials/SyncLib/TypeTagRegistry.cs` — the tag module (dictionary, convention,
  policies, ambient service).
- `Core/Loyc.Essentials/SyncLib/TypeTagAttribute.cs` — the attribute (data only).
- `Core/Loyc.Essentials/SyncLib/SyncTypeRegistry.cs` — synchronizer registry, dispatch tables,
  `DynamicSync<SM,T>`, `SyncDynamicExt` (incl. explicit-instance overloads).
- `Core/Loyc.Essentials/SyncLib/TypeStuff.cs` — `DefaultSynchronizer` registry hookup,
  `DefaultSyncField<SM,T>`, `ExtraSynchronizers<SM>` (collections/enums/dates).
- `Core/Loyc.Essentials/SyncLib/Impl/ObjectSyncher.cs` — static-tier tag read/write; mismatches go to
  the pluggable `TagMismatch` policy.
- `Core/Loyc.Essentials/SyncLib/ObjectMode.cs` — `NoTypeTag = 16`.
- Tests: `Core/Tests/SyncLib/SyncLibTests.Dynamic.cs` — 17 shared tests run in every backend fixture
  (round trips both directions between tiers; mismatched/unknown-tag errors *and* their
  lenient/substituting handler overrides; explicit-registry overloads; `NoTypeTag`; cyclic
  polymorphic graph with dedup; late registration; registry-pair swap; collection/date/enum defaults)
  + 4 `SyncDynamicJsonTests` (tag property naming, Newtonsoft mode, `NoTypeTag` omission, tag-absent
  fallback). Registered in `Program.cs`; net45 csprojs updated.

---

## Gotchas

- `SyncJson.Options.NewtonsoftCompatibility` defaults **true** → tag property `"$type"` (only `"\t"`
  when off).
- SyncProtobuf with a **deduplicated root**: an inner field that is the *same instance* as the root
  value becomes an unresolvable back-reference unless the root synchronizer sets `CurrentObject` —
  pre-existing dedup semantics; tests wrap values in a container.
- `SyncTypeRegistry.Add` records tags in the *ambient* `TypeTagRegistry.Default` at call time — code
  building parallel registry pairs must wrap the `Add` calls in `TypeTagRegistry.SetDefault(...)`.

---

## Possible future work

- `T = object` reading for self-describing formats: tag → registry, else `GetFieldType`-driven
  fallback to bool/long/double/string/`List<object?>`/`Dictionary<string, object?>` ("JsonValue-lite").
  On write it already works (runtime-type lookup).
- Schema modes: once the flag flows through `BeginSubObject`, query the registries for all
  registrations assignable to a dynamic field's static type and emit JSON Schema `oneOf` discriminated
  on the tag const (protobuf needs more thought — no true union of messages sharing a tag field).
- `Nullable<T>` of registered structs / nullable enums in `DefaultSynchronizer`.
