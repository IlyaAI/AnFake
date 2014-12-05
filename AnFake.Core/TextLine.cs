using System;
using System.Collections.Generic;

namespace AnFake.Core
{
	public sealed class TextLine
	{
		private const char Lf = '\n';
		private const char Cr = '\r';

		private readonly string _text;
		private readonly int _start;
		private readonly int _length;

		private TextLine(string text, int start, int length)
		{
			_text = text;
			_start = start;
			_length = length;
		}

		public int Start
		{
			get { return _start; }
		}

		public int Length
		{
			get { return _length; }
		}

		public string ToString(int frgStart, int frgLength)
		{
			if (frgStart >= _length)
				return String.Empty;

			var maxLength = _length - frgStart;
			if (frgLength > maxLength) frgLength = maxLength;

			return _text.Substring(_start + frgStart, frgLength);
		}

		public override string ToString()
		{
			return _text.Substring(_start, _length);
		}

		public static implicit operator String(TextLine line)
		{
			return line.ToString();
		}

		public static IEnumerable<TextLine> From(string text)
		{
			var start = 0;
			do
			{
				var index = text.IndexOf(Lf, start);
				if (index < 0) index = text.Length;

				yield return new TextLine(text, start, index - start);

				if (index + 1 < text.Length && text[index + 1] == Cr) index++;

				start = index + 1;
			} while (start < text.Length);
		}
	}
}