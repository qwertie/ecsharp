// Namespace documentation picked up by Doxygen
// Also includes NS documentation for Loyc.Syntax and Loyc.Utilities

/*! \mainpage Loyc
 *
 * \section intro_sec Documentation Comments
 *
 * This documentation is extacted from the source code. It covers the following libraries:
 * 
 * - Loyc.Essentials.dll
 * - Loyc.Collections.dll
 * - Loyc.Syntax.dll
 * - Loyc.Utilities.dll
 * - Ecs.exe
 * - LeMP.exe
 * - LLLPG.exe
 *
 * Caution: this documentation is updated less often than the source code and Doxygen 
 * sometimes trips over the syntax of the XML doc comments in the source code, 
 * causing incorrect output. I am thinking about whether to switch to a different
 * documentation generator; ideas welcome.
 * 
 * Click the "Namespaces" or "Classes" tab to begin.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>Main Loyc namespace. This namespace includes all general-purpose 
/// code in the Loyc megaproject. It includes the code of Loyc.Essentials.dll 
/// and Loyc.Collections.dll (collections, geometry, MiniTest, MessageSink, etc.),
/// and also the code of Loyc.Syntax.dll.</summary>
namespace Loyc
{
	/// <summary>Contains general-purpose interfaces (<see cref="Loyc.Collections.IListSource{T}"/>, ranges, etc.),
	/// collection implementations (<see cref="Loyc.Collections.DList{T}"/>, <see cref="Loyc.Collections.WeakValueDictionary{K,V}"/>, etc.),
	/// extension methods (<see cref="Loyc.Collections.LCExt"/>, <see cref="Loyc.Collections.EnumerableExt"/>, etc.), 
	/// helper classes (<see cref="Loyc.Collections.EmptyList{T}"/>, (<see cref="Loyc.Collections.Repeated{T}"/>, etc.), and
	/// adapter classes (<see cref="Loyc.Collections.ListSlice{T}"/>, <see cref="Loyc.Collections.BufferedSequence{T}"/>, etc.).
	/// </summary>
	namespace Collections
	{
		/// <summary>Contains helper classes and base classes for implementing collections (<see cref="Loyc.Collections.Impl.InternalList{T}"/>,
		/// <see cref="Loyc.Collections.Impl.ListExBase{T}"/>, <see cref="Loyc.Collections.Impl.ListSourceBase{T}"/>, etc.)
		/// Also contains the AList and CPTrie node classes, which perhaps should not be <c>public</c>...</summary>
		namespace Impl { }
	}

	/// <summary>Important interfaces of newer .NET versions than the version 
	/// for which Loyc was compiled</summary>
	namespace Compatibility { }

	/// <summary>Helper classes for multithreaded code.</summary>
	namespace Threading { }

	/// <summary>Contains general-purpose math algorithms beyond what is provided 
	/// in the .NET BCL (Base Class Library). Notable class: <see cref="Math.MathEx"/>.</summary>
	namespace Math { }

	/// <summary>Contains math code and data types for processing geometry (points, 
	/// lines, polygons, etc.). Basic geometry stuff is in Loyc.Essentials.dll, 
	/// while more advanced algorithms are found in Loyc.Utilities.dll.</summary>
	namespace Geometry { }

	/// <summary>A stripped-down NUnit lookalike which allows you to put simple unit 
	/// tests in an assembly without having to add a reference to <c>NUnit.Framework.dll</c>.</summary>
	namespace MiniTest { }

	/// <summary>Loyc.Syntax.dll: contains Loyc trees (<see cref="Loyc.Syntax.LNode"/>),
	/// lexing stuff, LLLPG base classes (BaseParser{T} and BaseLexer), the LES parser and 
	/// printer, data types related to source code management (e.g. <see cref="ISourceFile"/>, 
	/// <see cref="SourceRange"/>) and other general-purpose code related to the 
	/// manipulation of syntax.
	namespace Syntax
	{
		/// <summary>Contains classes related to Loyc Expression Syntax (LES),
		/// including the parser and printer (reachable through 
		/// <see cref="Loyc.Syntax.Les.LesLanguageService"/>).</summary>
		namespace Les { }

		/// <summary>Contains classes related to lexical analysis, such as 
		/// the universal token type (<see cref="Loyc.Syntax.Lexing.Token"/>) 
		/// and <see cref="Loyc.Syntax.Lexing.TokensToTree"/>.</summary>
		namespace Lexing { }
	}
	
	/// <summary>Contains general-purpose classes that are not considered important 
	/// enough to go directly into the <c>Loyc</c> namespace. Most of the classes 
	/// in this namespace are defined in Loyc.Utilities.dll.</summary>
	namespace Utilities { }

	/// <summary>Code related to LLLPG, the Loyc LL(k) Parser Generator (LLLPG.exe).</summary>
	namespace LLParserGenerator { }
}
