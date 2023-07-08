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
		/// <summary>
		///   Options that control general behavior of <see cref="SyncJson.Reader"/> and 
		///   <see cref="SyncJson.Writer"/>. Note: some behaviors such as deduplication 
		///   (including support for cyclic references) are controlled at the level of
		///   individual fields, and such options are not duplicated here.
		/// </summary>
		/// <remarks>
		///   <see cref="Reader"/> and <see cref="Writer"/> do not make a copy of this
		///   object before using it, so for the most part it is possible to change 
		///   these options after constructing the reader/writer and have those changes 
		///   take effect somewhere in the middle of a JSON object. However, certain 
		///   options are cached for performance reasons, e.g. <see cref="Writer"/> 
		///   caches the UTF-8 version of <see cref="Options.ForWriter.Indent"/> and 
		///   <see cref="Options.ForWriter.Newline"/> during initialization, and 
		///   therefore will not notice if these properties change later.
		/// </remarks>
		public class Options
		{
			public Options(bool compactMode = false) => Write.Minify = compactMode;

			/// <summary>If true, Newtonsoft-style special fields "$id" and "$ref" will
			/// be used for deduplication and resolution of circular references,
			/// and byte arrays are encoded in Base64. If false, more compact 
			/// SyncLib-style references and BAIS encoding is used instead. When
			/// reading </summary>
			public bool NewtonsoftCompatibility { get; set; } = true;

			/// <summary>A function for altering names used in the first argument of 
			/// ISyncManager.Sync. To use camelCase, set this to <see cref="SyncJson.ToCamelCase"/></summary>
			public Func<string, string>? NameConverter { get; set; }

			/// <summary>The <see cref="ObjectMode"/> used to read/write the root object.
			///   This option has no effect if you are using <see cref="NewWriter"/> or 
			///   <see cref="NewReader"/>.</summary>
			public ObjectMode RootMode { get; set; } = ObjectMode.Normal;

			/// <summary>When NewtonsoftCompatibility is off, this property controls 
			/// the way byte arrays and byte lists are written. In special cases it
			/// can also affect the way reading happens (see Remarks). When writing 
			/// with NewtonsoftCompatibility enabled, Base64 is written instead of BAIS.</summary>
			/// <remarks>
			/// The documentation of <see cref="JsonByteArrayMode"/> explains each mode.
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

			#region Writer-specific options

			public ForWriter Write { get; set; } = new ForWriter();

			public class ForWriter
			{
				/// <summary>This property provides a quick way to set the <see cref="Newline"/>,
				/// <see cref="Indent"/> and <see cref="SpaceAfterColon"/> properties.
				/// When true, the JSON output will have minimal spaces and no newlines.
				/// When set to false, these properties are reset to defaults.</summary>
				public bool Minify {
					set {
						if (value) {
							Newline = Indent = "";
							SpaceAfterColon = false;
						} else if (Newline == "") {
							Newline = "\n";
							Indent = "\t";
							SpaceAfterColon = true;
						}
					}
				}

				/// <summary>String that represents a newline. This string should be 
				/// "\n" (Unix/Windows/Mac), "\r" (old-style Mac), or "\r\n" (Windows/DOS). 
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
				public int MaxIndentDepth { get; set; } = 63;

				/// <summary>If this property is true, or if this property is null and 
				/// NewtonsoftCompatibility is off, character lists and character arrays 
				/// are written as strings.</summary>
				public bool? CharListAsString { get; set; } = null;

				/// <summary>Initial size of the output buffer when writing JSON (default: 1024).
				/// This property is ignored if you provide your own buffer to <see cref="SyncJson.NewWriter"/></summary>
				public int InitialBufferSize { get; set; } = 1024;
			}

			#endregion

			#region Reader-specific options

			// TODO: reconsider how custom type conversions work before initial release
			public ForReader Read { get; set; } = new ForReader();

			public class ForReader
			{
				/// <summary>Whether to accept <c>//</c> and <c>/* */</c> comments when reading JSON.</summary>
				public bool AllowComments { get; set; } = true;

				/// <summary>Whether to follow JSON rules strictly when reading JSON. When 
				///   this mode is disabled, the following syntax does not cause an exception:
				///   (1) a comma before a closing ']' or '}',
				///   (2) a number with leading '.' or leading '0' (with other digits),
				///   (3) the \0 (null character) escape sequence,
				///   (4) invalid escape sequences (instead, \q is read as if it were \\q),
				///   (5) non-string object keys (which are essentially ignored).
				/// </summary><remarks>
				///   The legality of comments and garbage after EOF is controlled 
				///   independently with the <see cref="AllowComments"/> and 
				///   <see cref="VerifyEof"/> properties.
				///   <para/>
				///   Although JSON technically prohibits control characters, I felt it wasn't
				///   worth the performance cost of detecting control characters when 
				///   decoding strings, so Strict mode allow them.
				/// </remarks>
				public bool Strict { get; set; } = false;

				/// <summary>If objects are nested more than this many levels deep, an
				///   exception will be thrown and the reader will refuse to read further.
				///   This option helps avoid <see cref="StackOverflowException"/> while reading,
				///   which normally forces a .NET process to terminate.</summary>
				public int MaxDepth { get; set; } = 64;

				/// <summary>When you attempt to read a primitive (such as a string or double),
				///   but an object or a list is encountered instead, this property controls 
				///   how that object is converted to the primitive type. If this property is 
				///   left with its default value of null, an object or list cannot be 
				///   converted to any primitive type, so FormatException is thrown instead. 
				///   If you provide a delegate, it is given the property name and raw bytes 
				///   of a UTF-8 JSON object or list, and whatever value it returns is the 
				///   conversion result.</summary>
				/// <remarks>
				///   The first byte of the Memory buffer is '{' if the input is an object,
				///   or '[' if the input is a list.
				/// <para/>
				///   One way of starting to handle the conversion request would be to call 
				///   <see cref="NewReader"/> to begin parsing the memory buffer.
				/// <para/>
				///   If the target type is a string, the simplest implementation is to return the 
				///   JSON itself, which can be accomplished as follows:
				/// <code>
				/// 	// This code requires .NET Core 3+ (use json.ToArray() otherwise)
				/// 	var options = new SyncJson.Options {
				/// 		ReadObjectAsPrimitive = (name, json, t) => Encoding.UTF8.GetString(json.Span)
				/// 	};
				/// </code>
				/// </remarks>
				public Func<string?, ReadOnlyMemory<byte>, long, Type, IConvertible>? ObjectToPrimitive { get; set; } = null;

				/// <summary>When you attempt to read an object or a list, but a primitive
				///   type is encountered instead, this property controls how that primitive
				///   is converted to an object or list (see remarks)</summary>
				/// <remarks>
				///   The boolean parameter is true when a list is required.
				/// <para/>
				///   If this property is left with its default value of null, 
				///   (1) If the JSON value is a string, ReadStringAsObject is used instead
				///   (2) If the JSON value is a number, ReadNumberAsObject is used instead
				///   (3) Otherwise, the value cannot be converted to an object or list, so
				///       FormatException is thrown.
				/// </remarks>
				public Func<string, ReadOnlyMemory<byte>, bool, Memory<byte>>? PrimitiveToObject { get; set; } = null;

				/// <summary>When you attempt to read an object, but a string is encountered 
				///   instead, this property controls how that string is converted to an object.
				///   If this property is left with its default value of null, a string cannot
				///   be converted to an object and FormatException is thrown instead.
				///   If you provide a delegate, it is given the property name and string,
				///   and it must return valid JSON that will be read instead.</summary>
				public Func<string, string, bool, Memory<byte>>? StringToObject { get; set; } = null;

				public Func<string, ReadOnlyMemory<byte>, IConvertible>? ObjectToNumber { get; set; } = null;

				/// <summary>When attempting to read a boolean value, a string can be accepted 
				///   instead if that string has the value specified here.</summary>
				public string TrueAsString { get; set; } = "true";
				/// <summary>When attempting to read a boolean value, a string can be accepted 
				///   instead if that string has the value specified here.</summary>
				public string FalseAsString { get; set; } = "false";

				/// <summary>When this property is true and the root object has been read successfully,
				///   the reader checks whether there is additional non-whitespace text beyond the end 
				///   of what was read, and throws an exception if extra junk is encountered.</summary>
				public bool VerifyEof { get; set; } = true;

				/// <summary>This property controls <see cref="Reader"/>'s behavior when 
				///   a request is made to read a field that does not exist. If this property
				///   is true, the missing field is treated exactly as if it were set to null.
				///   Default: false.</summary>
				public bool AllowMissingFields { get; set; } = false;

				/// <summary>This property requests that if a property is set to null but read as 
				///   a primitive type, the default value of that type should be returned instead
				///   of throwing an exception. For example, if <see cref="Reader.Sync(FieldId, int)"/>
				///   encounters a null, it will return 0 instead if throwing an exception if this
				///   property is true.</summary>
				/// <seealso cref="ObjectMode.ReadNullAsDefault"/>
				public bool ReadNullPrimitivesAsDefault { get; set; } = false;
			}

			#endregion
		}

		/// <summary>Converts a string to camelCase format, e.g. "FileSize" => "fileSize" and
		/// "SQLQuery" => "sqlQuery". Assignable to <see cref="Options.NameConverter"/>.</summary>
		public static string ToCamelCase(string name)
		{
			char c;
			if (name.Length > 0 && (c = char.ToLowerInvariant(name[0])) != name[0]) {
				var name2 = new StringBuilder(name);
				name2[0] = c;
				for (int i = 1; i < name2.Length && char.IsUpper(name.TryGet(i + 1, 'A')); i++) {
					if ((c = char.ToLowerInvariant(name2[i])) == name2[i])
						break;
					name2[i] = c;
				}
				return name2.ToString();
			}
			return name;
		}
	}

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
