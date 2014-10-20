using System;
using System.Globalization;
using System.Text;

namespace AnFake.Core
{
	public sealed class ArgumentsBuilder
	{
		private readonly StringBuilder _args = new StringBuilder();
		private readonly string _optionMarker;
		private readonly string _nameValueMarker;

		public ArgumentsBuilder(string optionMarker, string nameValueMarker)
		{
			_optionMarker = optionMarker;
			_nameValueMarker = nameValueMarker;
		}

		public ArgumentsBuilder NonQuotedValue(string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			_args.Append(value);

			return this;
		}

		public ArgumentsBuilder QuotedValue(string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			_args.Append("\"").Append(value.Replace("\"", "\"\"")).Append("\"");

			return this;
		}

		public ArgumentsBuilder Space()
		{
			if (_args.Length > 0 && !Char.IsWhiteSpace(_args[_args.Length - 1]))
			{
				_args.Append(" ");
			}

			return this;
		}

		public ArgumentsBuilder ValuedOption(string name)
		{
			_args.Append(_optionMarker).Append(name).Append(_nameValueMarker);			

			return this;
		}

		public ArgumentsBuilder Option(string name)
		{
			Space();

			_args.Append(_optionMarker).Append(name);

			return this;
		}		

		public override string ToString()
		{
			return _args.ToString();
		}
	}
}