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
			/// be used for deduplication and resolution of circular references,
			/// and byte arrays are encoded in Base64. If false, more compact 
			/// SyncLib-style references and BAIS encoding is used instead. When
			/// reading </summary>
			public bool NewtonsoftCompatibility { get; set; } = true;

			/// <summary>When NewtonsoftCompatibility is off, this property controls 
			/// the way byte arrays and byte lists are written. In special cases it
			/// can also affect the way reading happens (see Remarks). When writing 
			/// with NewtonsoftCompatibility enabled, Base64 is written instead of BAIS.</summary>
			/// <remarks>
			/// The documentation of <see cref="ByteArrayMode"/> explains each mode.
			/// Generally, ByteArrayMode.PrefixedBais mode is recommended for the
			/// most compact output and best debugging experience (since BIAS preserves 
			/// long runs of ASCII characters in the output), but Base64 is required
			/// for Newtonsoft compatibility.
			/// <para/>
			/// Therefore, when <see cref="NewtonsoftCompatibility"/> is on, the BAIS
			/// modes are treated as Base64. In addition, Newtonsoft writes byte lists
			/// (List&lt;byte>) as JSON arrays, so SyncJson.Writer replicates this
			/// behavior too.
			/// <para/>
			/// When reading a byte array, the encoding is normally autodetected
			/// and the value of this property is not important. However, if the JSON
			/// contains a string, and NewtonsoftCompatibility is off, and the string 
			/// does not start with '!' or '\b' (both of which indicate BAIS encoding),
			/// it is unclear whether the string is BAIS or Base64. In this case, the 
			/// string is interpreted as BAIS in <see cref="JsonByteArrayMode.Bais"/> 
			/// mode, and Base64 otherwise.
			/// <para/>
			/// Therefore, from a backward compatibility standpoint, switching from 
			/// <see cref="JsonByteArrayMode.Bais"/> to <see cref="JsonByteArrayMode.PrefixedBais"/>
			/// mode is a backward compatibility hazard as <see cref="SyncJson.Reader"/>
			/// may try to interpret an unprefixed BAIS string as Base64. However, any
			/// other mode change is safe, because data previously written will 
			/// contain enough information to detect the data format.
			/// </remarks>
			public JsonByteArrayMode ByteArrayMode { get; set; } = JsonByteArrayMode.PrefixedBais;

			/// <summary>If this property is true, or if this property is null and 
			/// NewtonsoftCompatibility is off, character lists and character arrays 
			/// are written as strings.</summary>
			public bool? WriteCharListAsString { get; set; } = null;

			/// <summary>A function for altering names used in the first argument of 
			/// ISyncManager.Sync. To use camelCase, set this to <see cref="SyncJson.ToCamelCase"/></summary>
			public Func<string, string>? NameConverter { get; set; }

			public SubObjectMode RootMode { get; set; } = SubObjectMode.DynamicType;

			/// <summary>Whether to accept <c>//</c> and <c>/* */</c> comments when reading JSON.</summary>
			public bool AllowComments { get; set; } = true;
			
			/// <summary>Whether to follow JSON rules strictly when reading JSON, by 
			///   (1) prohibiting a comma before a closing ']' or '}', and 
			///   (2) prohibiting numbers with a leading '.' or '0'.</summary>
			public bool ReadStrictly { get; set; } = false;

			/// <summary>When you attempt to read a primitive (such as a string or double),
			///   but an object or a list is encountered instead, this property controls 
			///   how that object is converted to the primitive type. If this property is 
			///   left with its default value of null, an object or list cannot be 
			///   converted to any primitive type, so FormatException is thrown instead. 
			///   If you provide a delegate, it is given the property name and raw bytes 
			///   of a UTF-8 JSON object or list, and whatever value it returns is the 
			///   conversion result.</summary>
			/// <remarks>
			/// The first byte of the Memory buffer is '{' if the input is an object,
			/// or '[' if the input is a list.
			/// <para/>
			/// One way of starting to handle the conversion request would be to call 
			/// <see cref="NewReader"/> to begin parsing the memory buffer.
			/// <para/>
			/// If the target type is a string, the simplest implementation is to return the 
			/// JSON itself, which can be accomplished as follows:
			/// <code>
			/// 	// This code requires .NET Core 3+ (use json.ToArray() otherwise)
			/// 	var options = new SyncJson.Options {
			/// 		ReadObjectAsPrimitive = (name, json, t) => Encoding.UTF8.GetString(json.Span)
			/// 	};
			/// </code>
			/// </remarks>
			public Func<string, Memory<byte>, Type, IConvertible>? ReadObjectAsPrimitive { get; set; } = null;

			/// <summary>When you attempt to read an object or a list, but a primitive
			///   type is encountered instead, this property controls how that primitive
			///   is converted to an object or list (see remarks)</summary>
			/// <remarks>
			/// If this property is left with its default value of null, 
			/// (1) If the JSON value is a string, ReadStringAsObject is used instead
			/// (2) If the JSON value is a number, ReadNumberAsObject is used instead
			/// (3) Otherwise, the value cannot be converted to an object or list, so
			///     FormatException is thrown.
			/// </remarks>
			public Func<string, Memory<byte>, bool, Memory<byte>>? ReadPrimitiveAsObject { get; set; } = null;

			/// <summary>When you attempt to read an object, but a string is encountered 
			///   instead, this property controls how that string is converted to an object.
			///   If this property is left with its default value of null, a string cannot
			///   be converted to an object and FormatException is thrown instead.
			///   If you provide a delegate, it is given the property name and string,
			///   and it must return valid JSON that will be read instead.</summary>
			public Func<string, string, bool, Memory<byte>>? ReadStringAsObject { get; set; } = null;

			public Func<string, Memory<byte>, IConvertible>? ReadObjectAsNumber { get; set; } = null;
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
			return name;
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

	/// <summary>Used to control how byte arrays are encoded by <see cref="SyncJson.Writer"/>.</summary>
	[Flags]
	public enum JsonByteArrayMode
	{
		/// <summary>Requests byte arrays be written as JSON arrays of numbers.</summary>
		Array = 0,
		/// <summary>Requests byte arrays be written as Base64.</summary>
		Base64 = 1,
		/// <summary>Requests byte arrays be written as unprefixed BAIS 
		/// (<see cref="ByteArrayInString"/>), so that byte arrays in the 
		/// ASCII range are simply written as ASCII strings. This mode is
		/// useful if your byte arrays often contain ASCII and you suspect 
		/// that you might want to change the data type of your byte arrays 
		/// into strings at some point, but usually PrefixedBais is 
		/// recommended instead, because the prefix makes it clear that the 
		/// string is not Base64-encoded.</summary>
		Bais = 2,
		/// <summary>Requests byte arrays be written as BAIS 
		/// (<see cref="ByteArrayInString"/>) with a prefix of '!' or '\b'
		/// to indicate that BAIS encoding is being used. Note: in
		/// <see cref="SyncJson.Options.NewtonsoftCompatibility"/> mode,
		/// BAIS is unavailable; Base64 is used instead.</summary>
		PrefixedBais = 6,
	}
}
