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
    /// A type that writes binary encoded loyc trees.
    /// </summary>
    public class LoycBinaryWriter : IDisposable
    {
        #region Constructors

        public LoycBinaryWriter(BinaryWriter writer, IReadOnlyDictionary<Type, BinaryNodeEncoder> encoders)
        {
            Writer = writer;
            LiteralEncoders = encoders;
        }
        public LoycBinaryWriter(Stream outputStream, IReadOnlyDictionary<Type, BinaryNodeEncoder> encoders)
            : this(new BinaryWriter(outputStream), encoders)
        { }
        public LoycBinaryWriter(BinaryWriter writer, LoycBinaryWriter other)
            : this(writer, other.LiteralEncoders)
        { }
        public LoycBinaryWriter(Stream outputStream, LoycBinaryWriter other)
            : this(new BinaryWriter(outputStream), other)
        { }
        public LoycBinaryWriter(BinaryWriter writer)
            : this(writer, DefaultEncoders)
        { }
        public LoycBinaryWriter(Stream outputStream)
            : this(new BinaryWriter(outputStream))
        { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the writer to the underlying stream of this instance.
        /// </summary>
        public BinaryWriter Writer { get; private set; }

        /// <summary>
        /// Gets the set of literal node encoders this writer uses.
        /// </summary>
        public IReadOnlyDictionary<Type, BinaryNodeEncoder> LiteralEncoders { get; private set; }

        #endregion

        #region Static

        /// <summary>
        /// Gets the default set of encoders.
        /// </summary>
        public static IReadOnlyDictionary<Type, BinaryNodeEncoder> DefaultEncoders
        {
            get
            {
                return new Dictionary<Type, BinaryNodeEncoder>()
                {
                    { typeof(sbyte), BinaryNodeEncoder.CreateLiteralEncoder<sbyte>(NodeEncodingType.Int8, (writer, value) => writer.Write(value)) },
                    { typeof(short), BinaryNodeEncoder.CreateLiteralEncoder<short>(NodeEncodingType.Int16, (writer, value) => writer.Write(value)) },
                    { typeof(int), BinaryNodeEncoder.CreateLiteralEncoder<int>(NodeEncodingType.Int32, (writer, value) => writer.Write(value)) },
                    { typeof(long), BinaryNodeEncoder.CreateLiteralEncoder<long>(NodeEncodingType.Int64, (writer, value) => writer.Write(value)) },

                    { typeof(byte), BinaryNodeEncoder.CreateLiteralEncoder<byte>(NodeEncodingType.UInt8, (writer, value) => writer.Write(value)) },
                    { typeof(ushort), BinaryNodeEncoder.CreateLiteralEncoder<ushort>(NodeEncodingType.UInt16, (writer, value) => writer.Write(value)) },
                    { typeof(uint), BinaryNodeEncoder.CreateLiteralEncoder<uint>(NodeEncodingType.UInt32, (writer, value) => writer.Write(value)) },
                    { typeof(ulong), BinaryNodeEncoder.CreateLiteralEncoder<ulong>(NodeEncodingType.UInt64, (writer, value) => writer.Write(value)) },

                    { typeof(float), BinaryNodeEncoder.CreateLiteralEncoder<float>(NodeEncodingType.Float32, (writer, value) => writer.Write(value)) },
                    { typeof(double), BinaryNodeEncoder.CreateLiteralEncoder<double>(NodeEncodingType.Float64, (writer, value) => writer.Write(value)) },
                    { typeof(decimal), BinaryNodeEncoder.CreateLiteralEncoder<decimal>(NodeEncodingType.Decimal, (writer, value) => writer.Write(value)) },

                    { typeof(char), BinaryNodeEncoder.CreateLiteralEncoder<char>(NodeEncodingType.Char, (writer, value) => writer.Write(value)) },
                    { typeof(bool), BinaryNodeEncoder.CreateLiteralEncoder<bool>(NodeEncodingType.Boolean, (writer, value) => writer.Write(value)) },
                    { typeof(string), BinaryNodeEncoder.CreateLiteralEncoder<string>(NodeEncodingType.String, (writer, state, value) => writer.WriteReference(state, value)) },
                    { typeof(@void), new BinaryNodeEncoder(NodeEncodingType.Void, (writer, state, node) => { }) }
                };
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Writes a LEB128 variable-length unsigned integer to the output stream.
        /// </summary>
        /// <param name="Value"></param>
        public void WriteULeb128(uint Value)
        {
            // C# translation of code borrowed from Wikipedia article:
            // https://en.wikipedia.org/wiki/LEB128
            do
            {
                byte b = (byte)(Value & 0x7F);
                Value >>= 7;
                if (Value != 0) /* more bytes to come */
                    b |= 0x80;
                Writer.Write(b);
            } while (Value != 0);
        }

        /// <summary>
        /// Writes the given list of items to the output stream.
        /// The resulting data is length-prefixed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Items"></param>
        /// <param name="WriteItem"></param>
        public void WriteList<T>(IReadOnlyList<T> Items, Action<T> WriteItem)
        {
            WriteULeb128((uint)Items.Count);
            WriteListContents(Items, WriteItem);
        }

        /// <summary>
        /// Writes the contents of the given list of items to the output stream.
        /// The resulting data is unprefixed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Items"></param>
        /// <param name="WriteItem"></param>
        public void WriteListContents<T>(IReadOnlyList<T> Items, Action<T> WriteItem)
        {
            foreach (var item in Items)
            {
                WriteItem(item);
            }
        }

        /// <summary>
        /// Writes the given encoding type to the output stream.
        /// </summary>
        /// <param name="Encoding"></param>
        public void WriteEncodingType(NodeEncodingType Encoding)
        {
            Writer.Write((byte)Encoding);
        }

        /// <summary>
        /// Writes the given template type to the output stream.
        /// </summary>
        /// <param name="Type"></param>
        public void WriteTemplateType(NodeTemplateType Type)
        {
            Writer.Write((byte)Type);
        }

        /// <summary>
        /// Writes a reference to the given symbol.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, Symbol Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        /// <summary>
        /// Writes a reference to the given string.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, string Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        /// <summary>
        /// Writes a reference to the given node template.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Value"></param>
        public void WriteReference(WriterState State, NodeTemplate Value)
        {
            WriteULeb128((uint)State.GetIndex(Value));
        }

        #endregion

        #region Node Writing

        /// <summary>
        /// Gets a node encoder for the given node.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public BinaryNodeEncoder GetEncoder(LNode Node)
        {
            if (Node.HasAttrs)
            {
                return BinaryNodeEncoder.AttributeEncoder;
            }
            else if (Node.IsCall)
            {
                if (Node.Target.IsId && !Node.Target.HasAttrs)
                {
                    return BinaryNodeEncoder.CallIdEncoder;
                }
                else
                {
                    return BinaryNodeEncoder.CallEncoder;
                }
            }
            else if (Node.IsId)
            {
                return BinaryNodeEncoder.IdEncoder;
            }
            else
            {
                object nodeVal = Node.Value;
                if (nodeVal == null)
                {
                    return BinaryNodeEncoder.NullEncoder;
                }
                else
                {
                    BinaryNodeEncoder result;
                    if (LiteralEncoders.TryGetValue(nodeVal.GetType(), out result))
                    {
                        return result;
                    }
                    else
                    {
                        throw new NotSupportedException("Node '" + Node.Print() + "' could not be encoded by any of the writer's known encoders.");
                    }
                }
            }
        }

        /// <summary>
        /// Writes the given node to the output stream, prefixed by its encoding type.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        public void WritePrefixedNode(WriterState State, LNode Node)
        {
            var encoder = GetEncoder(Node);
            WriteEncodingType(encoder.EncodingType);
            encoder.Encode(this, State, Node);
        }

        /// <summary>
        /// Writes the given node to the output stream, and returns
        /// its encoding type.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public NodeEncodingType WriteNode(WriterState State, LNode Node)
        {
            var encoder = GetEncoder(Node);
            encoder.Encode(this, State, Node);
            return encoder.EncodingType;
        }

        #endregion

        #region Header Writing

        /// <summary>
        /// Writes a symbol to the output stream.
        /// </summary>
        /// <returns></returns>
        public void WriteSymbol(string Symbol)
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes(Symbol);
            WriteULeb128((uint)data.Length);
            Writer.Write(data);
        }

        /// <summary>
        /// Writes the given string table to the output stream.
        /// </summary>
        /// <param name="Table"></param>
        public void WriteSymbolTable(IReadOnlyList<string> Table)
        {
            WriteList(Table, WriteSymbol);
        }

        /// <summary>
        /// Writes the given template definition to the output stream,
        /// prefixed by its template type.
        /// </summary>
        /// <param name="Template"></param>
        public void WriteTemplateDefinition(NodeTemplate Template)
        {
            WriteTemplateType(Template.TemplateType);
            Template.Write(this);
        }

        /// <summary>
        /// Writes the given template table to the output stream.
        /// </summary>
        /// <param name="Table"></param>
        public void WriteTemplateTable(IReadOnlyList<NodeTemplate> Table)
        {
            WriteList(Table, WriteTemplateDefinition);
        }

        /// <summary>
        /// Writes the given header to the output stream.
        /// </summary>
        /// <param name="Header"></param>
        public void WriteHeader(WriterState Header)
        {
            WriteSymbolTable(Header.Symbols);
            WriteTemplateTable(Header.Templates);
        }

        #endregion

        #region File Writing

        /// <summary>
        /// Writes the magic string to the output stream.
        /// </summary>
        public void WriteMagic()
        {
            Writer.Write(LoycBinaryHelpers.Magic.Select(Convert.ToByte).ToArray());
        }

        /// <summary>
        /// Writes the contents of a binary loyc file to the current output stream.
        /// </summary>
        /// <param name="Nodes"></param>
        public void WriteFileContents(IReadOnlyList<LNode> Nodes)
        {
            using (var memStream = new MemoryStream())
            using (var childWriter = new LoycBinaryWriter(memStream, this))
            {
                var state = new WriterState();
                childWriter.WriteList(Nodes, node => childWriter.WritePrefixedNode(state, node));

                memStream.Seek(0, SeekOrigin.Begin);

                WriteHeader(state);
                memStream.CopyTo(Writer.BaseStream);
            }
        }

        public void WriteFile(IReadOnlyList<LNode> Nodes)
        {
            WriteMagic();
            WriteFileContents(Nodes);
        }

        #endregion

        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
