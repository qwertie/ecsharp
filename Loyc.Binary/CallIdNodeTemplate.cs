using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// A template for calls nodes that have an id node as their target.
    /// </summary>
    public class CallIdNodeTemplate : NodeTemplate
    {
        public CallIdNodeTemplate(int targetSymbolIndex, IReadOnlyList<NodeEncodingType> argumentTypes)
        {
            TargetSymbolIndex = targetSymbolIndex;
            argTypes = argumentTypes;
        }

        public int TargetSymbolIndex { get; private set; }

        private IReadOnlyList<NodeEncodingType> argTypes;

        public override IReadOnlyList<NodeEncodingType> ArgumentTypes
        {
            get { return argTypes; }
        }

        public override LNode Instantiate(ReaderState State, IEnumerable<LNode> Arguments)
        {
            return State.NodeFactory.Call(State.SymbolTable[TargetSymbolIndex], Arguments);
        }

        /// <summary>
        /// Reads a call id node template definition.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static CallIdNodeTemplate Read(LoycBinaryReader Reader)
        {
            int symbolIndex = Reader.Reader.ReadInt32();
            var types = Reader.ReadList(Reader.ReadEncodingType);
            return new CallIdNodeTemplate(symbolIndex, types);
        }

        public override NodeTemplateType TemplateType
        {
            get { return NodeTemplateType.CallIdNode; }
        }

        /// <summary>
        /// Writes a call node template definition.
        /// </summary>
        /// <param name="Writer"></param>
        public override void Write(LoycBinaryWriter Writer)
        {
            Writer.Writer.Write(TargetSymbolIndex);
            Writer.WriteList(ArgumentTypes, Writer.WriteEncodingType);
        }

        public override bool Equals(object obj)
        {
            var other = obj as CallIdNodeTemplate;
            return other != null && 
                   this.TargetSymbolIndex == other.TargetSymbolIndex && 
                   this.ArgumentTypes.SequenceEqual(other.ArgumentTypes);
        }

        public override int GetHashCode()
        {
            int result = TemplateType.GetHashCode() ^ TargetSymbolIndex;
            foreach (var item in ArgumentTypes)
            {
                result = (result << 1) ^ item.GetHashCode();
            }
            return result;
        }
    }
}
