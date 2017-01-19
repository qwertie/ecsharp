/*
 * Created David on 7/20/2007 at 2:21 PM
 */

using System;
using System.Text;
using Loyc.Threading;
using System.Resources;
using System.Globalization;
using Loyc;

namespace Loyc
{
	/// <summary>
	/// Localize is a global hook into which a string-mapping localizer can be
	/// installed. It makes your program localization-ready with no effort.
	/// </summary><remarks>
	/// The idea of this Localize facility is to convince programmers to support
	/// localization by making it dead-easy to do. By default it is not connected to
	/// any translator (it just passes strings through), so people who are only
	/// writing a program for a one-language market can easily make their code
	/// "multiligual-ready" without doing any extra work. All you do is call the
	/// <c>.Localized()</c> extension method, which is actually shorter than the
	/// traditional <c>string.Format()</c>.
	/// <p/>
	/// The translation system itself is separate from this class, and connected 
	/// to `Localized()` by a delegate, so that multiple translation systems are 
	/// possible. This class should be suitable for use in any .NET program, and 
	/// some programs using this utility will want to plug-in a different localizer.
	/// <p/>
	/// Use it like this:
	/// <code>
	/// string result = "Hello, {0}".Localized(userName);
	/// </code>
	/// Or, for increased clarity, use named placeholders:
	/// <code>
	/// string result = "Hello, {person's name}".Localized("person's name", userName);
	/// </code>
	/// Whatever localizer is installed will look up the text in its database and
	/// return a translation. If no translation to the end user's language is
	/// available, an appropriate default translation should be returned: either the
	/// original text, or a translation to some default language, e.g. English.
	/// <para/>
	/// The localizer will need an external table of translations, conceptually like 
	/// this:
	/// <pre>
	/// | Key name      | Language | Translated text |
	/// | "Hello, {0}"  | "es"     | "Hola, {0}"     |
	/// | "Hello, {0}"  | "fr"     | "Bonjour, {0}"  |
	/// | "Load"        | "es"     | "Cargar"        |
	/// | "Load"        | "fr"     | "Charge"        |
	/// | "Save"        | "es"     | "Guardar"       |
	/// | "Save"        | "fr"     | "Enregistrer"   |
 	/// </pre>
	/// Many developers use a resx file to store translations. This class supports 
	/// that approach, as explained below.
	/// <para/>
	/// For longer messages, it is preferable to use a short name to represent the
	/// message so that, when the English language text is edited, the translation
	/// tables do not have to be updated. To do this, use the 
	/// <see cref="Symbol(string, string, object[])"/> method:
	/// <pre>
	/// string result = Localize.Symbol("ConfirmQuitWithoutSaving", 
	///		"Are you sure you want to quit without saving '{filename}'?", "filename", fileName);
	/// 
	/// // Enhanced C# syntax with symbol literal
	/// string result = Localize.Symbol(@@ConfirmQuitWithoutSaving, 
	///		"Are you sure you want to quit without saving '{filename}'?", "filename", fileName);
	/// </pre>
	/// This is most useful for long strings or paragraphs of text, but I expect
	/// that some projects, as a policy, will use symbols for all localizable text.
	/// (When using Localize.Symbol, the actual message is allowed to be null. In 
	/// that case, the symbol (first argument) is returned as a last resort if no 
	/// translation is found.
	/// <p/>
	/// If the variable argument list is not empty, <see cref="Localize.Formatter"/> 
	/// is called to build the completed string from the format string. It is 
	/// possible to perform formatting separately, for example:
	/// <code>
	/// Console.WriteLine("{0} is {0:X} in hexadecimal".Localized(), N);
	/// </code>
	/// Here, WriteLine itself performs the formatting instead. 
	/// <p/>
	/// As demonstrated above, Localize's default formatter, <see cref="StringExt.FormatCore"/>, 
	/// has an extra feature that the standard formatter does not: named arguments. 
	/// Here is an example:
	/// <code>
	/// ...
	/// string verb = (IsFileLoaded ? "parse" : "load").Localized();
	/// MessageBox.Show(
	///     "Not enough memory to {load/parse} '{filename}'.".Localized(
	///       "load/parse", verb, "filename", FileName));
	/// </code>
	/// As you can see, named arguments are mentioned in the format string by
	/// specifying an argument name such as <c>{filename}</c> instead of a number 
	/// like <c>{0}</c>. The variable argument list contains the same name followed 
	/// by its value, e.g. "filename", FileName. This feature gives you, the 
	/// developer, the opportunity to tell the person writing translations what 
	/// the purpose of a particular argument is.
	/// <p/>
	/// The translator must not change any of the arguments: the word "{filename}"
	/// is not to be translated.
	/// <p/>
	/// At run-time, the format string with named arguments is converted to a
	/// "normal" format string with numbered arguments. The above example would
	/// become "Could not {1} the file: {3}" and then be passed to string.Format.
	/// 
	/// <h3>Design rationale</h3>
	/// 
	/// Many developers don't want to spend time writing internationalization or
	/// localization code, and are tempted to write code that is only for one
	/// language. It's no wonder, because it's a relative pain in the neck.
	/// Microsoft suggests that code carry around a "ResourceManager" object and
	/// directly request strings from it:
	/// <code>
	/// private ResourceManager rm;
	/// rm = new ResourceManager("AssemblyName.Resources", this.GetType().Assembly);
	/// Console.Writeline(rm.GetString("StringIdentifier"));
	/// </code>
	/// This approach has drawbacks:
	/// 
	/// * It may be cumbersome to pass around a ResourceManager instance between all
	///   classes that might contain localizable strings; a global facility is
	///   much more convenient.
	/// * The programmer has to put all translations in the resource file;
	///   consequently, writing the code is bothersome because the programmer has
	///   to switch to the resource file and add the string to it. Someone reading
	///   the code, in turn, can't tell what the string says and has to load up
	///   the resource file to find out.
	/// * It is not easy to change the localization manager; for instance, what if
	///   someone wants to store translations in an .ini, .xml or .les file rather 
	///   than inside the assembly? What if the user wants to centralize all
	///   translations for a set of assemblies, rather than having separate
	///   resources in each assembly? 
	/// <p/>
	/// Microsoft does address the first of these drawbacks by providing a code 
	/// generator built into Visual Studio that gives you a global property for
	/// each string; see
	/// http://stackoverflow.com/questions/1142802/how-to-use-localization-in-c-sharp
	/// <p/>
	/// Even so, you may find that this class provides a more convenient approach 
	/// because your native-language strings are written right in your code, and 
	/// because you are guaranteed to get a string at runtime (not null) if the 
	/// desired language is not available.
	/// <p/>
	/// This class supports ResourceManager via the <see cref="UseResourceManager"/> 
	/// helper method. For example, after calling 
	/// <c>Localize.UseResourceManager(resourceManager)</c>, if you write 
	/// <code>
	/// "Save As...".Localized()
	/// </code>
	/// Then <c>resourceManager.GetString("Save As...")</c> is called to get the 
	/// translated string, or the original string if no translation was found. 
	/// You can even add a "name calculator" to encode your resx file's naming 
	/// convention, e.g. by removing spaces and punctuation (for details, see 
	/// <see cref="UseResourceManager"/>.)
	/// <p/>
	/// It is conventional in .NET programs to have one "main" resx file, e.g.
	/// Resources.resx, that contains default strings, along other files with 
	/// non-English translations (e.g. Resources.es.resx for Spanish). When using
	/// Localized() you would typically use a slightly different approach: you
	/// still have a Resources.resx file, but you leave the string table empty. 
	/// This causes Visual Studio to generate a Resources class with a 
	/// ResourceManager property so that you need can easily get the ResourceManager 
	/// object.
	/// <ol>
	/// <li>When your program starts, call <c>Localize.UseResourceManager(Resources.ResourceManager)</c>.</li>
	/// <li>Use the <c>Localized()</c> extension method to get translations of short strings.</li>
	/// <li>For long strings, use <c>Localize.Symbol("ShortAlias", "Long string", params...)</c>.
	/// The first argument is the string passed to ResourceManager.GetString()</li>
	/// </ol>
	/// In the open source world, most developers don't have a team of translators
	/// ready make translations for them. The idea of Loyc, for example, is that
	/// many different individuals--not one big team--of programmers will create
	/// and maintain features. By centralizing this translation facility, it should
	/// be straightforward for a single multilingual individual to translate the
	/// text of many modules made by many different people.
	/// <p/>
	/// To facilitate this, I propose that in addition to a translator, a program
	/// should be made to figure out all the strings/symbols for which translations 
	/// are needed. To do this it would scan source code (at compile time) for 
	/// calls to methods in this class and generate a list of strings and symbols 
	/// needing translation. It would also have to detect certain calls that
	/// perform translation implicity, such as IMessageSink.Write(). See
	/// <see cref="LocalizableAttribute"/>.
	/// <p/>
	/// TODO: expand I18N features based on Mozilla's L20N.
	/// </remarks>
	public static class Localize
	{
		public static ThreadLocalVariable<LocalizerDelegate> _localizer = new ThreadLocalVariable<LocalizerDelegate>(Passthru, autoFallback: true);
		public static ThreadLocalVariable<FormatterDelegate> _formatter = new ThreadLocalVariable<FormatterDelegate>(StringExt.FormatCore, autoFallback: true);

