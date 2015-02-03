using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///     Text related tools.
	/// </summary>
	public static class Text
	{
		/// <summary>
		///     Represents line in text document.
		/// </summary>
		public sealed class TextLine
		{
			private readonly LinkedListNode<string> _line;

			internal TextLine(LinkedListNode<string> line)
			{
				_line = line;
			}

			/// <summary>
			///     Line content excluding line separator(s) (not null).
			/// </summary>
			public string Text
			{
				get { return _line.Value; }
			}

			/// <summary>
			///     Inserts new line before current one.
			/// </summary>
			/// <param name="line">line to be inserted (not null)</param>
			/// <param name="args">formating arguments</param>
			public void InsertBefore(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("TextLine.InsertBefore(line, ...): line must not be null");

				_line.List.AddBefore(_line, String.Format(line, args));
			}

			/// <summary>
			///     Inserts new line after current one.
			/// </summary>
			/// <param name="line">line to be inserted (not null)</param>
			/// <param name="args">formating arguments</param>
			public void InsertAfter(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("TextLine.InsertAfter(line, ...): line must not be null");

				_line.List.AddAfter(_line, String.Format(line, args));
			}

			/// <summary>
			///     Replaces (entirely) current line with new one.
			/// </summary>
			/// <param name="line">new line (not null)</param>
			/// <param name="args">formating arguments</param>
			public void Replace(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("TextLine.Replace(line, ...): line must not be null");

				_line.Value = String.Format(line, args);
			}

			/// <summary>
			///     Replaces matched substring with new one in current line.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="value">value to be replaced to (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			public void Replace(string pattern, string value, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextLine.Replace(pattern, value[, ignoreCase]): pattern must not be null");
				if (value == null)
					throw new ArgumentException("TextLine.Replace(pattern, value[, ignoreCase]): value must not be null");

				var rx = Rx(pattern, ignoreCase);
				_line.Value = new TextReplacer(_line.Value, value)
					.Replace(rx.Matches(_line.Value))
					.ToString();
			}

			/// <summary>
			///     Replaces matched substrings with new one evaluated by given function in current line.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="newValue">function which evaluates new value (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			public void Replace(string pattern, Func<int, string, string> newValue, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextLine.Replace(pattern, newValue[, ignoreCase]): pattern must not be null");
				if (newValue == null)
					throw new ArgumentException("TextLine.Replace(pattern, newValue[, ignoreCase]): newValue must not be null");

				var rx = Rx(pattern, ignoreCase);
				var replacer = new TextReplacer(_line.Value, newValue);
				foreach (Match match in rx.Matches(_line.Value))
				{
					replacer.Replace(match.Groups);
				}

				_line.Value = replacer.ToString();
			}			

			/// <summary>
			///     Removes current line.
			/// </summary>
			public void Remove()
			{
				_line.List.Remove(_line);
			}

			/// <summary>
			///     Returns line content. Equals to Text property.
			/// </summary>
			/// <returns>line content</returns>
			public override string ToString()
			{
				return _line.Value;
			}
		}

		/// <summary>
		///     Represents text document.
		/// </summary>
		/// <remarks>
		///		The TextDoc is agnostic to line separators from input file (accepted '\n', '\r' and '\r\n') and always normalizes them using <c>Environment.NewLine</c>.
		/// </remarks>
		public sealed class TextDoc
		{
			private readonly FileItem _file;
			private readonly Encoding _encoding;
			private LinkedList<string> _lines;
			private string _text;

			internal TextDoc(FileItem file)
			{
				_file = file;

				if (file.Exists())
				{
					using (var reader = new StreamReader(file.Path.Full))
					{
						_encoding = reader.CurrentEncoding;
						LoadLines(reader);
					}
				}
				else
				{
					_encoding = Encoding.UTF8;
					LoadEmpty();
				}				
			}

			internal TextDoc(Encoding encoding, TextReader reader)
			{
				_file = null;
				_encoding = encoding;

				LoadLines(reader);
			}

			internal TextDoc(string text)
				: this(Encoding.UTF8, new StringReader(text))
			{				
			}						

			/// <summary>
			///     Document content as whole text (not null).
			/// </summary>
			/// <remarks>
			///		Lines are separated by <c>Environment.NewLine</c>.
			/// </remarks>
			public string Text
			{
				get { return GetText(); }
			}

			/// <summary>
			///     Document content splitted per lines (not null). Line separator not included.
			/// </summary>
			public IEnumerable<string> Lines
			{
				get { return GetLines(); }
			}

			/// <summary>
			///     Returns true if at least one line matched by pattern and false otherwise.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			/// <returns>true if at least one line matched</returns>
			public bool HasLine(string pattern, bool ignoreCase = false)
			{
				return MatchedLines(pattern, ignoreCase).Any();
			}

			/// <summary>
			///     Returns the first line in document (not null).
			/// </summary>
			/// <returns>first line</returns>
			public TextLine FirstLine()
			{
				var lines = GetLines(true);
				return new TextLine(lines.First);
			}

			/// <summary>
			///     Returns the last line in document (not null).
			/// </summary>
			/// <returns>last line</returns>
			public TextLine LastLine()
			{
				var lines = GetLines(true);
				return new TextLine(lines.Last);
			}

			/// <summary>
			///     Returns first line matched by pattern and throws otherwise.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			/// <returns>first matched line</returns>
			/// <exception cref="InvalidConfigurationException">if no one line matched</exception>
			public TextLine MatchedLine(string pattern, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextDoc.MatchedLine(pattern[, ignoreCase]): pattern must not be null");

				var rx = Rx(pattern, ignoreCase);

				var lines = GetLines(true);
				var line = lines.First;
				while (line != null)
				{
					if (rx.IsMatch(line.Value))
						return new TextLine(line);

					line = line.Next;
				}

				throw new InvalidConfigurationException(String.Format("There is no one line matched by pattern '{0}'.", pattern));
			}

			/// <summary>
			///     Returns all lines matched by pattern. If no one empty sequence returned.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			/// <returns>all matched lines</returns>
			public IEnumerable<TextLine> MatchedLines(string pattern, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextDoc.MatchedLines(pattern[, ignoreCase]): pattern must not be null");

				var rx = Rx(pattern, ignoreCase);

				var lines = GetLines(true);
				var line = lines.First;
				var matched = new List<TextLine>();

				while (line != null)
				{
					if (rx.IsMatch(line.Value))
						matched.Add(new TextLine(line));

					line = line.Next;
				}

				return matched;
			}

			/// <summary>
			///     Performs action for each matched line.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="action">action to be performed (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			public void ForEachMatchedLine(string pattern, Action<TextLine> action, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextDoc.ForEachMatchedLine(pattern, action[, ignoreCase]): pattern must not be null");
				if (action == null)
					throw new ArgumentException("TextDoc.ForEachMatchedLine(pattern, action[, ignoreCase]): action must not be null");

				foreach (var line in MatchedLines(pattern, ignoreCase))
				{
					action(line);
				}
			}

			/// <summary>
			///     Performs action for each line in document.
			/// </summary>
			/// <param name="action">action to be performed (not null)</param>
			public void ForEachLine(Action<TextLine> action)
			{
				if (action == null)
					throw new ArgumentException("TextDoc.ForEachLine(action): action must not be null");

				var lines = GetLines(true);
				var line = lines.First;
				while (line != null)
				{
					action(new TextLine(line));
					line = line.Next;
				}
			}

			/// <summary>
			///     Replaces matched substring with new one in whole document.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="value">value to be replaced to (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			public void Replace(string pattern, string value, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextDoc.Replace(pattern, value[, ignoreCase]): pattern must not be null");
				if (value == null)
					throw new ArgumentException("TextDoc.Replace(pattern, value[, ignoreCase]): value must not be null");

				var text = GetText();
				var rx = Rx(pattern, ignoreCase);
				SetText(
					new TextReplacer(text, value)
						.Replace(rx.Matches(text))
						.ToString());
			}

			/// <summary>
			///     Replaces matched substrings with new one evaluated by given function in whole document.
			/// </summary>
			/// <param name="pattern">Regex pattern (not null)</param>
			/// <param name="newValue">function which evaluates new value (not null)</param>
			/// <param name="ignoreCase">true to match ignoring case</param>
			public void Replace(string pattern, Func<int, string, string> newValue, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("TextDoc.Replace(pattern, newValue[, ignoreCase]): pattern must not be null");
				if (newValue == null)
					throw new ArgumentException("TextDoc.Replace(pattern, newValue[, ignoreCase]): newValue must not be null");

				var text = GetText();
				var rx = Rx(pattern, ignoreCase);
				var replacer = new TextReplacer(text, newValue);

				foreach (Match match in rx.Matches(text))
				{
					replacer.Replace(match.Groups);
				}

				SetText(replacer.ToString());
			}

			/// <summary>
			///     Saves changes to the same file which text was loaded from.
			/// </summary>
			/// <remarks>
			///     This method throws an exception if TextDoc was created from plain text.
			/// </remarks>
			/// <exception cref="InvalidConfigurationException">if document wasn't created from file</exception>
			public void Save()
			{
				if (_file == null)
					throw new InvalidConfigurationException("Unable to save text document because it wasn't loaded from file.");

				using (var writer = new StreamWriter(_file.Path.Full, false, _encoding))
				{
					writer.Write(GetText());
				}
			}

			/// <summary>
			/// 		Saves changes to specified file.
			/// </summary>
			/// <param name="file">file to save to (not null)</param>
			public void SaveTo(FileItem file)
			{
				SaveTo(file, _encoding);
			}

			///  <summary>
			/// 		Saves changes to specified file.
			///  </summary>
			///  <param name="file">file to save to (not null)</param>
			/// <param name="encoding">encoding to write with</param>
			public void SaveTo(FileItem file, Encoding encoding)
			{
				if (file == null)
					throw new ArgumentException("TextDoc.SaveTo(file[, encoding]): file must not be null");
				if (encoding == null)
					throw new ArgumentException("TextDoc.SaveTo(file, encoding): encoding must not be null");

				file.EnsurePath();

				using (var writer = new StreamWriter(file.Path.Full, false, encoding))
				{
					writer.Write(GetText());
				}
			}

			private string GetText()
			{
				return _text ?? (_text = Join(_lines));
			}

			private void SetText(string text)
			{
				_text = text;
				InvalidateLines();
			}

			private LinkedList<string> GetLines(bool writable = false)
			{
				if (_lines == null)
				{
					LoadLines(new StringReader(_text));
				}

				if (writable)
				{
					_text = null;
				}

				return _lines;
			}

			private void InvalidateLines()
			{
				_lines = null;
			}

			private void LoadLines(TextReader reader)
			{
				_lines = new LinkedList<string>();

				string line;
				while ((line = reader.ReadLine()) != null)
				{
					_lines.AddLast(line);
				}

				if (_lines.Count == 0)
				{
					_lines.AddLast(String.Empty);
				}
			}

			private void LoadEmpty()
			{
				_lines = new LinkedList<string>();
				_lines.AddLast(String.Empty);
			}
		}

		/// <summary>
		///		Helper class for support multi-occurances replacement.
		/// </summary>
		private sealed class TextReplacer
		{
			private readonly string _original;
			private readonly StringBuilder _target;
			private readonly Func<int, string, string> _newValue;
			private int _offset;

			public TextReplacer(string original, Func<int, string, string> newValue)
			{
				_original = original;
				_newValue = newValue;
				_target = new StringBuilder(original.Length);
				_offset = 0;
			}

			public TextReplacer(string original, string newValue)
				: this(original, (i, x) => newValue)
			{
			}

			public TextReplacer Replace(IEnumerable captures)
			{
				var index = 0;
				foreach (Capture capture in captures)
				{
					var value = _newValue(index++, capture.Value);
					if (value == null)
						continue;

					_target.Append(_original, _offset, capture.Index - _offset);
					_target.Append(value);
					_offset = capture.Index + capture.Length;
				}

				return this;
			}

			public override string ToString()
			{
				if (_offset < _original.Length)
				{
					_target.Append(_original, _offset, _original.Length - _offset);
					_offset = _original.Length;
				}

				return _target.ToString();
			}
		}

		/// <summary>
		///     Creates TextDoc representation for specified file.
		/// </summary>
		/// <param name="file">file to be loaded as text (not null)</param>
		/// <returns>TextDoc instance</returns>
		/// <seealso cref="TextDoc"/>
		public static TextDoc AsTextDoc(this FileItem file)
		{
			if (file == null)
				throw new ArgumentException("Text.AsTextDoc(file): file must not be null");

			return new TextDoc(file);
		}

		/// <summary>
		///     Creates TextDoc representation for specified string.
		/// </summary>
		/// <param name="text">string to be presented as document (not null)</param>
		/// <returns>TextDoc instance</returns>
		/// <seealso cref="TextDoc"/>
		public static TextDoc AsTextDoc(this string text)
		{
			if (text == null)
				throw new ArgumentException("Text.AsTextDoc(text): text must not be null");

			return new TextDoc(text);
		}

		/// <summary>
		///     Splits given text to the lines.
		/// </summary>
		/// <remarks>
		///     Method is agnostic to line separators (accepted '\n', '\r' and '\r\n'). Returned lines don't include separator itself.
		/// </remarks>
		/// <param name="text">text to be splitted (not null)</param>
		/// <returns>sequence of lines</returns>
		public static IEnumerable<string> GetLines(this string text)
		{
			if (text == null)
				throw new ArgumentException("Text.GetLines(text): text must not be null");

			if (text.Length == 0)
				yield return String.Empty;

			var reader = new StringReader(text);
			
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				yield return line;
			}
		}

		/// <summary>
		///     Joins given lines to whole text.
		/// </summary>
		/// <remarks>
		///     Method uses <c>Environment.NewLine</c> as line separator.
		/// </remarks>
		/// <param name="lines">sequence of lines to be joinded (not null)</param>
		/// <returns>text</returns>
		public static string Join(IEnumerable<string> lines)
		{
			if (lines == null)
				throw new ArgumentException("Text.Join(lines): lines must not be null");

			return String.Join(Environment.NewLine, lines);
		}

		///  <summary>
		/// 		Equals to <see cref="WriteTo(AnFake.Core.FileItem,string)">WriteTo(file, text, Encoding.UTF8)</see>.
		///  </summary>		
		///  <param name="file">file to write to (not null)</param>
		///  <param name="text">text to be written (not null)</param>		
		public static void WriteTo(FileItem file, string text)
		{
			WriteTo(file, text, Encoding.UTF8);
		}

		///  <summary>
		/// 		Writes text to specified file.
		///  </summary>
		///  <remarks>
		/// 		If file already exists it will be overwritten. Text is always written in UTF8 encoding and with normalized line endings.
		///  </remarks>
		///  <param name="file">file to write to (not null)</param>
		///  <param name="text">text to be written (not null)</param>
		/// <param name="encoding">encoding to write with (not null)</param>
		public static void WriteTo(FileItem file, string text, Encoding encoding)
		{
			if (file == null)
				throw new ArgumentException("Text.WriteTo(file, text[, encoding]): file must not be null");
			if (text == null)
				throw new ArgumentException("Text.WriteTo(file, text[, encoding]): text must not be null");
			if (encoding == null)
				throw new ArgumentException("Text.WriteTo(file, text, encoding): encoding must not be null");

			file.EnsurePath();

			using (var writer = new StreamWriter(file.Path.Full, false, encoding))
			{
				foreach (var line in text.GetLines())
				{
					writer.WriteLine(line);
				}
			}
		}

		/// <summary>
		///		Reads text from specified file.
		/// </summary>
		/// <remarks>
		///		If file doesn't exist then exception will be thrown. Text is always read with normalized line endings.
		/// </remarks>
		/// <param name="file">file to read from</param>
		/// <returns>text</returns>
		public static string ReadFrom(FileItem file)
		{
			if (file == null)
				throw new ArgumentException("Text.ReadFrom(file, text): file must not be null");

			var sb = new StringBuilder(512);
			using (var reader = new StreamReader(file.Path.Full))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					sb.AppendLine(line);
				}				
			}

			return sb.ToString();
		}

		/// <summary>
		///		Replaces matched substrings with new one evaluated by given function in given string.
		/// </summary>
		/// <param name="text">original text to replace in</param>
		/// <param name="pattern">Regex pattern (not null)</param>
		/// <param name="newValue">function which evaluates new value (not null)</param>
		/// <param name="ignoreCase">true to match ignoring case</param>
		/// <returns>new string with replaced occurrences</returns>
		public static string Replace(this string text, string pattern, Func<int, string, string> newValue, bool ignoreCase = false)
		{
			if (text == null)
				throw new ArgumentException("Text.Replace(text, pattern, newValue[, ignoreCase]): text must not be null");
			if (pattern == null)
				throw new ArgumentException("Text.Replace(text, pattern, newValue[, ignoreCase]): pattern must not be null");
			if (newValue == null)
				throw new ArgumentException("Text.Replace(text, pattern, newValue[, ignoreCase]): newValue must not be null");

			var rx = Rx(pattern, ignoreCase);
			var replacer = new TextReplacer(text, newValue);
			foreach (Match match in rx.Matches(text))
			{
				replacer.Replace(match.Groups);
			}

			return replacer.ToString();
		}

		public static string Parse1Group(this string text, string pattern, bool ignoreCase = false)
		{
			if (text == null)
				throw new ArgumentException("Text.Parse1Group(text, pattern[, ignoreCase]): text must not be null");
			if (pattern == null)
				throw new ArgumentException("Text.Parse1Group(text, pattern[, ignoreCase]): pattern must not be null");
			
			var match = GetMatch(Rx(pattern, ignoreCase).Matches(text), 2);

			return match.Groups[1].Value;
		}

		public static Tuple<string, string> Parse2Groups(this string text, string pattern, bool ignoreCase = false)
		{
			if (text == null)
				throw new ArgumentException("Text.Parse2Group(text, pattern[, ignoreCase]): text must not be null");
			if (pattern == null)
				throw new ArgumentException("Text.Parse2Group(text, pattern[, ignoreCase]): pattern must not be null");

			var match = GetMatch(Rx(pattern, ignoreCase).Matches(text), 2);

			return new Tuple<string, string>(match.Groups[1].Value, match.Groups[2].Value);
		}

		private static Match GetMatch(MatchCollection matches, int requiredGroups)
		{
			if (matches.Count == 0)
				throw new InvalidConfigurationException("String doesn't match specified pattern.");

			if (matches.Count > 1)
				throw new InvalidConfigurationException("String matches specified pattern more than once. ParseXGroup methods expect one and only one match.");

			var match = matches[0];
			if (match.Groups.Count < requiredGroups)
				throw new InvalidConfigurationException("Matched pattern doesn't have requested number of groups.");

			return match;
		}

		private static Regex Rx(string pattern, bool ignoreCase)
		{
			var options = ignoreCase
				? RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
				: RegexOptions.CultureInvariant;

			return new Regex(pattern, options);
		}
	}
}