using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// An enumeration of node template types.
    /// </summary>
    public enum NodeTemplateType : byte
    {
        /// <summary>
        /// A call node template.
        /// </summary>
        CallNode = 0,
        /// <summary>
        /// An attribute list node template.
        /// </summary>
        AttributeNode = 1,
        /// <summary>
        /// A template that captures a call to an id node.
        /// </summary>
        CallIdNode = 2
    }
}
