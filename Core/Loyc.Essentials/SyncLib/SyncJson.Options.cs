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
			/// <summary>String that represents a newline. This string should be 
			/// "\n" (Unix/Windows/Mac), "\r" (Mac only), or "\r\n" (Windows/DOS). 
			/// Also, the empty string "" can be used to disable both newlines and 
			/// indentation. Default: "\n"</summary>
			public string Newline { get; set; } = "\n";
			
			/// <summary>A string that is used to indent each line for each level of 
			/// object nesting. This property, which has no effect if Newline == "",
			/// should be either "\t" or zero or more spaces. Dafault: "\t"</summary>
			public string Indent { get; set; } = "\t";

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
