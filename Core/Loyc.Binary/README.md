# Binary Loyc Tree format
The binary loyc tree (BLT) file format is a succinct binary representation of loyc trees.
Its goal is to serve as an efficient format for program-to-program loyc tree transfer.
BLT optimizes for relatively large files, such as entire assemblies, and emphasizes read times and
on-disk size.

## File layout
The format has the following layout:
 * Magic string ("BLT")
 * Header
   * Symbol table (length-prefixed list of symbol definitions, which are really just character strings)
   * Template table (length-prefixed list of template definitions)
 * Top-level nodes (length-prefixed list of encoding-prefixed nodes)

This layout was chosen because it makes it easy to read BLT files from start to end, without any
seek operations.

## Some relevant notions
### Symbol table
All symbols and strings in the BLT format are encoded as a variable-length index into a single symbol table, 
which promotes string interning.

### Node templates
Loyc nodes tend to be similar in structure. 
For example, a node such as `#if(<call>, <call>, <call>)` will likely occur more than once in an assembly,
or even a single source file. BLT optimizes recurring constructs such as this by encoding call nodes, id nodes and nodes
with attributes as "template instantiations". A template is essentially a description of a binary encoded loyc node's 
memory layout.
All templates are stored in a single template table, and individual nodes instantiate a template by referring to the
template's index and then providing an unprefixed list of nodes, which are then parsed in accordance with the 
template's definition. Only top-level nodes need an encoding prefix in the BLT format: child node encodings are embedded
in the top-level nodes' templates.

Here's a quick example:

The expression `1 + 2 + x + y` can be parsed as `@+(@+(@+(1, 2), x), y)`.
`1` and `2` are Int32 constants, whereas `x` and `y` are id nodes.
The binary plus operator is a call. When encoded in the BLT format,
every call is reduced to a template and the template's arguments.
 * `@+(1, 2)`'s template is `Id(Int32, Int32)`;
 * `@+(@+(1, 2), x)` becomes `Id(TemplatedNode, Id)`;
 * `@+(@+(@+(1, 2), x), y)` is of the form `Id(TemplatedNode, Id)`.

Since the last two templates are duplicates, they need only be encoded in the template table once.
Calls to id nodes tend to occur a lot, so BLT special-cases the constructs and prefixes the 
template definition with the id node's symbol.
The resulting file will look more or less like this (example in a textual format for readability. The actual file uses a binary encoding):

`BLT`  
`Symbol table (3 items):`  
  * `$0: "+"`
  * `$1: "x"`
  * `$2: "y"`  

`Template table (2 items):`  
  * `#0: IdCall: $0(Int32, Int32)`
  * `#1: IdCall: $0(TemplatedNode, Id)`  

`Top-level node list (1 item):`
  * `TemplatedNode - #1 (@+(TemplatedNode, Id))`
     * `#1 (@+(TemplatedNode, Id))`
       * `#0 (@+(Int32, Int32))`
         * `1`
         * `2`
       * `$1 ("x")`
     * `$2 ("y")`

where `$i` represents an index in the symbol table, and `#i` represents an index in the template table.
 
## Data types
 * **Unprefixed list** - A generic list of items that is stored sequentially. Such a list does not have a length prefix.
 * **ULEB128** - An unsigned LEB128 variable-length integer. These integers are used as table indices, to conserve space.
 * **Prefixed list** - An ULEB128 length prefix followed by an unprefixed list whose length equals the length prefix.
 * **Symbol definition** - An ULEB128 integer that identifies the length of the string's data, in bytes, followed by the string's data, encoded as UTF-8.
 * **Encoding type** - A byte that identifies how a node is encoded.
 * **Template type** - A byte prefix that identifies the kind of template that is encoded.
 * **Template definition** - A template type followed by a prefixed list of encoding types.
 * **Unprefixed node** - A node's data. Its encoding is defined by an external encoding type.
 * **Encoding-prefixed node** - An encoding type followed by an unprefixed node.
 
## Encoding types
An encoding type can be one of the following values:
 * **Templated node = 0** - A templated node, which is encoded as an ULEB128 template index and an unprefixed list of unprefixed nodes. Call nodes and attribute nodes are encoded as templates nodes.
 * **Id node = 1** - An id node, which is encoded as an ULEB128 index in the symbol table.
 * **String = 2** - A string literal, which is encoded as an ULEB128 index in the symbol table.
 * **Int8 = 3** - An 8-bit signed integer literal, encoded as such.
 * **Int16 = 4** - A 16-bit signed integer literal, encoded as such.
 * **Int32 = 5** - A 32-bit signed integer literal, encoded as such.
 * **Int64 = 6** - A 64-bit signed integer literal, encoded as such.
 * **UInt8 = 7** - An 8-bit unsigned integer literal, encoded as such.
 * **UInt16 = 8** - A 16-bit unsigned integer literal, encoded as such.
 * **UInt32 = 9** - A 32-bit unsigned integer literal, encoded as such.
 * **UInt64 = 10** - A 64-bit unsigned integer literal, encoded as such.
 * **Float32 = 11** - A 32-bit single-precision IEEE floating-point literal.
 * **Float64 = 12** - A 64-bit double-precision IEEE floating-point literal.
 * **Char = 13** - A character literal.
 * **Boolean = 14** - A boolean literal.
 * **Void = 15** - The void singleton value.
 * **Null = 16** - The null singleton value.
 * **Decimal = 17** - A .NET System.Decimal value.

## Template types
A template type can be one of the following:
 * **Call = 0** - A generic call node. This definition kind is encoded as a prefixed list of encoding types, the first of which is the target's encoding type. The remainder of the list are the call node's argument encoding types.
 * **CallId = 1** - A call node whose target is an id node. The id node's symbol is mapped to an ULEB128 index into the symbol table, followed by a prefixed list of encoding types for the call node's arguments.
 * **Attributes = 2** - A node that has a list of attributes. Such a template definition is is encoded as a prefixed list of encoding types, the first of which is the inner node's encoding type. The remainder of the list are the attributes' encoding types.
