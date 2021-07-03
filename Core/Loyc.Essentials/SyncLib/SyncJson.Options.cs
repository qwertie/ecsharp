using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

#nullable enable

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		public class Options
		{
			public Options(bool compactMode = false)
			{
				if (compactMode) {
					Newline = Indent = "";
					SpaceAfterColon = false;
				}
			}

			/// <summary>String that represents a newline. This string should be 
			/// "\n" (Unix/Windows/Mac), "\r" (Mac only), or "\r\n" (Windows/DOS). 
			/// Also, the empty string "" can be used to disable both newlines and 
			/// indentation. Default: <see cref="Environment.NewLine"/></summary>
			public string Newline { get; set; } = Environment.NewLine;
			
			/// <summary>A string that is used to indent each line for each level of 
			/// object nesting. This property, which has no effect if Newline == "",
			/// should be either "\t" or zero or more spaces. Dafault: "\t"</summary>
			public string Indent { get; set; } = "\t";

			/// <summary>Whether to write a space after `:` in a key-value pair.</summary>
			public bool SpaceAfterColon { get; set; } = false;

			/// <summary>If this is true, Unicode characters above U+009F are written 
			/// using JSON escapes (e.g. \u00A3 instead of £). Default: false.
			/// Note: control characters are always written as escape sequences 
			/// because the JSON standard does not allow control characters.</summary>
			public bool EscapeUnicode { get; set; } = false;

			/// <summary>If the recursion depth exceeds this number when writing JSON, 
			/// the number of indents stops increasing.</summary>
			public int MaxIndentDepth { get; set; } = 255;
			
			/// <summary>If true, Newtonsoft-style special fields "$id" and "$ref" will
			/// be used for deduplication and resolution of circular references.
			/// If false, more compact SyncLib-style references are used instead. When
			/// reading JSON, this setting is ignored and both formats are supported.</summary>
			public bool NewtonsoftCompatibility { get; set; } = true;

			/// <summary>When set to true, <see cref="SyncJson.Writer"/> uses BAIS 
			/// encoding when Sync() is called on a byte array. BAIS uses about 37% as 
			/// many bytes (73% less) as a standard whitespace-free array encoding, or 
			/// less if the bytes contain long runs of ASCII characters in the range 
			/// 32 to 126 (because these are encoded verbatim). See Remarks regarding 
			/// the effect of using null or false for this property.</summary>
			/// <remarks>
			/// When writing JSON, if this property set to null, BAIS is written only 
			/// when NewtonsoftCompatibility is false. Null is the default value.
			/// <para/>
			/// When reading JSON, strings will be interpreted as BAIS in contexts where 
			/// a byte array is expected, unless this property is false. If this is 
			/// false, then the string will cause a type mismatch exception.
			/// </remarks>
			public bool? UseBais { get; set; } = null;

			/// <summary>A function for altering names used in the first argument of 
			/// ISyncManager.Sync. To use camelCase, set this to <see cref="SyncJson.ToCamelCase"/></summary>
			public Func<string, string>? NameConverter { get; set; }

			public SubObjectMode RootMode { get; set; } = SubObjectMode.DynamicType;
		}

		/// <summary>Gets a copy of a string with the first character changed to lowercase.
		/// Assignable to <see cref="Options.NameConverter"/>.</summary>
		public static string ToCamelCase(string name)
		{
			char c;
			if (name.Length > 0 && (c = char.ToLowerInvariant(name[0])) != name[0]) {
				var name2 = new StringBuilder(name);
				name2[0] = c;
				return name2.ToString();
			}
			return "";
		}
	}

    // TODO: review Json.NET (NewtonSoft.Json?) which "does have support for referential types containing cyclic relationships"
    // https://trycatch.me/xml-json-serialization-of-object-graphs-with-cyclic-references-in-net/
    //public static partial class SyncJson
    //{
    //    static readonly Symbol _typeMarker = (Symbol)"\t";
    //    static readonly Symbol _objectRef = (Symbol)"\r";
    //    //public SyncTypeRegistry? TypeRegistry { get; set; }
	//
    //    public SyncJson() { }
	//
    //    public void Save<T>(Stream stream, T rootObject)
    //    {
    //        // Goal:
    //        //{
    //        //    "\r": 1, // reference
    //        //    "\t": "Person", // type
    //        //    "Name": "Jack",
    //        //    "Age": 11,
    //        //    "Siblings": [
    //        //        { "\r": 2,
    //        //          "Name": "Jill",
    //        //          "Age": 9,
    //        //          "Siblings": [{ "\r": 1 }]
    //        //        }
    //        //    ]
    //        //}
	//
    //        var sync = new Synchronizer(stream, TypeRegistry ?? SyncTypeRegistry.Default);
    //        sync.Sync(null, rootObject);
    //    }
	//
    //    private SyncObjectFunc<JsonWriter, T> GetSyncObjectFunc<T>()
    //    {
    //    }
    //}
}
