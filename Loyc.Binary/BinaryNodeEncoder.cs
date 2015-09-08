using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary
{
    /// <summary>
    /// Defines a node encoder for the binary loyc tree format.
    /// </summary>
    public class BinaryNodeEncoder
    {
        public BinaryNodeEncoder(NodeEncodingType EncodingType, Func<LNode, bool> CanEncode, Action<LoycBinaryWriter, WriterState, LNode> Encode)
        {
            this.EncodingType = EncodingType;
            this.CanEncode = CanEncode;
            this.Encode = Encode;
        }

        /// <summary>
        /// Gets the encoder's encoding type.
        /// </summary>
        public NodeEncodingType EncodingType { get; private set; }

        /// <summary>
        /// Determines whether this encoder can encode the given node.
        /// </summary>
        public Func<LNode, bool> CanEncode { get; private set; }

        /// <summary>
        /// Encodes a given node.
        /// </summary>
        public Action<LoycBinaryWriter, WriterState, LNode> Encode { get; private set; }

        /// <summary>
        /// Creates a binary node encoder that encodes literals of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Encoding"></param>
        /// <param name="ValueEncoder"></param>
        /// <returns></returns>
        public static BinaryNodeEncoder CreateLiteralEncoder<T>(NodeEncodingType Encoding, Action<BinaryWriter, T> ValueEncoder)
        {
            return CreateLiteralEncoder<T>(Encoding, (writer, state, value) => ValueEncoder(writer.Writer, value));
        }

        /// <summary>
        /// Creates a binary node encoder that encodes literals of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Encoding"></param>
        /// <param name="ValueEncoder"></param>
        /// <returns></returns>
        public static BinaryNodeEncoder CreateLiteralEncoder<T>(NodeEncodingType Encoding, Func<BinaryWriter, Action<T>> ValueEncoder)
        {
            return CreateLiteralEncoder<T>(Encoding, (writer, state, value) => ValueEncoder(writer.Writer)(value));
        }

        /// <summary>
        /// Creates a binary node encoder that encodes literals of a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Encoding"></param>
        /// <param name="ValueEncoder"></param>
        /// <returns></returns>
        public static BinaryNodeEncoder CreateLiteralEncoder<T>(NodeEncodingType Encoding, Action<LoycBinaryWriter, WriterState, T> ValueEncoder)
        {
            return new BinaryNodeEncoder(Encoding,
                node => node.IsLiteral && !node.HasAttrs && node.Value is T,
                (writer, state, node) => ValueEncoder(writer, state, (T)node.Value));
        }

        /// <summary>
        /// Gets the binary node encoder for id nodes.
        /// </summary>
        public static BinaryNodeEncoder IdEncoder
        {
            get
            {
                return new BinaryNodeEncoder(NodeEncodingType.IdNode,
                    node => node.IsId && !node.HasAttrs,
                    (writer, state, node) => writer.WriteReference(state, node.Name));
            }
        }

        /// <summary>
        /// Gets the binary node encoder for null literals.
        /// </summary>
        public static BinaryNodeEncoder NullEncoder
        {
            get
            {
                return new BinaryNodeEncoder(NodeEncodingType.Null,
                    node => node.IsLiteral && !node.HasAttrs && node.Value == null,
                    (writer, state, node) => { });
            }
        }

        /// <summary>
        /// Gets the binary node encoder for attribute literals.
        /// </summary>
        public static BinaryNodeEncoder AttributeEncoder
        {
            get
            {
                return new BinaryNodeEncoder(NodeEncodingType.TemplatedNode,
                    node => node.HasAttrs,
                    (writer, state, node) =>
                    {
                        var nodeList = new LNode[] { node.WithoutAttrs() }.Concat(node.Attrs);
                        var encoders = nodeList.Select(item => Pair.Create(item, writer.GetEncoder(item))).ToArray();
                        var template = new AttributeNodeTemplate(encoders.Select(item => item.Value.EncodingType).ToArray());
                        writer.WriteReference(state, template);
                        writer.WriteListContents(encoders, item => item.Value.Encode(writer, state, item.Key)); 
                    });
            }
        }

        /// <summary>
        /// Gets the binary node encoder for call nodes whose target is not an id node.
        /// </summary>
        public static BinaryNodeEncoder CallEncoder
        {
            get
            {
                return new BinaryNodeEncoder(NodeEncodingType.TemplatedNode,
                    node => !node.HasAttrs && node.IsCall && (!node.Target.IsId || node.Target.HasAttrs),
                    (writer, state, node) =>
                    {
                        var nodeList = new LNode[] { node.Target }.Concat(node.Args);
                        var encoders = nodeList.Select(item => Pair.Create(item, writer.GetEncoder(item))).ToArray();
                        var template = new CallNodeTemplate(encoders.Select(item => item.Value.EncodingType).ToArray());
                        writer.WriteReference(state, template);
                        writer.WriteListContents(encoders, item => item.Value.Encode(writer, state, item.Key));
                    });
            }
        }

        /// <summary>
        /// Gets the binary node encoder for call nodes whose target is an id node.
        /// </summary>
        public static BinaryNodeEncoder CallIdEncoder
        {
            get
            {
                return new BinaryNodeEncoder(NodeEncodingType.TemplatedNode,
                    node => !node.HasAttrs && node.IsCall && node.Target.IsId && !node.Target.HasAttrs,
                    (writer, state, node) =>
                    {
                        int nodeTarget = state.GetIndex(node.Target.Name);
                        var encoders = node.Args.Select(item => Pair.Create(item, writer.GetEncoder(item))).ToArray();
                        var template = new CallIdNodeTemplate(nodeTarget, encoders.Select(item => item.Value.EncodingType).ToArray());
                        writer.WriteReference(state, template);
                        writer.WriteListContents(encoders, item => item.Value.Encode(writer, state, item.Key));
                    });
            }
        }
    }
}
