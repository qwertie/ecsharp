using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Loyc.Binary
{
    /// <summary>
    /// A base class for node templates.
    /// </summary>
    public abstract class NodeTemplate
    {
        /// <summary>
        /// Gets this node template's type.
        /// </summary>
        public abstract NodeTemplateType TemplateType { get; }

        /// <summary>
        /// Gets the template's list of argument types.
        /// </summary>
        public abstract IReadOnlyList<NodeEncodingType> ArgumentTypes { get; }

        /// <summary>
        /// Gets the number of arguments the node template takes.
        /// </summary>
        public int ArgumentCount { get { return ArgumentTypes.Count;  } }

        /// <summary>
        /// Instantiates this template.
        /// </summary>
        /// <param name="Arguments"></param>
        /// <returns></returns>
        public abstract LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments);

        /// <summary>
        /// Writes this node template's data to the given writer.
        /// </summary>
        /// <param name="Writer"></param>
        public abstract void Write(LoycBinaryWriter Writer);

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }
}
