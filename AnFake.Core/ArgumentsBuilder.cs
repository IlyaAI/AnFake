using System;
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

		public ArgumentsBuilder Command(string name)
		{
			if (_args.Length > 0)
			{
				_args.Append(" ");
			}
			_args.Append(name);

			return this;
		}

		public ArgumentsBuilder Param(string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			if (_args.Length > 0)
			{
				_args.Append(" ");
			}
			
			_args.Append("\"").Append(value.Replace("\"", "\"\"")).Append("\"");

			return this;
		}

		public ArgumentsBuilder Option(string name, string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			if (_args.Length > 0)
			{
				_args.Append(" ");
			}
			
			_args.Append(_optionMarker).Append(name).Append(_nameValueMarker);
			_args.Append("\"").Append(value.Replace("\"","\"\"")).Append("\"");
			
			return this;
		}

		public ArgumentsBuilder Option(string name, bool value)
		{
			if (value)
			{
				if (_args.Length > 0)
				{
					_args.Append(" ");
				}
				_args.Append(_optionMarker).Append(name);
			}

			return this;
		}

		public override string ToString()
		{
			return _args.ToString();
		}
	}
}