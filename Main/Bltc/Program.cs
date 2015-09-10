using Loyc;
using Loyc.Binary;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bltc
{
    public enum TreeFormat
    {
        Les,
        Blt,
        Unknown
    }

    public static class Program
    {
        public static void Main(string[] Args)
        {
            if (Args.Length != 1 && Args.Length != 2)
            {
                Console.WriteLine("bltc accepts one or two arguments: an input path and an output path.");
                Console.WriteLine("BLT files will be converted to LES files, and vice-versa.");
                return;
            }

            string filePath = Args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Input file '" + filePath + "' could not be found. Stopping here.");
                return;
            }

            var parseResults = ParseFile(filePath);
            var nodes = parseResults.Item1;
            var inputType = parseResults.Item2;
            if (nodes == null)
            {
                Console.WriteLine("Could not parse input. Stopping here.");
                return;
            }

            
            string outputPath = Args.Length > 1 ? Args[1] : Path.ChangeExtension(filePath, GetExtension(InvertType(inputType)));
            var outputType = InferTypeFromExtension(outputPath);

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                if (outputType == TreeFormat.Blt)
                {
                    LoycBinaryHelpers.WriteFile(fs, nodes.ToArray());
                }
                else
                {
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(LesLanguageService.Value.PrintMultiple(nodes));
                    }
                }
            }
        }

        private static Pair<IEnumerable<LNode>, TreeFormat> ParseFile(string FileName)
        {
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            {
                return ParseContents(fs, FileName);
            }
        }

        private static Pair<IEnumerable<LNode>, TreeFormat> ParseContents(Stream Input, string FileName)
        {
            try
            {
                switch (InferTypeFromExtension(FileName))
	            {
                    case TreeFormat.Blt:
                        return Pair.Create(ReadBltFile(Input, FileName), TreeFormat.Blt);
                    case TreeFormat.Les:
                        return Pair.Create(ReadLesFile(Input, FileName), TreeFormat.Les);
		            default:
                        return ReadUnknownFile(Input, FileName);
	            }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error while parsing input.");
                Console.WriteLine(ex);
                return Pair.Create<IEnumerable<LNode>, TreeFormat>(null, TreeFormat.Unknown);
            }
        }

        private static IEnumerable<LNode> ReadBltFile(Stream Input, string FileName)
        {
            var results = ReadBlt(Input, FileName);
            if (results == null)
            {
                Console.WriteLine("The given input file did not have a blt magic string.");
            }
            return results;
        }

        private static IEnumerable<LNode> ReadLesFile(Stream Input, string FileName)
        {
            using (var reader = new StreamReader(Input))
            {
                return LesLanguageService.Value.Parse((UString)reader.ReadToEnd(), FileName, new ConsoleMessageSink());
            }
        }

        private static Pair<IEnumerable<LNode>, TreeFormat> ReadUnknownFile(Stream Input, string FileName)
        {
            var nodes = ReadBlt(Input, FileName);
            if (nodes != null)
            {
                return Pair.Create<IEnumerable<LNode>, TreeFormat>(nodes, TreeFormat.Blt);
            }
            else
            {
                return Pair.Create(ReadLesFile(Input, FileName), TreeFormat.Les);
            }
        }

        private static TreeFormat InferTypeFromExtension(string FileName)
        {
            switch (Path.GetExtension(FileName).ToLower())
            {
                case ".blt":
                    return TreeFormat.Blt;
                case ".les":
                    return TreeFormat.Les;
                default:
                    return TreeFormat.Unknown;
            }
        }

        private static TreeFormat InvertType(TreeFormat Format)
        {
            switch (Format)
            {
                case TreeFormat.Les:
                    return TreeFormat.Blt;
                case TreeFormat.Blt:
                    return TreeFormat.Les;
                default:
                    return TreeFormat.Unknown;
            }
        }

        private static string GetExtension(TreeFormat Format)
        {
            switch (Format)
            {
                case TreeFormat.Les:
                    return ".les";
                case TreeFormat.Blt:
                    return ".blt";
                case TreeFormat.Unknown:
                default:
                    return string.Empty;
            }
        }

        private static IReadOnlyList<LNode> ReadBlt(Stream Input, string FileName)
        {
            var reader = new LoycBinaryReader(Input);
            bool isBlt = reader.CheckMagic();
            if (!isBlt)
            {
                Input.Seek(0, SeekOrigin.Begin);
                return null;
            }
            else
            {
                return reader.ReadFileContents(FileName);
            }
        }
    }
}
