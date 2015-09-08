using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for nodes that add a sequence of attributes to an inner node.
    /// </summary>
    public class AttributeNodeTemplate : NodeTemplate
    {
        public AttributeNodeTemplate(IReadOnlyList<NodeEncodingType> templateArgumentTypes)
        {
            Debug.Assert(Enumerable.Any(templateArgumentTypes));

            argTypes = templateArgumentTypes;
        }
        public AttributeNodeTemplate(NodeEncodingType attributeTargetType, IReadOnlyList<NodeEncodingType> attributeArgumentTypes)
        {
            var argTys = new List<NodeEncodingType>();
            argTys.Add(attributeTargetType);
            argTys.AddRange(attributeArgumentTypes);
            argTypes = argTys;
        }

        private IReadOnlyList<NodeEncodingType> argTypes;
        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
        }

        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return Arguments.First().WithAttrs(Arguments.Skip(1).ToArray());
        }

        /// <summary>
        /// Reads an attribute list node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static AttributeNodeTemplate Read(LoycBinaryReader Reader)
        {
            return new AttributeNodeTemplate(Reader.ReadList(Reader.ReadEncodingType));
        }

        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.AttributeNode; }
        }

        /// <summary>
        /// Writes an attribute list node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        public override bool Equals(object obj)
        {
            return obj is AttributeNodeTemplate && ArgumentTypes.SequenceEqual(((AttributeNodeTemplate)obj).ArgumentTypes);
        }

        public override int GetHashCode()
        {
            int result = (int)TemplateType;
            foreach (var item in ArgumentTypes)
            {
                result = (result << 1) ^ (int)item;
            }
            return result;
        }
    }
}
