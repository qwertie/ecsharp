# Binary Loyc tree format
The binary loyc tree (BLT) file format is a succinct binary representation of loyc trees.
Its goal is to serve as an efficient format for program-to-program loyc tree transfer.
BLT optimizes for relatively large files, such as entire assemblies, and emphasizes read times and
on-disk size.

## Some relevant notions
### Symbol table
All symbols and strings in the BLT format are encoded as a variable-length index into a single symbol table, 
which avoids making redundant copies.

### Node templates
Loyc nodes tend to be similar in structure. 
For example, a node such as `#if(<call>, <call>, <call>)` will likely occur more than once in an assembly,
or even a single source file. BLT optimizes for recurring constructs such as this by encoding call or attribute nodes
as "template instantiations". A template is essentially a description of what a binary encoded loyc node looks like.
All templates are stored in a single template table, and individual nodes instantiate a template by referring to the
template's index and then providing an unprefixed list of nodes, which are then parsed in accordance with the 
template's definition. These templates work quite well: only top-level nodes need to have their encoding specified on BLT.

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

 BLT
 Symbol table (3 items):
  * $0: `"+"`
  * $1: `"x"`
  * $2: `"y"`
 Template table (2 items):
  * #0: IdCall: `$0(Int32, Int32)`
  * #1: IdCall: `$0(TemplatedNode, Id)`
 1 node:
  * TemplatedNode - #1 (`@+(@+(@+(1, 2), x), y)`)
     * #1 (`@+(@+(1, 2), x)`)
       * #0 (`@+(1, 2)`)
         * 1
         * 2
       * $1 (`"x"`)
     * $2 (`"y"`)


## File layout
The format has the following layout:
 * Magic string ("BLT")
 * Header
   * Symbol table (prefixed list of symbol definitions)
   * Template table (prefixed list of template definitions)
 * Nodes (prefixed list of encoding-prefixed nodes)
 
## Data types
 * **Unprefixed list** - A generic list of items that is stored sequentially.
   Such a list does not have a length prefix.
 * **ULEB128** - An unsigned LEB128 variable-length integer.
 * **Prefixed list** - An ULEB128 length prefix followed by an unprefixed list whose length equals the length prefix.
 * **Symbol definition** - An ULEB128 integer that identifies the length of the string's data, in bytes, 
   followed by the string's data, encoded as UTF-8.
 * **Encoding type** - A byte that identifies how a node is encoded.
 * **Template type** - A byte prefix that identifies the kind of template that is encoded.
 * **Template definition** - A template type followed by a prefixed list of encoding types.
 * **Unprefixed node** - A node's data. Its encoding is defined by an external encoding type.
 * **Encoding-prefixed node** - An encoding type followed by an unprefixed node.
 
## Encoding types
 * **Templated node = 0** - A templated node, which is encoded as an ULEB128 template index and an unprefixed list of unprefixed nodes. 
   Call nodes and attribute nodes are encoded as templates nodes.
 * **Id node = 1** - An id node, which is encoded as an index in the symbol table.
