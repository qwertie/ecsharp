using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{    
    /// <summary>
    /// Defines a mutable view of a binary encoded loyc tree's header,
    /// </summary>
    public class WriterState
    {
        /// <summary>
        /// Creates a new mutable binary encoded loyc tree header.
        /// </summary>
        public WriterState()
        {
            this.stringTable = new Dictionary<string, int>();
            this.stringList = new List<string>();
            this.templates = new List<NodeTemplate>();
            this.templateTable = new Dictionary<NodeTemplate, int>();
        }

        /// <summary>
        /// Gets the encoded loyc tree's symbol table.
        /// </summary>
        public IReadOnlyList<string> Symbols { get { return stringList; } }

        private Dictionary<string, int> stringTable;
        private List<string> stringList;

        /// <summary>
        /// Gets the encoded loyc tree's list of templates.
        /// </summary>
        public IReadOnlyList<NodeTemplate> Templates { get { return templates; } }

        private Dictionary<NodeTemplate, int> templateTable;
        private List<NodeTemplate> templates;

        private static int GetOrAddIndex<T>(T Value, Dictionary<T, int> Table, List<T> Items)
        {
            int result;
            if (Table.TryGetValue(Value, out result))
            {
                return result;
            }
            else
            {
                int index = Table.Count;
                Items.Add(Value);
                Table[Value] = index;
                return index;
            }
        }

        /// <summary>
        /// Gets a symbol's index in the string table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int GetIndex(Symbol Value)
        {
            return GetIndex(Value.Name);
        }

        /// <summary>
        /// Gets a string's index in the string table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public int GetIndex(string Value)
        {
            return GetOrAddIndex(Value, stringTable, stringList);
        }

        /// <summary>
        /// Gets a template's index in the template table.
        /// The given value is added to the table
        /// if it's not already in there.
        /// </summary>
        /// <param name="Template"></param>
        /// <returns></returns>
        public int GetIndex(NodeTemplate Template)
        {
            return GetOrAddIndex(Template, templateTable, templates);
        }
    }
}
