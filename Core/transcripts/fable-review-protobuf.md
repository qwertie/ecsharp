 ▐▛███▜▌   Claude Code v2.1.202
▝▜█████▛▘  Fable 5 · Claude Team
  ▘▘ ▝▝    /workspace

 ▎ Extended: Fable 5 is included in your weekly limit
 ▎ Through July 12, you can use up to 50% of your weekly usage limit on Fable 5. If you hit your limit, you can continue on Fable 5 with usage credits. Fable 5 draws down usage faster than Opus 4.8. Run /model and select Fable to use it. Learn more

❯ /clear                                                                        

❯ /model                                                                        
  ⎿  Set model to Fable 5 and saved as your default for new sessions

❯ I accidentally used Opus to add the most recent commit about protocol         
  buffers, instead of you. Review and critique the work                         

● I'll review the protobuf commit. Let me start by looking at the commit and
  relevant context.

● Bash(git show --stat e1c1e998 | head -60)
  ⎿  commit e1c1e998c3066050c7875084ca22a1f025f437f3
     Author: David Piepgrass <qwertie256@gmail.com>
     Date:   Tue Jul 7 20:50:20 2026 +0000
     … +38 lines (ctrl+o to expand)

  Recalled 1 memory (ctrl+o to expand)

● Bash(git log --oneline -3 && git status --short | head -5)
  ⎿  e1c1e998 Add Loyc.SyncLib.SyncProtobuf (Protocol Buffers backend + .pr
     oto schema)
     cd5dbaff Tweak benchmarks (eg add protobuf-net bars for Calendar demo)
     ae0c5b10 Add SyncJson.Schema

  Read 3 files (ctrl+o to expand)

❯ also use SyncBinary as a reference to compare with the protobuf               
  implementation. SyncBinary is a fairly high standard of work                  

  Searched for 4 patterns, read 7 files, listed 1 directory (ctrl+o to expand)

