using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

namespace VsMultipleFileGenerator
{
    [Guid("6EE05D8F-AAF9-495e-A8FB-143CD2DC03F5")]
    public class HtmlImageEmbedderCustomTool : VsMultipleFileGenerator<string>
    {


        public override IEnumerator<string> GetEnumerator()
        {
            Stream inStream = File.OpenRead(base.InputFilePath);
            Regex regAnchor = new Regex("<img src=[\"']([^\"']+)[\"'][^>]+[/]?>", RegexOptions.IgnoreCase);
            try
            {
                StreamReader reader = new StreamReader(inStream);
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    MatchCollection mc = regAnchor.Matches(line);
                    foreach (Match match in mc)
                    {
                        // yield each element to the enumerator
                        yield return match.Groups[1].Value;
                    }
                }
            }
            finally
            {
                inStream.Close();
            }
        }

        protected override string GetFileName(string element)
        {
            return element.Substring(element.LastIndexOf('/') + 1);
        }

        public override byte[] GenerateContent(string element)
        {
            // create the image file
            WebRequest getImage = WebRequest.Create(element);

            return StreamToBytes(getImage.GetResponse().GetResponseStream());
        }

        public override byte[] GenerateSummaryContent()
        {
            // Im not going to put anything in here...
            return new byte[0];
        }

        public override string GetDefaultExtension()
        {
            return ".txt";
        }

        protected byte[] StreamToBytes(Stream stream)
        {
            MemoryStream outBuffer = new MemoryStream();

            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outBuffer.Write(buffer, 0, count);
            }

            return outBuffer.ToArray();
        }
    }
}