		/// <summary>Localizer method, which is a do-nothing pass-through by default</summary>
		public static LocalizerDelegate Localizer
		{
			get { return _localizer.Value; }
		}

		/// <summary>Sets the localizer method.</summary>
		/// <remarks><see cref="Localizer"/> is a thread-local value, but since
		/// .NET does not support inheritance of thread-local values, this method
		/// also sets the global default used by threads on which this method was 
		/// never called.
		/// <para/>
		/// This property follows the Ambient Service Pattern:
		/// http://core.loyc.net/essentials/ambient-service-pattern.html
		/// </remarks>
		public static SavedValue<LocalizerDelegate> SetLocalizer(LocalizerDelegate newValue)
		{
			return new SavedValue<LocalizerDelegate>(_localizer, newValue ?? Passthru);
		}

		/// <summary>Uses a standard <see cref="ResourceManager"/> object to obtain translations.</summary>
		/// <param name="manager">A ResourceManager that provides access to resources (resx embedded in an assembly)</param>
		/// <param name="cultureInfo">A value of <see cref="CultureInfo"/> that
		/// represents the language to look up in the ResourceManager. If this is
		/// null, the ResourceManager will use CultureInfo.CurrentUICulture.</param>
		/// <param name="resxNameCalculator">An optional function that will be 
		/// called when a translation is requested without providing a resource 
		/// key symbol. For example, if someone writes <c>"Save as...".Localized()</c>
		/// using the <see cref="Localized(string)"/> extension method, this 
		/// function is called on the string "Save as...". This function could
		/// be used to compute a resource name such as "strSaveAs" automatically,
		/// according to whatever naming convention is used in your resource file.
		/// </param>
		/// <param name="fallbackToPrevious">If a translation was not found in the 
		/// specified ResourceManager and this parameter is true, the previously-
		/// installed <see cref="Localizer"/> is called instead. Otherwise, the 
		/// dummy translator <see cref="Passthru"/> is used.</param>
		/// <returns></returns>
		public static SavedValue<LocalizerDelegate> UseResourceManager(ResourceManager manager, 
			CultureInfo culture = null, Func<string, string> resxNameCalculator = null, bool fallbackToPrevious = true)
		{
			CheckParam.IsNotNull("manager", manager);

			LocalizerDelegate fallback = (fallbackToPrevious ? Localizer : null) ?? Passthru;

			return SetLocalizer((Symbol resourceId, string defaultMessage) =>
			{
				string id;
				if (resourceId != null)
					id = resourceId.Name;
				else
					id = (resxNameCalculator != null ? resxNameCalculator(defaultMessage) : null) ?? defaultMessage;

				var translation = manager.GetString(id, culture);
				if (translation != null)
					return translation;
				else
					return fallback(resourceId, defaultMessage);
			});
		}