● Bash(ls Core/*.sln; grep -l "SyncProtobuf" Core/*.sln; echo ---; git show
      e1c1e998 -- Core/LoycCore.netstd.sln | head -30)
  ⎿  Core/LoycCore.netfx.sln
     Core/LoycCore.netstd.sln
     Core/LoycCore.netstd.sln
     … +31 lines (ctrl+o to expand)
  Read 1 file, listed 1 directory (ctrl+o to expand)

● Bash(diff <(sed 's/SyncJson/SyncXXX/g'
      Core/Loyc.SyncLib.SyncJson/Loyc.SyncLib.SyncJson.csproj) <(sed
      's/SyncProtobuf/SyncXXX/g' Core/Loyc.SyncLib.SyncProtobuf/Loyc.…)
  ⎿  32,40d31
     <     <Compile Update="SyncXXX.out.cs">
     <       <DesignTime>True</DesignTime>
     … +26 lines (ctrl+o to expand)

● Project files mirror SyncJson faithfully. Now the tests.

  Read 1 file (ctrl+o to expand)

● Bash(git show e1c1e998 -- Core/Tests/SyncLib/SyncMalformedInputTests.cs
      Core/Tests/SyncLib/SmallObjectSync.cs Core/Tests/Program.cs)
  ⎿  commit e1c1e998c3066050c7875084ca22a1f025f437f3
     Author: David Piepgrass <qwertie256@gmail.com>
     Date:   Tue Jul 7 20:50:20 2026 +0000
     … +117 lines (ctrl+o to expand)

  Searched for 1 pattern (ctrl+o to expand)

● Schema conventions match SyncJson. Now let me build and run the SyncLib test
  suite to verify the commit's tests actually pass.

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | tail -5)
  ⎿  /workspace/Core/Tests/SyncLib/SyncLibTests.Values.cs(304,55): warning
     CS8600: Converting null literal or possible null value to non-nullable
      type. [/workspace/Core/Tests/LoycCore.Tests.csproj]
     … +4 lines (ctrl+o to expand)
  ⎿  (timeout 6m 40s)

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet run --project
      Core/Tests --no-build -- 2 2>&1 | tail -30)
  ⎿  Specify which project file to use because Core/Tests contains more tha
     n one project file.

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet run --project
      Core/Tests/LoycCore.Tests.csproj --no-build -- 2 2>&1 | tail -25)
  ⎿  SyncProtobufTests.RoundTripReferenceTuples
     SyncProtobufTests.GenerativeRoundTripFuzz
     SyncProtobufTests.RoundTripIntegerScalarExtremes
     … +22 lines (ctrl+o to expand)
  ⎿  (timeout 10m)

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet run --project
      Core/Tests/LoycCore.Tests.csproj --no-build -- 2 2>&1 | grep -iE
      "fail|error|exception|…)
  ⎿  exit: 0
  ⎿  (timeout 10m)

  Searched for 2 patterns (ctrl+o to expand)

● Bash(find Core -name "WriterStateBase*" -not -path "*/obj/*" | head -3; grep
      -rn "Flush\|_idGen\|class WriterStateBase"
      Core/Loyc.Essentials/SyncLib/Impl/WriterState…)
  ⎿  Core/Loyc.Essentials/SyncLib/Impl/WriterStateBase.cs
     Core/Loyc.Essentials/SyncLib/Impl/WriterStateBase.cs:10:   public partial
      class WriterStateBase
     … +10 lines (ctrl+o to expand)


  Read 1 file, wrote 1 memory (ctrl+o to expand)

● Review of e1c1e998 (Loyc.SyncLib.SyncProtobuf)

  Verdict: solid B+. The architecture faithfully mirrors the SyncJson/SyncBinary
  family, all tests pass (I built and ran suite 2 — both SyncProtobufTests
  fixtures, the schema tests, and the malformed-input fuzzing are green), and
  the hard parts (dedup framing, auto-numbering alignment, bounds-checked
  reading) are done carefully. But it falls short of the SyncBinary standard in
  documentation accuracy, option fidelity, and a few feature gaps — and its
  biggest claim (real Protocol Buffers interop) is quietly untrue.

  What's genuinely good

  - Family conformance. Struct facade over a state class, Options shape, csproj
  structure (a clean copy of SyncJson's minus the .ecs machinery), test wiring
  in Program.cs, and the shared SyncLibTests suite reuse are all exactly right.
  The netstd.sln entry includes proper config blocks.
  - Alignment discipline. Auto field numbering advances even for omitted null
  fields on both sides (ResolveFieldId is called before the null check
  everywhere, e.g. SyncProtobuf.WriterState.cs:166,
  SyncProtobuf.ReaderState.cs:276), so reader and writer stay in sync. This is
  the kind of subtle invariant that's easy to get wrong.
  - Robust reader. Every read path is bounds-checked with FormatException,
  overflow in BeginSubObject is guarded (payloadEnd < payloadStart catches
  wraparound), and the handcrafted-evil-input tests include the
  10,000-deep-nesting case. The fuzz corpus passes without OOM.
  - Schema mode is thoughtfully engineered: cycle-breaking via InProgress,
  conflict detection by scratch re-record + FieldsEqual, name sanitization, and
  the anonymous-recursion depth guard.
  - Golden byte tests (WireFormat_IsRealProtobuf) that pin the actual wire bytes
  are exactly the right kind of test.

  The big one: the "Protocol Buffers" claim oversells

  Output is not parseable by protoc/protobuf-net using the generated schema, for
  two structural reasons:

  1. The always-present framing marker. Every sub-object/list LEN payload starts
  with a marker byte (WriterState.cs:310). A protoc parser reading a nested
  message hits 0x00 — field number 0, which is invalid. For packed scalar lists
  it decodes a spurious leading element (or misaligns fixed32/64 entirely).
  2. The root envelope. The root object is wrapped as field 1 of an implicit
  outer message (0x0A len ...), which the generated .proto doesn't describe.

  The marker was a deliberate trade (it's what makes the dedup-flag-toggle
  tolerance work, mirroring SyncBinary), but SyncBinary makes its markers
  configurable (Markers.None exists); here they're unconditional, so a "pure
  interop" mode is impossible. Worse, the XML docs make claims the code
  contradicts:

  - SyncProtobuf.cs:76-77: "Lists of packed scalars are byte-compatible with
  Protobuf's packed-repeated encoding" — false (marker byte).
  - Schema.cs:86-88: "nested messages match the wire format exactly" — false
  (marker byte).
  - SyncProtobuf.cs:82-87: describes a 2-value marker present "only when
  deduplication is requested" — contradicted by the 3-value always-present
  constants 25 lines below (SyncProtobuf.cs:112-118).
  - SyncProtobuf.cs:47: says BigInteger is "two's-complement, big-endian" —
  BigInteger.ToByteArray() is little-endian (the code comment at
  WriterState.cs:243 gets it right).

  And the obvious missing test: no cross-interop test with a real protobuf 
  implementation, even though protobuf-net is already referenced by the
  benchmarks project (previous commit). One round-trip through protobuf-net
  would have surfaced all of this. Either the interop claims should be softened
  to "protobuf wire primitives", or a marker-free compat mode should exist and
  be cross-tested.

  Dead code / option divergence

  - Options.Read.ReadNullPrimitivesAsDefault (SyncProtobuf.Options.cs:57) is 
  never consulted anywhere — I grepped; zero uses in SyncProtobuf. It's also
  defaulted to true while SyncBinary and SyncJson default false and actually
  implement it. Ship it implemented or delete it.
  - No overflow protection. SyncBinary throws OverflowException on out-of-range
  integers by default with SilentlyTruncateLargeNumbers as opt-out;
  SyncProtobuf's reader always silently truncates (unchecked((sbyte)...) etc.,
  Reader.cs:76-88) with no option. Protobuf-faithful, but the divergence from
  the sibling contract is undocumented.

  Feature gaps vs. the SyncBinary standard

  - String deduplication ignored. Writer.Sync(name, string, mode) drops mode
  (Writer.cs:94); SyncBinary honors ObjectMode.Deduplicate with #/@ markers
  (SyncBinary.WriterState.cs:486). Round-trips stay consistent, but shared
  strings are re-serialized every occurrence.
  - No streaming. SyncBinary reads incrementally from IScanner
  (AutoRead/ReadMoreBytes); SyncProtobuf's DrainScanner (ReaderState.cs:64)
  slurps the whole input up front — with a byte-at-a-time copy loop, no less. A
  consequence: the reader indexes the entire input in its constructor, so
  VerifyEof = false doesn't really permit trailing data (garbage that doesn't
  parse as fields throws during construction), unlike SyncBinary where you can
  read one object from a longer stream.
  - Error messages lack positions. SyncBinary reports "at byte N" with the
  offending byte; SyncProtobuf's Error() (ReaderState.cs:87) gives only a
  message. For a binary format, position info is the difference between
  debuggable and not.

  Smaller correctness nits

  - CheckAvailable (ReaderState.cs:113) computes pos + count in int; near-2GB
  inputs can overflow it, slipping past the check into Slice's
  ArgumentOutOfRangeException — violating the FormatException-only contract. Use
  (uint) or long arithmetic.
  - Duplicate field numbers: first wins (FindField scans forward); protobuf
  semantics are last-wins. Related: nothing stops a user assigning the same
  explicit ID to two fields — the writer emits both and the reader silently
  returns the first value for both. A debug-mode duplicate check would be cheap.
  - Field numbers are unvalidated: 0, negatives, >2^29-1, and user IDs colliding
  with TypeTagFieldNumber = 19000 (the comment claims non-collision, but it's
  convention, not enforcement) all round-trip internally but produce invalid
  protobuf.
  - WriterState.Flush() hides the base via new (WriterState.cs:384); calling
  through a WriterStateBase reference would silently flush nothing. Safe today,
  but a latent trap.

  Performance notes (acceptable for v1, worth knowing)

  - EndSubObject back-patches by inserting the length varint and shifting the
  payload (InsertVarintAt), so each byte moves once per nesting level — O(size x
  depth). Mainstream protobuf writers precompute sizes or reserve placeholder
  bytes.
  - Per-message List<FieldEntry> allocation in IndexFields; SyncBinary allocates
  nothing per object. Required for reordering, but poolable.

  Build wiring omission

  Loyc.SyncLib.SyncProtobuf.net45.csproj was created (with
  packages.config/app.config) but never added to LoycCore.netfx.sln — SyncJson's
  net45 twin is registered there at line 20. As committed, nothing builds the
  net45 project, so it's untested dead weight.

  ---
  Bottom line: internally consistent, well-tested as a SyncLib backend, and the
  engineering of the tricky invariants is genuinely good. Where it falls short
  of SyncBinary is polish and honesty of contract: a dead option, ignored string
  dedup, weaker diagnostics, no streaming, and documentation that promises
  protobuf interop the wire format can't deliver. If you want the "Protocol
  Buffers backend" label to hold, the highest-value fixes are: an interop test
  against protobuf-net, a markers-off compat mode (or corrected docs),
  implementing or removing ReadNullPrimitivesAsDefault, and registering the
  net45 project in the netfx solution.

✻ Baked for 14m 36s

❯ I certainly want the ProtocolBufferSynchronizer to produce fully valid        
  Protocol Buffer output and the schema writer must write a schema that matches 
  the output. Please fix all the problems you've found, most major first        
● This is a substantial redesign — the wire format itself has to change (drop
  framing markers and the root envelope, re-encode lists/dedup/type-tags as
  valid protobuf constructs, and make the schema describe exactly what's
  written). Before writing code I need to nail down the contracts the shared
  test suite and list machinery impose.

  Listed 1 directory (ctrl+o to expand)

● Bash(grep -n "class SyncBinaryTests\|public
      SyncBinaryTests\|_saveMode\|nonDefaultSettings"
      Core/Tests/SyncLib/SyncBinaryTests.cs | head; sed -n '1,80p'
      Core/Tests/S…)
  ⎿  3: public class SyncBinaryTests : SyncLibTests<SyncBinary.Reader, SyncB
     inary.Writer>
     6:         ObjectMode _saveMode;
     … +35 lines (ctrl+o to expand)


❯ If you see ways to improve the design, feel free to do that too               

● Good context — the user has also authorized design improvements. Now let me
  study the list machinery and shared test suite contracts that the new wire
  format must satisfy.

  Searched for 1 pattern, read 3 files, listed 1 directory (ctrl+o to expand)

● Bash(sed -n '1,120p' Core/Tests/SyncLib/SyncLibTests.Fuzz.cs)
  ⎿  using Loyc.Collections.Impl;
     using Loyc.MiniTest;
     using System;
     … +117 lines (ctrl+o to expand)

  Searched for 3 patterns (ctrl+o to expand)

● Bash(sed -n '208,320p' Core/Loyc.Essentials/SyncLib/TypeStuff.cs)
  ⎿     static class TupleSynchronizer<SyncManager> where SyncManager : ISyncM
     anager
        {
     … +109 lines (ctrl+o to expand)


● Bash(sed -n '320,360p' Core/Loyc.Essentials/SyncLib/TypeStuff.cs | grep -n -A4
      "Tuple<Item1, Item2> Sync"; grep -n "ObjectMode.Tuple, \|ObjectMode.Tuple
      |" Core/Loyc…)
  ⎿  325:                       var (begun, _, obj) =
     sync.BeginSubObject(name, value, ObjectMode.
     Tuple, 1);
     335:                       var (begun, _, obj) =
     sync.BeginSubObject(name, value, ObjectMode.
     … +13 lines (ctrl+o to expand)

● Bash(sed -n '1,60p' Core/Loyc.Essentials/SyncLib/FieldId.cs)
  ⎿  using System;
     using System.Collections.Generic;
     using System.Diagnostics;
     … +42 lines (ctrl+o to expand)

● Now I have all the contracts. Creating the task list, then implementing.
● Bash(sed -n '1,80p' Core/Loyc.Essentials/SyncLib/ObjectMode.cs; echo ===; grep
      -n "SyncList<SM" Core/Loyc.Essentials/SyncLib/SyncManagerExt.cs | head
      -5)
  ⎿  using System;
     using System.Collections.Generic;
     using System.Text;
     … +83 lines (ctrl+o to expand)

● Bash(sed -n '1,60p' Core/Loyc.Essentials/SyncLib/Impl/IListBuilder.cs; grep -n
      "public.*Pop\|public T Last\|public.*InternalArray\|public void Add"
      Core/Loyc.Essenti…)
  ⎿  using Loyc.Collections;
     using Loyc.Collections.Impl;
     using Loyc.Collections.MutableListExtensionMethods;
     … +65 lines (ctrl+o to expand)

● APIs confirmed. Writing the new WriterState now.

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.WriterState.cs)
  ⎿  Added 319 lines, removed 91 lines
      1 +using Loyc.Collections;                                         
      2  using Loyc.Collections.Impl;
      3  using Loyc.SyncLib.Impl;
      4  using System;
     ...
       6  using System.Diagnostics;
       7  using System.Numerics;
       8  using System.Runtime.CompilerServices;
       9 +using System.Runtime.Serialization;                            
      10  using System.Text;
      11  
      12  namespace Loyc.SyncLib;
      13  
      14  partial class SyncProtobuf
      15  {
      14 -  /// <summary>The mutable state behind <see cref="SyncProtobuf
         -.Writer"/>. Unlike                                             
      15 -  ///   <see cref="SyncBinary"/>, this writer keeps the whole o
         -utput in one contiguous                                        
      16 -  ///   in-memory buffer until <see cref="Flush"/> is called, b
         -ecause Protobuf messages                                       
      17 -  ///   are length-prefixed and the length is only known after 
         -the body is written (it is                                     
      18 -  ///   back-patched in place).</summary>                      
      19 -  internal class WriterState : WriterStateBase                 
      16 +  /// <summary>The mutable state behind <see cref="SyncProtobuf
         +.Writer"/>. The writer                                         
      17 +  ///   keeps the whole output in one contiguous in-memory buff
         +er until                                                       
      18 +  ///   <see cref="Flush"/> is called, because Protobuf message
         +s are length-prefixed                                          
      19 +  ///   and each length is only known after the message body ha
         +s been written (a                                              
      20 +  ///   reserved length byte is patched in place, and the body 
         +is shifted only in the                                         
      21 +  ///   rare case that a length prefix needs more than one byte
         +).</summary>                                                   
      22 +  internal class WriterState                                   
      23    {
      24      internal Options _opt;
      25 +    internal IBufferWriter<byte> _output;                      
      26 +    readonly ObjectIDGenerator _idGen = new ObjectIDGenerator()
         +; // IDs start at one                                          
      27  
      28      byte[] _data;
      29      int _pos;
      30  
      26 -    // The current object's last-used field number (for auto-nu
         -mbering). Saved/restored                                       
      27 -    // via the stack as sub-objects are entered and left.      
      31 +    // The current message's last-used field number (for auto-n
         +umbering).                                                     
      32 +    // Saved/restored via the stack as sub-messages are entered
         + and left.                                                     
      33      int _lastFieldId;
      34  
      35 +    // Object  = a message with tagged fields (used for objects
         +, tuples and the root)                                         
      36 +    // List    = a list container message whose elements are al
         +l stored in field 1                                            
      37 +    enum FrameKind : byte { Object, Tuple, List }              
      38 +                                                               
      39      struct WFrame
      40      {
      32 -      public ObjectMode Mode;                                  
      33 -      public int LenInsertPos; // where this payload's length v
         -arint will be inserted                                         
      34 -      public bool IsElement;   // true if this sub-object is a 
         -positional list element                                        
      41 +      public FrameKind Kind;                                   
      42 +      public int OuterLenPos;  // reserved length byte of this 
         +frame's outer LEN, or -1 (bare root)                           
      43 +      public int InnerLenPos;  // reserved length byte of an in
         +ner LEN (dedup `value` field or                                
      44 +                               // element wrapper `value` field
         +), or -1                                                       
      45 +      public int PackedLenPos; // List: reserved length byte of
         + the open packed container, or -1                              
      46 +      public int BodyStartPos; // position just after the last 
         +length prefix (detects an empty root)                          
      47        public int SavedLastFieldId;
      48      }
      49      InternalList<WFrame> _stack = new InternalList<WFrame>(8);
      50  
      51 +    ref WFrame Top => ref _stack.InternalArray[_stack.Count - 1
         +];                                                             
      52 +                                                               
      53      internal int Depth => _stack.Count;
      40 -    internal bool IsInsideList =>                              
      41 -      _stack.Count != 0 && (_stack.Last.Mode & (ObjectMode.List
         - | ObjectMode.Tuple)) != 0;                                    
      54 +    internal bool IsInsideList => _stack.Count != 0 && Top.Kind
         + != FrameKind.Object;                                          
      55 +    // True when writes should be encoded as list elements (tup
         +les use tagged fields instead)                                 
      56 +    bool InListFrame => _stack.Count != 0 && Top.Kind == FrameK
         +ind.List;                                                      
      57  
      43 -    public WriterState(IBufferWriter<byte> output, Options opti
         -ons) : base(output)                                            
      58 +    public WriterState(IBufferWriter<byte> output, Options opti
         +ons)                                                           
      59      {
      60 +      _output = output;                                        
      61        _opt = options;
      62        _data = new byte[System.Math.Max(16, options.Write.Initia
          lBufferSize)];
      63      }
     ...
      124        _data[_pos++] = (byte)v;
      125      }
      126  
      111 -    // Inserts a varint at position `at`, shifting existing by
          -tes [at.._pos) to the right.                                  
      112 -    void InsertVarintAt(int at, ulong v)                      
      127 +    // Reserves space for a length prefix whose value isn't kn
          +own yet, and returns its                                      
      128 +    // position for PatchLen. One byte is reserved because mos
          +t messages are under 128                                      
      129 +    // bytes; PatchLen shifts the payload only when the length
          + needs a longer varint.                                       
      130 +    int ReserveLen()                                          
      131      {
      114 -      int size = VarintSize(v);                               
      115 -      EnsureRoom(size);                                       
      116 -      int count = _pos - at;                                  
      117 -      Array.Copy(_data, at, _data, at + size, count);         
      118 -      int p = at;                                             
      132 +      WriteByte(0);                                           
      133 +      return _pos - 1;                                        
      134 +    }                                                         
      135 +                                                              
      136 +    // Sets the length prefix at `lenPos` (reserved by Reserve
          +Len) to the number of                                         
      137 +    // bytes written after it. If the varint needs more than t
          +he reserved byte, the                                         
      138 +    // payload is shifted right to make room.                 
      139 +    void PatchLen(int lenPos)                                 
      140 +    {                                                         
      141 +      int len = _pos - lenPos - 1;                            
      142 +      Debug.Assert(len >= 0);                                 
      143 +      if (len < 0x80) {                                       
      144 +        _data[lenPos] = (byte)len;                            
      145 +        return;                                               
      146 +      }                                                       
      147 +      int size = VarintSize((ulong)len);                      
      148 +      int extra = size - 1;                                   
      149 +      EnsureRoom(extra);                                      
      150 +      Array.Copy(_data, lenPos + 1, _data, lenPos + 1 + extra,
          + len);                                                        
      151 +      _pos += extra;                                          
      152 +      ulong v = (ulong)len;                                   
      153 +      int p = lenPos;                                         
      154        while (v >= 0x80) {
      155          _data[p++] = (byte)(v | 0x80);
      156          v >>= 7;
      157        }
      158        _data[p] = (byte)v;
      124 -      _pos += size;                                           
      159      }
      160  
      161      #endregion
     ...
      165      int ResolveFieldId(FieldId name)
      166      {
      167        int id = name.Id != int.MinValue ? name.Id : _lastFieldI
           d + 1;
      168 +      if ((uint)(id - 1) >= MaxUserFieldNumber || (id >= 19000
          + && id <= 19999))                                             
      169 +        throw new ArgumentException(                          
      170 +          "SyncProtobuf: '{0}' has invalid Protobuf field numb
          +er {1}. Field numbers must be in the range 1 to {2}, excluding
          + 19000-19999 (reserved by Protobuf)."                         
      171 +          .Localized(name.Name ?? "" + id, id, MaxUserFieldNum
          +ber));                                                        
      172        _lastFieldId = id;
      173        return id;
      174      }
     ...
      176      void WriteTag(int fieldNumber, WireType wt)
      177        => WriteVarint(((ulong)(uint)fieldNumber << 3) | (byte)w
           t);
      178  
      179 +    // In a list, scalar elements are stored in packed contain
          +ers (field 1, LEN).                                           
      180 +    // Opens a packed container if none is open.              
      181 +    void BeginPackedElement()                                 
      182 +    {                                                         
      183 +      ref WFrame f = ref Top;                                 
      184 +      Debug.Assert(f.Kind == FrameKind.List);                 
      185 +      if (f.PackedLenPos < 0) {                               
      186 +        WriteTag(1, WireType.Len);                            
      187 +        f.PackedLenPos = ReserveLen();                        
      188 +      }                                                       
      189 +    }                                                         
      190 +    // Closes the open packed container, if any (called before
          + writing a                                                    
      191 +    // length-delimited element and when the list ends).      
      192 +    void EndPackedContainer()                                 
      193 +    {                                                         
      194 +      ref WFrame f = ref Top;                                 
      195 +      if (f.PackedLenPos >= 0) {                              
      196 +        PatchLen(f.PackedLenPos);                             
      197 +        f.PackedLenPos = -1;                                  
      198 +      }                                                       
      199 +    }                                                         
      200 +                                                              
      201      #endregion
      202  
      203      #region Scalar field writers
     ...
      210  
      211      void WriteVarintValue(FieldId name, ulong bits)
      212      {
      153 -      if (IsInsideList) {                                     
      213 +      if (InListFrame) {                                      
      214 +        BeginPackedElement();                                 
      215          WriteVarint(bits);
      216        } else {
      217          WriteTag(ResolveFieldId(name), WireType.Varint);
     ...
      220      }
      221      internal void WriteVarintValueN(FieldId name, ulong? bits)
      222      {
      162 -      if (IsInsideList) {                                     
      163 -        if (bits == null) { WriteByte(0); }                   
      164 -        else { WriteByte(1); WriteVarint(bits.Value); }       
      223 +      if (InListFrame) {                                      
      224 +        // Nullable elements are wrapped: { 1: value } if pres
          +ent, {} if null                                               
      225 +        EndPackedContainer();                                 
      226 +        WriteTag(1, WireType.Len);                            
      227 +        int lenPos = ReserveLen();                            
      228 +        if (bits != null) {                                   
      229 +          WriteTag(1, WireType.Varint);                       
      230 +          WriteVarint(bits.Value);                            
      231 +        }                                                     
      232 +        PatchLen(lenPos);                                     
      233        } else {
      234          int id = ResolveFieldId(name);
      235          if (bits == null) return; // omit absent field
     ...
      241      internal void WriteFloatField(FieldId name, float value)
      242      {
      243        uint bits = FloatToBits(value);
      176 -      if (IsInsideList) WriteRawLE32(bits);                   
      244 +      if (InListFrame) { BeginPackedElement(); WriteRawLE32(bi
          +ts); }                                                        
      245        else { WriteTag(ResolveFieldId(name), WireType.I32); Wri
           teRawLE32(bits); }
      246      }
      247      internal void WriteFloatFieldN(FieldId name, float? value)
      248      {
      181 -      if (IsInsideList) {                                     
      182 -        if (value == null) WriteByte(0);                      
      183 -        else { WriteByte(1); WriteRawLE32(FloatToBits(value.Va
          -lue)); }                                                      
      249 +      if (InListFrame) {                                      
      250 +        EndPackedContainer();                                 
      251 +        WriteTag(1, WireType.Len);                            
      252 +        int lenPos = ReserveLen();                            
      253 +        if (value != null) {                                  
      254 +          WriteTag(1, WireType.I32);                          
      255 +          WriteRawLE32(FloatToBits(value.Value));             
      256 +        }                                                     
      257 +        PatchLen(lenPos);                                     
      258        } else {
      259          int id = ResolveFieldId(name);
      260          if (value == null) return;
      187 -        WriteTag(id, WireType.I32); WriteRawLE32(FloatToBits(v
          -alue.Value));                                                 
      261 +        WriteTag(id, WireType.I32);                           
      262 +        WriteRawLE32(FloatToBits(value.Value));               
      263        }
      264      }
      265  
      266      internal void WriteDoubleField(FieldId name, double value)
      267      {
      268        ulong bits = DoubleToBits(value);
      194 -      if (IsInsideList) WriteRawLE64(bits);                   
      269 +      if (InListFrame) { BeginPackedElement(); WriteRawLE64(bi
          +ts); }                                                        
      270        else { WriteTag(ResolveFieldId(name), WireType.I64); Wri
           teRawLE64(bits); }
      271      }
      272      internal void WriteDoubleFieldN(FieldId name, double? valu
           e)
      273      {
      199 -      if (IsInsideList) {                                     
      200 -        if (value == null) WriteByte(0);                      
      201 -        else { WriteByte(1); WriteRawLE64(DoubleToBits(value.V
          -alue)); }                                                     
      274 +      if (InListFrame) {                                      
      275 +        EndPackedContainer();                                 
      276 +        WriteTag(1, WireType.Len);                            
      277 +        int lenPos = ReserveLen();                            
      278 +        if (value != null) {                                  
      279 +          WriteTag(1, WireType.I64);                          
      280 +          WriteRawLE64(DoubleToBits(value.Value));            
      281 +        }                                                     
      282 +        PatchLen(lenPos);                                     
      283        } else {
      284          int id = ResolveFieldId(name);
      285          if (value == null) return;
      205 -        WriteTag(id, WireType.I64); WriteRawLE64(DoubleToBits(
          -value.Value));                                                
      286 +        WriteTag(id, WireType.I64);                           
      287 +        WriteRawLE64(DoubleToBits(value.Value));              
      288        }
      289      }
      290  
      209 -    // Length-delimited value from materialized bytes (string/
          -decimal/BigInteger).                                          
      210 -    void WriteLenValue(FieldId name, ReadOnlySpan<byte> bytes,
          - bool isNull)                                                 
      291 +    // Non-nullable length-delimited value (decimal, BigIntege
          +r): a plain LEN field,                                        
      292 +    // or a direct LEN element (`repeated bytes`) inside a lis
          +t.                                                            
      293 +    void WriteLenValue(FieldId name, ReadOnlySpan<byte> bytes)
      294      {
      212 -      if (IsInsideList) {                                     
      213 -        if (isNull) WriteVarint(0);                           
      214 -        else { WriteVarint((ulong)(bytes.Length + 1)); WriteRa
          -wBytes(bytes); }                                              
      295 +      if (InListFrame) {                                      
      296 +        EndPackedContainer();                                 
      297 +        WriteTag(1, WireType.Len);                            
      298        } else {
      299 +        WriteTag(ResolveFieldId(name), WireType.Len);         
      300 +      }                                                       
      301 +      WriteVarint((ulong)bytes.Length);                       
      302 +      WriteRawBytes(bytes);                                   
      303 +    }                                                         
      304 +    // Nullable length-delimited value (string, decimal?, BigI
          +nteger?): omitted when                                        
      305 +    // null in a message; wrapped as { 1: value } / {} (null) 
          +inside a list, so that                                        
      306 +    // null and empty values remain distinguishable.          
      307 +    void WriteLenValueN(FieldId name, ReadOnlySpan<byte> bytes
          +, bool isNull)                                                
      308 +    {                                                         
      309 +      if (InListFrame) {                                      
      310 +        EndPackedContainer();                                 
      311 +        WriteTag(1, WireType.Len);                            
      312 +        int lenPos = ReserveLen();                            
      313 +        if (!isNull) {                                        
      314 +          WriteTag(1, WireType.Len);                          
      315 +          WriteVarint((ulong)bytes.Length);                   
      316 +          WriteRawBytes(bytes);                               
      317 +        }                                                     
      318 +        PatchLen(lenPos);                                     
      319 +      } else {                                                
      320          int id = ResolveFieldId(name);
      321          if (isNull) return; // omit absent field
      322          WriteTag(id, WireType.Len);
     ...
      324          WriteRawBytes(bytes);
      325        }
      326      }
      327 +    // Deduplicated length-delimited value (string or byte[] w
          +ith                                                           
      328 +    // ObjectMode.Deduplicate): a wrapper message { 1: id, 2: 
          +value } on the first                                          
      329 +    // occurrence, or { 1: id } for a back-reference.         
      330 +    void WriteDedupLenValue(FieldId name, object obj, ReadOnly
          +Span<byte> bytes)                                             
      331 +    {                                                         
      332 +      long id = _idGen.GetId(obj, out bool firstTime);        
      333 +      if (InListFrame) {                                      
      334 +        EndPackedContainer();                                 
      335 +        WriteTag(1, WireType.Len);                            
      336 +      } else {                                                
      337 +        WriteTag(ResolveFieldId(name), WireType.Len);         
      338 +      }                                                       
      339 +      int lenPos = ReserveLen();                              
      340 +      WriteTag(1, WireType.Varint);                           
      341 +      WriteVarint((ulong)id);                                 
      342 +      if (firstTime) {                                        
      343 +        WriteTag(2, WireType.Len);                            
      344 +        WriteVarint((ulong)bytes.Length);                     
      345 +        WriteRawBytes(bytes);                                 
      346 +      }                                                       
      347 +      PatchLen(lenPos);                                       
      348 +    }                                                         
      349  
      224 -    internal void WriteStringField(FieldId name, string? value
          -)                                                             
      350 +    internal void WriteStringField(FieldId name, string? value
          +, ObjectMode mode)                                            
      351      {
      226 -      if (value == null) { WriteLenValue(name, default, isNull
          -: true); return; }                                            
      352 +      if (value == null) { WriteLenValueN(name, default, isNul
          +l: true); return; }                                           
      353        int byteCount = Utf8ByteCount(value);
      228 -      // Materialize into a temporary span. We could write in 
          -place, but null/length                                        
      229 -      // framing makes a temp buffer simpler and strings are u
          -sually short.                                                 
      230 -      byte[] tmp = byteCount <= 256 ? _scratch : new byte[byte
          -Count];                                                       
      354 +      byte[] tmp = byteCount <= _scratch.Length ? _scratch : n
          +ew byte[byteCount];                                           
      355        int written = Utf8GetBytes(value, tmp);
      356        Debug.Assert(written == byteCount);
      233 -      WriteLenValue(name, tmp.AsSpan(0, byteCount), isNull: fa
          -lse);                                                         
      357 +      if ((mode & ObjectMode.Deduplicate) != 0)               
      358 +        WriteDedupLenValue(name, value, tmp.AsSpan(0, byteCoun
          +t));                                                          
      359 +      else                                                    
      360 +        WriteLenValueN(name, tmp.AsSpan(0, byteCount), isNull:
          + false);                                                      
      361      }
      362      readonly byte[] _scratch = new byte[256];
      363  
      364      internal void WriteDecimalField(FieldId name, decimal valu
           e)
      238 -      => WriteLenValue(name, DecimalToBytes(value), isNull: fa
          -lse);                                                         
      365 +      => WriteLenValue(name, DecimalToBytes(value));          
      366      internal void WriteDecimalFieldN(FieldId name, decimal? va
           lue)
      240 -      => WriteLenValue(name, value == null ? default : Decimal
          -ToBytes(value.Value), value == null);                         
      367 +      => WriteLenValueN(name, value == null ? default : Decima
          +lToBytes(value.Value), value == null);                        
      368  
      369 +    // BigInteger: little-endian two's complement (BigInteger.
          +ToByteArray's format)                                         
      370      internal void WriteBigIntField(FieldId name, BigInteger va
           lue)
      243 -      => WriteLenValue(name, value.ToByteArray(), isNull: fals
          -e); // little-endian two's complement                         
      371 +      => WriteLenValue(name, value.ToByteArray());            
      372      internal void WriteBigIntFieldN(FieldId name, BigInteger? 
           value)
      245 -      => WriteLenValue(name, value == null ? default : value.V
          -alue.ToByteArray(), value == null);                           
      373 +      => WriteLenValueN(name, value == null ? default : value.
          +Value.ToByteArray(), value == null);                          
      374  
      375 +    // byte[] and other byte lists are stored as a Protobuf `b
          +ytes` value rather than                                       
      376 +    // as a list container, matching how Protobuf schemas norm
          +ally model binary data.                                       
      377 +    internal void WriteByteListField<Scanner>(FieldId name, Sc
          +anner scanner, object? list, ObjectMode mode)                 
      378 +      where Scanner : IScanner<byte>                          
      379 +    {                                                         
      380 +      bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
          +.Deduplicate)) != ObjectMode.NotNull;                         
      381 +      if (list == null && nullable) { WriteLenValueN(name, def
          +ault, isNull: true); return; }                                
      382 +                                                              
      383 +      bool dedup = (mode & ObjectMode.Deduplicate) != 0;      
      384 +      if (dedup) {                                            
      385 +        long id = _idGen.GetId(list!, out bool firstTime);    
      386 +        if (InListFrame) { EndPackedContainer(); WriteTag(1, W
          +ireType.Len); }                                               
      387 +        else WriteTag(ResolveFieldId(name), WireType.Len);    
      388 +        int lenPos = ReserveLen();                            
      389 +        WriteTag(1, WireType.Varint);                         
      390 +        WriteVarint((ulong)id);                               
      391 +        if (firstTime) {                                      
      392 +          WriteTag(2, WireType.Len);                          
      393 +          int valLenPos = ReserveLen();                       
      394 +          CopyScanner(scanner);                               
      395 +          PatchLen(valLenPos);                                
      396 +        }                                                     
      397 +        PatchLen(lenPos);                                     
      398 +      } else if (InListFrame) {                               
      399 +        // byte[] as a list element: wrapped like other nullab
          +le elements                                                   
      400 +        EndPackedContainer();                                 
      401 +        WriteTag(1, WireType.Len);                            
      402 +        int lenPos = ReserveLen();                            
      403 +        WriteTag(1, WireType.Len);                            
      404 +        int valLenPos = ReserveLen();                         
      405 +        CopyScanner(scanner);                                 
      406 +        PatchLen(valLenPos);                                  
      407 +        PatchLen(lenPos);                                     
      408 +      } else {                                                
      409 +        WriteTag(ResolveFieldId(name), WireType.Len);         
      410 +        int lenPos = ReserveLen();                            
      411 +        CopyScanner(scanner);                                 
      412 +        PatchLen(lenPos);                                     
      413 +      }                                                       
      414 +    }                                                         
      415 +    void CopyScanner<Scanner>(Scanner scanner) where Scanner :
          + IScanner<byte>                                               
      416 +    {                                                         
      417 +      Memory<byte> scratch = default;                         
      418 +      ReadOnlyMemory<byte> chunk;                             
      419 +      int skip = 0;                                           
      420 +      while ((chunk = scanner.Read(skip, -1, ref scratch)).Len
          +gth != 0) {                                                   
      421 +        WriteRawBytes(chunk.Span);                            
      422 +        skip = chunk.Length;                                  
      423 +      }                                                       
      424 +    }                                                         
      425 +                                                              
      426      #endregion
      427  
      428      #region Type tag
      429  
      430      internal void WriteTypeTag(string? tag)
      431      {
      253 -      // The type tag is stored as a reserved field number, be
          -fore other fields.                                            
      432 +      // The type tag is stored as a string field with a reser
          +ved field number.                                             
      433        if (tag == null) return;
      255 -      Debug.Assert(!IsInsideList);                            
      434 +      if (InListFrame)                                        
      435 +        throw new InvalidOperationException("SyncTypeTag canno
          +t be used inside a list.");                                   
      436        int byteCount = Utf8ByteCount(tag);
      257 -      byte[] tmp = byteCount <= 256 ? _scratch : new byte[byte
          -Count];                                                       
      437 +      byte[] tmp = byteCount <= _scratch.Length ? _scratch : n
          +ew byte[byteCount];                                           
      438        Utf8GetBytes(tag, tmp);
      439        WriteTag(TypeTagFieldNumber, WireType.Len);
      440        WriteVarint((ulong)byteCount);
     ...
      447  
      448      public (bool Begun, int Length, object? Object) BeginSubOb
           ject(FieldId name, object? childKey, ObjectMode mode, int list
           Length)
      449      {
      270 -      bool insideList = IsInsideList;                         
      450 +      FrameKind kind = (mode & ObjectMode.Tuple) == ObjectMode
          +.Tuple ? FrameKind.Tuple                                      
      451 +        : (mode & ObjectMode.List) != 0 ? FrameKind.List : Fra
          +meKind.Object;                                                
      452 +      bool dedup = (mode & ObjectMode.Deduplicate) != 0;      
      453        bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
           .Deduplicate)) != ObjectMode.NotNull;
      454        bool isNull = childKey == null && nullable;
      455  
      274 -      // Field number must be consumed for every field (even n
          -ull/back-ref) so the                                          
      275 -      // reader and writer stay aligned.                      
      276 -      int fieldId = insideList ? 0 : ResolveFieldId(name);    
      456 +      if (_stack.Count == 0)                                  
      457 +        return BeginRoot(childKey, kind, dedup, isNull);      
      458  
      459 +      bool inList = InListFrame;                              
      460 +      // The field number must be consumed for every field (ev
          +en a null one) so the                                         
      461 +      // reader and writer stay aligned on auto-assigned numbe
          +rs.                                                           
      462 +      int fieldNum = inList ? 0 : ResolveFieldId(name);       
      463 +                                                              
      464        if (isNull) {
      279 -        if (insideList) WriteVarint(0); // null element marker
      465 +        if (inList) {                                         
      466 +          // A null element is an empty wrapper message       
      467 +          EndPackedContainer();                               
      468 +          WriteTag(1, WireType.Len);                          
      469 +          WriteByte(0);                                       
      470 +        }                                                     
      471          return (false, 0, null);
      472        }
      473  
      283 -      bool dedup = (mode & ObjectMode.Deduplicate) != 0;      
      474        long dedupId = 0;
      475        bool firstTime = true;
      286 -      if (dedup) {                                            
      287 -        Debug.Assert(childKey != null);                       
      476 +      if (dedup)                                              
      477          dedupId = _idGen.GetId(childKey!, out firstTime);
      289 -      }                                                       
      478  
      291 -      // Every sub-object/list payload starts with a one-byte 
          -framing marker so the                                         
      292 -      // reader can detect deduplication regardless of its own
          - ObjectMode.                                                  
      293 -      if (!insideList)                                        
      294 -        WriteTag(fieldId, WireType.Len);                      
      295 -      int lenInsertPos = _pos;                                
      479 +      if (inList) { EndPackedContainer(); WriteTag(1, WireType
          +.Len); }                                                      
      480 +      else WriteTag(fieldNum, WireType.Len);                  
      481 +      int outerLenPos = ReserveLen();                         
      482 +      int innerLenPos = -1;                                   
      483  
      297 -      if (dedup && !firstTime) {                              
      298 -        // Back-reference: payload is [DedupBackRef][varint id
          -], no body.                                                   
      299 -        WriteByte(DedupBackRef);                              
      484 +      if (dedup) {                                            
      485 +        // Dedup wrapper: { 1: id, 2: body } on first occurren
          +ce, { 1: id } after                                           
      486 +        WriteTag(1, WireType.Varint);                         
      487          WriteVarint((ulong)dedupId);
      301 -        int payloadLen = _pos - lenInsertPos;                 
      302 -        InsertVarintAt(lenInsertPos, (ulong)(insideList ? payl
          -oadLen + 1 : payloadLen));                                    
      303 -        return (false, 0, childKey);                          
      488 +        if (!firstTime) {                                     
      489 +          PatchLen(outerLenPos);                              
      490 +          return (false, 0, childKey);                        
      491 +        }                                                     
      492 +        WriteTag(2, WireType.Len);                            
      493 +        innerLenPos = ReserveLen();                           
      494 +      } else if (inList && nullable) {                        
      495 +        // Nullable element wrapper: { 1: body }              
      496 +        WriteTag(1, WireType.Len);                            
      497 +        innerLenPos = ReserveLen();                           
      498        }
      499  
      500 +      _stack.Add(new WFrame {                                 
      501 +        Kind = kind,                                          
      502 +        OuterLenPos = outerLenPos,                            
      503 +        InnerLenPos = innerLenPos,                            
      504 +        PackedLenPos = -1,                                    
      505 +        BodyStartPos = _pos,                                  
      506 +        SavedLastFieldId = _lastFieldId,                      
      507 +      });                                                     
      508 +      _lastFieldId = 0;                                       
      509 +      return (true, kind == FrameKind.Object ? 1 : listLength,
          + childKey);                                                   
      510 +    }                                                         
      511 +                                                              
      512 +    // The root value is written as a bare message body (as a 
          +.proto-described file                                         
      513 +    // or network message would be), so there is no tag or len
          +gth prefix.                                                   
      514 +    (bool Begun, int Length, object? Object) BeginRoot(object?
          + childKey, FrameKind kind, bool dedup, bool isNull)           
      515 +    {                                                         
      516 +      if (kind != FrameKind.Object)                           
      517 +        throw new NotSupportedException(                      
      518 +          "SyncProtobuf: the root value must be an object (a P
          +rotobuf message), not a list or tuple. " +                    
      519 +          "Wrap the list in an object, or use a synchronizer w
          +hose root is an object.");                                    
      520 +      if (isNull)                                             
      521 +        return (false, 0, null); // a null root produces zero 
          +bytes                                                         
      522 +                                                              
      523 +      int innerLenPos = -1;                                   
      524        if (dedup) {
      307 -        WriteByte(DedupFirst);                                
      525 +        // The root of a deduplicated graph is always a first 
          +occurrence                                                    
      526 +        long dedupId = _idGen.GetId(childKey!, out bool _);   
      527 +        WriteTag(1, WireType.Varint);                         
      528          WriteVarint((ulong)dedupId);
      309 -      } else {                                                
      310 -        WriteByte(DedupNone);                                 
      529 +        WriteTag(2, WireType.Len);                            
      530 +        innerLenPos = ReserveLen();                           
      531        }
      312 -                                                              
      532        _stack.Add(new WFrame {
      314 -        Mode = mode,                                          
      315 -        LenInsertPos = lenInsertPos,                          
      316 -        IsElement = insideList,                               
      533 +        Kind = FrameKind.Object,                              
      534 +        OuterLenPos = -1,                                     
      535 +        InnerLenPos = innerLenPos,                            
      536 +        PackedLenPos = -1,                                    
      537 +        BodyStartPos = _pos,                                  
      538          SavedLastFieldId = _lastFieldId,
      539        });
      540        _lastFieldId = 0;
      320 -                                                              
      321 -      ObjectMode kind = mode & (ObjectMode.List | ObjectMode.T
          -uple);                                                        
      322 -      return (true, kind == 0 ? 1 : listLength, childKey);    
      541 +      return (true, 1, childKey);                             
      542      }
      543  
      544      public void EndSubObject()
      545      {
      327 -      var frame = _stack.Last;                                
      546 +      ref WFrame f = ref Top;                                 
      547 +      if (f.PackedLenPos >= 0)                                
      548 +        PatchLen(f.PackedLenPos);                             
      549 +      if (f.OuterLenPos < 0 && f.InnerLenPos < 0 && _pos == f.
          +BodyStartPos) {                                               
      550 +        // A bare root whose body is empty would be indistingu
          +ishable from a null                                           
      551 +        // root (zero bytes), so mark it as present with a res
          +erved boolean field.                                          
      552 +        WriteTag(PresentFieldNumber, WireType.Varint);        
      553 +        WriteVarint(1);                                       
      554 +      }                                                       
      555 +      if (f.InnerLenPos >= 0)                                 
      556 +        PatchLen(f.InnerLenPos);                              
      557 +      if (f.OuterLenPos >= 0)                                 
      558 +        PatchLen(f.OuterLenPos);                              
      559 +      int saved = f.SavedLastFieldId;                         
      560        _stack.Pop();
      329 -      _lastFieldId = frame.SavedLastFieldId;                  
      330 -                                                              
      331 -      int payloadLen = _pos - frame.LenInsertPos;             
      332 -      ulong lenValue = frame.IsElement ? (ulong)(payloadLen + 
          -1) : (ulong)payloadLen;                                       
      333 -      InsertVarintAt(frame.LenInsertPos, lenValue);           
      561 +      _lastFieldId = saved;                                   
      562      }
      563  
      564      #endregion
     ...
      609  
      610      #endregion
      611  
      384 -    public new IBufferWriter<byte> Flush()                    
      612 +    public IBufferWriter<byte> Flush()                        
      613      {
      614        if (_pos > 0) {
      615          var span = _output.GetSpan(_pos);

● API Error: Server error mid-response. The response above may be 
  incomplete.

✻ Cogitated for 28m 54s

❯ continue                                                                      

● Now the ReaderState rewrite.

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)
  ⎿  Added 437 lines, removed 207 lines
       9  
      10  partial class SyncProtobuf
      11  {
      12 +  /// <summary>The mutable state behind <see cref="SyncProtobuf
         +.Reader"/>. The whole                                          
      13 +  ///   input is kept in memory (Protobuf messages are length-p
         +refixed, and SyncLib                                           
      14 +  ///   supports reading fields in any order, so random access 
         +is required). Every read                                       
      15 +  ///   is bounds-checked; malformed input causes <see cref="Fo
         +rmatException"/>.</summary>                                    
      16    internal class ReaderState
      17    {
      14 -    const string BadFormat = "Invalid Protobuf data";          
      15 -                                                               
      18      readonly Options _opt;
      19      readonly ReadOnlyMemory<byte> _mem;
      20      Dictionary<long, object>? _objects;
      21  
      22      int _lastFieldId;
      23      readonly List<RFrame> _stack = new List<RFrame>(8);
      22 -    RFrame _top;                                               
      24 +    RFrame? _top;                                              
      25  
      24 -    enum ObjType : byte { Normal = ObjectMode.Normal, List = Ob
         -jectMode.List, Tuple = ObjectMode.Tuple }                      
      26 +    enum FrameKind : byte { Object, Tuple, List }              
      27  
      28      struct FieldEntry
      29      {
     ...
      34  
      35      class RFrame
      36      {
      35 -      public ObjType Type;                                     
      36 -      public ObjectMode Mode;                                  
      37 +      public FrameKind Kind;                                   
      38        public int SavedLastFieldId;
      38 -      // Normal object:                                        
      39 +      // Object/Tuple: the message's fields, indexed for reorde
         +ring support                                                   
      40        public List<FieldEntry>? Fields;
      41        public int Cursor;
      41 -      // List/Tuple:                                           
      42 -      public int ListPos, ListEnd;                             
      42 +      // List: the entries of field 1 within the list container
         + message                                                       
      43 +      public List<FieldEntry>? Entries;                        
      44 +      public int ElemIdx;                // next unconsumed ent
         +ry                                                             
      45 +      public int PackedPos, PackedEnd;   // current packed cont
         +ainer (Pos >= End: none open)                                  
      46        // Deduplication:
      47        public long Id;
      48        public bool HasId;
     ...
      52      {
      53        _mem = input;
      54        _opt = options;
      52 -      // The whole input is treated as a message body; index it
         -s top-level fields.                                            
      53 -      _top = new RFrame {                                      
      54 -        Type = ObjType.Normal,                                 
      55 -        Mode = ObjectMode.Normal,                              
      56 -        Fields = IndexFields(0, input.Length),                 
      57 -      };                                                       
      58 -      _stack.Add(_top);                                        
      55      }
      56  
      57      public ReaderState(IScanner<byte> scanner, Options options)
     ...
      59  
      60      static ReadOnlyMemory<byte> DrainScanner(IScanner<byte> sca
          nner)
      61      {
      66 -      var buf = new Loyc.Collections.Impl.InternalList<byte>(25
         -6);                                                            
      62 +      byte[] buf = new byte[256];                              
      63 +      int count = 0;                                           
      64        Memory<byte> scratch = default;
      65        ReadOnlyMemory<byte> chunk;
      66        int skip = 0;
      67        while ((chunk = scanner.Read(skip, -1, ref scratch)).Leng
          th != 0) {
      71 -        var span = chunk.Span;                                 
      72 -        for (int i = 0; i < span.Length; i++)                  
      73 -          buf.Add(span[i]);                                    
      68 +        if (count + chunk.Length > buf.Length)                 
      69 +          Array.Resize(ref buf, System.Math.Max(buf.Length * 2,
         + count + chunk.Length));                                       
      70 +        chunk.Span.CopyTo(buf.AsSpan(count));                  
      71 +        count += chunk.Length;                                 
      72          skip = chunk.Length;
      73        }
      76 -      return buf.InternalArray.AsMemory(0, buf.Count);         
      74 +      return buf.AsMemory(0, count);                           
      75      }
      76  
      79 -    internal int Depth => _stack.Count - 1;                    
      80 -    internal bool IsInsideList => _top.Type != ObjType.Normal; 
      81 -    internal bool? ReachedEndOfList => _top.Type != ObjType.Nor
         -mal ? _top.ListPos >= _top.ListEnd : (bool?)null;              
      82 -    internal int? MinimumListLength => _top.Type != ObjType.Nor
         -mal ? 0 : (int?)null;                                          
      77 +    internal int Depth => _stack.Count;                        
      78 +    internal bool IsInsideList => _top != null && _top.Kind != 
         +FrameKind.Object;                                              
      79 +    bool InListFrame => _top != null && _top.Kind == FrameKind.
         +List;                                                          
      80  
      81 +    internal bool? ReachedEndOfList {                          
      82 +      get {                                                    
      83 +        var top = _top;                                        
      84 +        if (top == null || top.Kind != FrameKind.List)         
      85 +          return null; // unknown for objects; null for tuples 
         +(length not stored)                                            
      86 +        return top.PackedPos >= top.PackedEnd && top.ElemIdx >=
         + top.Entries!.Count;                                           
      87 +      }                                                        
      88 +    }                                                          
      89 +    internal int? MinimumListLength => InListFrame ? 0 : (int?)
         +null;                                                          
      90 +                                                               
      91      #region Low-level decoding
      92  
      93      [DebuggerHidden]
      87 -    Exception Error(string msg) => new FormatException("{0} ({1
         -})".Localized(BadFormat, msg));                                
      94 +    Exception Error(int position, string msg) =>               
      95 +      new FormatException("Invalid Protobuf data at byte {0}: {
         +1}".Localized(position, msg));                                 
      96  
      97      ulong ReadVarint(ReadOnlySpan<byte> span, ref int pos)
      98      {
     ...
      100        int shift = 0;
      101        while (true) {
      102          if ((uint)pos >= (uint)span.Length)
       95 -          throw Error("unexpected end of data");              
      103 +          throw Error(pos, "unexpected end of data");         
      104          byte b = span[pos++];
      105          result |= (ulong)(b & 0x7F) << shift;
      106          if ((b & 0x80) == 0)
      107            return result;
      108          shift += 7;
      109          if (shift > 63)
      102 -          throw Error("varint is too long");                  
      110 +          throw Error(pos, "varint is too long");             
      111        }
      112      }
      113  
      106 -    byte ReadByteChecked(ReadOnlySpan<byte> span, ref int pos)
      107 -    {                                                         
      108 -      if ((uint)pos >= (uint)span.Length)                     
      109 -        throw Error("unexpected end of data");                
      110 -      return span[pos++];                                     
      111 -    }                                                         
      112 -                                                              
      114      void CheckAvailable(ReadOnlySpan<byte> span, int pos, int 
           count)
      115      {
      115 -      if (pos < 0 || count < 0 || pos + count > span.Length)  
      116 -        throw Error("value extends past end of data");        
      116 +      if (pos < 0 || count < 0 || (long)pos + count > span.Len
          +gth)                                                          
      117 +        throw Error(pos, "value extends past end of data");   
      118      }
      119  
      120      uint ReadLE32(ReadOnlySpan<byte> span, ref int pos)
     ...
      131        return lo | (hi << 32);
      132      }
      133  
      133 -    // Reads a length-prefixed byte range, validating the leng
          -th against the buffer.                                        
      134 -    ReadOnlySpan<byte> ReadLenPrefixed(ReadOnlySpan<byte> span
          -, ref int pos)                                                
      134 +    // Reads a length prefix and returns the [start, end) rang
          +e of the payload,                                             
      135 +    // validating it against the buffer and the MaxPayloadSize
          + option.                                                      
      136 +    (int Start, int End) ReadLenPayload(ReadOnlySpan<byte> spa
          +n, int pos)                                                   
      137      {
      138        ulong len = ReadVarint(span, ref pos);
      139        if (len > (ulong)_opt.MaxPayloadSize)
      138 -        throw Error("length-delimited payload is too large"); 
      140 +        throw Error(pos, "length-delimited payload is too larg
          +e");                                                          
      141        CheckAvailable(span, pos, (int)len);
      140 -      var result = span.Slice(pos, (int)len);                 
      141 -      pos += (int)len;                                        
      142 -      return result;                                          
      142 +      return (pos, pos + (int)len);                           
      143      }
      144  
      145      // Skips a field's value given its wire type, returning th
           e position after it.
     ...
      150          case WireType.I32: return pos + 4;
      151          case WireType.I64: return pos + 8;
      152          case WireType.Len:
      153 +          int lenPos = pos;                                   
      154            ulong len = ReadVarint(span, ref pos);
      155            if (len > (ulong)_opt.MaxPayloadSize)
      155 -            throw Error("length-delimited payload is too large
          -");                                                           
      156 -          return pos + (int)len;                              
      156 +            throw Error(lenPos, "length-delimited payload is t
          +oo large");                                                   
      157 +          long end = pos + (long)len;                         
      158 +          if (end > int.MaxValue)                             
      159 +            throw Error(lenPos, "length-delimited payload is t
          +oo large");                                                   
      160 +          return (int)end;                                    
      161          default:
      158 -          throw Error("unsupported wire type " + (int)wire);  
      162 +          throw Error(pos, "unsupported wire type " + (int)wir
          +e);                                                           
      163        }
      164      }
      165  
     ...
      167      {
      168        var span = _mem.Span;
      169        if (end > span.Length)
      166 -        throw Error("payload extends past end of data");      
      170 +        throw Error(start, "payload extends past end of data")
          +;                                                             
      171        var list = new List<FieldEntry>();
      172        int pos = start;
      173        while (pos < end) {
      174          ulong tag = ReadVarint(span, ref pos);
      175          int num = (int)(tag >> 3);
      176 +        if (num == 0 || tag > ((ulong)MaxFieldNumber << 3 | 7)
          +)                                                             
      177 +          throw Error(pos, "invalid field number");           
      178          var wire = (WireType)(byte)(tag & 7);
      179          int valueStart = pos;
      180          pos = SkipValue(span, pos, wire);
      181          if (pos > end)
      176 -          throw Error("field value extends past end of message
          -");                                                           
      182 +          throw Error(valueStart, "field value extends past en
          +d of message");                                               
      183          list.Add(new FieldEntry { Num = num, Wire = wire, Valu
           eStart = valueStart });
      184        }
      185        return list;
      186      }
      187  
      188 +    // Collects the entries of field 1 in a list container mes
          +sage [start, end).                                            
      189 +    // Other field numbers are permitted and ignored (like unk
          +nown fields).                                                 
      190 +    List<FieldEntry> IndexListEntries(int start, int end)     
      191 +    {                                                         
      192 +      var all = IndexFields(start, end);                      
      193 +      var entries = new List<FieldEntry>(all.Count);          
      194 +      foreach (var fe in all)                                 
      195 +        if (fe.Num == 1)                                      
      196 +          entries.Add(fe);                                    
      197 +      return entries;                                         
      198 +    }                                                         
      199 +                                                              
      200      #endregion
      201  
      202      #region Field lookup and numbering
      203  
      204 +    // If no sub-object was ever begun, the whole input acts a
          +s one message body                                            
      205 +    // (this supports reading fields directly from a NewReader
          + without a root object).                                      
      206 +    RFrame TopObject()                                        
      207 +    {                                                         
      208 +      if (_top == null) {                                     
      209 +        var root = new RFrame {                               
      210 +          Kind = FrameKind.Object,                            
      211 +          Fields = IndexFields(0, _mem.Length),               
      212 +        };                                                    
      213 +        Push(root);                                           
      214 +      }                                                       
      215 +      return _top!;                                           
      216 +    }                                                         
      217 +                                                              
      218      int ResolveFieldId(FieldId name)
      219      {
      220        int id = name.Id != int.MinValue ? name.Id : _lastFieldI
           d + 1;
     ...
      231  
      232      bool FindField(int id, out int valueStart, out WireType wi
           re)
      233      {
      202 -      var fields = _top.Fields!;                              
      234 +      var fields = TopObject().Fields!;                       
      235        // Fast path: fields are usually read in the order they 
           were written.
      204 -      int c = _top.Cursor;                                    
      236 +      int c = _top!.Cursor;                                   
      237        if (c < fields.Count && fields[c].Num == id) {
      238          var fe0 = fields[c];
      239          _top.Cursor = c + 1;
      240          valueStart = fe0.ValueStart; wire = fe0.Wire;
      241          return true;
      242        }
      211 -      for (int i = 0; i < fields.Count; i++) {                
      243 +      // Slow path: search backward so that, per the Protobuf 
          +specification, the                                            
      244 +      // last occurrence of a duplicated (non-repeated) field 
          +wins.                                                         
      245 +      for (int i = fields.Count - 1; i >= 0; i--) {           
      246          if (fields[i].Num == id) {
      247            var fe = fields[i];
      248            _top.Cursor = i + 1;
     ...
      256  
      257      #endregion
      258  
      259 +    #region List-element access                               
      260 +                                                              
      261 +    // Returns the payload bounds of the next length-delimited
          + list element.                                                
      262 +    (int Start, int End) NextElemPayload(ReadOnlySpan<byte> sp
          +an)                                                           
      263 +    {                                                         
      264 +      var top = _top!;                                        
      265 +      Debug.Assert(top.Kind == FrameKind.List);               
      266 +      if (top.PackedPos < top.PackedEnd)                      
      267 +        throw Error(top.PackedPos, "expected a packed scalar e
          +lement (lists cannot mix element kinds)");                    
      268 +      if (top.ElemIdx >= top.Entries!.Count)                  
      269 +        throw Error(_mem.Length, "read past the end of a list"
          +);                                                            
      270 +      var entry = top.Entries[top.ElemIdx++];                 
      271 +      if (entry.Wire != WireType.Len)                         
      272 +        throw Error(entry.ValueStart, "expected a length-delim
          +ited list element");                                          
      273 +      return ReadLenPayload(span, entry.ValueStart);          
      274 +    }                                                         
      275 +                                                              
      276 +    // Positions the packed-container cursor at the next packe
          +d scalar, opening the                                         
      277 +    // next container (field-1 entry) as needed. Returns false
          + at the end of the list.                                      
      278 +    bool OpenPackedScalar(ReadOnlySpan<byte> span)            
      279 +    {                                                         
      280 +      var top = _top!;                                        
      281 +      Debug.Assert(top.Kind == FrameKind.List);               
      282 +      while (top.PackedPos >= top.PackedEnd) {                
      283 +        if (top.ElemIdx >= top.Entries!.Count)                
      284 +          return false;                                       
      285 +        var entry = top.Entries[top.ElemIdx++];               
      286 +        if (entry.Wire != WireType.Len)                       
      287 +          throw Error(entry.ValueStart, "expected a packed sca
          +lar container");                                              
      288 +        (top.PackedPos, top.PackedEnd) = ReadLenPayload(span, 
          +entry.ValueStart);                                            
      289 +      }                                                       
      290 +      return true;                                            
      291 +    }                                                         
      292 +                                                              
      293 +    // Finds field `num` in a small wrapper message [start, en
          +d) without allocating.                                        
      294 +    bool FindWrapperField(ReadOnlySpan<byte> span, int start, 
          +int end, int num, out int valueStart, out WireType wire)      
      295 +    {                                                         
      296 +      int pos = start;                                        
      297 +      while (pos < end) {                                     
      298 +        ulong tag = ReadVarint(span, ref pos);                
      299 +        var wt = (WireType)(byte)(tag & 7);                   
      300 +        if ((int)(tag >> 3) == num) {                         
      301 +          valueStart = pos; wire = wt;                        
      302 +          return true;                                        
      303 +        }                                                     
      304 +        pos = SkipValue(span, pos, wt);                       
      305 +        if (pos > end)                                        
      306 +          throw Error(pos, "field value extends past end of me
          +ssage");                                                      
      307 +      }                                                       
      308 +      valueStart = 0; wire = default;                         
      309 +      return false;                                           
      310 +    }                                                         
      311 +                                                              
      312 +    #endregion                                                
      313 +                                                              
      314      #region Scalar readers
      315  
      316      internal long ReadInt(FieldId name) => unchecked((long)Rea
           dRawVarintField(name));
     ...
      319      ulong ReadRawVarintField(FieldId name)
      320      {
      321        var span = _mem.Span;
      233 -      if (IsInsideList) {                                     
      234 -        int p = _top.ListPos;                                 
      322 +      if (InListFrame) {                                      
      323 +        if (!OpenPackedScalar(span))                          
      324 +          throw Error(_mem.Length, "read past the end of a lis
          +t");                                                          
      325 +        var top = _top!;                                      
      326 +        int p = top.PackedPos;                                
      327          ulong v = ReadVarint(span, ref p);
      236 -        _top.ListPos = p;                                     
      328 +        if (p > top.PackedEnd)                                
      329 +          throw Error(top.PackedPos, "packed element extends p
          +ast its container");                                          
      330 +        top.PackedPos = p;                                    
      331          return v;
      332        }
      333        if (TryGetField(name, out int vs, out _)) {
      334          int p = vs;
      335          return ReadVarint(span, ref p);
      336        }
      243 -      return 0; // absent -> default                          
      337 +      return 0; // absent -> default (standard Protobuf semant
          +ics)                                                          
      338      }
      339  
      340      internal long? ReadIntN(FieldId name)
     ...
      347      ulong? ReadRawVarintFieldN(FieldId name)
      348      {
      349        var span = _mem.Span;
      256 -      if (IsInsideList) {                                     
      257 -        int p = _top.ListPos;                                 
      258 -        byte present = ReadByteChecked(span, ref p);          
      259 -        ulong? result = present == 0 ? (ulong?)null : ReadVari
          -nt(span, ref p);                                              
      260 -        _top.ListPos = p;                                     
      261 -        return result;                                        
      262 -      }                                                       
      263 -      if (TryGetField(name, out int vs, out _)) {             
      350 +      if (InListFrame) {                                      
      351 +        // Nullable elements are wrapped: {} = null, { 1: valu
          +e } otherwise                                                 
      352 +        var (s, e) = NextElemPayload(span);                   
      353 +        if (!FindWrapperField(span, s, e, 1, out int vs, out _
          +))                                                            
      354 +          return null;                                        
      355          int p = vs;
      356          return ReadVarint(span, ref p);
      357        }
      358 +      if (TryGetField(name, out int vs2, out _)) {            
      359 +        int p = vs2;                                          
      360 +        return ReadVarint(span, ref p);                       
      361 +      }                                                       
      362        return null; // absent -> null
      363      }
      364  
      270 -    internal float ReadFloat(FieldId name) => ReadFloatN(name)
          - ?? default;                                                  
      271 -    internal float? ReadFloatN(FieldId name)                  
      365 +    internal float ReadFloatRaw(FieldId name)                 
      366      {
      367        var span = _mem.Span;
      274 -      if (IsInsideList) {                                     
      275 -        // Non-nullable float lists write raw bits; nullable f
          -loat lists write a                                            
      276 -        // presence byte. The element sync method decides whic
          -h by its static type,                                         
      277 -        // so this method (float?) always uses presence framin
          -g.                                                            
      278 -        int p = _top.ListPos;                                 
      279 -        byte present = ReadByteChecked(span, ref p);          
      280 -        float? result = present == 0 ? (float?)null : BitsToFl
          -oat(ReadLE32(span, ref p));                                   
      281 -        _top.ListPos = p;                                     
      282 -        return result;                                        
      368 +      if (InListFrame) {                                      
      369 +        if (!OpenPackedScalar(span))                          
      370 +          throw Error(_mem.Length, "read past the end of a lis
          +t");                                                          
      371 +        var top = _top!;                                      
      372 +        if (top.PackedEnd - top.PackedPos < 4)                
      373 +          throw Error(top.PackedPos, "packed element extends p
          +ast its container");                                          
      374 +        int p = top.PackedPos;                                
      375 +        float f = BitsToFloat(ReadLE32(span, ref p));         
      376 +        top.PackedPos = p;                                    
      377 +        return f;                                             
      378        }
      379        if (TryGetField(name, out int vs, out _)) {
      380          int p = vs;
      381          return BitsToFloat(ReadLE32(span, ref p));
      382        }
      288 -      return null;                                            
      383 +      return 0;                                               
      384      }
      290 -    internal float ReadFloatRaw(FieldId name) // non-nullable 
          -list/field element                                            
      385 +    internal float? ReadFloatN(FieldId name)                  
      386      {
      387        var span = _mem.Span;
      293 -      if (IsInsideList) {                                     
      294 -        int p = _top.ListPos;                                 
      295 -        float f = BitsToFloat(ReadLE32(span, ref p));         
      296 -        _top.ListPos = p;                                     
      297 -        return f;                                             
      298 -      }                                                       
      299 -      if (TryGetField(name, out int vs, out _)) {             
      388 +      if (InListFrame) {                                      
      389 +        var (s, e) = NextElemPayload(span);                   
      390 +        if (!FindWrapperField(span, s, e, 1, out int vs, out _
          +))                                                            
      391 +          return null;                                        
      392          int p = vs;
      393          return BitsToFloat(ReadLE32(span, ref p));
      394        }
      303 -      return 0;                                               
      395 +      if (TryGetField(name, out int vs2, out _)) {            
      396 +        int p = vs2;                                          
      397 +        return BitsToFloat(ReadLE32(span, ref p));            
      398 +      }                                                       
      399 +      return null;                                            
      400      }
      401  
      402      internal double ReadDoubleRaw(FieldId name)
      403      {
      404        var span = _mem.Span;
      309 -      if (IsInsideList) {                                     
      310 -        int p = _top.ListPos;                                 
      405 +      if (InListFrame) {                                      
      406 +        if (!OpenPackedScalar(span))                          
      407 +          throw Error(_mem.Length, "read past the end of a lis
          +t");                                                          
      408 +        var top = _top!;                                      
      409 +        if (top.PackedEnd - top.PackedPos < 8)                
      410 +          throw Error(top.PackedPos, "packed element extends p
          +ast its container");                                          
      411 +        int p = top.PackedPos;                                
      412          double d = BitsToDouble(ReadLE64(span, ref p));
      312 -        _top.ListPos = p;                                     
      413 +        top.PackedPos = p;                                    
      414          return d;
      415        }
      416        if (TryGetField(name, out int vs, out _)) {
     ...
      422      internal double? ReadDoubleN(FieldId name)
      423      {
      424        var span = _mem.Span;
      324 -      if (IsInsideList) {                                     
      325 -        int p = _top.ListPos;                                 
      326 -        byte present = ReadByteChecked(span, ref p);          
      327 -        double? result = present == 0 ? (double?)null : BitsTo
          -Double(ReadLE64(span, ref p));                                
      328 -        _top.ListPos = p;                                     
      329 -        return result;                                        
      330 -      }                                                       
      331 -      if (TryGetField(name, out int vs, out _)) {             
      425 +      if (InListFrame) {                                      
      426 +        var (s, e) = NextElemPayload(span);                   
      427 +        if (!FindWrapperField(span, s, e, 1, out int vs, out _
          +))                                                            
      428 +          return null;                                        
      429          int p = vs;
      430          return BitsToDouble(ReadLE64(span, ref p));
      431        }
      432 +      if (TryGetField(name, out int vs2, out _)) {            
      433 +        int p = vs2;                                          
      434 +        return BitsToDouble(ReadLE64(span, ref p));           
      435 +      }                                                       
      436        return null;
      437      }
      438  
      338 -    // Reads a length-delimited payload (string/decimal/BigInt
          -eger) as a byte span.                                         
      339 -    // Returns false when the value is null/absent.           
      439 +    // Reads a non-nullable length-delimited value (decimal, B
          +igInteger): a plain                                           
      440 +    // LEN field, or a direct LEN element inside a list. Retur
          +ns false when absent.                                         
      441      bool TryReadLenBytes(FieldId name, out ReadOnlySpan<byte> 
           bytes)
      442      {
      443        var span = _mem.Span;
      343 -      if (IsInsideList) {                                     
      344 -        int p = _top.ListPos;                                 
      345 -        ulong v = ReadVarint(span, ref p);                    
      346 -        if (v == 0) { _top.ListPos = p; bytes = default; retur
          -n false; } // null element                                    
      347 -        if (v - 1 > (ulong)_opt.MaxPayloadSize)               
      348 -          throw Error("length-delimited payload is too large")
          -;                                                             
      349 -        int len = (int)(v - 1);                               
      350 -        CheckAvailable(span, p, len);                         
      351 -        bytes = span.Slice(p, len);                           
      352 -        _top.ListPos = p + len;                               
      444 +      if (InListFrame) {                                      
      445 +        var (s, e) = NextElemPayload(span);                   
      446 +        bytes = span.Slice(s, e - s);                         
      447          return true;
      448        }
      355 -      if (TryGetField(name, out int vs, out _)) {             
      356 -        int p = vs;                                           
      357 -        bytes = ReadLenPrefixed(span, ref p);                 
      449 +      if (TryGetField(name, out int vs, out WireType wire)) { 
      450 +        if (wire != WireType.Len)                             
      451 +          throw Error(vs, "expected a length-delimited value")
          +;                                                             
      452 +        var (s, e) = ReadLenPayload(span, vs);                
      453 +        bytes = span.Slice(s, e - s);                         
      454          return true;
      455        }
      456        bytes = default;
      457 +      return false;                                           
      458 +    }                                                         
      459 +                                                              
      460 +    // Reads a nullable length-delimited value (string, decima
          +l?, BigInteger?):                                             
      461 +    // omitted when null in a message; wrapped as { 1: value }
          + / {} inside a list.                                          
      462 +    // Returns false when the value is null/absent.           
      463 +    bool TryReadLenBytesN(FieldId name, out ReadOnlySpan<byte>
          + bytes)                                                       
      464 +    {                                                         
      465 +      var span = _mem.Span;                                   
      466 +      if (InListFrame) {                                      
      467 +        var (s, e) = NextElemPayload(span);                   
      468 +        if (!FindWrapperField(span, s, e, 1, out int vs, out W
          +ireType wire)) {                                              
      469 +          bytes = default;                                    
      470 +          return false; // empty wrapper = null element       
      471 +        }                                                     
      472 +        if (wire != WireType.Len)                             
      473 +          throw Error(vs, "expected a length-delimited value")
          +;                                                             
      474 +        var (vStart, vEnd) = ReadLenPayload(span, vs);        
      475 +        if (vEnd > e)                                         
      476 +          throw Error(vs, "value extends past end of its wrapp
          +er");                                                         
      477 +        bytes = span.Slice(vStart, vEnd - vStart);            
      478 +        return true;                                          
      479 +      }                                                       
      480 +      if (TryGetField(name, out int fvs, out WireType fwire)) 
          +{                                                             
      481 +        if (fwire != WireType.Len)                            
      482 +          throw Error(fvs, "expected a length-delimited value"
          +);                                                            
      483 +        var (s, e) = ReadLenPayload(span, fvs);               
      484 +        bytes = span.Slice(s, e - s);                         
      485 +        return true;                                          
      486 +      }                                                       
      487 +      bytes = default;                                        
      488        return false; // absent -> null
      489      }
      490  
      364 -    internal string? ReadString(FieldId name)                 
      491 +    internal string? ReadString(FieldId name, ObjectMode mode)
      492      {
      366 -      if (!TryReadLenBytes(name, out var bytes))              
      493 +      if ((mode & ObjectMode.Deduplicate) != 0)               
      494 +        return (string?)ReadDedupLenValue(name, isString: true
          +);                                                            
      495 +      if (!TryReadLenBytesN(name, out var bytes))             
      496          return null;
      368 -      #if NETSTANDARD2_0                                      
      369 -      return Encoding.UTF8.GetString(bytes.ToArray());        
      370 -      #else                                                   
      371 -      return Encoding.UTF8.GetString(bytes);                  
      372 -      #endif                                                  
      497 +      return Utf8Decode(bytes);                               
      498      }
      499  
      375 -    internal decimal ReadDecimal(FieldId name) => ReadDecimalN
          -(name) ?? default;                                            
      376 -    internal decimal? ReadDecimalN(FieldId name)              
      500 +    // Reads a deduplicated string or byte[]: a wrapper { 1: i
          +d, 2: value } on the                                          
      501 +    // first occurrence, { 1: id } for a back-reference, or ab
          +sent/{} for null.                                             
      502 +    object? ReadDedupLenValue(FieldId name, bool isString)    
      503      {
      504 +      var span = _mem.Span;                                   
      505 +      int s, e;                                               
      506 +      if (InListFrame) {                                      
      507 +        (s, e) = NextElemPayload(span);                       
      508 +        if (s == e)                                           
      509 +          return null; // empty wrapper = null element        
      510 +      } else {                                                
      511 +        if (!TryGetField(name, out int vs, out WireType wire))
      512 +          return null; // absent -> null                      
      513 +        if (wire != WireType.Len)                             
      514 +          throw Error(vs, "expected a length-delimited value")
          +;                                                             
      515 +        (s, e) = ReadLenPayload(span, vs);                    
      516 +      }                                                       
      517 +      if (!FindWrapperField(span, s, e, 1, out int idStart, ou
          +t _))                                                         
      518 +        throw Error(s, "deduplicated value has no id");       
      519 +      int p = idStart;                                        
      520 +      long id = (long)ReadVarint(span, ref p);                
      521 +      if (FindWrapperField(span, s, e, 2, out int valStart, ou
          +t WireType valWire)) {                                        
      522 +        if (valWire != WireType.Len)                          
      523 +          throw Error(valStart, "expected a length-delimited v
          +alue");                                                       
      524 +        var (vs2, ve2) = ReadLenPayload(span, valStart);      
      525 +        object value = isString                               
      526 +          ? (object)Utf8Decode(span.Slice(vs2, ve2 - vs2))    
      527 +          : span.Slice(vs2, ve2 - vs2).ToArray();             
      528 +        _objects ??= new Dictionary<long, object>();          
      529 +        _objects[id] = value;                                 
      530 +        return value;                                         
      531 +      }                                                       
      532 +      if (_objects == null || !_objects.TryGetValue(id, out va
          +r existing))                                                  
      533 +        throw Error(s, "dangling deduplication back-reference"
          +);                                                            
      534 +      return existing;                                        
      535 +    }                                                         
      536 +                                                              
      537 +    internal decimal ReadDecimal(FieldId name)                
      538 +    {                                                         
      539        if (!TryReadLenBytes(name, out var bytes))
      540 +        return default;                                       
      541 +      return DecimalFromBytes(bytes);                         
      542 +    }                                                         
      543 +    internal decimal? ReadDecimalN(FieldId name)              
      544 +    {                                                         
      545 +      if (!TryReadLenBytesN(name, out var bytes))             
      546          return null;
      547 +      return DecimalFromBytes(bytes);                         
      548 +    }                                                         
      549 +    decimal DecimalFromBytes(ReadOnlySpan<byte> bytes)        
      550 +    {                                                         
      551        if (bytes.Length != 16)
      381 -        throw Error("decimal payload is not 16 bytes");       
      552 +        throw new FormatException("Invalid Protobuf data: deci
          +mal payload is not 16 bytes".Localized());                    
      553        int[] bits = new int[4];
      554        for (int i = 0; i < 4; i++)
      555          bits[i] = unchecked((int)(uint)(bytes[i * 4] | (bytes[
           i * 4 + 1] << 8) | (bytes[i * 4 + 2] << 16) | (bytes[i * 4 + 3
           ] << 24)));
      556        return new decimal(bits);
      557      }
      558  
      388 -    internal BigInteger ReadBigInt(FieldId name) => ReadBigInt
          -N(name) ?? default;                                           
      389 -    internal BigInteger? ReadBigIntN(FieldId name)            
      559 +    internal BigInteger ReadBigInt(FieldId name)              
      560      {
      561        if (!TryReadLenBytes(name, out var bytes))
      562 +        return default;                                       
      563 +      return BigIntFromBytes(bytes);                          
      564 +    }                                                         
      565 +    internal BigInteger? ReadBigIntN(FieldId name)            
      566 +    {                                                         
      567 +      if (!TryReadLenBytesN(name, out var bytes))             
      568          return null;
      569 +      return BigIntFromBytes(bytes);                          
      570 +    }                                                         
      571 +    static BigInteger BigIntFromBytes(ReadOnlySpan<byte> bytes
          +)                                                             
      572 +    {                                                         
      573        #if NETSTANDARD2_0
      574        return new BigInteger(bytes.ToArray());
      575        #else
     ...
      577        #endif
      578      }
      579  
      580 +    // Locates a byte[] value written by WriterState.WriteByte
          +ListField.                                                    
      581 +    // Start >= 0: the value occupies [Start, Start+Length).  
      582 +    // Start == -1: the value is null. Start == -2: Backref ho
          +lds an earlier byte[].                                        
      583 +    internal (int Start, int Length, object? Backref, long Id,
          + bool HasId) ReadByteListField(FieldId name, ObjectMode mode) 
      584 +    {                                                         
      585 +      if ((mode & ObjectMode.Deduplicate) != 0) {             
      586 +        // ReadDedupLenValue registers the byte[] it creates, 
          +so back-references                                            
      587 +        // resolve to the same array instance.                
      588 +        object? value = ReadDedupLenValue(name, isString: fals
          +e);                                                           
      589 +        if (value == null)                                    
      590 +          return (-1, 0, null, 0, false);                     
      591 +        return (-2, 0, value, 0, false);                      
      592 +      }                                                       
      593 +      bool nullable = (mode & ObjectMode.NotNull) == 0;       
      594 +      bool found = nullable                                   
      595 +        ? TryReadLenBytesN(name, out ReadOnlySpan<byte> bytes)
      596 +        : TryReadLenBytes(name, out bytes);                   
      597 +      if (!found)                                             
      598 +        return (-1, 0, null, 0, false);                       
      599 +      // Report the position instead of the span so the caller
          + (a struct method                                             
      600 +      // with a generic builder) can slice the input itself.  
      601 +      unsafe {                                                
      602 +        // Convert span back to offsets within _mem           
      603 +      }                                                       
      604 +      // The span came from _mem.Span, so compute its offset v
          +ia reference math is                                          
      605 +      // unnecessary: re-derive using the same lookups is comp
          +lex, so instead we                                            
      606 +      // return offsets directly from a second location-based 
          +helper below.                                                 
      607 +      throw new InvalidOperationException(); // replaced below
          +; see ReadByteListBounds                                      
      608 +    }                                                         
      609 +                                                              
      610 +    internal ReadOnlySpan<byte> InputSpan => _mem.Span;       
      611 +                                                              
      612      #endregion
      613  
      614      #region Type tag
      615  
      616      internal string? ReadTypeTag()
      617      {
      406 -      if (IsInsideList)                                       
      618 +      if (InListFrame)                                        
      619          return null;
      620        if (!FindField(TypeTagFieldNumber, out int vs, out _))
      621          return null;
      622        var span = _mem.Span;
      411 -      int p = vs;                                             
      412 -      var bytes = ReadLenPrefixed(span, ref p);               
      413 -      #if NETSTANDARD2_0                                      
      414 -      return Encoding.UTF8.GetString(bytes.ToArray());        
      415 -      #else                                                   
      416 -      return Encoding.UTF8.GetString(bytes);                  
      417 -      #endif                                                  
      623 +      var (s, e) = ReadLenPayload(span, vs);                  
      624 +      return Utf8Decode(span.Slice(s, e - s));                
      625      }
      626  
      627      #endregion
     ...
      631      public (bool Begun, int Length, object? Object) BeginSubOb
           ject(FieldId name, ObjectMode mode)
      632      {
      633        var span = _mem.Span;
      427 -      bool insideList = IsInsideList;                         
      428 -      ObjectMode kind = mode & (ObjectMode.List | ObjectMode.T
          -uple);                                                        
      634 +      FrameKind kind = (mode & ObjectMode.Tuple) == ObjectMode
          +.Tuple ? FrameKind.Tuple                                      
      635 +        : (mode & ObjectMode.List) != 0 ? FrameKind.List : Fra
          +meKind.Object;                                                
      636 +      bool dedup = (mode & ObjectMode.Deduplicate) != 0;      
      637 +      bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
          +.Deduplicate)) != ObjectMode.NotNull;                         
      638  
      430 -      int payloadStart, payloadEnd;                           
      431 -      if (insideList) {                                       
      432 -        int p = _top.ListPos;                                 
      433 -        if (p >= _top.ListEnd)                                
      639 +      if (_top == null)                                       
      640 +        return BeginRoot(span, kind, dedup);                  
      641 +                                                              
      642 +      if (InListFrame) {                                      
      643 +        if (ReachedEndOfList == true)                         
      644            return (false, 0, null);
      435 -        ulong v = ReadVarint(span, ref p);                    
      436 -        if (v == 0) { _top.ListPos = p; return (false, 0, null
          -); } // null element                                          
      437 -        if (v - 1 > (ulong)_opt.MaxPayloadSize)               
      438 -            throw Error("length-delimited payload is too large
          -");                                                           
      439 -          int frameLen = (int)(v - 1);                        
      440 -        payloadStart = p;                                     
      441 -        payloadEnd = p + frameLen;                            
      442 -        _top.ListPos = payloadEnd;                            
      443 -      } else {                                                
      444 -        if (!TryGetField(name, out int vs, out WireType wire))
          - {                                                            
      445 -          return (false, 0, null); // absent -> null object/li
          -st                                                            
      645 +        var (s, e) = NextElemPayload(span);                   
      646 +        if (dedup)                                            
      647 +          return BeginDedup(span, s, e, kind);                
      648 +        if (nullable) {                                       
      649 +          // Nullable element wrapper: {} = null, { 1: body } 
          +otherwise                                                     
      650 +          if (!FindWrapperField(span, s, e, 1, out int vs, out
          + WireType wire))                                              
      651 +            return (false, 0, null);                          
      652 +          if (wire != WireType.Len)                           
      653 +            throw Error(vs, "expected a length-delimited value
          +");                                                           
      654 +          var (bs, be) = ReadLenPayload(span, vs);            
      655 +          return PushBody(kind, bs, be, 0, false);            
      656          }
      447 -        if (wire != WireType.Len)                             
      448 -          throw Error("expected a length-delimited value");   
      449 -        int p = vs;                                           
      450 -        ulong len = ReadVarint(span, ref p);                  
      451 -        payloadStart = p;                                     
      452 -        if (len > (ulong)_opt.MaxPayloadSize)                 
      453 -            throw Error("length-delimited payload is too large
          -");                                                           
      454 -          payloadEnd = p + (int)len;                          
      657 +        return PushBody(kind, s, e, 0, false); // NotNull elem
          +ent: direct body                                              
      658        }
      659  
      457 -      if (payloadEnd < payloadStart || payloadEnd > span.Lengt
          -h)                                                            
      458 -        throw Error("object payload extends past end of data")
          -;                                                             
      660 +      // Field context: consume the field number even if the f
          +ield is absent                                                
      661 +      if (!TryGetField(name, out int fvs, out WireType fwire))
      662 +        return (false, 0, null); // absent -> null object/list
      663 +      if (fwire != WireType.Len)                              
      664 +        throw Error(fvs, "expected a length-delimited value");
      665 +      var (ps, pe) = ReadLenPayload(span, fvs);               
      666 +      if (dedup)                                              
      667 +        return BeginDedup(span, ps, pe, kind);                
      668 +      return PushBody(kind, ps, pe, 0, false);                
      669 +    }                                                         
      670  
      460 -      // Every sub-object/list payload begins with a framing m
          -arker (written even when                                      
      461 -      // deduplication was off), so the reader detects dedup f
          -rom the data itself and                                       
      462 -      // tolerates the Deduplicate flag being toggled between 
          -writing and reading.                                          
      463 -      long objId = 0;                                         
      464 -      bool hasId = false;                                     
      465 -      int bodyStart;                                          
      466 -      {                                                       
      467 -        int p = payloadStart;                                 
      468 -        if (p >= payloadEnd) throw Error("empty object payload
          -");                                                           
      469 -          byte marker = ReadByteChecked(span, ref p);         
      470 -        if (marker == DedupNone) {                            
      471 -          bodyStart = p;                                      
      472 -        } else if (marker == DedupFirst) {                    
      473 -          objId = (long)ReadVarint(span, ref p);              
      474 -          hasId = true;                                       
      475 -          bodyStart = p;                                      
      476 -        } else if (marker == DedupBackRef) {                  
      477 -          ulong id = ReadVarint(span, ref p);                 
      478 -          if (_objects == null || !_objects.TryGetValue((long)
          -id, out var existing))                                        
      479 -            throw Error("dangling deduplication back-reference
          -");                                                           
      480 -          return (false, 0, existing);                        
      481 -        } else {                                              
      482 -          throw Error("invalid object framing marker");       
      483 -        }                                                     
      671 +    // The root value is a bare message body occupying the who
          +le input.                                                     
      672 +    (bool Begun, int Length, object? Object) BeginRoot(ReadOnl
          +ySpan<byte> span, FrameKind kind, bool dedup)                 
      673 +    {                                                         
      674 +      if (kind != FrameKind.Object)                           
      675 +        throw new NotSupportedException(                      
      676 +          "SyncProtobuf: the root value must be an object (a P
          +rotobuf message), not a list or tuple.");                     
      677 +      if (_mem.Length == 0)                                   
      678 +        return (false, 0, null); // zero bytes = null root    
      679 +      if (dedup)                                              
      680 +        return BeginDedup(span, 0, _mem.Length, kind);        
      681 +      return PushBody(kind, 0, _mem.Length, 0, false);        
      682 +    }                                                         
      683 +                                                              
      684 +    // Interprets [s, e) as a dedup wrapper { 1: id, 2: body }
          + / { 1: id } / {}.                                            
      685 +    (bool Begun, int Length, object? Object) BeginDedup(ReadOn
          +lySpan<byte> span, int s, int e, FrameKind kind)              
      686 +    {                                                         
      687 +      if (s == e)                                             
      688 +        return (false, 0, null); // empty wrapper = null      
      689 +      if (!FindWrapperField(span, s, e, 1, out int idStart, ou
          +t _))                                                         
      690 +        throw Error(s, "deduplicated object has no id");      
      691 +      int p = idStart;                                        
      692 +      long id = (long)ReadVarint(span, ref p);                
      693 +      if (FindWrapperField(span, s, e, 2, out int valStart, ou
          +t WireType valWire)) {                                        
      694 +        if (valWire != WireType.Len)                          
      695 +          throw Error(valStart, "expected a length-delimited v
          +alue");                                                       
      696 +        var (bs, be) = ReadLenPayload(span, valStart);        
      697 +        return PushBody(kind, bs, be, id, true);              
      698        }
      699 +      // Back-reference: no body                              
      700 +      if (_objects == null || !_objects.TryGetValue(id, out va
          +r existing))                                                  
      701 +        throw Error(s, "dangling deduplication back-reference"
          +);                                                            
      702 +      return (false, 0, existing);                            
      703 +    }                                                         
      704  
      705 +    (bool Begun, int Length, object? Object) PushBody(FrameKin
          +d kind, int start, int end, long id, bool hasId)              
      706 +    {                                                         
      707        var frame = new RFrame {
      487 -        Type = (ObjType)kind,                                 
      488 -        Mode = mode,                                          
      708 +        Kind = kind,                                          
      709          SavedLastFieldId = _lastFieldId,
      490 -        Id = objId,                                           
      710 +        Id = id,                                              
      711          HasId = hasId,
      712        };
      493 -      if (kind == 0) {                                        
      494 -        frame.Fields = IndexFields(bodyStart, payloadEnd);    
      713 +      if (kind == FrameKind.List) {                           
      714 +        frame.Entries = IndexListEntries(start, end);         
      715        } else {
      496 -        frame.ListPos = bodyStart;                            
      497 -        frame.ListEnd = payloadEnd;                           
      716 +        frame.Fields = IndexFields(start, end);               
      717        }
      718        Push(frame);
      500 -                                                              
      501 -      return (true, kind == 0 ? 1 : int.MaxValue, null);      
      719 +      return (true, kind == FrameKind.Object ? 1 : int.MaxValu
          +e, null);                                                     
      720      }
      721  
      722      public void EndSubObject()
      723      {
      506 -      var frame = _top;                                       
      724 +      var frame = _top!;                                      
      725        _stack.RemoveAt(_stack.Count - 1);
      508 -      _top = _stack[_stack.Count - 1];                        
      726 +      _top = _stack.Count > 0 ? _stack[_stack.Count - 1] : nul
          +l;                                                            
      727        _lastFieldId = frame.SavedLastFieldId;
      728      }
      729  
     ...
      738  
      739      internal void SetCurrentObject(object value)
      740      {
      523 -      if (_top.HasId) {                                       
      741 +      if (_top != null && _top.HasId) {                       
      742          _objects ??= new Dictionary<long, object>();
      743          _objects[_top.Id] = value;
      744        }
      745      }
      746  
      529 -    internal FieldId NextField                                
      747 +    internal void RegisterObject(long id, object value)       
      748      {
      749 +      _objects ??= new Dictionary<long, object>();            
      750 +      _objects[id] = value;                                   
      751 +    }                                                         
      752 +                                                              
      753 +    internal FieldId NextField {                              
      754        get {
      532 -        if (_top.Type != ObjType.Normal || _top.Fields == null
          - || _top.Cursor >= _top.Fields.Count)                         
      755 +        var top = _top;                                       
      756 +        if (top == null || top.Kind == FrameKind.List || top.F
          +ields == null)                                                
      757            return FieldId.Missing;
      534 -        return new FieldId(null, _top.Fields[_top.Cursor].Num)
          -;                                                             
      758 +        // Skip the reserved field numbers (_type and _present
          + markers)                                                     
      759 +        for (int i = top.Cursor; i < top.Fields.Count; i++) { 
      760 +          int num = top.Fields[i].Num;                        
      761 +          if (num < PresentFieldNumber)                       
      762 +            return new FieldId(null, num);                    
      763 +        }                                                     
      764 +        return FieldId.Missing;                               
      765        }
      766      }
      767  
     ...
      769      {
      770        if (IsInsideList)
      771          return SyncType.Unknown;
      772 +      if (_top == null && _mem.Length == 0)                   
      773 +        return SyncType.Missing;                              
      774        int id = name.Id != int.MinValue ? name.Id : _lastFieldI
           d + 1;
      775        // Note: this does not advance _lastFieldId (it's only a
            peek).
      776        if (!FindFieldPeek(id, out WireType wire))
     ...
      785      }
      786      bool FindFieldPeek(int id, out WireType wire)
      787      {
      556 -      var fields = _top.Fields;                               
      788 +      var fields = TopObject().Fields;                        
      789        if (fields != null)
      558 -        foreach (var fe in fields)                            
      559 -          if (fe.Num == id) { wire = fe.Wire; return true; }  
      790 +        for (int i = fields.Count - 1; i >= 0; i--)           
      791 +          if (fields[i].Num == id) { wire = fields[i].Wire; re
          +turn true; }                                                  
      792        wire = default;
      793        return false;
      794      }
      795  
      564 -    internal void VerifyEof()                                 
      796 +    static string Utf8Decode(ReadOnlySpan<byte> bytes)        
      797      {
      566 -      if (!_opt.Read.VerifyEof)                               
      567 -        return;                                               
      568 -      // The root frame indexes the whole input; a well-formed
          - stream has exactly one                                       
      569 -      // top-level field (the root object) covering all the by
          -tes.                                                          
      570 -      var root = _stack[0];                                   
      571 -      if (root.Fields != null && root.Fields.Count > 1)       
      572 -        throw Error("unexpected trailing data after root objec
          -t");                                                          
      798 +      #if NETSTANDARD2_0                                      
      799 +      return Encoding.UTF8.GetString(bytes.ToArray());        
      800 +      #else                                                   
      801 +      return Encoding.UTF8.GetString(bytes);                  
      802 +      #endif                                                  
      803      }
      804  
      805      static float BitsToFloat(uint bits)

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)
  ⎿  Added 26 lines, removed 17 lines
      436        return null;
      437      }
      438  
      439 -    // Reads a non-nullable length-delimited value (decimal, B
          -igInteger): a plain                                           
      440 -    // LEN field, or a direct LEN element inside a list. Retur
          -ns false when absent.                                         
      441 -    bool TryReadLenBytes(FieldId name, out ReadOnlySpan<byte> 
          -bytes)                                                        
      439 +    // Locates a non-nullable length-delimited value (decimal,
          + BigInteger, bytes): a                                        
      440 +    // plain LEN field, or a direct LEN element inside a list.
          + False when absent.                                           
      441 +    bool TryReadLenBounds(FieldId name, out int start, out int
          + end)                                                         
      442      {
      443        var span = _mem.Span;
      444        if (InListFrame) {
      445 -        var (s, e) = NextElemPayload(span);                   
      446 -        bytes = span.Slice(s, e - s);                         
      445 +        (start, end) = NextElemPayload(span);                 
      446          return true;
      447        }
      448        if (TryGetField(name, out int vs, out WireType wire)) {
      449          if (wire != WireType.Len)
      450            throw Error(vs, "expected a length-delimited value")
           ;
      452 -        var (s, e) = ReadLenPayload(span, vs);                
      453 -        bytes = span.Slice(s, e - s);                         
      451 +        (start, end) = ReadLenPayload(span, vs);              
      452          return true;
      453        }
      456 -      bytes = default;                                        
      454 +      start = end = 0;                                        
      455        return false;
      456      }
      457  
      460 -    // Reads a nullable length-delimited value (string, decima
          -l?, BigInteger?):                                             
      458 +    // Locates a nullable length-delimited value (string, deci
          +mal?, BigInteger?):                                           
      459      // omitted when null in a message; wrapped as { 1: value }
            / {} inside a list.
      460      // Returns false when the value is null/absent.
      463 -    bool TryReadLenBytesN(FieldId name, out ReadOnlySpan<byte>
          - bytes)                                                       
      461 +    bool TryReadLenBoundsN(FieldId name, out int start, out in
          +t end)                                                        
      462      {
      463        var span = _mem.Span;
      464        if (InListFrame) {
      465          var (s, e) = NextElemPayload(span);
      466          if (!FindWrapperField(span, s, e, 1, out int vs, out W
           ireType wire)) {
      469 -          bytes = default;                                    
      467 +          start = end = 0;                                    
      468            return false; // empty wrapper = null element
      469          }
      470          if (wire != WireType.Len)
      471            throw Error(vs, "expected a length-delimited value")
           ;
      474 -        var (vStart, vEnd) = ReadLenPayload(span, vs);        
      475 -        if (vEnd > e)                                         
      472 +        (start, end) = ReadLenPayload(span, vs);              
      473 +        if (end > e)                                          
      474            throw Error(vs, "value extends past end of its wrapp
           er");
      477 -        bytes = span.Slice(vStart, vEnd - vStart);            
      475          return true;
      476        }
      477        if (TryGetField(name, out int fvs, out WireType fwire)) 
           {
      478          if (fwire != WireType.Len)
      479            throw Error(fvs, "expected a length-delimited value"
           );
      483 -        var (s, e) = ReadLenPayload(span, fvs);               
      484 -        bytes = span.Slice(s, e - s);                         
      480 +        (start, end) = ReadLenPayload(span, fvs);             
      481          return true;
      482        }
      487 -      bytes = default;                                        
      483 +      start = end = 0;                                        
      484        return false; // absent -> null
      485      }
      486  
      487 +    bool TryReadLenBytes(FieldId name, out ReadOnlySpan<byte> 
          +bytes)                                                        
      488 +    {                                                         
      489 +      bool found = TryReadLenBounds(name, out int s, out int e
          +);                                                            
      490 +      bytes = found ? _mem.Span.Slice(s, e - s) : default;    
      491 +      return found;                                           
      492 +    }                                                         
      493 +    bool TryReadLenBytesN(FieldId name, out ReadOnlySpan<byte>
          + bytes)                                                       
      494 +    {                                                         
      495 +      bool found = TryReadLenBoundsN(name, out int s, out int 
          +e);                                                           
      496 +      bytes = found ? _mem.Span.Slice(s, e - s) : default;    
      497 +      return found;                                           
      498 +    }                                                         
      499 +                                                              
      500      internal string? ReadString(FieldId name, ObjectMode mode)
      501      {
      502        if ((mode & ObjectMode.Deduplicate) != 0)

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)
  ⎿  Added 10 lines, removed 20 lines
      587      }
      588  
      589      // Locates a byte[] value written by WriterState.WriteByte
           ListField.
      590 -    // Start >= 0: the value occupies [Start, Start+Length).  
      591 -    // Start == -1: the value is null. Start == -2: Backref ho
          -lds an earlier byte[].                                        
      592 -    internal (int Start, int Length, object? Backref, long Id,
          - bool HasId) ReadByteListField(FieldId name, ObjectMode mode) 
      590 +    // Start >= 0: the value occupies [Start, Start+Length) of
          + InputSpan.                                                   
      591 +    // Start == -1: the value is null. Start == -2: Backref ho
          +lds a byte[] instance                                         
      592 +    // (a deduplicated value; ReadDedupLenValue registers each
          + new byte[] it creates,                                       
      593 +    // so back-references resolve to the same array instance).
      594 +    internal (int Start, int Length, object? Backref) ReadByte
          +ListField(FieldId name, ObjectMode mode)                      
      595      {
      596        if ((mode & ObjectMode.Deduplicate) != 0) {
      595 -        // ReadDedupLenValue registers the byte[] it creates, 
          -so back-references                                            
      596 -        // resolve to the same array instance.                
      597          object? value = ReadDedupLenValue(name, isString: fals
           e);
      598 -        if (value == null)                                    
      599 -          return (-1, 0, null, 0, false);                     
      600 -        return (-2, 0, value, 0, false);                      
      598 +        return value == null ? (-1, 0, (object?)null) : (-2, 0
          +, value);                                                     
      599        }
      600        bool nullable = (mode & ObjectMode.NotNull) == 0;
      601        bool found = nullable
      604 -        ? TryReadLenBytesN(name, out ReadOnlySpan<byte> bytes)
      605 -        : TryReadLenBytes(name, out bytes);                   
      602 +        ? TryReadLenBoundsN(name, out int s, out int e)       
      603 +        : TryReadLenBounds(name, out s, out e);               
      604        if (!found)
      607 -        return (-1, 0, null, 0, false);                       
      608 -      // Report the position instead of the span so the caller
          - (a struct method                                             
      609 -      // with a generic builder) can slice the input itself.  
      610 -      unsafe {                                                
      611 -        // Convert span back to offsets within _mem           
      612 -      }                                                       
      613 -      // The span came from _mem.Span, so compute its offset v
          -ia reference math is                                          
      614 -      // unnecessary: re-derive using the same lookups is comp
          -lex, so instead we                                            
      615 -      // return offsets directly from a second location-based 
          -helper below.                                                 
      616 -      throw new InvalidOperationException(); // replaced below
          -; see ReadByteListBounds                                      
      605 +        return (-1, 0, null);                                 
      606 +      return (s, e - s, null);                                
      607      }
      608  
      609      internal ReadOnlySpan<byte> InputSpan => _mem.Span;

● Now the main class docs and constants (SyncProtobuf.cs), then Options and the
  two facades.

● API Error: Server error mid-response. The response above may be 
  incomplete.

✻ Sautéed for 4m 39s

❯ continue                                                                      
● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.cs)
  ⎿  Added 102 lines, removed 62 lines
       10  ///   the <a href="https://protobuf.dev/programming-guides/enc
           oding/">Protocol Buffers</a>
       11  ///   wire format. Call <see cref="Write{T}(T, SyncObjectFunc{
           Writer, T}, Options?)"/> or
       12  ///   <see cref="Read{T}(ReadOnlyMemory{byte}, SyncObjectFunc{
           Reader, T}, Options?)"/> to
       13 -///   (de)serialize an object, or <see cref="NewWriter"/> / <s
          -ee cref="NewReader(ReadOnlyMemory{byte}, Options?)"/>         
       14 -///   to obtain a low-level (de)serializer.                   
       13 +///   (de)serialize an object, <see cref="NewWriter"/> / <see 
          +cref="NewReader(ReadOnlyMemory{byte}, Options?)"/>            
       14 +///   to obtain a low-level (de)serializer, or <see cref="Writ
          +eSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>            
       15 +///   to generate a <c>.proto</c> schema describing the output
          +.                                                             
       16  /// </summary>
       17  /// <remarks>
       18 +///   The output is standard, valid Protocol Buffers: any Prot
          +obuf implementation (such                                     
       19 +///   as protoc-generated code or protobuf-net) can parse it u
          +sing the proto3 schema                                        
       20 +///   produced by <see cref="SyncProtobuf.Schema"/>, and <see 
          +cref="Reader"/> can parse                                     
       21 +///   messages produced by other Protobuf implementations from
          + the same schema.                                             
       22 +///   <para/>                                                 
       23  ///   Unlike <see cref="SyncBinary"/>, this format identifies 
           every field by an integer
       18 -///   ID, exactly like Protocol Buffers. Therefore <see cref="
          -Reader"/> and                                                 
       19 -///   <see cref="Writer"/> report <see cref="ISyncManager.Need
          -sIntegerIds"/> = true and                                     
       24 +///   ID. Therefore <see cref="Reader"/> and <see cref="Writer
          +"/> report                                                    
       25 +///   <see cref="ISyncManager.NeedsIntegerIds"/> = true and   
       26  ///   <see cref="ISyncManager.SupportsReordering"/> = true: fi
           elds may be read in any
       27  ///   order, and unknown fields are skipped.
       28  ///
       23 -/// <h3>Field IDs</h3>                                        
       29 +/// <h3>Field numbers</h3>                                    
       30  ///
       31  ///   Each call to a <c>Sync</c> method carries a <see cref="F
           ieldId"/>. If the FieldId
       32  ///   specifies an integer ID (i.e. <c>FieldId.Id != int.MinVa
           lue</c>, as produced by the
       33  ///   <c>(name, id)</c> tuple conversion or by a private <see 
           cref="Symbol"/> pool), that
       34  ///   ID becomes the Protobuf field number. Otherwise the fiel
           d number is auto-assigned as
       29 -///   <c>N + 1</c>, where <c>N</c> is the last field number us
          -ed in the current object                                      
       30 -///   (starting from 0). This matches the convention documente
          -d for                                                         
       31 -///   <see cref="SyncBinary.FieldIdMode.Integers"/>. The auto-
          -numbering advances for every                                  
       32 -///   field synchronized (whether or not a value is physically
          - written), so the reader and                                  
       33 -///   writer stay in agreement as long as they synchronize the
          - same fields in the same                                      
       34 -///   order.                                                  
       35 +///   <c>N + 1</c>, where <c>N</c> is the last field number us
          +ed in the current message                                     
       36 +///   (starting from 0). The auto-numbering advances for every
          + field synchronized (whether                                  
       37 +///   or not a value is physically written), so the reader and
          + writer stay in agreement as                                  
       38 +///   long as they synchronize the same fields in the same ord
          +er. Valid field numbers are                                   
       39 +///   1 to 536,870,909; the range 19000-19999 (reserved by Pro
          +tobuf) and the two highest                                    
       40 +///   numbers (reserved by SyncProtobuf, see below) are reject
          +ed.                                                           
       41  ///
       36 -/// <h3>Wire format</h3>                                      
       42 +/// <h3>Scalar wire format</h3>                               
       43  ///
       38 -///   Every field is preceded by a <i>tag</i>: an unsigned LEB
          -128 varint equal to                                           
       39 -///   <c>(fieldNumber &lt;&lt; 3) | wireType</c>. The wire typ
          -es used are the standard                                      
       40 -///   Protobuf ones:                                          
       44 +///   Every field is preceded by a <i>tag</i>: a varint equal 
          +to                                                            
       45 +///   <c>(fieldNumber &lt;&lt; 3) | wireType</c>, using the st
          +andard Protobuf wire types:                                   
       46  ///   <ul>
       47  ///   <li><b>VARINT (0)</b> — <c>bool</c>, <c>char</c> and all
            integer types. Signed
       48  ///       integers are stored as their 64-bit two's-complement
            bit pattern (so negative
       49  ///       numbers occupy 10 bytes, exactly like Protobuf <c>in
           t32</c>/<c>int64</c>).</li>
       50  ///   <li><b>I64 (1)</b> — <c>double</c> (8 bytes, little-endi
           an IEEE 754).</li>
       46 -///   <li><b>LEN (2)</b> — length-delimited payloads: <c>strin
          -g</c> (UTF-8), byte arrays,                                   
       47 -///       <c>decimal</c> (16 bytes), <c>BigInteger</c> (two's-
          -complement, big-endian),                                      
       48 -///       sub-messages, lists and tuples.</li>                
       51  ///   <li><b>I32 (5)</b> — <c>float</c> (4 bytes, little-endia
           n IEEE 754).</li>
       52 +///   <li><b>LEN (2)</b> — length-delimited payloads: <c>strin
          +g</c> (UTF-8), <c>byte[]</c>                                  
       53 +///       (Protobuf <c>bytes</c>), <c>decimal</c> (16 bytes: t
          +he little-endian layout of                                    
       54 +///       <see cref="decimal.GetBits(decimal)"/>), <see cref="
          +System.Numerics.BigInteger"/>                                 
       55 +///       (little-endian two's complement, the format of <c>Bi
          +gInteger.ToByteArray()</c>),                                  
       56 +///       sub-messages, and the list/tuple/deduplication conta
          +iners described below.</li>                                   
       57  ///   </ul>
       58  ///
       59  ///   <b>Null and absent fields.</b> A field whose value is nu
           ll (a null nullable scalar,
       53 -///   string, byte array or sub-object) is simply omitted, and
          - the reader returns null                                      
       54 -///   when a requested field is absent — matching Protobuf's "
          -absent means default"                                         
       55 -///   convention. Because presence is encoded structurally, no
          - special null bit-patterns                                    
       56 -///   are needed (contrast <see cref="SyncBinary"/>, which res
          -erves NaN and 0xFF values).                                   
       57 -///   To preserve round-trip fidelity, non-null scalars are al
          -ways written, even zero.                                      
       60 +///   string, byte array, list or sub-object) is simply omitte
          +d, and the reader returns                                     
       61 +///   null when a requested nullable field is absent — matchin
          +g Protobuf's "absent means                                    
       62 +///   default" convention. Reading an absent field as a non-nu
          +llable primitive returns the                                  
       63 +///   type's default value, exactly like Protobuf. To preserve
          + round-trip fidelity,                                         
       64 +///   non-null values are always written, even zero (in schema
          + terms, every scalar field                                    
       65 +///   is <c>optional</c>, i.e. it has explicit presence).     
       66  ///
       59 -/// <h3>Sub-messages, lists and tuples</h3>                   
       67 +/// <h3>The root object</h3>                                  
       68  ///
       61 -///   A sub-object, list or tuple is written as a single LEN f
          -ield: the tag is followed by                                  
       62 -///   a varint byte-length and then the payload. For a normal 
          -object the payload is the                                     
       63 -///   concatenation of its (tag, value) fields; the reader ind
          -exes them by field number so                                  
       64 -///   they can be read in any order. For a list or tuple the p
          -ayload is the concatenation                                   
       65 -///   of its elements, written positionally with no tags:     
       69 +///   The root object is written as a bare message body — no e
          +nvelope — just like a                                         
       70 +///   message serialized by any other Protobuf library. Becaus
          +e of this, the root must be                                   
       71 +///   an object (not a list or tuple), a null root is encoded 
          +as zero bytes, and a                                          
       72 +///   non-null root that happens to contain no fields is marke
          +d with a reserved boolean                                     
       73 +///   field (number 536,870,910, called <c>_present</c> in gen
          +erated schemas) so that it                                    
       74 +///   remains distinguishable from null.                      
       75 +///                                                           
       76 +/// <h3>Sub-objects, lists and tuples</h3>                    
       77 +///                                                           
       78 +///   A sub-object is a nested message: a LEN field containing
          + the concatenation of its                                     
       79 +///   (tag, value) fields, which can be read in any order.    
       80 +///   <para/>                                                 
       81 +///   A <b>tuple</b> is also a nested message; its elements ar
          +e stored as fields                                            
       82 +///   auto-numbered 1, 2, 3, ... (a null element is an omitted
          + field, which stays                                           
       83 +///   position-safe because element numbering advances regardl
          +ess).                                                         
       84 +///   <para/>                                                 
       85 +///   A <b>list</b> field is a nested <i>list container</i> me
          +ssage in which all elements                                   
       86 +///   are stored in field 1 (in generated schemas this contain
          +er is a message type like                                     
       87 +///   <c>Int32List { repeated int32 items = 1; }</c>). This on
          +e level of nesting is what                                    
       88 +///   allows SyncLib to distinguish a null list (field omitted
          +) from an empty one (empty                                    
       89 +///   container), and to nest lists inside lists. Within the c
          +ontainer:                                                     
       90  ///   <ul>
       67 -///   <li>A scalar element is written raw (VARINT / I32 / I64)
          -, which is self-delimiting.</li>                              
       68 -///   <li>A length-delimited element (string, BigInteger, sub-
          -object, nested list) is                                       
       69 -///       prefixed by a varint <c>length + 1</c>, where a stor
          -ed 0 denotes a null element.</li>                             
       91 +///   <li>Non-nullable scalar elements are stored <i>packed</i
          +>: field 1 is one                                             
       92 +///       length-delimited block of concatenated scalar values
          +, exactly like Protobuf's                                     
       93 +///       packed repeated encoding.</li>                      
       94 +///   <li>Non-nullable length-delimited elements (<c>decimal</
          +c>, BigInteger, sub-objects                                   
       95 +///       synchronized with <see cref="ObjectMode.NotNull"/>) 
          +are stored as one field-1                                     
       96 +///       entry per element, like an ordinary repeated field.<
          +/li>                                                          
       97 +///   <li>Nullable elements (strings, nullable scalars, nullab
          +le sub-objects, nested                                        
       98 +///       lists) are each wrapped in a single-field message <c
          +>{ optional T value = 1; }</c>                                
       99 +///       — the same idea as Protobuf's well-known wrapper typ
          +es — where an empty wrapper                                   
      100 +///       represents a null element. This keeps null distingui
          +shable from default values                                    
      101 +///       such as the empty string.</li>                      
      102  ///   </ul>
       71 -///   The reader knows the payload's byte range from the outer
          - length prefix, so it detects                                 
       72 -///   the end of a list when the read cursor reaches that boun
          -dary                                                          
       73 -///   (<see cref="ISyncManager.ReachedEndOfList"/>). This is a
          - slight divergence from                                       
       74 -///   idiomatic Protobuf, where a repeated message field re-em
          -its its tag per element; here                                 
       75 -///   a list is a self-contained length-delimited container, w
          -hich lets SyncLib represent                                   
       76 -///   nested lists, null elements and heterogeneous tuples uni
          -formly. Lists of packed                                       
       77 -///   scalars are byte-compatible with Protobuf's packed-repea
          -ted encoding.                                                 
      103 +///   <c>byte[]</c> and other byte lists are not stored as lis
          +t containers at all; they                                     
      104 +///   are written as a single Protobuf <c>bytes</c> value.    
      105  ///
      106  /// <h3>Deduplication and cyclic graphs</h3>
      107  ///
       81 -///   When a field or element is synchronized with <see cref="
          -ObjectMode.Deduplicate"/>,                                    
       82 -///   the LEN payload begins with a one-byte marker (0 = first
          - occurrence, 1 =                                              
       83 -///   back-reference) followed by a varint object ID. A first 
          -occurrence is followed by the                                 
       84 -///   object body; a back-reference is not. This lets <see cre
          -f="SyncProtobuf"/> serialize                                  
       85 -///   shared references and cyclic object graphs (see the Jack
          --and-Jill example in                                          
       86 -///   <see cref="ISyncManager"/>). These markers are a SyncLib
          - extension and are only                                       
       87 -///   present when deduplication is requested.                
      108 +///   A field or element synchronized with <see cref="ObjectMo
          +de.Deduplicate"/> is stored                                   
      109 +///   as a <i>reference wrapper</i> message <c>{ uint64 id = 1
          +; T value = 2; }</c>: the                                     
      110 +///   first occurrence of an object stores both its ID and its
          + value, and each later                                        
      111 +///   occurrence stores only the ID. This lets SyncProtobuf se
          +rialize shared references                                     
      112 +///   and cyclic object graphs (see the Jack-and-Jill example 
          +in <see cref="ISyncManager"/>)                                
      113 +///   while remaining valid Protobuf. Deduplication also works
          + for strings and byte                                         
      114 +///   arrays.                                                 
      115 +///   <para/>                                                 
      116 +///   <b>Caution:</b> because the wrapper is only present when
          + Deduplicate is requested,                                    
      117 +///   the writer and reader must agree on which fields use Ded
          +uplicate. Unlike                                              
      118 +///   <see cref="SyncBinary"/> (whose optional markers make th
          +e flag self-describing),                                      
      119 +///   toggling <see cref="ObjectMode.Deduplicate"/> is a break
          +ing change to the data                                        
      120 +///   stream.                                                 
      121 +///                                                           
      122 +/// <h3>Type tags</h3>                                        
      123 +///                                                           
      124 +///   <see cref="ISyncManager.SyncTypeTag(string?)"/> stores t
          +he tag as a string field                                      
      125 +///   with the reserved number 536,870,911 (<c>_type</c> in ge
          +nerated schemas). Protobuf                                    
      126 +///   parsers that don't know about it simply skip it.        
      127  /// </remarks>
      128  public partial class SyncProtobuf
      129  {
     ...
      138      I32 = 5,      // fixed32, sfixed32, float
      139    }
      140  
      102 -  /// <summary>The field number used to store an object's type
          - tag (see                                                     
      103 -  ///   <see cref="ISyncManager.SyncTypeTag(string?)"/>). Prot
          -obuf reserves field numbers                                   
      104 -  ///   19000-19999, so this value never collides with a user 
          -field number.</summary>                                       
      105 -  internal const int TypeTagFieldNumber = 19000;              
      141 +  /// <summary>The largest field number permitted by Protocol 
          +Buffers (2^29 - 1).</summary>                                 
      142 +  internal const int MaxFieldNumber = 536_870_911;            
      143  
      107 -  /// <summary>Object framing marker (first byte of every sub-
          -object/list/tuple LEN                                         
      108 -  ///   payload): the object is not deduplicated; its body fol
          -lows immediately. Making                                      
      109 -  ///   this marker always present lets the reader detect dedu
          -plication from the data                                       
      110 -  ///   itself, so the <see cref="ObjectMode.Deduplicate"/> fl
          -ag may be toggled between                                     
      111 -  ///   writing and reading (as with <see cref="SyncBinary"/>)
          -.</summary>                                                   
      112 -  internal const byte DedupNone = 0;                          
      113 -  /// <summary>Object framing marker: first occurrence of a de
          -duplicated object; a varint                                   
      114 -  ///   object ID and then the body follow.</summary>         
      115 -  internal const byte DedupFirst = 1;                         
      116 -  /// <summary>Object framing marker: a back-reference to a pr
          -eviously written object;                                      
      117 -  ///   only a varint object ID follows and no body is present
          -.</summary>                                                   
      118 -  internal const byte DedupBackRef = 2;                       
      144 +  /// <summary>The largest field number available to user fiel
          +ds. The two numbers above                                     
      145 +  ///   it are reserved for <see cref="TypeTagFieldNumber"/> a
          +nd                                                            
      146 +  ///   <see cref="PresentFieldNumber"/>.</summary>           
      147 +  internal const int MaxUserFieldNumber = MaxFieldNumber - 2; 
      148 +                                                              
      149 +  /// <summary>The reserved field number used to store an obje
          +ct's type tag (see                                            
      150 +  ///   <see cref="ISyncManager.SyncTypeTag(string?)"/>) as a 
          +string field. This is the                                     
      151 +  ///   maximum legal Protobuf field number; generated schemas
          + declare it as                                                
      152 +  ///   <c>optional string _type</c>.</summary>               
      153 +  internal const int TypeTagFieldNumber = MaxFieldNumber;     
      154 +                                                              
      155 +  /// <summary>The reserved field number of the boolean marker
          + written when a non-null                                      
      156 +  ///   root object would otherwise serialize to zero bytes (w
          +hich is the encoding of a                                     
      157 +  ///   null root). Generated schemas declare it as <c>optiona
          +l bool _present</c>.</summary>                                
      158 +  internal const int PresentFieldNumber = MaxFieldNumber - 1; 
      159  }

● Now Options.cs — removing the dead reader options.

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Options.cs)
  ⎿  Added 12 lines, removed 24 lines
      15    /// <remarks>
      16    ///   <see cref="Reader"/> and <see cref="Writer"/> do not co
          py this object, so changing
      17    ///   its properties affects any reader/writer that was const
          ructed with it.
      18 +  ///   <para/>                                                
      19 +  ///   Note: reader behaviors that are options in other format
         +s are fixed here by                                            
      20 +  ///   Protobuf semantics: an absent field read as a non-nulla
         +ble primitive returns the                                      
      21 +  ///   type's default value, integers too large for the reques
         +ted type are truncated to                                      
      22 +  ///   its low bits (as Protobuf parsers do), and the root mes
         +sage always occupies the                                       
      23 +  ///   entire input.                                          
      24    /// </remarks>
      25    public class Options
      26    {
     ...
      33      /// <summary>The <see cref="ObjectMode"/> used to read/writ
          e the root object.
      34      ///   This option has no effect if you use <see cref="NewWr
          iter"/> or
      35      ///   <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"
          /> directly.</summary>
      36 +    /// <remarks>The same mode must be used when writing and wh
         +en reading; in                                                 
      37 +    ///   particular, toggling <see cref="ObjectMode.Deduplicat
         +e"/> changes the wire                                          
      38 +    ///   format of the root object (see <see cref="SyncProtobu
         +f"/>).</remarks>                                               
      39      public ObjectMode RootMode { get; set; } = ObjectMode.Norma
          l;
      40  
      41      #region Writer-specific options
     ...
      44  
      45      public class ForWriter
      46      {
      38 -      /// <summary>Initial size of the output buffer, in bytes 
         -(default: 512). Ignored                                        
      39 -      ///   if you provide your own <see cref="System.Buffers.I
         -BufferWriter{T}"/> to                                          
      40 -      ///   <see cref="NewWriter"/> (the writer still uses an i
         -nternal contiguous                                             
      41 -      ///   buffer for length back-patching, but this controls 
         -its initial size).</summary>                                   
      47 +      /// <summary>Initial size, in bytes, of the writer's inte
         +rnal contiguous                                                
      48 +      ///   buffer, and of the output buffer that <see cref="Sy
         +ncProtobuf.Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/> 
      49 +      ///   allocates (default: 512).</summary>                
      50        public int InitialBufferSize { get; set; } = 512;
      51      }
      52  
      53      #endregion
      46 -                                                               
      47 -    #region Reader-specific options                            
      48 -                                                               
      49 -    public ForReader Read { get; set; } = new ForReader();     
      50 -                                                               
      51 -    public class ForReader                                     
      52 -    {                                                          
      53 -      /// <summary>If true, a null in the data stream that is r
         -ead as a non-nullable                                          
      54 -      ///   primitive returns the type's default value instead 
         -of throwing                                                    
      55 -      ///   <see cref="FormatException"/>. Because absent field
         -s are already treated as                                       
      56 -      ///   null/default, this rarely matters.</summary>       
      57 -      public bool ReadNullPrimitivesAsDefault { get; set; } = t
         -rue;                                                           
      58 -                                                               
      59 -      /// <summary>When true (the default) and the root object 
         -has been read                                                  
      60 -      ///   successfully, the reader verifies that the data str
         -eam has ended, throwing                                        
      61 -      ///   <see cref="FormatException"/> if trailing bytes rem
         -ain.</summary>                                                 
      62 -      public bool VerifyEof { get; set; } = true;              
      63 -    }                                                          
      64 -                                                               
      65 -    #endregion                                                 
      54    }
      55  }

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Writer.cs)
  ⎿  Added 3 lines, removed 3 lines
      93  
      94      public string? Sync(FieldId name, string? savable, ObjectMo
          de mode = ObjectMode.Normal)
      95      {
      96 -      _s.WriteStringField(name, savable);                      
      96 +      _s.WriteStringField(name, savable, mode);                
      97        return savable;
      98      }
      99  
     ...
      144        where Scanner : IScanner<byte>
      145        where ListBuilder : IListBuilder<List, byte>
      146      {
      147 -      var saver = new ScannerSaver<Writer, Scanner, byte, Sync
          -Primitive<Writer>>(new SyncPrimitive<Writer>(), mode);        
      148 -      saver.Write(ref this, name, scanner!, saving, tupleLengt
          -h);                                                           
      147 +      // Byte lists are stored as a Protobuf `bytes` value, no
          +t as a list container                                         
      148 +      _s.WriteByteListField(name, scanner, saving, mode);     
      149        return saving;
      150      }
      151  

● Now the Reader facade.

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Reader.cs)
  ⎿  Added 14 lines, removed 9 lines
      18    {
      19      options ??= _defaultOptions;
      20      Reader reader = NewReader(input, options);
      21 -    var result = SyncManagerExt.Sync(reader, null, default(T), 
         -sync, options.RootMode);                                       
      22 -    reader._s.VerifyEof();                                     
      23 -    return result;                                             
      21 +    return SyncManagerExt.Sync(reader, null, default(T), sync, 
         +options.RootMode);                                             
      22    }
      23    public static T? ReadI<T>(ReadOnlyMemory<byte> input, SyncObj
          ectFunc<ISyncManager, T> sync, Options? options = null)
      24    {
      25      options ??= _defaultOptions;
      26      Reader reader = NewReader(input, options);
      29 -    var result = SyncManagerExt.Sync(reader, null, default(T), 
         -sync, options.RootMode);                                       
      30 -    reader._s.VerifyEof();                                     
      31 -    return result;                                             
      27 +    return SyncManagerExt.Sync(reader, null, default(T), sync, 
         +options.RootMode);                                             
      28    }
      29    public static T? Read<T>(byte[] input, SyncObjectFunc<Reader,
           T> sync, Options? options = null)
      30      => Read(input.AsMemory(), sync, options);
     ...
      84      public char   Sync(FieldId name, char savable)   => uncheck
          ed((char)(ushort)_s.ReadUInt(name));
      85  
      86      public string? Sync(FieldId name, string? savable, ObjectMo
          de mode = ObjectMode.Normal)
      91 -      => _s.ReadString(name);                                  
      87 +      => _s.ReadString(name, mode);                            
      88  
      89      public int Sync(FieldId name, int savable, int bits, bool s
          igned = true) => unchecked((int)_s.ReadInt(name));
      90      public long Sync(FieldId name, long savable, int bits, bool
           signed = true) => _s.ReadInt(name);
     ...
      119        where Scanner : IScanner<byte>
      120        where ListBuilder : IListBuilder<List, byte>
      121      {
      126 -      var loader = new ListLoader<Reader, List, byte, ListBuil
          -der, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), build
          -er, mode, tupleLength);                                       
      127 -      return loader.Sync(ref this, name, saving);             
      122 +      // Byte lists are stored as a Protobuf `bytes` value, no
          +t as a list container                                         
      123 +      var (start, length, backref) = _s.ReadByteListField(name
          +, mode);                                                      
      124 +      if (start == -1)                                        
      125 +        return default; // null                               
      126 +      if (start == -2)                                        
      127 +        return builder.CastList(backref!);                    
      128 +      builder.Alloc(length);                                  
      129 +      var span = _s.InputSpan.Slice(start, length);           
      130 +      for (int i = 0; i < length; i++)                        
      131 +        builder.Add(span[i]);                                 
      132 +      return builder.List;                                    
      133      }
      134  
      135      public List? SyncListCharImpl<Scanner, List, ListBuilder>(

● Fixing a precedence bug I noticed in WriterState's error message, then moving
  to the Schema rewrite.

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.WriterState.cs)
  ⎿  Added 2 lines, removed 2 lines
      167        int id = name.Id != int.MinValue ? name.Id : _lastFieldI
           d + 1;
      168        if ((uint)(id - 1) >= MaxUserFieldNumber || (id >= 19000
            && id <= 19999))
      169          throw new ArgumentException(
      170 -          "SyncProtobuf: '{0}' has invalid Protobuf field numb
          -er {1}. Field numbers must be in the range 1 to {2}, excluding
          - 19000-19999 (reserved by Protobuf)."                         
      171 -          .Localized(name.Name ?? "" + id, id, MaxUserFieldNum
          -ber));                                                        
      170 +          "SyncProtobuf: field '{0}' has invalid Protobuf fiel
          +d number {1}. Field numbers must be in the range 1 to {2}, exc
          +luding 19000-19999 (reserved by Protobuf)."                   
      171 +          .Localized(name.Name ?? "(unnamed)", id, MaxUserFiel
          +dNumber));                                                    
      172        _lastFieldId = id;
      173        return id;
      174      }

● Now the biggest piece — the SchemaState rewrite with wrapper-message
  synthesis.

● Write(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.SchemaState.cs)
  ⎿  Added 311 lines, removed 85 lines
       7  
       8  partial class SyncProtobuf
       9  {
      10 -  /// <summary>The kind of a <see cref="ProtoType"/>: a scalar,
         - a reference to a message,                                     
      11 -  ///   or a repeated (list) element type.</summary>           
      12 -  internal enum ProtoKind { Scalar, Message, List }            
      10 +  /// <summary>The kind of a <see cref="ProtoType"/>. Scalar an
         +d Message correspond                                           
      11 +  ///   directly to .proto types; List, Opt and Ref are the con
         +tainer messages that                                           
      12 +  ///   <see cref="SyncProtobuf.Writer"/> uses for lists, nulla
         +ble list elements and                                          
      13 +  ///   deduplicated values (see <see cref="SyncProtobuf"/>). E
         +ach of the latter is                                           
      14 +  ///   rendered as a generated wrapper message type.</summary>
      15 +  internal enum ProtoKind {                                    
      16 +    Scalar,  // e.g. "int32", "string", "bytes"                
      17 +    Message, // a recorded message (identified by MessageKey)  
      18 +    List,    // list container: { repeated Element items = 1; }
      19 +    Opt,     // nullable element wrapper: { optional Element va
         +lue = 1; }                                                     
      20 +    Ref,     // dedup wrapper: { uint64 id = 1; Element value =
         + 2; }                                                          
      21 +  }                                                            
      22  
      23    /// <summary>The type of one field in a recorded <c>.proto</c
          > schema.</summary>
      24    internal class ProtoType
      25    {
      26      public ProtoKind Kind;
      18 -    public string? Scalar;     // Kind == Scalar (e.g. "int32",
         - "string", "bytes")                                            
      27 +    public string? Scalar;     // Kind == Scalar               
      28      public object? MessageKey; // Kind == Message (a Type, or a
           synthetic key for anonymous messages)
      20 -    public ProtoType? Element; // Kind == List (the element typ
         -e)                                                             
      29 +    public ProtoType? Element; // Kind == List/Opt/Ref         
      30  
      31      public static ProtoType MakeScalar(string s) => new ProtoTy
          pe { Kind = ProtoKind.Scalar, Scalar = s };
      32      public static ProtoType MakeMessage(object key) => new Prot
          oType { Kind = ProtoKind.Message, MessageKey = key };
      33      public static ProtoType MakeList(ProtoType? element) => new
           ProtoType { Kind = ProtoKind.List, Element = element };
      25 -                                                               
      26 -    public static bool Equal(ProtoType? a, ProtoType? b)       
      27 -    {                                                          
      28 -      if (ReferenceEquals(a, b)) return true;                  
      29 -      if (a == null || b == null || a.Kind != b.Kind) return fa
         -lse;                                                           
      30 -      return a.Kind switch {                                   
      31 -        ProtoKind.Scalar => a.Scalar == b.Scalar,              
      32 -        ProtoKind.Message => Equals(a.MessageKey, b.MessageKey)
         -,                                                              
      33 -        ProtoKind.List => Equal(a.Element, b.Element),         
      34 -        _ => false,                                            
      35 -      };                                                       
      36 -    }                                                          
      34 +    public static ProtoType MakeOpt(ProtoType element) => new P
         +rotoType { Kind = ProtoKind.Opt, Element = element };          
      35 +    public static ProtoType MakeRef(ProtoType element) => new P
         +rotoType { Kind = ProtoKind.Ref, Element = element };          
      36    }
      37  
      38    internal class ProtoField
     ...
      48    {
      49      public object Key;
      50      public List<ProtoField> Fields = new List<ProtoField>();
      52 -    public string? TagName;      // from SyncTypeTag, or "Anony
         -mous"                                                          
      51 +    public string? TagName;      // from SyncTypeTag, or "Tuple
         +"/"Anonymous"                                                  
      52      public string? AssignedName; // final, unique, sanitized na
          me (assigned at render time)
      53      public bool InProgress;      // true while being recorded (
          breaks cycles)
      54 +    public bool Merged;          // true if merged into an iden
         +tical anonymous message                                        
      55      public ProtoMessage(object key) { Key = key; }
      56    }
      57  
      58    /// <summary>The core of <see cref="SyncProtobuf.Schema"/>: r
          ecords the messages that a
      59    ///   synchronizer describes while running in <see cref="Sync
          Mode.Schema"/> mode, then
      60 -  ///   renders them as a proto3 <c>.proto</c> document.</summa
         -ry>                                                            
      60 +  ///   renders them as a proto3 <c>.proto</c> document that ex
         +actly describes the wire                                       
      61 +  ///   output of <see cref="SyncProtobuf.Writer"/>.</summary> 
      62    internal class SchemaState
      63    {
      64      internal Options _opt;
     ...
       69  
       70      class Frame
       71      {
       72 +      // Message frame (objects and tuples):                  
       73        public ProtoMessage? Msg;       // message that receives
            fields
       72 -      public ProtoType? ListType;     // if IsList: the list t
          -ype whose Element we fill                                     
       73 -      public bool IsList, IsTuple;                            
       74 +      public bool IsTuple;                                    
       75 +      public int LastFieldId;                                 
       76 +      public ProtoMessage? Def;       // definition recorded b
          +y this frame                                                  
       77 +      public ProtoMessage? CompareTo; // if Def is a scratch r
          +e-record, the original                                        
       78 +      // List frame:                                          
       79 +      public ProtoType? ListType;     // the List type whose E
          +lement we fill                                                
       80 +      public bool IsList;                                     
       81        public int ItemCount;
       82        public int ExpectedItems = 1;
       76 -      public int LastFieldId;                                 
       77 -      public ProtoMessage? Def;        // definition recorded 
          -by this frame                                                 
       78 -      public ProtoMessage? CompareTo;  // if Def is a scratch 
          -re-record, the original                                       
       83      }
       84  
       81 -    Dictionary<object, ProtoMessage> _defs = new Dictionary<ob
          -ject, ProtoMessage>();                                        
       82 -    List<ProtoMessage> _defsInOrder = new List<ProtoMessage>()
          -;                                                             
       83 -    List<Frame> _stack = new List<Frame>();                   
       85 +    readonly Dictionary<object, ProtoMessage> _defs = new Dict
          +ionary<object, ProtoMessage>();                               
       86 +    readonly List<ProtoMessage> _defsInOrder = new List<ProtoM
          +essage>();                                                    
       87 +    readonly List<Frame> _stack = new List<Frame>();          
       88 +    ProtoType? _rootType;           // the type of the root va
          +lue (Ref(...) if RootMode deduplicates)                       
       89 +    ProtoMessage? _rootPlainDef;    // the root message, if wr
          +itten bare (gets the _present field)                          
       90  
       91      public SchemaState(Options options) { _opt = options; }
       92  
       93      Frame? Top => _stack.Count > 0 ? _stack[_stack.Count - 1] 
           : null;
       94  
       95      public int Depth => _stack.Count;
       90 -    public bool IsInsideList => Top?.IsList ?? false;         
       96 +    public bool IsInsideList => Top != null && (Top.IsList || 
          +Top.IsTuple);                                                 
       97      public bool? ReachedEndOfList {
       98        get {
       99          var top = Top;
       94 -        return top != null && top.IsList ? top.ItemCount >= to
          -p.ExpectedItems : (bool?)null;                                
      100 +        if (top == null || !top.IsList)                       
      101 +          return null; // tuples: length is not stored (mirror
          +s the reader)                                                 
      102 +        return top.ItemCount >= top.ExpectedItems;            
      103        }
      104      }
       97 -    public int? MinimumListLength => IsInsideList ? 0 : (int?)
          -null;                                                         
      105 +    public int? MinimumListLength => Top != null && Top.IsList
          + ? 0 : (int?)null;                                            
      106  
      107      #region Recording
      108  
      109      int ResolveNumber(Frame f, FieldId field)
      110      {
      111        int id = field.Id != int.MinValue ? field.Id : f.LastFie
           ldId + 1;
      112 +      if ((uint)(id - 1) >= MaxUserFieldNumber || (id >= 19000
          + && id <= 19999))                                             
      113 +        throw new ArgumentException(                          
      114 +          "SyncProtobuf: field '{0}' has invalid Protobuf fiel
          +d number {1}. Field numbers must be in the range 1 to {2}, exc
          +luding 19000-19999 (reserved by Protobuf)."                   
      115 +          .Localized(field.Name ?? "(unnamed)", id, MaxUserFie
          +ldNumber));                                                   
      116        f.LastFieldId = id;
      117        return id;
      118      }
      119  
      108 -    // Attaches a sub-object/list field to its parent, unless 
          -this is the root value                                        
      109 -    // (which has no parent to attach to — it is just recorded
          - as a top-level message).                                     
      120 +    // Attaches a field/element to the current message or list
          +, unless this is the                                          
      121 +    // root value (which has no parent; it is just recorded as
          + a top-level message).                                        
      122      void AttachRef(FieldId field, ProtoType type, bool optiona
           l)
      123      {
      124        if (Top != null)
     ...
      133            "SyncProtobuf.Schema cannot describe a value at dept
           h zero. " +
      134            "Wrap it in a root object (e.g. use SyncProtobuf.Wri
           teSchema).");
      135        if (top.IsList) {
      124 -        // A schema saver visits one element (or, for a tuple,
          - ExpectedItems of them);                                      
      125 -        // the element type becomes the repeated field's eleme
          -nt type.                                                      
      136 +        // In the wire format, nullable elements are wrapped: 
          +{ optional T value = 1; }                                     
      137 +        var elemType = optional ? ProtoType.MakeOpt(type) : ty
          +pe;                                                           
      138          top.ItemCount++;
      127 -        top.ListType!.Element = type;                         
      139 +        if (top.ListType!.Element == null)                    
      140 +          top.ListType.Element = elemType;                    
      141 +        else if (!TypeEqual(top.ListType.Element, elemType))  
      142 +          throw new InvalidOperationException(                
      143 +            "SyncProtobuf.Schema: a list was synchronized with
          + elements of more than one type. " +                          
      144 +            "All elements of a list must be synchronized the s
          +ame way (use a tuple for heterogeneous sequences).");         
      145        } else {
      146          int num = ResolveNumber(top, field);
      147          top.Msg!.Fields.Add(new ProtoField {
     ...
      150        }
      151      }
      152  
      136 -    public void SyncScalar(FieldId field, string protoType, bo
          -ol optional)                                                  
      137 -      => Attach(field, ProtoType.MakeScalar(protoType), option
          -al);                                                          
      153 +    public void SyncScalar(FieldId field, string protoType, bo
          +ol optional, bool dedup = false)                              
      154 +    {                                                         
      155 +      if (dedup)                                              
      156 +        Attach(field, ProtoType.MakeRef(ProtoType.MakeScalar(p
          +rotoType)), optional: false);                                 
      157 +      else                                                    
      158 +        Attach(field, ProtoType.MakeScalar(protoType), optiona
          +l);                                                           
      159 +    }                                                         
      160  
      139 -    // For SyncListBoolImpl/Byte/Char, which do not call Begin
          -SubObject.                                                    
      140 -    public void SyncScalarList(FieldId field, string elemProto
          -Type)                                                         
      141 -      => Attach(field, ProtoType.MakeList(ProtoType.MakeScalar
          -(elemProtoType)), optional: false);                           
      142 -                                                              
      161      public (bool Begun, int Length, object? Object) BeginSubOb
           ject(FieldId field, object? childKey, ObjectMode mode, int lis
           tLength)
      162      {
      145 -      if ((mode & ObjectMode.List) != 0) {                    
      163 +      bool isTuple = (mode & ObjectMode.Tuple) == ObjectMode.T
          +uple;                                                         
      164 +      bool isList = !isTuple && (mode & ObjectMode.List) != 0;
      165 +      bool dedup = (mode & ObjectMode.Deduplicate) != 0;      
      166 +      bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
          +.Deduplicate)) != ObjectMode.NotNull;                         
      167 +                                                              
      168 +      if (Top == null && !isList && !isTuple && _rootType == n
          +ull) {                                                        
      169 +        // Recording the root object; note its type for the sc
          +hema's root comment                                           
      170 +        var begun = BeginMessage(field, childKey, dedup, nulla
          +ble, isTuple: false);                                         
      171 +        // (BeginMessage sets _rootType via the rootHook becau
          +se Top was null)                                              
      172 +        return begun;                                         
      173 +      }                                                       
      174 +                                                              
      175 +      if (isList) {                                           
      176          if (Top == null)
      147 -          throw new InvalidOperationException(                
      148 -            "SyncProtobuf.Schema: the root value must be a mes
          -sage/object, not a list.");                                   
      149 -        bool isTuple = (mode & ObjectMode.Tuple) == ObjectMode
          -.Tuple;                                                       
      177 +          throw new NotSupportedException(                    
      178 +            "SyncProtobuf: the root value must be an object (a
          + Protobuf message), not a list or tuple.");                   
      179          var listType = ProtoType.MakeList(null);
      151 -        Attach(field, listType, optional: false);             
      180 +        ProtoType fieldType = dedup ? ProtoType.MakeRef(listTy
          +pe) : listType;                                               
      181 +        // A null list is an omitted field / empty Opt wrapper
          +; the list field                                              
      182 +        // itself is a message type, so no `optional` keyword 
          +is involved.                                                  
      183 +        Attach(field, fieldType, optional: nullable && !dedup 
          +&& Top.IsList);                                               
      184          _stack.Add(new Frame {
      153 -          IsList = true, IsTuple = isTuple, ListType = listTyp
          -e,                                                            
      154 -          ExpectedItems = isTuple ? (listLength >= 0 ? listLen
          -gth : int.MaxValue) : 1,                                      
      185 +          IsList = true, ListType = listType,                 
      186 +          ExpectedItems = 1, // a schema saver visits one elem
          +ent per list                                                  
      187          });
      156 -        return (true, isTuple && listLength >= 0 ? listLength 
          -: 1, null);                                                   
      188 +        return (true, 1, null);                               
      189        }
      190  
      191 +      if (Top == null && isTuple)                             
      192 +        throw new NotSupportedException(                      
      193 +          "SyncProtobuf: the root value must be an object (a P
          +rotobuf message), not a list or tuple.");                     
      194 +                                                              
      195 +      return BeginMessage(field, childKey, dedup, nullable, is
          +Tuple);                                                       
      196 +    }                                                         
      197 +                                                              
      198 +    (bool Begun, int Length, object? Object) BeginMessage(Fiel
          +dId field, object? childKey, bool dedup, bool nullable, bool i
          +sTuple)                                                       
      199 +    {                                                         
      200        // In Schema mode a non-Type childKey is only a sample i
           nstance; its type
      201        // identifies the message (see ObjectSyncher and the ISy
           ncManager docs).
      202        if (childKey != null && !(childKey is Type))
      203          childKey = childKey.GetType();
      204  
      205 +      bool atRoot = Top == null;                              
      206 +                                                              
      207        if (childKey == null) {
      165 -        // Anonymous message: recorded inline, not deduplicate
          -d (so it cannot recur).                                       
      208 +        // Anonymous message (typically a tuple): recorded inl
          +ine, not deduplicated                                         
      209          if (Depth >= MaxAnonymousDepth)
      210            throw new InvalidOperationException(
      211              "SyncProtobuf.Schema: object nesting is too deep. 
           This usually means a recursive type " +
      212              "was synchronized via BeginSubObject with childKey
            == null; in Schema mode childKey should " +
      213              "identify the type (helper methods pass typeof(T))
           .");
      214          var anonKey = new object();
      172 -        var anon = new ProtoMessage(anonKey) { TagName = "Anon
          -ymous", InProgress = true };                                  
      215 +        var anon = new ProtoMessage(anonKey) { TagName = isTup
          +le ? "Tuple" : "Anonymous", InProgress = true };              
      216          _defs.Add(anonKey, anon);
      217          _defsInOrder.Add(anon);
      175 -        AttachRef(field, ProtoType.MakeMessage(anonKey), optio
          -nal: false);                                                  
      176 -        _stack.Add(new Frame { Msg = anon, Def = anon });     
      218 +        AttachMessageRef(field, anonKey, dedup, nullable, atRo
          +ot);                                                          
      219 +        _stack.Add(new Frame { Msg = anon, Def = anon, IsTuple
          + = isTuple });                                                
      220          return (true, 1, null);
      221        }
      222  
      223        if (_defs.TryGetValue(childKey, out var existing)) {
      224          if (existing.InProgress) {
      225            // A cycle (or a repeat while re-recording): referen
           ce without recording again.
      183 -          AttachRef(field, ProtoType.MakeMessage(childKey), op
          -tional: false);                                               
      184 -          return (false, 0, DeclinedObject(childKey, mode));  
      226 +          AttachMessageRef(field, childKey, dedup, nullable, a
          +tRoot);                                                       
      227 +          return (false, 0, DeclinedObject(childKey, dedup, nu
          +llable));                                                     
      228          }
      229          // Re-record into a scratch definition and compare at 
           EndSubObject, to catch a
      230          // type that is synchronized in two conflicting ways.
      231          existing.InProgress = true;
      232          var scratch = new ProtoMessage(childKey);
      190 -        AttachRef(field, ProtoType.MakeMessage(childKey), opti
          -onal: false);                                                 
      191 -        _stack.Add(new Frame { Msg = scratch, Def = scratch, C
          -ompareTo = existing });                                       
      233 +        AttachMessageRef(field, childKey, dedup, nullable, atR
          +oot);                                                         
      234 +        _stack.Add(new Frame { Msg = scratch, Def = scratch, C
          +ompareTo = existing, IsTuple = isTuple });                    
      235          return (true, 1, null);
      236        }
      237  
      195 -      var newDef = new ProtoMessage(childKey) { InProgress = t
          -rue };                                                        
      238 +      var newDef = new ProtoMessage(childKey) { InProgress = t
          +rue, TagName = isTuple ? "Tuple" : null };                    
      239        _defs.Add(childKey, newDef);
      240        _defsInOrder.Add(newDef);
      198 -      AttachRef(field, ProtoType.MakeMessage(childKey), option
          -al: false);                                                   
      199 -      _stack.Add(new Frame { Msg = newDef, Def = newDef });   
      241 +      AttachMessageRef(field, childKey, dedup, nullable, atRoo
          +t);                                                           
      242 +      _stack.Add(new Frame { Msg = newDef, Def = newDef, IsTup
          +le = isTuple });                                              
      243 +      if (atRoot) {                                           
      244 +        _rootType = dedup ? ProtoType.MakeRef(ProtoType.MakeMe
          +ssage(childKey)) : ProtoType.MakeMessage(childKey);           
      245 +        if (!dedup)                                           
      246 +          _rootPlainDef = newDef;                             
      247 +      }                                                       
      248        return (true, 1, null);
      249      }
      250  
      251 +    void AttachMessageRef(FieldId field, object key, bool dedu
          +p, bool nullable, bool atRoot)                                
      252 +    {                                                         
      253 +      if (atRoot) {                                           
      254 +        if (_rootType == null) {                              
      255 +          _rootType = dedup ? ProtoType.MakeRef(ProtoType.Make
          +Message(key)) : ProtoType.MakeMessage(key);                   
      256 +          if (!dedup && _defs.TryGetValue(key, out var rootDef
          +))                                                            
      257 +            _rootPlainDef = rootDef;                          
      258 +        }                                                     
      259 +        return;                                               
      260 +      }                                                       
      261 +      ProtoType type = ProtoType.MakeMessage(key);            
      262 +      if (dedup)                                              
      263 +        type = ProtoType.MakeRef(type);                       
      264 +      // Nullable message ELEMENTS are wrapped in an Opt messa
          +ge on the wire;                                               
      265 +      // nullable message FIELDS are simply omitted (message f
          +ields have presence).                                         
      266 +      bool wrapAsOpt = nullable && !dedup && Top != null && To
          +p.IsList;                                                     
      267 +      Attach(field, type, optional: wrapAsOpt);               
      268 +    }                                                         
      269 +                                                              
      270      public void EndSubObject()
      271      {
      272        if (_stack.Count == 0)
     ...
      294        var top = Top;
      295        if (top == null || top.IsList)
      296          throw new InvalidOperationException("SyncTypeTag can o
           nly be used inside an object (not a list).");
      230 -      if (top.Def != null && tag != null)                     
      297 +      if (top.Def != null && tag != null) {                   
      298          top.Def.TagName ??= tag;
      299 +        // The tag is stored on the wire as a string field wit
          +h a reserved number                                           
      300 +        if (top.Msg != null && !HasField(top.Msg, TypeTagField
          +Number))                                                      
      301 +          top.Msg.Fields.Add(new ProtoField {                 
      302 +            Name = "_type", Number = TypeTagFieldNumber, Optio
          +nal = true,                                                   
      303 +            Type = ProtoType.MakeScalar("string"),            
      304 +          });                                                 
      305 +      }                                                       
      306        return tag;
      307      }
      308 +    static bool HasField(ProtoMessage msg, int number)        
      309 +    {                                                         
      310 +      foreach (var f in msg.Fields)                           
      311 +        if (f.Number == number)                               
      312 +          return true;                                        
      313 +      return false;                                           
      314 +    }                                                         
      315  
      235 -    static bool FieldsEqual(List<ProtoField> a, List<ProtoFiel
          -d> b)                                                         
      316 +    bool FieldsEqual(List<ProtoField> a, List<ProtoField> b)  
      317      {
      318        if (a.Count != b.Count)
      319          return false;
      320        for (int i = 0; i < a.Count; i++) {
      321          if (a[i].Name != b[i].Name || a[i].Number != b[i].Numb
           er || a[i].Optional != b[i].Optional
      241 -          || !ProtoType.Equal(a[i].Type, b[i].Type))          
      322 +          || !TypeEqual(a[i].Type, b[i].Type))                
      323            return false;
      324        }
      325        return true;
      326      }
      327  
      328 +    // Structural type equality. Two anonymous messages (e.g. 
          +two occurrences of the                                        
      329 +    // same tuple shape) are equal if their definitions are st
          +ructurally equal.                                             
      330 +    bool TypeEqual(ProtoType? a, ProtoType? b)                
      331 +    {                                                         
      332 +      if (ReferenceEquals(a, b)) return true;                 
      333 +      if (a == null || b == null || a.Kind != b.Kind) return f
          +alse;                                                         
      334 +      switch (a.Kind) {                                       
      335 +        case ProtoKind.Scalar: return a.Scalar == b.Scalar;   
      336 +        case ProtoKind.Message:                               
      337 +          if (Equals(a.MessageKey, b.MessageKey)) return true;
      338 +          if (a.MessageKey is Type && b.MessageKey is Type) re
          +turn false;                                                   
      339 +          return MessagesEqual(a.MessageKey!, b.MessageKey!); 
      340 +        default: return TypeEqual(a.Element, b.Element);      
      341 +      }                                                       
      342 +    }                                                         
      343 +    bool MessagesEqual(object keyA, object keyB)              
      344 +    {                                                         
      345 +      if (!_defs.TryGetValue(keyA, out var a) || !_defs.TryGet
          +Value(keyB, out var b))                                       
      346 +        return false;                                         
      347 +      return a.TagName == b.TagName && FieldsEqual(a.Fields, b
          +.Fields);                                                     
      348 +    }                                                         
      349 +                                                              
      350      // Chooses the Object returned when BeginSubObject decline
           s because the type is
      351      // already (being) recorded. ObjectSyncher casts this to T
           , so for a value type
      352      // return a boxed default(T) rather than null (unless the 
           caller avoided boxing).
      250 -    static object? DeclinedObject(object childKey, ObjectMode 
          -mode)                                                         
      353 +    static object? DeclinedObject(object childKey, bool dedup,
          + bool nullable)                                               
      354      {
      252 -      bool avoidBoxing = (mode & (ObjectMode.Deduplicate | Obj
          -ectMode.NotNull)) == ObjectMode.NotNull;                      
      355 +      bool avoidBoxing = !dedup && !nullable;                 
      356        if (!avoidBoxing && childKey is Type type && type.IsValu
           eType)
      357          return Activator.CreateInstance(type);
      358        return null;
     ...
      362  
      363      #region Rendering
      364  
      365 +    // Wrapper messages generated for List/Opt/Ref types, keye
          +d by structural signature                                     
      366 +    Dictionary<string, (string Name, ProtoType Type)>? _wrappe
          +rs;                                                           
      367 +    List<(string Name, ProtoType Type)>? _wrappersInOrder;    
      368 +    HashSet<string>? _usedNames;                              
      369 +                                                              
      370      public void Render(IBufferWriter<byte> output)
      371      {
      372 +      MergeAnonymousDuplicates();                             
      373 +      _usedNames = new HashSet<string>();                     
      374 +      _wrappers = new Dictionary<string, (string, ProtoType)>(
          +);                                                            
      375 +      _wrappersInOrder = new List<(string, ProtoType)>();     
      376        AssignNames();
      377  
      378 +      // Synthesize wrapper messages for every List/Opt/Ref ty
          +pe, in a deterministic order                                  
      379 +      foreach (var def in _defsInOrder)                       
      380 +        if (!def.Merged)                                      
      381 +          foreach (var f in def.Fields)                       
      382 +            TypeRefName(f.Type);                              
      383 +      string? rootName = _rootType != null ? TypeRefName(_root
          +Type) : null;                                                 
      384 +                                                              
      385        var sb = new StringBuilder();
      386        sb.Append("// Generated by Loyc.SyncLib SyncProtobuf.Sch
           ema.\n");
      387        sb.Append("// A proto3 schema describing the messages th
           at SyncProtobuf.Writer produces.\n");
      388 +      if (rootName != null)                                   
      389 +        sb.Append("// The root of the serialized data is one "
          +).Append(rootName).Append(" message.\n");                     
      390        sb.Append("syntax = \"proto3\";\n");
      391  
      392        foreach (var def in _defsInOrder) {
      393 +        if (def.Merged)                                       
      394 +          continue;                                           
      395          sb.Append('\n');
      396          sb.Append("message ").Append(def.AssignedName).Append(
           " {");
      274 -        if (def.Fields.Count == 0) {                          
      397 +        bool hasPresent = def == _rootPlainDef;               
      398 +        if (def.Fields.Count == 0 && !hasPresent) {           
      399            sb.Append("}\n");
      400            continue;
      401          }
      402          sb.Append('\n');
      279 -        foreach (var f in def.Fields) {                       
      280 -          sb.Append("  ");                                    
      281 -          if (f.Type.Kind == ProtoKind.List)                  
      282 -            sb.Append("repeated ");                           
      283 -          else if (f.Optional)                                
      284 -            sb.Append("optional ");                           
      285 -          sb.Append(TypeName(f.Type.Kind == ProtoKind.List ? f
          -.Type.Element! : f.Type));                                    
      286 -          sb.Append(' ').Append(f.Name).Append(" = ").Append(f
          -.Number).Append(";\n");                                       
      403 +        foreach (var f in def.Fields)                         
      404 +          AppendField(sb, f.Type, f.Optional, false, f.Name, f
          +.Number);                                                     
      405 +        if (hasPresent) {                                     
      406 +          // Written only when the root message body would oth
          +erwise be empty                                               
      407 +          // (zero bytes is the encoding of a null root)      
      408 +          AppendField(sb, ProtoType.MakeScalar("bool"), true, 
          +false, "_present", PresentFieldNumber);                       
      409          }
      410          sb.Append("}\n");
      411        }
      412  
      413 +      // _wrappersInOrder can grow while wrappers reference ot
          +her wrappers, but all                                         
      414 +      // were synthesized above, so a plain loop suffices     
      415 +      foreach (var (name, type) in _wrappersInOrder) {        
      416 +        sb.Append('\n');                                      
      417 +        sb.Append("message ").Append(name).Append(" {\n");    
      418 +        switch (type.Kind) {                                  
      419 +          case ProtoKind.List:                                
      420 +            AppendField(sb, type.Element!, false, true, "items
          +", 1);                                                        
      421 +            break;                                            
      422 +          case ProtoKind.Opt:                                 
      423 +            AppendField(sb, type.Element!, true, false, "value
          +", 1);                                                        
      424 +            break;                                            
      425 +          default:                                            
      426 +            AppendField(sb, ProtoType.MakeScalar("uint64"), fa
          +lse, false, "id", 1);                                         
      427 +            AppendField(sb, type.Element!, true, false, "value
          +", 2);                                                        
      428 +            break;                                            
      429 +        }                                                     
      430 +        sb.Append("}\n");                                     
      431 +      }                                                       
      432 +                                                              
      433        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
      434        var span = output.GetSpan(bytes.Length);
      435        bytes.CopyTo(span);
      436        output.Advance(bytes.Length);
      437      }
      438  
      297 -    string TypeName(ProtoType type)                           
      439 +    void AppendField(StringBuilder sb, ProtoType type, bool op
          +tional, bool repeated, string name, int number)               
      440      {
      441 +      sb.Append("  ");                                        
      442 +      if (repeated)                                           
      443 +        sb.Append("repeated ");                               
      444 +      else if (optional && type.Kind == ProtoKind.Scalar)     
      445 +        sb.Append("optional "); // message-typed fields alread
          +y have explicit presence                                      
      446 +      sb.Append(TypeRefName(type)).Append(' ').Append(name).Ap
          +pend(" = ").Append(number).Append(";\n");                     
      447 +    }                                                         
      448 +                                                              
      449 +    // Returns the .proto type name for a field of type `type`
          +, generating a wrapper                                        
      450 +    // message for List/Opt/Ref types on first use.           
      451 +    string TypeRefName(ProtoType type)                        
      452 +    {                                                         
      453        switch (type.Kind) {
      300 -        case ProtoKind.Scalar: return type.Scalar!;           
      301 -        case ProtoKind.Message: return _defs[type.MessageKey!]
          -.AssignedName!;                                               
      454 +        case ProtoKind.Scalar:                                
      455 +          return type.Scalar!;                                
      456 +        case ProtoKind.Message:                               
      457 +          return _defs[Canonical(type.MessageKey!)].AssignedNa
          +me!;                                                          
      458          default:
      303 -          throw new NotSupportedException(                    
      304 -            "SyncProtobuf.Schema cannot express a list whose e
          -lements are themselves lists " +                              
      305 -            "(repeated-of-repeated) in a .proto file. Wrap the
          - inner list in a message type.");                             
      459 +          string sig = Signature(type);                       
      460 +          if (_wrappers!.TryGetValue(sig, out var wrapper))   
      461 +            return wrapper.Name;                              
      462 +          // Name the wrapper after its element, e.g. Int32Lis
          +t, StringOpt, PersonRef                                       
      463 +          string elemName = BaseNameOf(type.Element!);        
      464 +          string suffix = type.Kind == ProtoKind.List ? "List"
          + : type.Kind == ProtoKind.Opt ? "Opt" : "Ref";                
      465 +          string name = UniqueName(elemName + suffix);        
      466 +          _wrappers.Add(sig, (name, type));                   
      467 +          _wrappersInOrder!.Add((name, type));                
      468 +          return name;                                        
      469        }
      470      }
      471  
      472 +    string BaseNameOf(ProtoType type)                         
      473 +    {                                                         
      474 +      switch (type.Kind) {                                    
      475 +        case ProtoKind.Scalar: return ScalarBaseName(type.Scal
          +ar!);                                                         
      476 +        case ProtoKind.Message: return _defs[Canonical(type.Me
          +ssageKey!)].AssignedName!;                                    
      477 +        default: return TypeRefName(type); // a wrapper's own 
          +name (e.g. Int32OptList)                                      
      478 +      }                                                       
      479 +    }                                                         
      480 +                                                              
      481 +    static string ScalarBaseName(string scalar) => scalar swit
          +ch {                                                          
      482 +      "bool" => "Bool", "int32" => "Int32", "int64" => "Int64"
          +,                                                             
      483 +      "uint32" => "UInt32", "uint64" => "UInt64",             
      484 +      "float" => "Float", "double" => "Double",               
      485 +      "string" => "String", "bytes" => "Bytes",               
      486 +      _ => SanitizeMessageName(scalar),                       
      487 +    };                                                        
      488 +                                                              
      489 +    string Signature(ProtoType type) => type.Kind switch {    
      490 +      ProtoKind.Scalar => "s:" + type.Scalar,                 
      491 +      ProtoKind.Message => "m:" + _defs[Canonical(type.Message
          +Key!)].AssignedName,                                          
      492 +      ProtoKind.List => "L(" + Signature(type.Element!) + ")",
      493 +      ProtoKind.Opt => "O(" + Signature(type.Element!) + ")", 
      494 +      _ => "R(" + Signature(type.Element!) + ")",             
      495 +    };                                                        
      496 +                                                              
      497 +    #endregion                                                
      498 +                                                              
      499 +    #region Name assignment and anonymous-message merging     
      500 +                                                              
      501 +    // Keys of anonymous messages that were merged into an ide
          +ntical earlier one                                            
      502 +    Dictionary<object, object>? _keyRemap;                    
      503 +                                                              
      504 +    object Canonical(object key)                              
      505 +      => _keyRemap != null && _keyRemap.TryGetValue(key, out v
          +ar canon) ? canon : key;                                      
      506 +                                                              
      507 +    // Two structurally identical anonymous messages (e.g. two
          + (int, string) tuples)                                        
      508 +    // become a single message in the rendered schema.        
      509 +    void MergeAnonymousDuplicates()                           
      510 +    {                                                         
      511 +      _keyRemap = new Dictionary<object, object>();           
      512 +      var canonicalAnons = new List<ProtoMessage>();          
      513 +      foreach (var def in _defsInOrder) {                     
      514 +        def.Merged = false;                                   
      515 +        if (def.Key is Type)                                  
      516 +          continue;                                           
      517 +        foreach (var canon in canonicalAnons) {               
      518 +          if (canon.TagName == def.TagName && FieldsEqual(cano
          +n.Fields, def.Fields)) {                                      
      519 +            def.Merged = true;                                
      520 +            _keyRemap[def.Key] = canon.Key;                   
      521 +            break;                                            
      522 +          }                                                   
      523 +        }                                                     
      524 +        if (!def.Merged)                                      
      525 +          canonicalAnons.Add(def);                            
      526 +      }                                                       
      527 +    }                                                         
      528 +                                                              
      529      void AssignNames()
      530      {
      311 -      var used = new HashSet<string>();                       
      531        foreach (var def in _defsInOrder) {
      532 +        if (def.Merged)                                       
      533 +          continue;                                           
      534          string baseName = SanitizeMessageName(def.TagName ?? T
           ypeNameOf(def.Key));
      314 -        string name = baseName;                               
      315 -        for (int i = 2; !used.Add(name); i++)                 
      316 -          name = baseName + i;                                
      317 -        def.AssignedName = name;                              
      535 +        def.AssignedName = UniqueName(baseName);              
      536        }
      537      }
      538  
      539 +    string UniqueName(string baseName)                        
      540 +    {                                                         
      541 +      string name = baseName;                                 
      542 +      for (int i = 2; !_usedNames!.Add(name); i++)            
      543 +        name = baseName + i;                                  
      544 +      return name;                                            
      545 +    }                                                         
      546 +                                                              
      547      static string TypeNameOf(object key)
      548        => key is Type type ? type.Name : (key.ToString() ?? "Me
           ssage");
      549  

● Cleaning up a redundant root-handling path I left in SchemaState.

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.SchemaState.cs)
  ⎿  Removed 7 lines
      165        bool dedup = (mode & ObjectMode.Deduplicate) != 0;
      166        bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
           .Deduplicate)) != ObjectMode.NotNull;
      167  
      168 -      if (Top == null && !isList && !isTuple && _rootType == n
          -ull) {                                                        
      169 -        // Recording the root object; note its type for the sc
          -hema's root comment                                           
      170 -        var begun = BeginMessage(field, childKey, dedup, nulla
          -ble, isTuple: false);                                         
      171 -        // (BeginMessage sets _rootType via the rootHook becau
          -se Top was null)                                              
      172 -        return begun;                                         
      173 -      }                                                       
      174 -                                                              
      168        if (isList) {
      169          if (Top == null)
      170            throw new NotSupportedException(

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.SchemaState.cs)
  ⎿  Removed 5 lines
      233        _defsInOrder.Add(newDef);
      234        AttachMessageRef(field, childKey, dedup, nullable, atRoo
           t);
      235        _stack.Add(new Frame { Msg = newDef, Def = newDef, IsTup
           le = isTuple });
      236 -      if (atRoot) {                                           
      237 -        _rootType = dedup ? ProtoType.MakeRef(ProtoType.MakeMe
          -ssage(childKey)) : ProtoType.MakeMessage(childKey);           
      238 -        if (!dedup)                                           
      239 -          _rootPlainDef = newDef;                             
      240 -      }                                                       
      236        return (true, 1, null);
      237      }
      238  

● Now updating the Schema facade (string dedup, byte/bool/char lists).

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Schema.cs)
  ⎿  Added 2 lines, removed 1 line
      150      public long Sync(FieldId name, long savable, int bits, boo
           l signed = true) { _s.SyncScalar(name, "int64", false); return
            default; }
      151      public BigInteger Sync(FieldId name, BigInteger savable, i
           nt bits, bool signed = true) { _s.SyncScalar(name, "bytes", fa
           lse); return default; }
      152  
      153 -    public string? Sync(FieldId name, string? savable, ObjectM
          -ode mode = ObjectMode.Normal) { _s.SyncScalar(name, "string", 
          -true); return default; }                                      
      153 +    public string? Sync(FieldId name, string? savable, ObjectM
          +ode mode = ObjectMode.Normal)                                 
      154 +      { _s.SyncScalar(name, "string", true, dedup: (mode & Obj
          +ectMode.Deduplicate) != 0); return default; }                 
      155  
      156      public bool?   Sync(FieldId name, bool? savable)   { _s.Sy
           ncScalar(name, "bool", true); return default; }
      157      public sbyte?  Sync(FieldId name, sbyte? savable)  { _s.Sy
           ncScalar(name, "int32", true); return default; }

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Schema.cs)
  ⎿  Added 18 lines, removed 3 lines
      176        where Scanner : IScanner<bool>
      177        where ListBuilder : IListBuilder<List, bool>
      178      {
      179 -      _s.SyncScalarList(name, "bool");                        
      179 +      SyncScalarList(name, "bool", mode, tupleLength);        
      180        return default;
      181      }
      182      public List? SyncListByteImpl<Scanner, List, ListBuilder>(
           FieldId name, Scanner scanner, List? saving, ListBuilder build
           er, ObjectMode mode, int tupleLength = -1)
      183        where Scanner : IScanner<byte>
      184        where ListBuilder : IListBuilder<List, byte>
      185      {
      186 -      _s.SyncScalarList(name, "uint32");                      
      186 +      // Byte lists are stored as a Protobuf `bytes` value, no
          +t as a list container                                         
      187 +      bool nullable = (mode & (ObjectMode.NotNull | ObjectMode
          +.Deduplicate)) != ObjectMode.NotNull;                         
      188 +      _s.SyncScalar(name, "bytes", nullable, dedup: (mode & Ob
          +jectMode.Deduplicate) != 0);                                  
      189        return default;
      190      }
      191      public List? SyncListCharImpl<Scanner, List, ListBuilder>(
           FieldId name, Scanner scanner, List? saving, ListBuilder build
           er, ObjectMode mode, int tupleLength = -1)
      192        where Scanner : IScanner<char>
      193        where ListBuilder : IListBuilder<List, char>
      194      {
      193 -      _s.SyncScalarList(name, "uint32");                      
      195 +      SyncScalarList(name, "uint32", mode, tupleLength);      
      196        return default;
      197      }
      198  
      199 +    // Records a list/tuple of scalar elements the same way th
          +e writer encodes it                                           
      200 +    // (a list container, or a message with auto-numbered fiel
          +ds for a tuple)                                               
      201 +    void SyncScalarList(FieldId name, string elemType, ObjectM
          +ode mode, int tupleLength)                                    
      202 +    {                                                         
      203 +      var (begun, _, _) = _s.BeginSubObject(name, null, mode |
          + ObjectMode.List, tupleLength);                               
      204 +      if (begun) {                                            
      205 +        int n = (mode & ObjectMode.Tuple) == ObjectMode.Tuple 
          +&& tupleLength > 0 ? tupleLength : 1;                         
      206 +        for (int i = 0; i < n; i++)                           
      207 +          _s.SyncScalar((string?)null, elemType, false);      
      208 +        _s.EndSubObject();                                    
      209 +      }                                                       
      210 +    }                                                         
      211 +                                                              
      212      #endregion
      213    }
      214  }
● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Schema.cs)
  ⎿  Added 7 lines, removed 5 lines
      83    ///   <see cref="EndSubObject"/> to throw. The message name i
          s the .NET type name unless
      84    ///   <see cref="SyncTypeTag"/> is called.
      85    /// <para/>
      86 -  ///   The generated <c>.proto</c> is a <i>logical</i> schema:
         - scalar fields and nested                                      
      87 -  ///   messages match the wire format exactly; collections are
         - shown as                                                      
      88 -  ///   <c>repeated</c> fields (SyncProtobuf actually packs the
         -m into one length-delimited                                    
      89 -  ///   field); and <c>decimal</c>/<see cref="BigInteger"/> (wh
         -ich Protobuf lacks) map to                                     
      90 -  ///   <c>bytes</c>.                                          
      86 +  ///   The generated <c>.proto</c> describes the wire format e
         +xactly: any Protobuf                                           
      87 +  ///   implementation can use it to parse <see cref="SyncProto
         +buf.Writer"/>'s output.                                        
      88 +  ///   Lists, nullable list elements and deduplicated values a
         +ppear as generated                                             
      89 +  ///   wrapper message types (e.g. <c>Int32List</c>, <c>String
         +Opt</c>, <c>PersonRef</c> —                                    
      90 +  ///   see <see cref="SyncProtobuf"/> for the encoding); <c>de
         +cimal</c> and                                                  
      91 +  ///   <see cref="BigInteger"/> (which Protobuf lacks) map to 
         +<c>bytes</c>; and a                                            
      92 +  ///   comment at the top names the root message.             
      93    /// </remarks>
      94    public partial struct Schema : ISyncManager
      95    {

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Loyc.SyncLib.SyncProtobuf/Loyc.SyncLib.SyncProtobuf.csproj -v q 2>&1
      | grep -E "error|Erro…)
  ⎿  Build succeeded.
         0 Error(s)
  ⎿  (timeout 5m)

● Library compiles. Let me check PersonSync's dedup usage (critical for the
  cyclic-graph path), then update the tests.

  Read 1 file (ctrl+o to expand)

● Write(Core/Tests/SyncLib/SyncProtobufTests.cs)
  ⎿  Added 111 lines, removed 14 lines
      1  using Loyc.MiniTest;
      2  using System;
      3 +using System.Linq;                                              
      4  
      5  namespace Loyc.SyncLib.Tests
      6  {
     ...
      11    public class SyncProtobufTests : SyncLibTests<SyncProtobuf.Re
          ader, SyncProtobuf.Writer>
      12    {
      13      SyncProtobuf.Options _options = new SyncProtobuf.Options();
      13 -    ObjectMode _saveMode;                                      
      14 +    ObjectMode _extraMode;    // extra root-mode flags for the 
         +nonDefaultSettings fixture                                     
      15 +    ObjectMode _lastRootMode; // the root mode used by the last
         + Write                                                         
      16  
      17      // The Protobuf format is binary, so its byte stream is not
           valid UTF-8.
      18      protected override bool IsUTF8 => false;
     ...
      22        if (nonDefaultSettings)
      23        {
      24          // A tiny initial buffer stresses the writer's buffer g
          rowth and in-place
      23 -        // length back-patching; reading with RootMode=Deduplic
         -ate while most tests                                           
      24 -        // write with RootMode=Normal exercises the "toggle the
         - dedup flag" tolerance.                                        
      25 +        // length patching, and deduplicating the root exercise
         +s the dedup-wrapper                                            
      26 +        // encoding on every test.                             
      27          _options = new SyncProtobuf.Options {
      28            Write = { InitialBufferSize = 1 },
      29          };
      28 -        _saveMode = ObjectMode.Deduplicate;                    
      30 +        _extraMode = ObjectMode.Deduplicate;                   
      31        }
      32      }
      33  
      34      protected override T Read<T>(byte[] data, SyncObjectFunc<Sy
          ncProtobuf.Reader, T> sync)
      35      {
      34 -      _options.RootMode = _saveMode;                           
      36 +      // In SyncProtobuf the Deduplicate flag changes the wire 
         +format, so unlike                                              
      37 +      // SyncBinary (whose markers make the flag self-describin
         +g), the reader must                                            
      38 +      // use the same root mode the writer used.               
      39 +      _options.RootMode = _lastRootMode;                       
      40        return SyncProtobuf.Read<T>(data, sync, _options)!;
      41      }
      42  
      43      protected override byte[] Write<T>(T value, SyncObjectFunc<
          SyncProtobuf.Writer, T> sync, ObjectMode mode)
      44      {
      40 -      _options.RootMode = mode;                                
      45 +      _options.RootMode = _lastRootMode = mode | _extraMode;   
      46        return SyncProtobuf.Write(value, sync, _options).ToArray(
          );
      47      }
      48  
      49      #region Protobuf-specific tests
      50  
      46 -    // Proves the wire format really is Protocol Buffers: field
         - 1 as a varint of 150 is                                       
      47 -    // the canonical Protobuf example (0x08 0x96 0x01). The ext
         -ra 0x0A/0x04 is SyncLib's                                      
      48 -    // length-delimited root envelope, and 0x00 is the (no-dedu
         -p) object framing marker.                                      
      51 +    // Proves the wire format really is Protocol Buffers: the o
         +utput for field 1 as a                                         
      52 +    // varint of 150 is 0x08 0x96 0x01 — the canonical example 
         +from the Protobuf                                              
      53 +    // encoding documentation — with no envelope or markers of 
         +any kind.                                                      
      54      [Test]
      55      public void WireFormat_IsRealProtobuf()
      56      {
     ...
      60        }).ToArray();
      61  
      62        ExpectList(data, new byte[] {
      58 -        0x0A,             // root tag: field 1, wire type LEN(2
         -) -> (1<<3)|2                                                  
      59 -        0x04,             // root message length               
      60 -        0x00,             // object framing marker: not dedupli
         -cated                                                          
      63          0x08,             // field 1, wire type VARINT(0) -> (1
          <<3)|0
      64          0x96, 0x01,       // varint 150
      65        });
     ...
       75          return 0;
       76        }).ToArray();
       77  
       76 -      // root tag, len(12), marker, field tag, then ten varint
          - bytes (0xFF*9, 0x01)                                         
       78        ExpectList(data, new byte[] {
       78 -        0x0A, 0x0C, 0x00, 0x08,                               
       79 +        0x08,                                                 
       80          0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
           0x01,
       81        });
       82      }
       83  
       84 +    // A list is a nested container message whose elements are
          + packed into field 1 —                                        
       85 +    // so the packed block itself is byte-identical to Protobu
          +f's packed encoding.                                          
       86 +    [Test]                                                    
       87 +    public void WireFormat_PackedList()                       
       88 +    {                                                         
       89 +      byte[] data = SyncProtobuf.Write<int[]>(null, (SyncProto
          +buf.Writer sm, int[]? _) => {                                 
       90 +        sm.SyncList(("list", 1), new[] { 1, 2, 3 });          
       91 +        return null!;                                         
       92 +      }).ToArray();                                           
       93 +                                                              
       94 +      ExpectList(data, new byte[] {                           
       95 +        0x0A, 0x05,       // field 1 (the list container), LEN
          +, 5 bytes                                                     
       96 +        0x0A, 0x03,       // field 1 within the container (pac
          +ked items), LEN, 3 bytes                                      
       97 +        0x01, 0x02, 0x03, // varints 1, 2, 3                  
       98 +      });                                                     
       99 +    }                                                         
      100 +                                                              
      101 +    // A string field is a plain Protobuf string; a UTF-8 test
          +.                                                             
      102 +    [Test]                                                    
      103 +    public void WireFormat_String()                           
      104 +    {                                                         
      105 +      byte[] data = SyncProtobuf.Write<string>("testing", (Syn
          +cProtobuf.Writer sm, string? _) => {                          
      106 +        sm.Sync(("s", 2), "testing");                         
      107 +        return "";                                            
      108 +      }).ToArray();                                           
      109 +                                                              
      110 +      ExpectList(data, new byte[] {                           
      111 +        0x12, 0x07,       // field 2, LEN, 7 bytes            
      112 +        (byte)'t', (byte)'e', (byte)'s', (byte)'t', (byte)'i',
          + (byte)'n', (byte)'g',                                        
      113 +      });                                                     
      114 +    }                                                         
      115 +                                                              
      116 +    // A null root is zero bytes; a non-null root with no fiel
          +ds gets the reserved                                          
      117 +    // _present marker so the two remain distinguishable.     
      118 +    [Test]                                                    
      119 +    public void NullRootVersusEmptyRoot()                     
      120 +    {                                                         
      121 +      var options = new SyncProtobuf.Options();               
      122 +      byte[] nullRoot = SyncProtobuf.Write<Person>(null!, new 
          +PersonSync<SyncProtobuf.Writer>().Sync, options).ToArray();   
      123 +      ExpectList(nullRoot, new byte[0]);                      
      124 +      Assert.IsNull(SyncProtobuf.Read<Person>(nullRoot, new Pe
          +rsonSync<SyncProtobuf.Reader>().Sync, options));              
      125 +                                                              
      126 +      // An object whose every field is null writes no ordinar
          +y fields at all                                               
      127 +      byte[] emptyRoot = SyncProtobuf.Write<object>(new object
          +(), (SyncProtobuf.Writer sm, object? _) => {                  
      128 +        sm.Sync("a", (int?)null);                             
      129 +        return new object();                                  
      130 +      }, options).ToArray();                                  
      131 +      Assert.IsTrue(emptyRoot.Length > 0, "empty root must be 
          +marked as present");                                          
      132 +      var read = SyncProtobuf.Read<(bool, int?)>(emptyRoot, (S
          +yncProtobuf.Reader sm, (bool, int?) _) => {                   
      133 +        return (true, sm.Sync("a", (int?)null));              
      134 +      }, options);                                            
      135 +      Assert.AreEqual((true, (int?)null), read);              
      136 +    }                                                         
      137 +                                                              
      138      // A null nullable field is omitted entirely (absent == nu
           ll), so it takes no bytes
      139      // while still keeping later fields' auto-assigned numbers
            aligned.
      140      [Test]
     ...
      180        Assert.AreEqual((11, 22, 33), read);
      181      }
      182  
      183 +    // Per the Protobuf spec, when a non-repeated field occurs
          + twice, the last wins.                                        
      184 +    [Test]                                                    
      185 +    public void DuplicateFieldLastOneWins()                   
      186 +    {                                                         
      187 +      byte[] data = {                                         
      188 +        0x08, 0x01, // field 1 = 1                            
      189 +        0x08, 0x02, // field 1 = 2 (again)                    
      190 +      };                                                      
      191 +      int read = SyncProtobuf.Read<int>(data, (SyncProtobuf.Re
          +ader sm, int _) => sm.Sync(("x", 1), 0));                     
      192 +      Assert.AreEqual(2, read);                               
      193 +    }                                                         
      194 +                                                              
      195      // The reader reports NeedsIntegerIds, and NextField expos
           es the integer field IDs
      196      // (with auto-numbering starting at 1).
      197      [Test]
     ...
      238        Assert.AreEqual((100, 200), read);
      239      }
      240  
      241 +    // Field numbers that Protobuf does not permit are rejecte
          +d up front.                                                   
      242 +    [Test]                                                    
      243 +    public void InvalidFieldNumbersAreRejected()              
      244 +    {                                                         
      245 +      foreach (int badId in new[] { 0, -1, 19000, 19500, 19999
          +, 536_870_910, 536_870_911, int.MaxValue }) {                 
      246 +        try {                                                 
      247 +          SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, 
          +int _) => {                                                   
      248 +            sm.Sync(("x", badId), 1);                         
      249 +            return 0;                                         
      250 +          });                                                 
      251 +          Fail("Field number {0} should have been rejected", b
          +adId);                                                        
      252 +        } catch (ArgumentException) { }                       
      253 +      }                                                       
      254 +    }                                                         
      255 +                                                              
      256 +    // byte[] is stored as a Protobuf `bytes` value, not as a 
          +list container.                                               
      257 +    [Test]                                                    
      258 +    public void WireFormat_ByteArrayIsBytes()                 
      259 +    {                                                         
      260 +      byte[] data = SyncProtobuf.Write<byte[]>(null, (SyncProt
          +obuf.Writer sm, byte[]? _) => {                               
      261 +        sm.SyncList(("blob", 1), new byte[] { 0xDE, 0xAD, 0xBE
          +, 0xEF });                                                    
      262 +        return null!;                                         
      263 +      }).ToArray();                                           
      264 +                                                              
      265 +      ExpectList(data, new byte[] {                           
      266 +        0x0A, 0x04,             // field 1, LEN, 4 bytes      
      267 +        0xDE, 0xAD, 0xBE, 0xEF, // raw bytes, no per-byte enco
          +ding                                                          
      268 +      });                                                     
      269 +    }                                                         
      270 +                                                              
      271      #endregion
      272    }
      273  }

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)
  ⎿  Added 1 line
      39        // Object/Tuple: the message's fields, indexed for reorde
          ring support
      40        public List<FieldEntry>? Fields;
      41        public int Cursor;
      42 +      public byte DupState; // 0 = unknown, 1 = no duplicate fi
         +eld numbers, 2 = duplicates exist                              
      43        // List: the entries of field 1 within the list container
           message
      44        public List<FieldEntry>? Entries;
      45        public int ElemIdx;                // next unconsumed ent
          ry

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)
  ⎿  Added 24 lines, removed 8 lines
      232  
      233      bool FindField(int id, out int valueStart, out WireType wi
           re)
      234      {
      235 -      var fields = TopObject().Fields!;                       
      236 -      // Fast path: fields are usually read in the order they 
          -were written.                                                 
      237 -      int c = _top!.Cursor;                                   
      238 -      if (c < fields.Count && fields[c].Num == id) {          
      235 +      var frame = TopObject();                                
      236 +      var fields = frame.Fields!;                             
      237 +      // Fast path: fields are usually read in the order they 
          +were written. Skipped                                         
      238 +      // if any field number occurs twice, because then the la
          +st occurrence must win.                                       
      239 +      int c = frame.Cursor;                                   
      240 +      if (c < fields.Count && fields[c].Num == id && !HasDupli
          +cateFieldNumbers(frame)) {                                    
      241          var fe0 = fields[c];
      240 -        _top.Cursor = c + 1;                                  
      242 +        frame.Cursor = c + 1;                                 
      243          valueStart = fe0.ValueStart; wire = fe0.Wire;
      244          return true;
      245        }
      244 -      // Slow path: search backward so that, per the Protobuf 
          -specification, the                                            
      245 -      // last occurrence of a duplicated (non-repeated) field 
          -wins.                                                         
      246 +      // Search backward so that, per the Protobuf specificati
          +on, the last                                                  
      247 +      // occurrence of a duplicated (non-repeated) field wins.
      248        for (int i = fields.Count - 1; i >= 0; i--) {
      249          if (fields[i].Num == id) {
      250            var fe = fields[i];
      249 -          _top.Cursor = i + 1;                                
      251 +          frame.Cursor = i + 1;                               
      252            valueStart = fe.ValueStart; wire = fe.Wire;
      253            return true;
      254          }
     ...
      257        return false;
      258      }
      259  
      260 +    static bool HasDuplicateFieldNumbers(RFrame frame)        
      261 +    {                                                         
      262 +      if (frame.DupState == 0) {                              
      263 +        frame.DupState = 1;                                   
      264 +        var fields = frame.Fields!;                           
      265 +        if (fields.Count > 1) {                               
      266 +          var seen = new HashSet<int>();                      
      267 +          foreach (var fe in fields)                          
      268 +            if (!seen.Add(fe.Num)) { frame.DupState = 2; break
          +; }                                                           
      269 +        }                                                     
      270 +      }                                                       
      271 +      return frame.DupState == 2;                             
      272 +    }                                                         
      273 +                                                              
      274      #endregion
      275  
      276      #region List-element access

● Updating stale comments in the malformed-input tests, then reading the schema
  tests fully to regenerate goldens.

● Update(Core/Tests/SyncLib/SyncMalformedInputTests.cs)
  ⎿  Added 3 lines, removed 3 lines
      198      {
      199        var options = new SyncProtobuf.Options();
      200        var evil = new List<byte[]> {
      201 -        new byte[0],                                          
      201 +        new byte[0],                                  // a nul
          +l root; reading it is fine                                    
      202          new byte[] { 0x0A },                          // tag w
           ith no length
      203          new byte[] { 0x0A, 0x7F },                    // lengt
           h 127 but no data
      204          new byte[] { 0x0A, 0x01 },                    // lengt
           h 1 but no body byte
      205 -        new byte[] { 0x0A, 0x01, 0x02 },              // frami
          -ng marker 2 (backref) to nothing                              
      206 -        new byte[] { 0x0A, 0x02, 0x01, 0xFF },        // dedup
          --first, id, then a truncated field tag                        
      205 +        new byte[] { 0x0A, 0x01, 0x02 },              // sub-m
          +essage containing a bare 0x02                                 
      206 +        new byte[] { 0x0A, 0x02, 0x01, 0xFF },        // sub-m
          +essage with a truncated field                                 
      207          new byte[] { 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x7F }, // huge varint at root
      208          new byte[] { 0x0A, 0x05, 0x00, 0x08, 0xFF, 0xFF, 0xFF 
           }, // varint field value truncated
      209          Enumerable.Range(0, 10_000).Select(_ => (byte)0x0A).To
           Array(), // deep nesting must not overflow the stack

  Read 1 file (ctrl+o to expand)

● Now regenerating the schema golden tests to match the new wrapper-based
  schemas.

● Update(Core/Tests/SyncLib/SyncProtobufSchemaTests.cs)
  ⎿  Added 96 lines, removed 20 lines
      106      static void CheckProto(string expected, string actual)
      107        => AreEqual(expected.Replace("\r\n", "\n"), actual.Repla
           ce("\r\n", "\n"));
      108  
      109 -    const string Header =                                     
      109 +    static string Header(string rootName) =>                  
      110        "// Generated by Loyc.SyncLib SyncProtobuf.Schema.\n" +
      111        "// A proto3 schema describing the messages that SyncPro
           tobuf.Writer produces.\n" +
      112 +      "// The root of the serialized data is one " + rootName 
          ++ " message.\n" +                                             
      113        "syntax = \"proto3\";\n";
      114  
      115 +    // _present marks a non-null root whose message body would
          + otherwise be empty                                           
      116 +    const string Present = "  optional bool _present = 5368709
          +10;\n";                                                       
      117 +                                                              
      118      [Test]
      119      public void PrimitiveFieldsSchema()
      120      {
      117 -      CheckProto(Header + @"                                  
      121 +      CheckProto(Header("PrimModel") + @"                     
      122  message PrimModel {
      123    int32 Int = 1;
      124    uint32 UInt = 2;
     ...
      136    uint32 Ch = 14;
      137    optional string Name = 15;
      138    optional int32 MaybeInt = 16;
      135 -  repeated uint32 Blob = 17;                                  
      136 -  repeated int32 Ints = 18;                                   
      137 -  repeated bool Bools = 19;                                   
      138 -  repeated uint32 Chars = 20;                                 
      139 -  repeated string Strings = 21;                               
      139 +  optional bytes Blob = 17;                                   
      140 +  Int32List Ints = 18;                                        
      141 +  BoolList Bools = 19;                                        
      142 +  UInt32List Chars = 20;                                      
      143 +  StringOptList Strings = 21;                                 
      144 +".Replace("\r\n", "\n") + Present + @"}                       
      145 +                                                              
      146 +message Int32List {                                           
      147 +  repeated int32 items = 1;                                   
      148  }
      149 +                                                              
      150 +message BoolList {                                            
      151 +  repeated bool items = 1;                                    
      152 +}                                                             
      153 +                                                              
      154 +message UInt32List {                                          
      155 +  repeated uint32 items = 1;                                  
      156 +}                                                             
      157 +                                                              
      158 +message StringOpt {                                           
      159 +  optional string value = 1;                                  
      160 +}                                                             
      161 +                                                              
      162 +message StringOptList {                                       
      163 +  repeated StringOpt items = 1;                               
      164 +}                                                             
      165  ".Replace("\r\n", "\n"),
      166          SyncProtobuf.WriteSchemaStringI<PrimModel>(SyncPrim));
      167      }
     ...
      169      [Test]
      170      public void RecursiveTypeSchema()
      171      {
      148 -      // Explicit field numbers (1,2,3) and a recursive "repea
          -ted Person" field.                                            
      149 -      CheckProto(Header + @"                                  
      172 +      // Explicit field numbers (1,2,3); the siblings are dedu
          +plicated Person                                               
      173 +      // references, so their element type is the generated Pe
          +rsonRef wrapper.                                              
      174 +      CheckProto(Header("Person") + @"                        
      175  message Person {
      176    optional string Name = 1;
      177    int32 Age = 2;
      153 -  repeated Person Siblings = 3;                               
      178 +  PersonRefList Siblings = 3;                                 
      179 +".Replace("\r\n", "\n") + Present + @"}                       
      180 +                                                              
      181 +message PersonRef {                                           
      182 +  uint64 id = 1;                                              
      183 +  Person value = 2;                                           
      184  }
      185 +                                                              
      186 +message PersonRefList {                                       
      187 +  repeated PersonRef items = 1;                               
      188 +}                                                             
      189  ".Replace("\r\n", "\n"),
      190          SyncProtobuf.WriteSchemaStringI<Person>(SyncPerson));
      191      }
     ...
      193      [Test]
      194      public void CyclicTypesSchema()
      195      {
      162 -      // Parent <-> Child cycle with deduplication (dedup is a
          - wire concern and does                                        
      163 -      // not appear in the logical .proto schema).            
      164 -      CheckProto(Header + @"                                  
      196 +      // Parent <-> Child cycle with deduplication: deduplicat
          +ed values appear as                                           
      197 +      // Ref wrapper messages { id, value } in both the schema
          + and the wire format.                                         
      198 +      CheckProto(Header("Parent") + @"                        
      199  message Parent {
      200    optional string Name = 1;
      167 -  repeated Child Children = 2;                                
      168 -}                                                             
      201 +  ChildRefList Children = 2;                                  
      202 +".Replace("\r\n", "\n") + Present + @"}                       
      203  
      204  message Child {
      205    optional string Name = 1;
      172 -  Parent Father = 2;                                          
      173 -  Parent Mother = 3;                                          
      206 +  ParentRef Father = 2;                                       
      207 +  ParentRef Mother = 3;                                       
      208  }
      209 +                                                              
      210 +message ChildRef {                                            
      211 +  uint64 id = 1;                                              
      212 +  Child value = 2;                                            
      213 +}                                                             
      214 +                                                              
      215 +message ChildRefList {                                        
      216 +  repeated ChildRef items = 1;                                
      217 +}                                                             
      218 +                                                              
      219 +message ParentRef {                                           
      220 +  uint64 id = 1;                                              
      221 +  Parent value = 2;                                           
      222 +}                                                             
      223  ".Replace("\r\n", "\n"),
      224          SyncProtobuf.WriteSchemaStringI<Parent>(SyncParent));
      225      }
     ...
      227      [Test]
      228      public void TypeTagNamesTheMessage()
      229      {
      182 -      CheckProto(Header + @"                                  
      230 +      // The type tag is stored on the wire, so it appears in 
          +the schema too.                                               
      231 +      CheckProto(Header("Ellipse") + @"                       
      232  message Ellipse {
      233 +  optional string _type = 536870911;                          
      234    double Size = 1;
      185 -}                                                             
      186 -".Replace("\r\n", "\n"),                                      
      235 +".Replace("\r\n", "\n") + Present + "}\n",                    
      236          SyncProtobuf.WriteSchemaStringI<Shape>(SyncShape));
      237      }
      238  
      239      [Test]
      240 +    public void TupleAndDedupStringSchema()                   
      241 +    {                                                         
      242 +      string proto = SyncProtobuf.WriteSchemaStringI<object>((
          +sm, _) => {                                                   
      243 +        var sm2 = sm;                                         
      244 +        DefaultSynchronizer.Sync(ref sm2, "Pair", (5, "five"))
          +;                                                             
      245 +        sm.Sync("Tag", "t", ObjectMode.Deduplicate);          
      246 +        return _;                                             
      247 +      });                                                     
      248 +      CheckProto(Header("Object") + @"                        
      249 +message Object {                                              
      250 +  Tuple Pair = 1;                                             
      251 +  StringRef Tag = 2;                                          
      252 +".Replace("\r\n", "\n") + Present + @"}                       
      253 +                                                              
      254 +message Tuple {                                               
      255 +  int32 field1 = 1;                                           
      256 +  optional string field2 = 2;                                 
      257 +}                                                             
      258 +                                                              
      259 +message StringRef {                                           
      260 +  uint64 id = 1;                                              
      261 +  optional string value = 2;                                  
      262 +}                                                             
      263 +".Replace("\r\n", "\n"), proto);                              
      264 +    }                                                         
      265 +                                                              
      266 +    [Test]                                                    
      267      public void ExplicitFieldNumbersWithGaps()
      268      {
      269        string proto = SyncProtobuf.WriteSchemaStringI<int>((sm,
            _) => {

● Now the protobuf-net interop tests. First check what version the benchmarks
  use and whether the test project can take the dependency.

  Listed 1 directory (ctrl+o to expand)

● Update(Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs)  ⎿  Added 8 lines, removed 3 lines
      292  
      293      // Positions the packed-container cursor at the next packe
           d scalar, opening the
      294      // next container (field-1 entry) as needed. Returns false
            at the end of the list.
      295 +    // Also accepts the unpacked encoding (one tagged entry pe
          +r scalar), which other                                        
      296 +    // Protobuf implementations may produce for repeated scala
          +r fields.                                                     
      297      bool OpenPackedScalar(ReadOnlySpan<byte> span)
      298      {
      299        var top = _top!;
     ...
      302          if (top.ElemIdx >= top.Entries!.Count)
      303            return false;
      304          var entry = top.Entries[top.ElemIdx++];
      303 -        if (entry.Wire != WireType.Len)                       
      304 -          throw Error(entry.ValueStart, "expected a packed sca
          -lar container");                                              
      305 -        (top.PackedPos, top.PackedEnd) = ReadLenPayload(span, 
          -entry.ValueStart);                                            
      305 +        if (entry.Wire == WireType.Len)                       
      306 +          (top.PackedPos, top.PackedEnd) = ReadLenPayload(span
          +, entry.ValueStart);                                          
      307 +        else {                                                
      308 +          top.PackedPos = entry.ValueStart;                   
      309 +          top.PackedEnd = SkipValue(span, entry.ValueStart, en
          +try.Wire);                                                    
      310 +        }                                                     
      311        }
      312        return true;
      313      }

● Write(Core/Tests/SyncLib/SyncProtobufInteropTests.cs)
  ⎿  Wrote 248 lines to Core/Tests/SyncLib/SyncProtobufInteropTests.cs
       1 using Loyc.MiniTest;
       2 using ProtoBuf;
       3 using System;
       4 using System.Collections.Generic;
       5 using System.IO;
       6 using System.Linq;
       7 
       8 namespace Loyc.SyncLib.Tests
       9 {
      10   /// <summary>
     … +260 lines (ctrl+o to expand)

● Registering the interop fixture and adding the protobuf-net package.

● Update(Core/Tests/Program.cs)
  ⎿  File must be read first

  Searched for 1 pattern (ctrl+o to expand)

● Bash(sed -n '125,145p' Core/Tests/Program.cs; echo ===; sed -n '15,35p'
      Core/Tests/LoycCore.Tests.csproj)
  ⎿                             new SyncBinaryTests(false),
                                new SyncBinaryTests(true),
                                new SyncBinaryReaderTests(),
     … +40 lines (ctrl+o to expand)

● Update(Core/Tests/Program.cs)
  ⎿  Added 1 line
      135          new SyncProtobufTests(false),
      136          new SyncProtobufTests(true),
      137          new SyncProtobufSchemaTests(),
      138 +        new SyncProtobufInteropTests(),                       
      139          new SyncMalformedInputTests()
      140        );
      141      }

● Update(Core/Tests/LoycCore.Tests.csproj)
  ⎿  Added 1 line
      23      <ProjectReference Include="..\Loyc.SyncLib.SyncProtobuf\Loy
          c.SyncLib.SyncProtobuf.csproj" />
      24      <Compile Include="..\AssemblyVersion.cs" Link="Properties\A
          ssemblyVersion.cs" />
      25      <PackageReference Include="Newtonsoft.Json" Version="13.0.1
          " />
      26 +    <PackageReference Include="protobuf-net" Version="3.2.56" /
         +>                                                              
      27      <Compile Update="SyncLib\BigStandardModel.out.cs">
      28        <DesignTime>True</DesignTime>
      29        <AutoGen>True</AutoGen>

● Now the netfx.sln registration, then build everything.

  Searched for 1 pattern (ctrl+o to expand)

● Bash(cd /workspace/Core && python3 - <<'EOF'
      import re…)
  ⎿  done

● Solution updated. Now building the test project to shake out compile errors.

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -E "error|Build
      succeeded" | head -30)
  ⎿  MSBUILD : error MSB1009: Project file does not exist.

● Bash(cd /workspace && DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec --
      dotnet build Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -E
      "error|Build succeeded" |…)
  ⎿  /home/qwert/.nuget/packages/system.runtime.compilerservices.unsafe/6.0
     .0/buildTransitive/netcoreapp2.0/System.Runtime.CompilerServices.Unsaf
     e.targets(4,5): error : System.Runtime.CompilerServices.Unsafe doesn't
     … +7 lines (ctrl+o to expand)
  ⎿  (timeout 6m 40s)

● protobuf-net 3.x won't work on netcoreapp2.1 — dropping to protobuf-net 2.4.9,
  which has identical API surface for these tests.

● Update(Core/Tests/LoycCore.Tests.csproj)
  ⎿  Added 1 line, removed 1 line
      23      <ProjectReference Include="..\Loyc.SyncLib.SyncProtobuf\Loy
          c.SyncLib.SyncProtobuf.csproj" />
      24      <Compile Include="..\AssemblyVersion.cs" Link="Properties\A
          ssemblyVersion.cs" />
      25      <PackageReference Include="Newtonsoft.Json" Version="13.0.1
          " />
      26 -    <PackageReference Include="protobuf-net" Version="3.2.56" /
         ->                                                              
      26 +    <PackageReference Include="protobuf-net" Version="2.4.9" />
      27      <Compile Update="SyncLib\BigStandardModel.out.cs">
      28        <DesignTime>True</DesignTime>
      29        <AutoGen>True</AutoGen>

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -E "error|Build
      succeeded" | head -30)
  ⎿  /workspace/Core/Tests/SyncLib/SyncProtobufInteropTests.cs(79,4): error
      CS0103: The name 'ExpectList' does not exist in the current context [
     /workspace/Core/Tests/LoycCore.Tests.csproj]
     … +19 lines (ctrl+o to expand)
  ⎿  (timeout 6m 40s)

● Update(Core/Tests/SyncLib/SyncProtobufInteropTests.cs)
  ⎿  Added 1 line, removed 1 line
      15    ///   (such as unpacked repeated scalars).
      16    /// </summary>
      17    [TestFixture]
      18 -  public class SyncProtobufInteropTests : Assert               
      18 +  public class SyncProtobufInteropTests : TestHelpers          
      19    {
      20      static byte[] PbSerialize<T>(T value)
      21      {

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -E "error|Build
      succeeded" | head -30)
  ⎿  /workspace/Core/Tests/SyncLib/SyncProtobufInteropTests.cs(18,42): erro
     r CS0246: The type or namespace name 'TestHelpers' could not be found
     (are you missing a using directive or an assembly reference?) [/worksp
     … +5 lines (ctrl+o to expand)
  ⎿  (timeout 6m 40s)

  Searched for 1 pattern (ctrl+o to expand)

● Bash(sed -n '1,12p' Core/Tests/TestHelpers.cs)
  ⎿  using System;
     using System.Collections.Generic;
     using System.Linq;
     … +10 lines (ctrl+o to expand)

● Update(Core/Tests/SyncLib/SyncProtobufInteropTests.cs)
  ⎿  Added 1 line, removed 1 line
      15    ///   (such as unpacked repeated scalars).
      16    /// </summary>
      17    [TestFixture]
      18 -  public class SyncProtobufInteropTests : TestHelpers          
      18 +  public class SyncProtobufInteropTests : Loyc.Collections.Impl
         +.TestHelpers                                                   
      19    {
      20      static byte[] PbSerialize<T>(T value)
      21      {
● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -E "error|Build
      succeeded" | sort -u | head -…)
  ⎿  Build succeeded.
  ⎿  (timeout 6m 40s)

● Everything compiles. Running the SyncLib suite.

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet run --project
      Core/Tests/LoycCore.Tests.csproj --no-build -- 2 2>&1 | tail -60)
  ⎿  SyncProtobufTests.RoundTripDeeplyNestedObjects
     SyncProtobufTests.WireFormat_IsRealProtobuf
     SyncProtobufTests.WireFormat_NegativeIntIsTenByteVarint
     … +57 lines (ctrl+o to expand)
  ⎿  (timeout 10m)

● Update(Core/Tests/SyncLib/SyncProtobufTests.cs)
  ⎿  Added 3 lines, removed 3 lines
      86      [Test]
      87      public void WireFormat_PackedList()
      88      {
      89 -      byte[] data = SyncProtobuf.Write<int[]>(null, (SyncProtob
         -uf.Writer sm, int[]? _) => {                                   
      90 -        sm.SyncList(("list", 1), new[] { 1, 2, 3 });           
      91 -        return null!;                                          
      89 +      byte[] data = SyncProtobuf.Write(new[] { 1, 2, 3 }, (Sync
         +Protobuf.Writer sm, int[]? v) => {                             
      90 +        sm.SyncList(("list", 1), v);                           
      91 +        return v!;                                             
      92        }).ToArray();
      93  
      94        ExpectList(data, new byte[] {

● Update(Core/Tests/SyncLib/SyncProtobufTests.cs)
  ⎿  Added 3 lines, removed 3 lines
      257      [Test]
      258      public void WireFormat_ByteArrayIsBytes()
      259      {
      260 -      byte[] data = SyncProtobuf.Write<byte[]>(null, (SyncProt
          -obuf.Writer sm, byte[]? _) => {                               
      261 -        sm.SyncList(("blob", 1), new byte[] { 0xDE, 0xAD, 0xBE
          -, 0xEF });                                                    
      262 -        return null!;                                         
      260 +      byte[] data = SyncProtobuf.Write(new byte[] { 0xDE, 0xAD
          +, 0xBE, 0xEF }, (SyncProtobuf.Writer sm, byte[]? v) => {      
      261 +        sm.SyncList(("blob", 1), v);                          
      262 +        return v!;                                            
      263        }).ToArray();
      264  
      265        ExpectList(data, new byte[] {

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/Tests/LoycCore.Tests.csproj -v q 2>&1 | grep -cE "error" ;
      DOTNET_ROLL_FORWARD=Major ~/.lo…)
  ⎿  0
     GREP_DONE
  ⎿  (timeout 10m)

● Bash(which protoc || (sudo apt-get install -y protobuf-compiler 2>&1 | tail
      -1))
  ⎿  E: Unable to locate package protobuf-compiler

● Bash(sudo apt-get update -qq 2>&1 | tail -1 && sudo apt-get install -y -qq
      protobuf-compiler 2>&1 | tail -2 && protoc --version)
  ⎿  Processing triggers for man-db (2.12.0-4build2) ...
     Processing triggers for libc-bin (2.39-0ubuntu8.7) ...
     libprotoc 3.21.12
  ⎿  (timeout 3m)

● protoc installed. Writing the five generated schemas (exactly as the passing
  golden tests confirm the library emits them) and compiling with protoc.

● Bash(mkdir -p /tmp/claude-1000/-workspace/bc1f8b9c-535e-487b-9b0f-9b6fb7551bd8
      /scratchpad/protos && cd
      /tmp/claude-1000/-workspace/bc1f8b9c-535e-487b-9b0f-9b6fb7551b…)
  ⎿  OK: ellipse.proto
     OK: family.proto
     OK: person.proto
     … +2 lines (ctrl+o to expand)
  ⎿  Shell cwd was reset to /workspace

● Bash(SCRATCH=/tmp/claude-1000/-workspace/bc1f8b9c-535e-487b-9b0f-9b6fb7551bd8/
      scratchpad/decode-demo && mkdir -p $SCRATCH && cd $SCRATCH && cat >
      decode-demo.csproj…)
  ⎿  wrote 46 bytes
     Name: "Jack"
     Age: 11
     … +23 lines (ctrl+o to expand)
  ⎿  Shell cwd was reset to /workspace
  ⎿  (timeout 5m)

  Searched for 1 pattern (ctrl+o to expand)

● Bash(DOTNET_ROLL_FORWARD=Major ~/.local/bin/mise exec -- dotnet build
      Core/LoycCore.netstd.sln -v q 2>&1 | grep -E "error|Build succeeded" |
      sort -u; DOTNET_ROLL_FOR…)
  ⎿  Build succeeded.
     0
     SyncProtobufTests.WireFormat_PackedList
     … +3 lines (ctrl+o to expand)
  ⎿  (timeout 10m)

  Wrote 1 memory (ctrl+o to expand)
● Bash(git status --short | head -20; git diff --stat | tail -3)
  ⎿   M Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Options.cs
      M Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.Reader.cs
      M Core/Loyc.SyncLib.SyncProtobuf/SyncProtobuf.ReaderState.cs
     … +20 lines (ctrl+o to expand)

● Bash(git diff --stat Core/LoycCore.Benchmarks/)
  ⎿   .../Components/ResultPanel.razor                   | 28 +++++++++++++
     +++++++++
      .../Engine/BenchmarkQueueService.cs                | 20 +++++++++++++
     … +6 lines (ctrl+o to expand)

● The rewrite is complete and fully verified. SyncProtobuf now produces genuine
  Protocol Buffers, and the schema generator describes the output exactly.

  What was done

  Wire format redesign (the big fix — the old framing markers and root envelope
  made the output unparseable by any protobuf implementation):

  - Root is now a bare message body, like any protobuf message. A null root is
  zero bytes; a non-null root that happens to have no fields gets a reserved
  optional bool _present = 536870910 marker so null and empty stay
  distinguishable.
  - Lists became a container message whose elements live in field 1:
  non-nullable scalars are stored packed (byte-identical to protobuf's packed
  encoding), and nullable elements (strings, int?, nullable objects, nested
  lists) each get a wrapper message { optional T value = 1 } — the same pattern
  as protobuf's well-known wrapper types — where an empty wrapper means null.
  This preserves SyncLib's null-vs-empty and null-vs-"" distinctions inside
  valid protobuf.
  - Deduplication became a Ref wrapper message { uint64 id = 1; T value = 2 }
  (back-reference = id only). It now also works for strings and byte arrays —
  closing the feature gap vs. SyncBinary. The trade-off, documented prominently:
  the Deduplicate flag must match between writer and reader; the old toggle
  tolerance is impossible in valid protobuf.
  - Tuples became messages with auto-numbered fields — this also fixed a latent
  bug where the old schema generator emitted a wrong repeated type for
  heterogeneous tuples.
  - byte[] is now protobuf bytes (raw, one LEN value) instead of per-byte
  varints, and the type tag moved from the illegal field 19000 to 536870911
  (_type), which protoc accepts.

  Schema generator rewritten to describe the wire exactly: it emits the wrapper
  messages (Int32List, StringOpt, PersonRef…, memoized and merged when
  structurally identical), the _type/_present fields, and a header comment
  naming the root message.

  All review findings fixed: dead ReadNullPrimitivesAsDefault and now-moot
  VerifyEof removed (absent→default and truncation are documented protobuf
  semantics); duplicate fields are last-wins per spec; field numbers validated
  (1–536,870,909, excluding 19000-19999, throws ArgumentException); errors
  include byte positions; the CheckAvailable integer overflow uses long math;
  DrainScanner copies chunks instead of bytes; length back-patching reserves a
  byte so most messages never shift; the new Flush() hiding is gone (WriterState
  no longer inherits WriterStateBase); the net45 project is registered in
  LoycCore.netfx.sln; and all the contradictory XML docs were rewritten to
  describe the real format.

  Robustness beyond our own output: the reader also accepts unpacked repeated
  scalars, which protoc/protobuf-net emit by default — tested.

  Verification

  - Full SyncLib suite green (menu 2): both SyncProtobuf fixtures (including the
  new dedup-everywhere fixture), all round-trip/fuzz tests, schema goldens,
  malformed-input corpus — zero failures.
  - protobuf-net interop tests (new SyncProtobufInteropTests, 7 tests): scalars,
  nested messages, packed and unpacked int lists, string lists with nulls, and
  dedup wrappers all parse correctly in both directions.
  - Real protoc 3.21: all five generated schemas compile cleanly, and protoc 
  --decode=Person correctly decodes a cyclic Jack-and-Jill graph written by
  SyncProtobuf — the dedup ids and the cycle-closing back-reference are plainly
  visible in its output.
  - The canonical wire test is now literally the protobuf documentation example:
  field 1 = 150 encodes as 08 96 01, nothing else.

  One heads-up: the working tree also contains ~82 lines of modifications under
  Core/LoycCore.Benchmarks/ (ResultPanel.razor, BenchmarkQueueService.cs, etc.)
  that predate-or-parallel this session — I didn't touch those, so keep them
  separate when committing. Nothing is committed yet; say the word if you'd like
  a commit.

✻ Churned for 25m 4s