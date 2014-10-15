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
			return this;
		}

		public ArgumentsBuilder Param(string value)
		{
			return this;
		}

		public ArgumentsBuilder Option(string name, string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			// TODO: escape it
			_args.Append(_optionMarker).Append(name).Append(_nameValueMarker).Append(value);
			
			return this;
		}

		public ArgumentsBuilder Option(string name, bool value)
		{
			if (value)
			{
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