		/// <summary>Formatting delegate, which is <see cref="StringExt.FormatCore"/> by default.</summary>
		public static FormatterDelegate Formatter
		{
			get { return _formatter.Value; }
		}
		
		/// <summary>Sets the formatter method.</summary>
		/// <remarks><see cref="Formatter"/> is a thread-local value, but since
		/// .NET does not support inheritance of thread-local values, this method
		/// also sets the global default used by threads on which this method was 
		/// never called.
		/// </remarks>
		public static SavedValue<FormatterDelegate> SetFormatter(FormatterDelegate newValue)
		{
			return new SavedValue<FormatterDelegate>(_formatter, newValue ?? StringExt.FormatCore);
		}

		/// <summary>
		/// This is the dummy translator, which is the default value of Localizer. 
		/// It passes strings through untranslated. A msgId symbol cannot be handled 
		/// so it is simply converted to a string.
		/// </summary>
		public static string Passthru(Symbol msgId, string msg)
		{
            return msg ?? (msgId == null ? null : msgId.Name);
		}

		#region Main Localize() and Symbol() methods

		/// <summary>
		/// This is the heart of the Localize class, which localizes and formats a string.
		/// </summary>
		/// <param name="resourceId">Resource ID used to look up a translated format
		/// string using the current user-defined <see cref="Localizer"/>. If this 
		/// parameter is null, a message must be provided; otherwise, the message is only 
		/// used if no translation is associated with the specified Symbol.</param>
		/// <param name="message">The message to be translated, which may include 
		/// argument placeholders (e.g. "{0}"). The default formatter also accepts 
		/// named parameters like "{firstName}"; see <see cref="StringExt.FormatCore"/> 
		/// for details.</param>
		/// <param name="args">Arguments given to <see cref="Formatter"/> to fill in 
		/// placeholders after the Localizer is called. If args is null or empty then 
		/// Formatter is not called.</param>
		/// <returns>The translated and formatted string.</returns>
		public static string Symbol(this Symbol resourceId, [Localizable] string message, params object[] args)
		{
			string localized = Localizer(resourceId, message);
			if (args == null || args.Length == 0)
				return localized;
			else
				return Formatter(localized, args);
		}
		
