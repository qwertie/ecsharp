using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// Defines an immutable view of a binary encoded loyc tree's header,
    /// as well as a node factory.
    /// </summary>
    public class ReaderState
    {
        /// <summary>
        /// Creates a new immutable header from the given node factory, symbol table and template table.
        /// </summary>
        /// <param name="nodeFactory"></param>
        /// <param name="symbolTable"></param>
        /// <param name="templateTable"></param>
        public ReaderState(LNodeFactory nodeFactory, IReadOnlyList<string> symbolTable, IReadOnlyList<NodeTemplate> templateTable)
        {
            this.NodeFactory = nodeFactory;
            this.SymbolTable = symbolTable;
            this.SymbolPool = SymbolPool.@new();
            this.TemplateTable = templateTable;
        }

        /// <summary>
        /// Gets the reader's node factory.
        /// </summary>
        public LNodeFactory NodeFactory { get; private set; }

        /// <summary>
        /// Gets the reader's symbol pool.
        /// </summary>
        public SymbolPool SymbolPool { get; private set; }

        /// <summary>
        /// Gets the reader's symbol table.
        /// </summary>
        public IReadOnlyList<string> SymbolTable { get; private set; }

        /// <summary>
        /// Gets the reader's template table.
        /// </summary>
        public IReadOnlyList<NodeTemplate> TemplateTable { get; private set; }
    }
}
