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
	public static class Text
	{
		public const char Lf = '\n';
		public const char Cr = '\r';

		public sealed class TextLine
		{
			private readonly LinkedListNode<string> _line;

			internal TextLine(LinkedListNode<string> line)
			{
				_line = line;
			}

			public string Text
			{
				get { return _line.Value; }
			}

			public void InsertBefore(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("");

				_line.List.AddBefore(_line, String.Format(line, args));
			}

			public void InsertAfter(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("");

				_line.List.AddAfter(_line, String.Format(line, args));
			}

			public void Replace(string line, params object[] args)
			{
				if (line == null)
					throw new ArgumentException("");

				_line.Value = String.Format(line, args);
			}

			public void Replace(string pattern, string value, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("");
				if (value == null)
					throw new ArgumentException("");

				var rx = Rx(pattern, ignoreCase);
				_line.Value = new TextReplacer(_line.Value, value)
					.Replace(rx.Matches(_line.Value))
					.ToString();
			}

			public void Replace(string pattern, Func<int, string, string> newValue, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("");
				if (newValue == null)
					throw new ArgumentException("");

				var rx = Rx(pattern, ignoreCase);
				var replacer = new TextReplacer(_line.Value, newValue);
				foreach (Match match in rx.Matches(_line.Value))
				{
					replacer.Replace(match.Groups);
				}

				_line.Value = replacer.ToString();
			}

			public void Remove()
			{
				_line.List.Remove(_line);
			}

			public override string ToString()
			{
				return _line.Value;
			}
		}

		public sealed class TextDoc
		{
			private readonly FileItem _file;
			private readonly Encoding _encoding;
			private LinkedList<string> _lines;
			private string _text;

			internal TextDoc(FileItem file)
			{
				_file = file;

				using (var reader = new StreamReader(file.Path.Full))
				{
					_encoding = reader.CurrentEncoding;
					_text = reader.ReadToEnd();
				}
			}

			internal TextDoc(string text)
			{
				_file = null;
				_text = text;
				_encoding = Encoding.UTF8;
			}

			public string Text
			{
				get { return GetText(); }
			}

			public IEnumerable<string> Lines
			{
				get { return GetLines(); }
			}

			public bool HasLine(string pattern, bool ignoreCase = false)
			{
				return MatchedLines(pattern, ignoreCase).Any();
			}

			public TextLine FirstLine()
			{
				var lines = GetLines(true);
				return new TextLine(lines.First);
			}

			public TextLine LastLine()
			{
				var lines = GetLines(true);
				return new TextLine(lines.Last);
			}

			public TextLine MatchedLine(string pattern, bool ignoreCase = false)
			{
				var rx = Rx(pattern, ignoreCase);

				var lines = GetLines(true);
				var line = lines.First;
				while (line != null)
				{
					if (rx.IsMatch(line.Value))
						return new TextLine(line);

					line = line.Next;
				}

				throw new InvalidConfigurationException("");
			}

			public IEnumerable<TextLine> MatchedLines(string pattern, bool ignoreCase = false)
			{
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

			public void ForEachMatchedLine(string pattern, Action<TextLine> action, bool ignoreCase = false)
			{
				foreach (var line in MatchedLines(pattern, ignoreCase))
				{
					action(line);
				}
			}

			public void ForEachLine(Action<TextLine> action)
			{
				var lines = GetLines(true);
				var line = lines.First;				
				while (line != null)
				{
					action(new TextLine(line));
					line = line.Next;
				}
			}

			public void Replace(string pattern, string value, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("");
				if (value == null)
					throw new ArgumentException("");

				var text = GetText();
				var rx = Rx(pattern, ignoreCase);
				SetText(
					new TextReplacer(text, value)
						.Replace(rx.Matches(text))
						.ToString());
			}

			public void Replace(string pattern, Func<int, string, string> newValue, bool ignoreCase = false)
			{
				if (pattern == null)
					throw new ArgumentException("");
				if (newValue == null)
					throw new ArgumentException("");

				var text = GetText();
				var rx = Rx(pattern, ignoreCase);
				var replacer = new TextReplacer(text, newValue);

				foreach (Match match in rx.Matches(text))
				{
					replacer.Replace(match.Groups);
				}

				SetText(replacer.ToString());
			}
			
			public void Save()
			{
				if (_file == null)
					throw new InvalidConfigurationException("");

				using (var writer = new StreamWriter(_file.Path.Full, false, _encoding))
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
					_lines = new LinkedList<string>(_text.GetLines());					
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
		}

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

		public static TextDoc AsTextDoc(this FileItem file)
		{
			if (file == null)
				throw new ArgumentException("");

			return new TextDoc(file);
		}

		public static TextDoc AsTextDoc(this string text)
		{
			if (text == null)
				throw new ArgumentException("");

			return new TextDoc(text);
		}

		public static IEnumerable<string> GetLines(this string text)
		{
			if (text == null)
				throw new ArgumentException("");

			var start = 0;
			do
			{
				var index = text.IndexOfAny(new []{Lf, Cr}, start);
				if (index < 0) index = text.Length;

				yield return text.Substring(start, index - start);

				if (index + 1 < text.Length 
					&& (  (text[index] == Lf && text[index + 1] == Cr) 
						||(text[index] == Cr && text[index + 1] == Lf))) index++;

				start = index + 1;
			} while (start < text.Length);
		}

		public static string Join(IEnumerable<string> lines)
		{
			if (lines == null)
				throw new ArgumentException("");

			return String.Join(Environment.NewLine, lines);
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