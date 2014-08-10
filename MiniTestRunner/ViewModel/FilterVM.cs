using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UpdateControls.Fields;

namespace MiniTestRunner.ViewModel
{
	public class FilterVM
	{
		Independent<string> _RowFilter = new Independent<string>("TreeVM.RowFilter", default(string));
		public string RowFilter
		{
			get { return _RowFilter.Value; }
			set { _RowFilter.Value = value; }
		}

		Independent<bool> _HideSuccess = new Independent<bool>("TreeVM.HideSuccess", default(bool));
		public bool HideSuccess
		{
			get { return _HideSuccess.Value; }
			set { _HideSuccess.Value = value; }
		}

		Independent<bool> _HideIgnored = new Independent<bool>("TreeVM.HideIgnored", default(bool));
		public bool HideIgnored
		{
			get { return _HideIgnored.Value; }
			set { _HideIgnored.Value = value; }
		}

		public IEnumerable<RowModel> ApplyTo(IEnumerable<RowModel> list)
		{
			return list.Where(m => FilterAllows(m));
		}
		public bool FilterAllows(RowModel m)
		{
			var s = m.Status;
			return (!HideIgnored || s != TestStatus.Ignored)
				&& (!HideSuccess || (s != TestStatus.Success && s != TestStatus.SuccessWithMessage))
				&& (string.IsNullOrEmpty(RowFilter) || SmartFilter(m.Name + "|" + m.Summary, RowFilter) > -1);
		}

		/// <summary>A programmer-friendly search filter.</summary>
		/// <param name="filter">User-defined filter string</param>
		/// <param name="input">A string to be searched</param>
		/// <returns>Location where matching started, or -1 if no match.
		/// Returns 0 if s is null or empty.</returns>
		/// <remarks>
		/// Interpretation of the filter string:
		/// <ul>
		/// <li>The filter is a series of "words", literal strings that must be 
		/// matched character-for-character (except spaces).</li>
		/// <li>A new "word" starts at every capital letter, or after a space; e.g. 
		/// "ML train" can match "MagLev_train".</li>
		/// <li>Lowercase letters in the filter are case-insensitive, but uppercase 
		/// letters can only match uppercase letters, e.g. "ml king" can find "ML King" 
		/// (although not MagLev king), but "ML King" cannot find "ml king".</li>
		/// <li>The order of words in the string being searched must match the filter, 
		/// e.g. "ML train" can match "MagLev train" but not "train ML".</li>
		/// </ul>
		/// In the string being searched, matching cannot begin "mid-word",
		/// specifically, at a lowercase letter preceded by another letter. For 
		/// example, "tic" cannot match "fanatic" or "FANAtic", but it can match 
		/// "FANATIC" and "FanaTic".
		/// <para/>
		/// TODO: allow íntërnàtioñal characters in the input to match ASCII
		/// </remarks>
		public static int SmartFilter(string input, string filter)
		{
			int f = 0, i = 0, f2, start = -1;

		AdvanceInFilter:
			if (f >= filter.Length)
				return i;

			// Skip spaces in filter, get first char fc
			char fc;
			for (fc = filter[f]; fc == ' '; fc = filter[f])
				if (++f == filter.Length)
					return start;

			// Get length of next "word" in filter, which is only 1 char 
			// in case of punctuation or if the filter is ALL-UPPERCASE.
			f2 = f + 1;
			bool fcIsLetter = char.IsLetter(fc);
			if (fcIsLetter)
				// Get length of next "word" in filter, which is really just a 
				// letter followed by a sequence of lowercase letters
				for (; f2 < filter.Length && char.IsLower(filter[f2]); f2++) { }
			else if (char.IsDigit(fc))
				// Get length of digit sequence in filter
				for (; f2 < filter.Length && char.IsDigit(filter[f2]); f2++) { }
				
			// Start or continue scanning input
			int fLen = f2 - f;
			char lastc = '\0', fcUpper = fcIsLetter ? char.ToUpper(fc) : fc;
			for (; i < input.Length; i++)
			{
				char c = input[i];
				if ((c == fc || c == fcUpper) && (!char.IsLower(c) || !char.IsLetter(lastc)))
				{
					// First character matched. Continue matching against filter[f..f2]
					for (int fi = f + 1; fi < f2; fi++) {
						char a = filter[fi], b = input[i + fi - f];
						if (a != b && char.ToUpper(a) != b)
							goto NoMatch;
					}
					// Match success!
					if (f == 0)
						start = i;
					i += f2 - f;
					f = f2;
					goto AdvanceInFilter;
				}
			NoMatch:
				lastc = c;
			}
			return -1; // Fail
		}
	}
}