		/// <inheritdoc cref="Symbol(Loyc.Symbol, string, object[])"/>
		public static string Symbol(this string resourceId, [Localizable] string message, params object[] args)
			{ return Symbol((Symbol)resourceId, message, args); }
		
		/// <summary>Finds and formats a localization of the given message. If none is 
		/// found, the original string is formatted.</summary>
		/// <param name="message">The message to translate, which may include argument 
		/// placeholders (e.g. "{0}"). The default formatter also accepts named 
		/// parameters like "{firstName}"; see <see cref="StringExt.FormatCore"/> for 
		/// details.</param>
		/// <param name="args">Arguments given to <see cref="Formatter"/> to fill in 
		/// placeholders after the Localizer is called. If args is null or empty then 
		/// Formatter is not called.</param>
		/// <returns>The translated and formatted string.</returns>
		public static string Localized([Localizable] this string message, params object[] args)
			{ return Symbol((Symbol)null, message, args); }

		#endregion

		#region Versions of Localize() and Symbol() specialized for 0, 1 or 2 arguments

		public static string Localized([Localizable] this string message)
			{ return Symbol((Symbol)null, message, (object[])null); }

		static ScratchBuffer<object[]> _1arg = new ScratchBuffer<object[]>(() => new object[1]);
		static ScratchBuffer<object[]> _2args = new ScratchBuffer<object[]>(() => new object[2]);

		public static string Localized([Localizable] this string message, object arg1)
		{
			object[] buf = _1arg.Value;
			buf[0] = arg1;
			var result = Symbol((Symbol)null, message, buf); 
			buf[0] = null;
			return result;
		}
		public static string Symbol(Symbol resourceId, [Localizable] string message, object arg1)
		{
			object[] buf = _1arg.Value;
			buf[0] = arg1;
			var result = Symbol(resourceId, message, buf); 
			buf[0] = null;
			return result;
		}

		public static string Localized([Localizable] this string message, object arg1, object arg2)
		{
			object[] buf = _2args.Value;
			buf[0] = arg1;
			buf[1] = arg2;
			var result = Symbol((Symbol)null, message, buf); 
			buf[0] = buf[1] = null;
			return result;
		}
		public static string Symbol(Symbol resourceId, [Localizable] string message, object arg1, object arg2)
		{
			object[] buf = _2args.Value;
			buf[0] = arg1;
			buf[1] = arg2;
			var result = Symbol(resourceId, message, buf);
			buf[0] = buf[1] = null;
			return result;
		}

		#endregion
	}

	/// <summary>
	/// I plan to use this attribute someday to gather all the localizable strings 
	/// in an application. This attribute should be applied to a string function 
	/// parameter if the method calls Localized() using that parameter as the 
	/// format string.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
	public class LocalizableAttribute : System.Attribute { }

	public delegate string FormatterDelegate(string format, params object[] args);
	public delegate string LocalizerDelegate(Symbol resourceId, string defaultMessage);
}

