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
    /// A type that reads binary encoded loyc trees.
    /// </summary>
    public class LoycBinaryReader : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Creates a new loyc binary reader from the given 
        /// binary reader and set of decoders and template parsers.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="encodings">A mapping of literal node encodings to decoders.</param>
        /// <param name="templateParsers"></param>
        public LoycBinaryReader(BinaryReader reader,
            IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> literalEncoding,
            IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> templateParsers)
        {
            Reader = reader;
            LiteralEncodings = literalEncoding;
            TemplateParsers = templateParsers;
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given 
        /// input stream and set of decoders and template parsers.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="encodings">A mapping of literal node encodings to decoders.</param>
        /// <param name="templateParsers"></param>
        public LoycBinaryReader(Stream inputStream,
            IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> literalEncoding,
            IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> templateParsers)
            : this(new BinaryReader(inputStream), literalEncoding, templateParsers)
        {
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given binary reader.
        /// The default set of decoders and template parsers are used.
        /// </summary>
        /// <param name="reader"></param>
        public LoycBinaryReader(BinaryReader reader)
            : this(reader, DefaultEncodings, DefaultTemplateParsers)
        {
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given input stream.
        /// The default set of decoders and template parsers are used.
        /// </summary>
        /// <param name="inputStream"></param>
        public LoycBinaryReader(Stream inputStream)
            : this(new BinaryReader(inputStream))
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the inner binary reader.
        /// </summary>
        public BinaryReader Reader { get; private set; }

        /// <summary>
        /// Gets the mapping of literal encodings to decoders that this binary reader uses.
        /// Templated nodes and id nodes are treated as special cases, and are not part of this dictionary.
        /// </summary>
        public IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> LiteralEncodings { get; private set; }

        /// <summary>
        /// Gtes the mapping of node template types to node template parsers that this binary reader uses.
        /// </summary>
        public IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> TemplateParsers { get; private set; }

        #endregion

        #region Static

        /// <summary>
        /// Gets the default decoder dictionary.
        /// </summary>
        public static IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> DefaultEncodings
        {
            get
            {
                return new Dictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>>()
                {
                    { NodeEncodingType.String, (reader, state) => state.NodeFactory.Literal(reader.ReadStringReference(state)) },
                    { NodeEncodingType.Int8, CreateLiteralNodeReader(reader => reader.ReadSByte()) },
                    { NodeEncodingType.Int16, CreateLiteralNodeReader(reader => reader.ReadInt16()) },
                    { NodeEncodingType.Int32, CreateLiteralNodeReader(reader => reader.ReadInt32()) },
                    { NodeEncodingType.Int64, CreateLiteralNodeReader(reader => reader.ReadInt64()) },
                    { NodeEncodingType.UInt8, CreateLiteralNodeReader(reader => reader.ReadByte()) },
                    { NodeEncodingType.UInt16, CreateLiteralNodeReader(reader => reader.ReadUInt16()) },
                    { NodeEncodingType.UInt32, CreateLiteralNodeReader(reader => reader.ReadUInt32()) },
                    { NodeEncodingType.UInt64, CreateLiteralNodeReader(reader => reader.ReadUInt64()) },
                    { NodeEncodingType.Float32, CreateLiteralNodeReader(reader => reader.ReadSingle()) },
                    { NodeEncodingType.Float64, CreateLiteralNodeReader(reader => reader.ReadDouble()) },
                    { NodeEncodingType.Decimal, CreateLiteralNodeReader(reader => reader.ReadDecimal()) },
                    { NodeEncodingType.Boolean, CreateLiteralNodeReader(reader => reader.ReadBoolean()) },
                    { NodeEncodingType.Char, CreateLiteralNodeReader(reader => reader.ReadChar()) },
                    { NodeEncodingType.Void, CreateLiteralNodeReader(reader => @void.Value) },
                    { NodeEncodingType.Null, CreateLiteralNodeReader<object>(reader => null) }
                };
            }
        }

        /// <summary>
        /// Creates a decoder that creates a literal node based on a literal reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadLiteral"></param>
        /// <returns></returns>
        public static Func<LoycBinaryReader, ReaderState, LNode> CreateLiteralNodeReader<T>(Func<BinaryReader, T> ReadLiteral)
        {
            return (reader, state) => state.NodeFactory.Literal(ReadLiteral(reader.Reader));
        }

        /// <summary>
        /// Gets the default template parser dictionary.
        /// </summary>
        public static IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> DefaultTemplateParsers
        {
            get
            {
                return new Dictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>>()
                {
                    { NodeTemplateType.CallNode, CallNodeTemplate.Read },
                    { NodeTemplateType.CallIdNode, CallIdNodeTemplate.Read },
                    { NodeTemplateType.AttributeNode, AttributeNodeTemplate.Read }
                };
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Reads a LEB128 variable-length unsigned integer from the input stream.
        /// </summary>
        /// <param name="Value"></param>
        public uint ReadULeb128()
        {
            // C# translation of code borrowed from Wikipedia article:
            // https://en.wikipedia.org/wiki/LEB128
            uint result = 0;
            int shift = 0;
            while (true) 
            {
                byte b = Reader.ReadByte();
                result |= (uint)((b & 0x7F) << shift);
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }
            return result;
        }

        /// <summary>
        /// Reads an encoding type from the stream.
        /// </summary>
        /// <returns></returns>
        public NodeEncodingType ReadEncodingType()
        {
            return (NodeEncodingType)Reader.ReadByte();
        }

        /// <summary>
        /// Reads a length-prefixed list of items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadItem"></param>
        /// <returns></returns>
        public IReadOnlyList<T> ReadList<T>(Func<T> ReadItem)
        {
            int count = (int)ReadULeb128();
            return ReadListContents(ReadItem, count);
        }

        /// <summary>
        /// Reads an unprefixed list of items of the given length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadItem"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public IReadOnlyList<T> ReadListContents<T>(Func<T> ReadItem, int Length)
        {
            var results = new T[Length];
            for (int i = 0; i < Length; i++)
            {
                results[i] = ReadItem();
            }
            return results;
        }

        #endregion

        #region Header Parsing

        /// <summary>
        /// Reads a symbol as defined in the symbol table.
        /// </summary>
        /// <returns></returns>
        public string ReadSymbol()
        {
            int length = (int)ReadULeb128();
            byte[] data = Reader.ReadBytes(length);
            return UTF8Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Reads the symbol table.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> ReadSymbolTable()
        {
            return ReadList(ReadSymbol);
        }

        /// <summary>
        /// Reads the template definition table.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<NodeTemplate> ReadTemplateTable()
        {
            return ReadList(ReadTemplateDefinition);
        }

        /// <summary>
        /// Reads a single template definition.
        /// </summary>
        /// <returns></returns>
        public NodeTemplate ReadTemplateDefinition()
        {
            NodeTemplateType type = (NodeTemplateType)Reader.ReadByte();
            if (!TemplateParsers.ContainsKey(type))
            {
                throw new InvalidDataException("Unknown node template type.");
            }
            return TemplateParsers[type](this);
        }

        /// <summary>
        /// Reads a binary encoded loyc file's header.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public ReaderState ReadHeader(string Identifier)
        {
            var symbolTable = ReadSymbolTable();
            var templateTable = ReadTemplateTable();
            return new ReaderState(new LNodeFactory(new EmptySourceFile(Identifier)), symbolTable, templateTable);
        }

        #endregion

        #region Body Parsing

        /// <summary>
        /// Reads a reference to a symbol.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public Symbol ReadSymbolReference(ReaderState State)
        {
            return State.SymbolPool.GetGlobalOrCreateHere(ReadStringReference(State));
        }

        /// <summary>
        /// Reads a reference to a string in the symbol table.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public string ReadStringReference(ReaderState State)
        {
            int index = (int)ReadULeb128();

            if (index >= State.SymbolTable.Count)
            {
                throw new InvalidDataException("Symbol index out of bounds.");
            }

            return State.SymbolTable[index];
        }

        /// <summary>
        /// Reads a reference to a node template.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public NodeTemplate ReadTemplateReference(ReaderState State)
        {
            int index = (int)ReadULeb128();

            if (index >= State.TemplateTable.Count)
            {
                throw new InvalidDataException("Template index out of bounds.");
            }

            return State.TemplateTable[index];
        }

        /// <summary>
        /// Reads a template-prefixed templated node.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public LNode ReadTemplatedNode(ReaderState State)
        {
            var template = ReadTemplateReference(State);
            var args = template.ArgumentTypes.Select(type => ReadNode(State, type)).ToArray();
            return template.Instantiate(State, args);
        }

        /// <summary>
        /// Reads a node with the given encoding.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Encoding"></param>
        /// <returns></returns>
        public LNode ReadNode(ReaderState State, NodeEncodingType Encoding)
        {
            if (Encoding == NodeEncodingType.TemplatedNode)
            {
                return ReadTemplatedNode(State);
            }
            else if (Encoding == NodeEncodingType.IdNode)
            {
                return State.NodeFactory.Id(ReadSymbolReference(State));
            }

            Func<LoycBinaryReader, ReaderState, LNode> parser;

            if (LiteralEncodings.TryGetValue(Encoding, out parser))
            {
                return parser(this, State);
            }
            else
            {
                throw new InvalidDataException("Unknown node encoding: '" + Encoding + "'.");
            }
        }

        #endregion

        #region File Parsing

        /// <summary>
        /// Reads the file's magic string, and returns a boolean value
        /// that tells if it matched the loyc binary tree format's magic string.
        /// </summary>
        /// <returns></returns>
        public bool CheckMagic()
        {
            return Reader.ReadBytes(LoycBinaryHelpers.Magic.Length).Select(Convert.ToChar).SequenceEqual(LoycBinaryHelpers.Magic);
        }

        /// <summary>
        /// Reads a file encoded in the loyc binary tree format.
        /// This checks the magic number first, and then parses the 
        /// file's contents.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public IReadOnlyList<LNode> ReadFile(string Identifier)
        {
            if (!CheckMagic())
            {
                throw new InvalidDataException("The given stream's magic number did not read '" + LoycBinaryHelpers.Magic + "', which is the loyc binary tree format's magic string.");
            }

            return ReadFileContents(Identifier);
        }

        /// <summary>
        /// Reads the contents of a file encoded in the loyc binary tree format.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public IReadOnlyList<LNode> ReadFileContents(string Identifier)
        {
            var header = ReadHeader(Identifier);
            return ReadList(() => ReadNode(header, ReadEncodingType()));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Reader.Dispose();
        }

        #endregion
    }
}
