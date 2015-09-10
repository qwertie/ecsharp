using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for a call node.
    /// </summary>
    public class CallNodeTemplate : NodeTemplate
    {
        public CallNodeTemplate(IReadOnlyList<NodeEncodingType> templateArgumentTypes)
        {
            Debug.Assert(Enumerable.Any(templateArgumentTypes));

            argTypes = templateArgumentTypes;
        }
        public CallNodeTemplate(NodeEncodingType callTargetType, IReadOnlyList<NodeEncodingType> callArgumentTypes)
        {
            var argTyList = new List<NodeEncodingType>();
            argTyList.Add(callTargetType);
            argTyList.AddRange(callArgumentTypes);
            argTypes = argTyList;
        }

        private IReadOnlyList<NodeEncodingType> argTypes;
        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
        }

        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.CallNode; }
        }

        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return State.NodeFactory.Call(Arguments.First(), Arguments.Skip(1));
        }

        /// <summary>
        /// Reads a call node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static CallNodeTemplate Read(LoycBinaryReader Reader)
        {
            return new CallNodeTemplate(Reader.ReadList(Reader.ReadEncodingType));
        }

        /// <summary>
        /// Writes a call node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        public override bool Equals(object obj)
        {
            return obj is CallNodeTemplate && ArgumentTypes.SequenceEqual(((CallNodeTemplate)obj).ArgumentTypes);
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
