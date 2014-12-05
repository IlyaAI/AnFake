using System.Linq;
using System.Text.RegularExpressions;

namespace AnFake.Core
{
	public static class Text
	{
		public sealed class EditableText
		{
			private readonly FileItem _file;
			private string _text;

			public EditableText(FileItem file)
			{
				_file = file;
			}

			public bool HasLine(string pattern, bool ignoreCase = false)
			{
				var options = RegexOptions.CultureInvariant;
				if (ignoreCase)
				{
					options |= RegexOptions.IgnoreCase;
				}

				return TextLine.From(_text).Any(x => Regex.IsMatch(x, pattern, options));
			}

			public void AppendLine(string fromat, params object[] args)
			{
			}

			public void Save()
			{
			}
		}

		public static EditableText AsEditableText(this FileItem file)
		{
			return new EditableText(file);
		}
	}
}