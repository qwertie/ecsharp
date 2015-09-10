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
    /// Contains some helpers functions and constants that
    /// may be useful when reading or writing binary
    /// encoded loyc trees.
    /// </summary>
    public static class LoycBinaryHelpers
    {
        /// <summary>
        /// Gets the magic number for binary loyc tree files.
        /// </summary>
        public const string Magic = "BLT";

        /// <summary>
        /// Reads the given binary loyc tree file.
        /// </summary>
        /// <param name="InputStream"></param>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public static IReadOnlyList<LNode> ReadFile(Stream InputStream, string Identifier)
        {
            using (var reader = new LoycBinaryReader(InputStream))
            {
                return reader.ReadFile(Identifier);
            }
        }

        /// <summary>
        /// Writes a binary loyc tree file that contains the given list of nodes to the given output stream.
        /// </summary>
        /// <param name="OutputStream"></param>
        /// <param name="Nodes"></param>
        public static void WriteFile(Stream OutputStream, IReadOnlyList<LNode> Nodes)
        {
            using (var writer = new LoycBinaryWriter(OutputStream))
            {
                writer.WriteFile(Nodes);
            }
        }
    }
}
