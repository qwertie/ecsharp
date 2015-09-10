using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Binary
{
    /// <summary>
    /// An enumeration of ways to encode a node.
    /// </summary>
    public enum NodeEncodingType : byte
    {
        /// <summary>
        /// A templated node, which is encoded as a template index and inline data.
        /// Call nodes and attribute nodes are encoded as templates nodes.
        /// </summary>
        TemplatedNode = 0,

        /// <summary>
        /// An id node, which is encoded as an index in the symbol table.
        /// </summary>
        IdNode = 1,

        /// <summary>
        /// A string literal, which is encoded as an index in the symbol table.
        /// </summary>
        String = 2,

        /// <summary>
        /// An 8-bit signed integer literal.
        /// </summary>
        Int8 = 3,
        /// <summary>
        /// A 16-bit signed integer literal.
        /// </summary>
        Int16 = 4,
        /// <summary>
        /// A 32-bit signed integer literal.
        /// </summary>
        Int32 = 5,
        /// <summary>
        /// A 64-bit signed integer literal.
        /// </summary>
        Int64 = 6,

        /// <summary>
        /// An 8-bit unsigned integer literal.
        /// </summary>
        UInt8 = 7,
        /// <summary>
        /// A 16-bit unsigned integer literal.
        /// </summary>
        UInt16 = 8,
        /// <summary>
        /// A 32-bit unsigned integer literal.
        /// </summary>
        UInt32 = 9,
        /// <summary>
        /// A 64-bit unsigned integer literal.
        /// </summary>
        UInt64 = 10,

        /// <summary>
        /// A 32-bit single-precision IEEE floating-point literal.
        /// </summary>
        Float32 = 11,

        /// <summary>
        /// A 64-bit double-precision IEEE floating-point literal.
        /// </summary>
        Float64 = 12,

        /// <summary>
        /// A character literal.
        /// </summary>
        Char = 13,

        /// <summary>
        /// A boolean literal.
        /// </summary>
        Boolean = 14,

        /// <summary>
        /// The void singleton value.
        /// </summary>
        Void = 15,

        /// <summary>
        /// The null singleton value.
        /// </summary>
        Null = 16,

        /// <summary>
        /// A decimal literal
        /// </summary>
        Decimal = 17
    }
}
