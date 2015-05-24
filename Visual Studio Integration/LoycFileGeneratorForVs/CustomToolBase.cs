using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Loyc
{
	/// <summary>Helper class with boilerplate common to all single-file generators.</summary>
	public abstract class CustomToolBase : IVsSingleFileGenerator
	{
		/// <summary>Output file extension, including the dot. Although MS calls 
		/// it the "default" extension, there is no way to change your mind and 
		/// specify a different extension later.</summary>
		protected abstract string DefaultExtension();
		public int DefaultExtension(out string defExt)
		{
			return (defExt = DefaultExtension()).Length;
		}
		
		/// <summary>This method runs the single-file generator.</summary>
		/// <param name="inputFilePath">Input file name</param>
		/// <param name="inputFileContents">Input file contents</param>
		/// <param name="defaultNamespace">This is the contents of the "Default 
		/// Namespace" field in the VS Properties panel.</param>
		/// <param name="progressCallback">Call progressCallback.GeneratorError() 
		/// to send errors to VS, where the first argument (fWarning) is 1 for 
		/// warnings and 0 for errors. You cannot send errors from a worker thread.</param>
		/// <returns>The contents of the output file, as a byte array.</returns>
		protected abstract byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback);

		public virtual int Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IntPtr[] outputFileContents, out uint outputSize, IVsGeneratorProgress progressCallback)
		{
			try {
				byte[] outputBytes = Generate(inputFilePath, inputFileContents, defaultNamespace, progressCallback);
				if (outputBytes != null) {
					outputSize = (uint)outputBytes.Length;
					outputFileContents[0] = Marshal.AllocCoTaskMem(outputBytes.Length);
					Marshal.Copy(outputBytes, 0, outputFileContents[0], outputBytes.Length);
				} else {
					outputFileContents[0] = IntPtr.Zero;
					outputSize = 0;
				}
				return 0; // S_OK
			} catch(Exception e) {
				// Error msg in Visual Studio only gives the exception message, 
				// not the stack trace. Workaround:
				throw new COMException(string.Format("{0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
			}
		}
	}
}
